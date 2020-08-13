namespace Jackett.Common.Services.Interfaces
{
    public interface IServiceConfigService
    {
        void Install();
        void Uninstall();
        bool ServiceExists();
        bool ServiceRunning();
        void Start();
        void Stop();
    }
}
