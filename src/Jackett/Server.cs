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
        WebApi webApi;

        public Server()
        {
            indexerManager = new IndexerManager();
            webApi = new WebApi(indexerManager);

            listener = new HttpListener();
            listener.Prefixes.Add("http://*:9117/");
        }

        public async void Start()
        {
            listener.Start();
            while (true)
            {
                var context = await listener.GetContextAsync();
                ProcessHttpRequest(context);
            }
        }

        public void Stop()
        {
            listener.Stop();
            listener.Abort();
        }

        async void ProcessHttpRequest(HttpListenerContext context)
        {
            if (webApi.HandleRequest(context))
            {
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
