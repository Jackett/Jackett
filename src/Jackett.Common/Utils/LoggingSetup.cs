using System.IO;
using Jackett.Common.Models.Config;
using Jackett.Common.Services;
using Jackett.Common.Utils.Logging;
using NLog;
using NLog.Config;
using NLog.Targets;

namespace Jackett.Common.Utils
{
    public static class LoggingSetup
    {
        public static LoggingConfiguration GetLoggingConfiguration(RuntimeSettings settings, bool fileOnly = false)
        {
            var logFileName = settings.CustomLogFileName ?? "log.txt";
            var logLevel = settings.TracingEnabled ? NLog.LogLevel.Debug : NLog.LogLevel.Info;

            var logConfig = new LoggingConfiguration();

            var logFile = new CleanseFileTarget
            {
                Layout = "${longdate} ${level} ${message}${onexception:inner=${newline}${newline}[v${assembly-version}] ${exception:format=ToString}${newline}}",
                FileName = Path.Combine(settings.DataFolder, logFileName),
                ArchiveFileName = Path.Combine(settings.DataFolder, logFileName + ".{#####}.txt"),
                ArchiveAboveSize = 2097152, // 2 MB
                MaxArchiveFiles = 5,
                KeepFileOpen = false,
                ArchiveNumbering = ArchiveNumberingMode.DateAndSequence
            };
            logConfig.AddTarget("file", logFile);

            var microsoftLifetimeRule = new LoggingRule
            {
                LoggerNamePattern = "Microsoft.Hosting.Lifetime",
                Final = true
            };
            microsoftLifetimeRule.SetLoggingLevels(LogLevel.Info, LogLevel.Fatal);
            microsoftLifetimeRule.Targets.Add(logFile);
            logConfig.LoggingRules.Add(microsoftLifetimeRule);

            var microsoftRule = new LoggingRule
            {
                LoggerNamePattern = "Microsoft.*",
                Final = true
            };
            microsoftRule.SetLoggingLevels(LogLevel.Warn, LogLevel.Fatal);
            microsoftRule.Targets.Add(logFile);

            var microsoftDebugRule = new LoggingRule
            {
                LoggerNamePattern = "Microsoft.*",
                Final = true
            };
            microsoftDebugRule.SetLoggingLevels(LogLevel.Debug, LogLevel.Info);
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
                    Layout = "${date:format=MM-dd HH\\:mm\\:ss} ${level} ${message}${onexception:inner=${newline}${newline}[v${assembly-version}] ${exception:format=ToString}${newline}}"
                };
                logConfig.AddTarget("console", logConsole);

                var logConsoleRule = new LoggingRule("*", logLevel, logConsole);
                logConfig.LoggingRules.Add(logConsoleRule);

                var logService = new LogCacheService
                {
                    Layout = "${message}${onexception:inner=${newline}${newline}[v${assembly-version}] ${exception:format=ToString}${newline}}"
                };
                logConfig.AddTarget("service", logService);

                var serviceRule = new LoggingRule("*", logLevel, logService);
                logConfig.LoggingRules.Add(serviceRule);

                microsoftLifetimeRule.Targets.Add(logConsole);
                microsoftLifetimeRule.Targets.Add(logService);

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
    }
}
