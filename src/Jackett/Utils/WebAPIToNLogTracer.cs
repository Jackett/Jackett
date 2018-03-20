using System;
using System.Net.Http;
using System.Web.Http.Tracing;
using Jackett.Common;
using Jackett.Common.Models.Config;

namespace Jackett.Utils
{
    public class WebAPIToNLogTracer : ITraceWriter
    {
        private ServerConfig _serverConfig;

        public WebAPIToNLogTracer(ServerConfig serverConfig)
        {
            _serverConfig = serverConfig;
        }

        public void Trace(HttpRequestMessage request, string category, TraceLevel level, 
            Action<TraceRecord> traceAction)
        {
            if (_serverConfig.RuntimeSettings.TracingEnabled)
            {
                TraceRecord rec = new TraceRecord(request, category, level);
                traceAction(rec);
                WriteTrace(rec);
            }
        }

        protected void WriteTrace(TraceRecord rec)
        {
            var message = string.Format("{0} {1} {2}", rec.Operator, rec.Operation, rec.Message);
            switch (rec.Level)
            {
                case TraceLevel.Debug:
                    Engine.Logger.Debug(message);
                    break;
                case TraceLevel.Error:
                    Engine.Logger.Error(message);
                    break;
                case TraceLevel.Fatal:
                    Engine.Logger.Fatal(message);
                    break;
                case TraceLevel.Info:
                    Engine.Logger.Info(message);
                    break;
                case TraceLevel.Off:
                    // Do nothing?
                    break;
                case TraceLevel.Warn:
                    Engine.Logger.Warn(message);
                    break;
               
            }
         
            System.Diagnostics.Trace.WriteLine(message, rec.Category);
        }
    }
}
