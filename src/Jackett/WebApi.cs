using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace Jackett
{
    public class WebApi
    {
        static string WebContentFolder = "WebContent";
        static string[] StaticFiles = Directory.EnumerateFiles(WebContentFolder, "*", SearchOption.AllDirectories).ToArray();

        public enum WebApiMethod
        {
            GetConfigForm,
            ConfigureIndexer,
            GetIndexers
        }
        static Dictionary<string, WebApiMethod> WebApiMethods = new Dictionary<string, WebApiMethod>
        {
            { "get_config_form", WebApiMethod.GetConfigForm },
            { "configure_indexer", WebApiMethod.ConfigureIndexer },
            { "get_indexers", WebApiMethod.GetIndexers }
        };

        IndexerManager indexerManager;

        public WebApi(IndexerManager indexerManager)
        {
            this.indexerManager = indexerManager;
        }

        public bool HandleRequest(HttpListenerContext context)
        {
            string path = context.Request.Url.AbsolutePath.TrimStart('/');
            if (path == "")
                path = "index.html";

            var sysPath = Path.Combine(WebContentFolder, path.Replace("/", Path.DirectorySeparatorChar.ToString()));
            if (Array.IndexOf(StaticFiles, sysPath) > -1)
            {
                ServeStaticFile(context, path);
                return true;
            }

            WebApi.WebApiMethod apiMethod;
            if (WebApi.WebApiMethods.TryGetValue(path, out apiMethod))
            {
                ProcessWebApiRequest(context, apiMethod);
                return true;
            }

            return false;
        }

        async void ServeStaticFile(HttpListenerContext context, string file)
        {
            var contentFile = File.ReadAllBytes(Path.Combine(WebContentFolder, file));
            context.Response.ContentType = MimeMapping.GetMimeMapping(file);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.OutputStream.WriteAsync(contentFile, 0, contentFile.Length);
            context.Response.OutputStream.Close();
        }

        async Task<JToken> ReadPostDataJson(Stream stream)
        {
            string postData = await new StreamReader(stream).ReadToEndAsync();
            return JObject.Parse(postData);
        }

        async void ProcessWebApiRequest(HttpListenerContext context, WebApiMethod method)
        {
            var query = HttpUtility.ParseQueryString(context.Request.Url.Query);

            JToken jsonReply = new JObject();

            context.Response.ContentType = "text/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            switch (method)
            {
                case WebApiMethod.GetConfigForm:
                    try
                    {
                        var postData = await ReadPostDataJson(context.Request.InputStream);
                        string indexerString = (string)postData["indexer"];
                        var indexer = indexerManager.GetIndexer(indexerString);
                        var config = await indexer.GetConfigurationForSetup();
                        jsonReply["config"] = config.ToJson();
                        jsonReply["result"] = "success";
                    }
                    catch (Exception ex)
                    {
                        jsonReply["result"] = "error";
                        jsonReply["error"] = ex.Message;
                    }
                    break;
                case WebApiMethod.ConfigureIndexer:
                    try
                    {
                        var postData = await ReadPostDataJson(context.Request.InputStream);
                        string indexerString = (string)postData["indexer"];
                        var indexer = indexerManager.GetIndexer(indexerString);
                        await indexer.ApplyConfiguration(postData["config"]);
                        await indexer.VerifyConnection();
                        jsonReply["result"] = "success";
                    }
                    catch (Exception ex)
                    {
                        jsonReply["result"] = "error";
                        jsonReply["error"] = ex.Message;
                        if (ex is ExceptionWithConfigData)
                        {
                            jsonReply["config"] = ((ExceptionWithConfigData)ex).ConfigData.ToJson();
                        }
                    }
                    break;
                case WebApiMethod.GetIndexers:
                    try
                    {
                        jsonReply["result"] = "success";
                        JArray items = new JArray();
                        foreach (var i in indexerManager.Indexers)
                        {
                            var indexer = i.Value;
                            var item = new JObject();
                            item["id"] = i.Key;
                            item["display_name"] = indexer.DisplayName;
                            item["display_description"] = indexer.DisplayDescription;
                            item["is_configured"] = indexer.IsConfigured;
                            item["site_link"] = indexer.SiteLink;
                            items.Add(item);
                        }
                        jsonReply["items"] = items;
                    }
                    catch (Exception ex)
                    {
                        jsonReply["result"] = "error";
                        jsonReply["error"] = ex.Message;
                    }
                    break;
                default:
                    jsonReply["result"] = "error";
                    jsonReply["error"] = "Invalid API method";
                    break;
            }

            ReplyWithJson(context, jsonReply);
        }

        async void ReplyWithJson(HttpListenerContext context, JToken json)
        {
            byte[] jsonBytes = Encoding.UTF8.GetBytes(json.ToString());
            await context.Response.OutputStream.WriteAsync(jsonBytes, 0, jsonBytes.Length);
            context.Response.OutputStream.Close();
        }

    }
}
