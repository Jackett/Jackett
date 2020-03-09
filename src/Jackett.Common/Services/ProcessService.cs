using System.Diagnostics;
using System.Text;
using Jackett.Common.Services.Interfaces;
using NLog;

namespace Jackett.Common.Services
{

    public class ProcessService : IProcessService
    {
        private readonly Logger logger;

        public ProcessService(Logger l) => logger = l;

        private void Run(string exe, string args, bool asAdmin, DataReceivedEventHandler d, DataReceivedEventHandler r)
        {
            var startInfo = new ProcessStartInfo()
            {
                CreateNoWindow = true,
                UseShellExecute = false,
                FileName = exe,
                Arguments = args,
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                RedirectStandardInput = true
            };

            if (asAdmin)
            {
                startInfo.Verb = "runas";
                startInfo.UseShellExecute = true;
                startInfo.RedirectStandardError = false;
                startInfo.RedirectStandardOutput = false;
                startInfo.RedirectStandardInput = false;
            }
            logger.Debug("Running " + startInfo.FileName + " " + startInfo.Arguments);
            var proc = Process.Start(startInfo);

            if (!asAdmin)
            {
                proc.OutputDataReceived += d;
                proc.ErrorDataReceived += r;
                proc.BeginErrorReadLine();
                proc.BeginOutputReadLine();
            }
            proc.WaitForExit();
            if (!asAdmin)
            {
                proc.OutputDataReceived -= d;
                proc.ErrorDataReceived -= r;
            }
        }

        public string StartProcessAndGetOutput(string exe, string args, bool keepnewlines = false, bool asAdmin = false)
        {
            var sb = new StringBuilder();
            DataReceivedEventHandler rxData = (a, e) =>
            {
                if (keepnewlines || !string.IsNullOrWhiteSpace(e.Data))
                {
                    sb.AppendLine(e.Data);
                }
            };
            DataReceivedEventHandler rxError = (s, e) =>
            {
                if (keepnewlines || !string.IsNullOrWhiteSpace(e.Data))
                {
                    sb.AppendLine(e.Data);
                }
            };

            Run(exe, args, asAdmin, rxData, rxError);
            return sb.ToString();
        }

        public void StartProcessAndLog(string exe, string args, bool asAdmin = false)
        {
            var sb = new StringBuilder();
            DataReceivedEventHandler rxData = (a, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.Debug(e.Data);
                }
            };
            DataReceivedEventHandler rxError = (s, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    logger.Error(e.Data);
                }
            };

            Run(exe, args, asAdmin, rxData, rxError);
        }
    }
}
