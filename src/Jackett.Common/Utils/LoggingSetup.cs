using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Targets;
using System;
using System.IO;
using System.Text;

namespace Jackett.Common.Utils
{
    public static class LoggingSetup
    {
        public static LoggingConfiguration GetLoggingConfiguration(RuntimeSettings settings, bool fileOnly = false)
        {
            var logFileName = settings.CustomLogFileName ?? "log.txt";
            var logLevel = settings.TracingEnabled ? NLog.LogLevel.Debug : NLog.LogLevel.Info;
            // Add custom date time format renderer as the default is too long
            ConfigurationItemFactory.Default.LayoutRenderers.RegisterDefinition("simpledatetime", typeof(SimpleDateTimeRenderer));

            var logConfig = new LoggingConfiguration();
            var logFile = new FileTarget();
            logConfig.AddTarget("file", logFile);
            logFile.Layout = "${longdate} ${level} ${message} ${exception:format=ToString}";
            logFile.FileName = Path.Combine(settings.DataFolder, logFileName);
            logFile.ArchiveFileName = Path.Combine(settings.DataFolder, logFileName + ".{#####}.txt");
            logFile.ArchiveAboveSize = 500000;
            logFile.MaxArchiveFiles = 5;
            logFile.KeepFileOpen = false;
            logFile.ArchiveNumbering = ArchiveNumberingMode.DateAndSequence;
            var logFileRule = new LoggingRule("*", logLevel, logFile);
            logConfig.LoggingRules.Add(logFileRule);

            if (!fileOnly)
            {
                var logConsole = new ColoredConsoleTarget();
                logConfig.AddTarget("console", logConsole);

                logConsole.Layout = "${simpledatetime} ${level} ${message} ${exception:format=ToString}";
                var logConsoleRule = new LoggingRule("*", logLevel, logConsole);
                logConfig.LoggingRules.Add(logConsoleRule);

                var logService = new LogCacheService();
                logConfig.AddTarget("service", logService);

                var serviceMicrosoftRule = new LoggingRule();
                serviceMicrosoftRule.LoggerNamePattern = "Microsoft.*";
                serviceMicrosoftRule.SetLoggingLevels(LogLevel.Debug, LogLevel.Info);
                serviceMicrosoftRule.Final = true;
                if (settings.TracingEnabled)
                {
                    serviceMicrosoftRule.Targets.Add(logService);
                }
                logConfig.LoggingRules.Add(serviceMicrosoftRule);

                var serviceRule = new LoggingRule("*", logLevel, logService);
                logConfig.LoggingRules.Add(serviceRule);
            }

            return logConfig;
        }

        [LayoutRenderer("simpledatetime")]
        public class SimpleDateTimeRenderer : LayoutRenderer
        {
            protected override void Append(StringBuilder builder, LogEventInfo logEvent)
            {
                builder.Append(DateTime.Now.ToString("MM-dd HH:mm:ss"));
            }
        }
    }
}
