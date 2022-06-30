using System;
using System.IO;
using System.Text;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using NLog.Targets;

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

            var logFile = new FileTarget
            {
                Layout = "${longdate} ${level} ${message} ${exception:format=ToString}",
                FileName = Path.Combine(settings.DataFolder, logFileName),
                ArchiveFileName = Path.Combine(settings.DataFolder, logFileName + ".{#####}.txt"),
                ArchiveAboveSize = 2097152, // 2 MB
                MaxArchiveFiles = 5,
                KeepFileOpen = false,
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence
            };
            logConfig.AddTarget("file", logFile);

            var microsoftRule = new LoggingRule
            {
                LoggerNamePattern = "Microsoft.*",
                Final = true
            };
            microsoftRule.SetLoggingLevels(LogLevel.Warn, LogLevel.Fatal);
            microsoftRule.Targets.Add(logFile);

            var microsoftDebugRule = new LoggingRule
            {
                LoggerNamePattern = "Microsoft.*"
            };
            microsoftDebugRule.SetLoggingLevels(LogLevel.Debug, LogLevel.Info);
            microsoftDebugRule.Final = true;
            if (settings.TracingEnabled)
            {
                microsoftDebugRule.Targets.Add(logFile);
            }
            logConfig.LoggingRules.Add(microsoftDebugRule);

            var logFileRule = new LoggingRule("*", logLevel, logFile);
            logConfig.LoggingRules.Add(logFileRule);

            if (!fileOnly)
            {
                var logConsole = new ColoredConsoleTarget
                {
                    Layout = "${simpledatetime} ${level} ${message} ${exception:format=ToString}"
                };
                logConfig.AddTarget("console", logConsole);

                var logConsoleRule = new LoggingRule("*", logLevel, logConsole);
                logConfig.LoggingRules.Add(logConsoleRule);

                var logService = new LogCacheService();
                logConfig.AddTarget("service", logService);

                var serviceRule = new LoggingRule("*", logLevel, logService);
                logConfig.LoggingRules.Add(serviceRule);

                microsoftRule.Targets.Add(logConsole);
                microsoftRule.Targets.Add(logService);

                if (settings.TracingEnabled)
                {
                    microsoftDebugRule.Targets.Add(logConsole);
                    microsoftDebugRule.Targets.Add(logService);
                }
            }

            logConfig.LoggingRules.Add(microsoftRule);

            return logConfig;
        }

        [LayoutRenderer("simpledatetime")]
        public class SimpleDateTimeRenderer : LayoutRenderer
        {
            protected override void Append(StringBuilder builder, LogEventInfo logEvent) =>
                builder.Append(DateTime.Now.ToString("MM-dd HH:mm:ss"));
        }
    }
}
