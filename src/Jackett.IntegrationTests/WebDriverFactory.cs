// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Jackett.IntegrationTests
{
    // This factory class is just one example of how you might initialize Selenium. If your project already has its own means
    // of initializing Selenium, you can keep using that as-is and ignore this sample file; skip ahead to SamplePageTests.cs.
    public static class WebDriverFactory
    {
        public static IWebDriver CreateFromEnvironmentVariableSettings()
        {
            // This environment variable gets set by our Azure Pipelines build definition in ./azure-pipelines.yml.
            // That file uses a matrix strategy to run multiple different build jobs for different combinations of OS/browser.
            // Each job sets this environment variable accordingly, and we use it to decide which browser the tests will use.
            const string BROWSER_ENVIRONMENT_VARIABLE = "SELENIUM_BROWSER";
            var browserEnvVar = Environment.GetEnvironmentVariable(BROWSER_ENVIRONMENT_VARIABLE);

            // It's convenient to have a default to make it easier for a developer to run a plain "dotnet test" command.
            // In our CI builds in Azure Pipelines, we'll always specify the browser explicitly instead of using this.
            const string DEFAULT_BROWSER = "chrome";

            // Headless browsers generally use small window sizes by default. Some of the axe checks require
            // elements to be present in the viewport to be assessable, so using a viewport size based on your
            // most common usage is a good idea to prevent axe from having to do extra work to scroll items into
            // view and avoid issues with elements not fitting into the viewport.
            const int windowWidth = 1920;
            const int windowHeight = 1080;

            switch (browserEnvVar ?? DEFAULT_BROWSER)
            {
                case "chrome":
                    // The ChromeWebDriver environment variable comes pre-set in the Azure Pipelines VM Image we
                    // specify in azure-pipelines.yml. ChromeDriver requires that you use *matching* versions of Chrome and
                    // the Chrome WebDriver; in the build agent VMs, using this environment variable will make sure that we use
                    // the pre-installed version of ChromeDriver that matches the pre-installed version of Chrome.
                    //
                    // See https://docs.microsoft.com/en-us/azure/devops/pipelines/test/continuous-test-selenium
                    //
                    // Environment.CurrentDirectory is where the Selenium.Webdriver.ChromeDriver NuGet package will place the
                    // version of the Chrome WebDriver that it comes with. We fall back to this so that if a developer working on
                    // the project hasn't separately installed ChromeDriver, the test will still be able to run on their machine.
                    var chromeDriverDirectory = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER") ?? Environment.CurrentDirectory;

                    // The tests will work fine in non-headless mode; we recommend using --headless for performance and
                    // because it makes it easier to run the tests in non-graphical environments (eg, most Docker containers)
                    var chromeOptions = new ChromeOptions();
                    chromeOptions.AddArgument("--headless");
                    chromeOptions.AddArgument($"--window-size={windowWidth},{windowHeight}");

                    return new ChromeDriver(chromeDriverDirectory, chromeOptions);

                default:
                    throw new ArgumentException($"Unknown browser type '{browserEnvVar}' specified in '{BROWSER_ENVIRONMENT_VARIABLE}' environment variable");
            }
        }
    }
}
