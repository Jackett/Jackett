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
    public class Server
    {
        HttpListener listener;
        IndexerManager indexerManager;

        static string[] StaticFiles = Directory.EnumerateFiles("HtmlContent", "*", SearchOption.AllDirectories).Select(Path.GetFileName).ToArray();

        enum WebApiMethod
        {
            GetConfigForm,
            ConfigureIndexer
        }
        static Dictionary<string, WebApiMethod> WebApiMethods = new Dictionary<string, WebApiMethod>
        {
            { "get_config_form", WebApiMethod.GetConfigForm },
            { "configure_indexer", WebApiMethod.ConfigureIndexer }
        };

        public Server()
        {
            indexerManager = new IndexerManager();

            listener = new HttpListener();
            listener.Prefixes.Add("http://*:9117/");
        }

        public async void Start()
        {
            listener.Start();
            while (true)
            {
                var context = await listener.GetContextAsync();
                ProcessContext(context);
            }
        }

        public void Stop()
        {
            listener.Stop();
            listener.Abort();
        }

        static Dictionary<string, string> MimeMapping = new Dictionary<string, string> {
            { ".html", "text/html" },
            { ".js", "application/javascript" }
        };

        async void ServeStaticFile(HttpListenerContext context, string file)
        {
            var contentFile = File.ReadAllBytes(Path.Combine("HtmlContent", file));

            string contentType;
            MimeMapping.TryGetValue(Path.GetExtension(file), out contentType);

            context.Response.ContentType = contentType;
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            await context.Response.OutputStream.WriteAsync(contentFile, 0, contentFile.Length);
            context.Response.OutputStream.Close();
        }

        async void ProcessWebApiRequest(HttpListenerContext context, WebApiMethod method)
        {
            var query = HttpUtility.ParseQueryString(context.Request.Url.Query);

            string postData = await new StreamReader(context.Request.InputStream).ReadToEndAsync();
            JToken dataJson = JObject.Parse(postData);
            JToken jsonReply = new JObject();
            var indexerString = (string)dataJson["indexer"];
            IndexerInterface indexer;

            try
            {
                indexer = indexerManager.GetIndexer(indexerString);
            }
            catch (Exception ex)
            {
                jsonReply["result"] = "error";
                jsonReply["error"] = ex.Message;
                ReplyWithJson(context, jsonReply);
                return;
            }

            context.Response.ContentType = "text/json";
            context.Response.StatusCode = (int)HttpStatusCode.OK;

            switch (method)
            {
                case WebApiMethod.GetConfigForm:
                    try
                    {
                        var config = await indexer.GetConfigurationForSetup();
                        jsonReply = config.ToJson();
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
                        await indexer.ApplyConfiguration(dataJson);
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

        async void ProcessContext(HttpListenerContext context)
        {
            Console.WriteLine(context.Request.Url.Query);

            string path = context.Request.Url.AbsolutePath.TrimStart('/');
            if (path == "")
                path = "index.html";

            if (Array.IndexOf(StaticFiles, path) > -1)
            {
                ServeStaticFile(context, path);
                return;
            }

            WebApiMethod apiMethod;
            if (WebApiMethods.TryGetValue(path, out apiMethod))
            {
                ProcessWebApiRequest(context, apiMethod);
                return;
            }

            var query = HttpUtility.ParseQueryString(context.Request.Url.Query);

            var inputStream = context.Request.InputStream;
            var reader = new StreamReader(inputStream, context.Request.ContentEncoding);
            var bytes = await reader.ReadToEndAsync();

            var indexer = context.Request.Url.AbsolutePath.TrimStart('/').Replace("/api", "").ToLower();

            var responseBytes = Encoding.UTF8.GetBytes(Properties.Resources.validator_reply);
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = responseBytes.LongLength;
            context.Response.ContentType = "application/rss+xml";
            await context.Response.OutputStream.WriteAsync(responseBytes, 0, responseBytes.Length);
            context.Response.Close();
        }
    }
}
