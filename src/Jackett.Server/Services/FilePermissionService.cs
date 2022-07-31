using System;
#if ISLINUXMUSL
using System.Diagnostics;
#endif
using Jackett.Common.Services.Interfaces;
using Mono.Unix;
using NLog;

namespace Jackett.Server.Services
{
    public class FilePermissionService : IFilePermissionService
    {
        private readonly Logger logger;

        public FilePermissionService(Logger l) => logger = l;

        public void MakeFileExecutable(string path)
        {
            logger.Debug($"Attempting to give execute permission to: {path}");
            try
            {
#if ISLINUXMUSL
                // Fix this error in Alpine Linux
                // Error System.DllNotFoundException: Unable to load shared library 'Mono.Unix' or one of its dependencies.
                // In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable:
                // Error loading shared library libMono.Unix: No such file or directory
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WindowStyle = ProcessWindowStyle.Hidden,
                        FileName = "chmod",
                        Arguments = $"+x \"{path}\""
                    }
                };
                process.Start();
                var stdErr = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.ExitCode != 0)
                    throw new Exception(stdErr);
#else
                var jackettUpdaterFI = new UnixFileInfo(path)
                {
                    FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute | FileAccessPermissions.GroupRead
                        | FileAccessPermissions.OtherRead
                };
#endif
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
