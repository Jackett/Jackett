namespace Jackett.Common.Services.Interfaces
{
    public interface IProcessService
    {
        void StartProcessAndLog(string exe, string args, bool asAdmin = false);
        string StartProcessAndGetOutput(string exe, string args, bool keepnewlines = false, bool asAdmin = false);
    }
}
