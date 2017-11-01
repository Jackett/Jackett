namespace Jackett.Services.Interfaces
{
    public interface ITrayLockService
    {
        void WaitForSignal();
        void Signal();
    }
}
