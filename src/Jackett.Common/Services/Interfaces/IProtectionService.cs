namespace Jackett.Common.Services.Interfaces
{
    public interface IProtectionService
    {
        string Protect(string plainText);
        string UnProtect(string plainText);
        string LegacyProtect(string plainText);
        string LegacyUnProtect(string plainText);
    }
}
