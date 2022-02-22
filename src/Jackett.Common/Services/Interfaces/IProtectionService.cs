namespace Jackett.Common.Services.Interfaces
{
    public interface IProtectionService
    {
        string Protect(string plainText);
        string UnProtect(string plainText);
    }
}
