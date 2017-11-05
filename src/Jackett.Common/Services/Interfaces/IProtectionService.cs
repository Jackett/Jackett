namespace Jackett.Services.Interfaces
{
    public interface IProtectionService
    {
        string Protect(string plainText);
        string UnProtect(string plainText);
    }
}
