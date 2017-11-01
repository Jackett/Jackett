namespace Jackett.Services.Interfaces
{
    public interface IUpdateService
    {
        void StartUpdateChecker();
        void CheckForUpdatesNow();
        void CleanupTempDir();
    }
}
