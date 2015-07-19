using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Services
{
    public interface IProcessService
    {
        void StartProcessAndLog(string exe, string args);
    }

    public class ProcessService : IProcessService
    {
        private Logger logger;

        public ProcessService(Logger l)
        {
            logger = l;
        }

        public void StartProcessAndLog(string exe, string args)
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

            var proc = Process.Start(startInfo);
            proc.OutputDataReceived += proc_OutputDataReceived;
            proc.ErrorDataReceived += proc_ErrorDataReceived;
            proc.BeginErrorReadLine();
            proc.BeginOutputReadLine();
            proc.WaitForExit();
            proc.OutputDataReceived -= proc_OutputDataReceived;
            proc.ErrorDataReceived -= proc_ErrorDataReceived;
        }

        void proc_ErrorDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                logger.Error(e.Data);
            }
        }

        void proc_OutputDataReceived(object sender, DataReceivedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(e.Data))
            {
                logger.Debug(e.Data);
            }
        }
    }
}
