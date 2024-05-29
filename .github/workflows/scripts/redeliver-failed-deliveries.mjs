// This script uses GitHub's Octokit SDK to make API requests. For more information, see https://docs.github.com/en/rest/guides/scripting-with-the-rest-api-and-javascript?apiVersion=2022-11-28
import { App, Octokit } from "octokit";

const sleep = ms => new Promise(r => setTimeout(r, ms));
let secondDelivery = true;

//
async function checkAndRedeliverWebhooks() {
  // Get the values of environment variables that were set by the GitHub Actions workflow.
  const APP_ID = process.env.APP_ID;
  const PRIVATE_KEY = process.env.PRIVATE_KEY;
  const TOKEN = process.env.TOKEN;
  const LAST_REDELIVERY_VARIABLE_NAME = process.env.LAST_REDELIVERY_VARIABLE_NAME;
  
  const WORKFLOW_REPO_NAME = process.env.WORKFLOW_REPO;
  const WORKFLOW_REPO_OWNER = process.env.WORKFLOW_REPO_OWNER;

  // Create an instance of the octokit `App` using the app ID and private key values that were set in the GitHub Actions workflow.
  //
  // This will be used to make API requests to the webhook-related endpoints.
  const app = new App({
    appId: APP_ID,
    privateKey: PRIVATE_KEY,
  });

  // Create an instance of `Octokit` using the token values that were set in the GitHub Actions workflow.
  //
  // This will be used to update the configuration variable that stores the last time that this script ran.
  const octokit = new Octokit({ 
    auth: TOKEN,
  });

  try {
    // Get the last time that this script ran from the configuration variable. If the variable is not defined, use the current time minus 24 hours.
    const lastStoredRedeliveryTime = await getVariable({
      variableName: LAST_REDELIVERY_VARIABLE_NAME,
      repoOwner: WORKFLOW_REPO_OWNER,
      repoName: WORKFLOW_REPO_NAME,
      octokit,
    });
    const lastWebhookRedeliveryTime = lastStoredRedeliveryTime || (Date.now() - (24 * 60 * 60 * 1000)).toString();

    // Record the time that this script started redelivering webhooks.
    const newWebhookRedeliveryTime = Date.now().toString();

    // Get the webhook deliveries that were delivered after `lastWebhookRedeliveryTime`.
    const deliveries = await fetchWebhookDeliveriesSince({lastWebhookRedeliveryTime, app});

    // Consolidate deliveries that have the same globally unique identifier (GUID). The GUID is constant across redeliveries of the same delivery.
    let deliveriesByGuid = {};
    for (const delivery of deliveries) {
      deliveriesByGuid[delivery.guid]
        ? deliveriesByGuid[delivery.guid].push(delivery)
        : (deliveriesByGuid[delivery.guid] = [delivery]);
    }

    // For each GUID value, if no deliveries for that GUID have been successfully delivered within the time frame, get the delivery ID of one of the deliveries with that GUID.
    //
    // This will prevent duplicate redeliveries if a delivery has failed multiple times.
    // This will also prevent redelivery of failed deliveries that have already been successfully redelivered.
    let failedDeliveryIDs = [];
    for (const guid in deliveriesByGuid) {
      const deliveries = deliveriesByGuid[guid];
      const anySucceeded = deliveries.some(
        (delivery) => delivery.status === "OK"
      );
      if (!anySucceeded) {
        failedDeliveryIDs.push(deliveries[0].id);
      }
    }

    // Redeliver any failed deliveries.
    for (const deliveryId of failedDeliveryIDs) {
      await redeliverWebhook({deliveryId, app});
      // its likely ProBot was asleep when the first redeliver was sent and we know PRoBot startup takes about 12s
      // so the first redeliver will likely timeout after 10s, but will have started ProBot
      if (secondDelivery) {
        secondDelivery = false;
        // so we wait 13s so that the rest of the redeliveries will succeed
        await sleep(13000);
      }
    }

    // Update the configuration variable (or create the variable if it doesn't already exist) to store the time that this script started.
    // This value will be used next time this script runs.
    await updateVariable({
      variableName: LAST_REDELIVERY_VARIABLE_NAME,
      value: newWebhookRedeliveryTime,
      variableExists: Boolean(lastStoredRedeliveryTime),
      repoOwner: WORKFLOW_REPO_OWNER,
      repoName: WORKFLOW_REPO_NAME,
      octokit,
      });

    // Log the number of redeliveries.
    console.log(
      `Redelivered ${
        failedDeliveryIDs.length
      } failed webhook deliveries out of ${
        deliveries.length
      } total deliveries since ${Date(lastWebhookRedeliveryTime)}.`
    );
  } catch (error) {
    // If there was an error, log the error so that it appears in the workflow run log, then throw the error so that the workflow run registers as a failure.
    if (error.response) {
      console.error(
        `Failed to check and redeliver webhooks: ${error.response.data.message}`
      );
    }
    console.error(error);
    throw(error);
  }
}

