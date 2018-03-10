using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using CloudFlareUtilities;
using CurlSharp;
using CurlSharp.Enums;
using Jackett.Common.Models.Config;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Utils.Clients
{
    public class UnixLibCurlWebClient : WebClient
    {
        public UnixLibCurlWebClient(IProcessService p, Logger l, IConfigurationService c, ServerConfig sc)
            : base(p: p,
                   l: l,
                   c: c,
                   sc: sc)
        {
        }

        private string CloudFlareChallengeSolverSolve(string challengePageContent, Uri uri)
        {
            var solution = ChallengeSolver.Solve(challengePageContent, uri.Host);
            string clearanceUri = uri.Scheme + Uri.SchemeDelimiter + uri.Host + ":" + uri.Port + solution.ClearanceQuery;
            return clearanceUri;
        }

        override public void Init()
        {
            try
            {
                logger.Info("LibCurl init " + Curl.GlobalInit(CurlInitFlag.All).ToString());
                CurlHelper.OnErrorMessage += (msg) =>
                {
                    logger.Error(msg);
                };
            }
            catch (Exception e)
            {
                logger.Warn("Libcurl failed to initalize. Did you install it?");
                logger.Warn("Debian: apt-get install libcurl4-openssl-dev");
                logger.Warn("Redhat: yum install libcurl-devel");
                throw e;
            }

            var version = Curl.Version;
            logger.Info("LibCurl version " + version);

            if (!serverConfig.RuntimeSettings.DoSSLFix.HasValue && version.IndexOf("NSS") > -1)
            {
                logger.Info("NSS Detected SSL ECC workaround enabled.");
                serverConfig.RuntimeSettings.DoSSLFix = true;
            }
        }

        // Wrapper for Run which takes care of CloudFlare challenges, calls RunCurl
        override protected async Task<WebClientByteResult> Run(WebRequest request)
        {
            WebClientByteResult result = await RunCurl(request);

            // check if we've received a CloudFlare challenge
            string[] server;
            if (result.Status == HttpStatusCode.ServiceUnavailable && result.Headers.TryGetValue("server", out server) && (server[0] == "cloudflare-nginx" || server[0] == "cloudflare"))
            {
                logger.Info("UnixLibCurlWebClient: Received a new CloudFlare challenge");

                // solve the challenge
                string pageContent = Encoding.UTF8.GetString(result.Content);
                Uri uri = new Uri(request.Url);
                string clearanceUri = CloudFlareChallengeSolverSolve(pageContent, uri);
                logger.Info(string.Format("UnixLibCurlWebClient: CloudFlare clearanceUri: {0}", clearanceUri));

                // wait...
                await Task.Delay(5000);

                // request clearanceUri to get cf_clearance cookie
                var response = await CurlHelper.GetAsync(clearanceUri, serverConfig, request.Cookies, request.Referer);
                logger.Info(string.Format("UnixLibCurlWebClient: received CloudFlare clearance cookie: {0}", response.Cookies));

                // add new cf_clearance cookies to the original request
                request.Cookies = response.Cookies + request.Cookies;

                // re-run the original request with updated cf_clearance cookie
                result = await RunCurl(request);

                // add cf_clearance cookie to the final result so we update the config for the next request
                result.Cookies = response.Cookies + " " + result.Cookies;
            }
            return result;
        }

        protected async Task<WebClientByteResult> RunCurl(WebRequest request)
        {
            CurlHelper.CurlResponse response;
            if (request.Type == RequestType.GET)
            {
                response = await CurlHelper.GetAsync(request.Url, serverConfig, request.Cookies, request.Referer, request.Headers);
            }
            else
            {
                if (!string.IsNullOrEmpty(request.RawBody))
                {
                    logger.Debug("UnixLibCurlWebClient: Posting " + request.RawBody);
                }
                else if (request.PostData != null && request.PostData.Count() > 0)
                {
                    logger.Debug("UnixLibCurlWebClient: Posting " + StringUtil.PostDataFromDict(request.PostData));
                }

                response = await CurlHelper.PostAsync(request.Url, serverConfig, request.PostData, request.Cookies, request.Referer, request.Headers, request.RawBody);
            }

            var result = new WebClientByteResult()
            {
                Content = response.Content,
                Cookies = response.Cookies,
                Status = response.Status
            };

            if (response.HeaderList != null)
            {
                foreach (var header in response.HeaderList)
                {
                    var key = header[0].ToLowerInvariant();
                    
                    result.Headers[key] = new string[] { header[1] }; // doesn't support multiple identical headers?

                    switch (key)
                    {
                        case "location":
                            result.RedirectingTo = header[1];
                            break;
                        case "refresh":
                            if (response.Status == System.Net.HttpStatusCode.ServiceUnavailable)
                            {
                                //"Refresh: 8;URL=/cdn-cgi/l/chk_jschl?pass=1451000679.092-1vJFUJLb9R"
                                var redirval = "";
                                var value = header[1];
                                var start = value.IndexOf("=");
                                var end = value.IndexOf(";");
                                var len = value.Length;
                                if (start > -1)
                                {
                                    redirval = value.Substring(start + 1);
                                    result.RedirectingTo = redirval;
                                    // normally we don't want a serviceunavailable (503) to be a redirect, but that's the nature
                                    // of this cloudflare approach..don't want to alter BaseWebResult.IsRedirect because normally
                                    // it shoudln't include service unavailable..only if we have this redirect header.
                                    result.Status = System.Net.HttpStatusCode.Redirect;
                                    var redirtime = Int32.Parse(value.Substring(0, end));
                                    System.Threading.Thread.Sleep(redirtime * 1000);
                                }
                            }
                            break;
                    }
                }
            }

            ServerUtil.ResureRedirectIsFullyQualified(request, result);
            return result;
        }
    }
}
