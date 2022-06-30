namespace Jackett.Common.Services.Interfaces
{
    public interface IUpdateService
    {
        void StartUpdateChecker();
        void CheckForUpdatesNow();
        void CleanupTempDir();
        void CheckUpdaterLock();
    }
}