// This function will fetch all of the webhook deliveries that were delivered since `lastWebhookRedeliveryTime`.
// It uses the `octokit.paginate.iterator()` method to iterate through paginated results. For more information, see "[AUTOTITLE](/rest/guides/scripting-with-the-rest-api-and-javascript#making-paginated-requests)."
//
// If a page of results includes deliveries that occurred before `lastWebhookRedeliveryTime`,
// it will store only the deliveries that occurred after `lastWebhookRedeliveryTime` and then stop.
// Otherwise, it will store all of the deliveries from the page and request the next page.
async function fetchWebhookDeliveriesSince({lastWebhookRedeliveryTime, app}) {
  const iterator = app.octokit.paginate.iterator(
    "GET /app/hook/deliveries",
    {
      per_page: 100,
      headers: {
        "x-github-api-version": "2022-11-28",
      },
    }
  );

  const deliveries = [];

  for await (const { data } of iterator) {
    const oldestDeliveryTimestamp = new Date(
      data[data.length - 1].delivered_at
    ).getTime();

    if (oldestDeliveryTimestamp < lastWebhookRedeliveryTime) {
      for (const delivery of data) {
        if (
          new Date(delivery.delivered_at).getTime() > lastWebhookRedeliveryTime
        ) {
          deliveries.push(delivery);
        } else {
          break;
        }
      }
      break;
    } else {
      deliveries.push(...data);
    }
  }

  return deliveries;
}

// This function will redeliver a failed webhook delivery.
async function redeliverWebhook({deliveryId, app}) {
  await app.octokit.request("POST /app/hook/deliveries/{delivery_id}/attempts", {
    delivery_id: deliveryId,
  });
}

// This function gets the value of a configuration variable.
// If the variable does not exist, the endpoint returns a 404 response and this function returns `undefined`.
async function getVariable({ variableName, repoOwner, repoName, octokit }) {
  try {
    const {
      data: { value },
    } = await octokit.request(
      "GET /repos/{owner}/{repo}/actions/variables/{name}",
      {
        owner: repoOwner,
        repo: repoName,
        name: variableName,
      }
    );
    return value;
  } catch (error) {
    if (error.status === 404) {
      return undefined;
    } else {
      throw error;
    }
  }
}

// This function will update a configuration variable (or create the variable if it doesn't already exist). For more information, see "[AUTOTITLE](/actions/learn-github-actions/variables#defining-configuration-variables-for-multiple-workflows)."
async function updateVariable({
  variableName,
  value,
  variableExists,
  repoOwner,
  repoName,
  octokit,
}) {
  if (variableExists) {
    await octokit.request(
      "PATCH /repos/{owner}/{repo}/actions/variables/{name}",
      {
        owner: repoOwner,
        repo: repoName,
        name: variableName,
        value: value,
      }
    );
  } else {
    await octokit.request("POST /repos/{owner}/{repo}/actions/variables", {
      owner: repoOwner,
      repo: repoName,
      name: variableName,
      value: value,
    });
  }
}

// This will execute the `checkAndRedeliverWebhooks` function.
(async () => {
  await checkAndRedeliverWebhooks();
})();
