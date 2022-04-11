using System;
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
                var jackettUpdaterFI = new UnixFileInfo(path)
                {
                    FileAccessPermissions = FileAccessPermissions.UserReadWriteExecute | FileAccessPermissions.GroupRead | FileAccessPermissions.OtherRead
                };
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
