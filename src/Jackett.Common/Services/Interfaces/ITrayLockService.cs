namespace Jackett.Common.Services.Interfaces
{
    public interface ITrayLockService
    {
        void WaitForSignal();
        void Signal();
    }
}
