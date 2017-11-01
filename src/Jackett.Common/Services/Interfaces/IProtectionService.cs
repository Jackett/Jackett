namespace Jackett.Services.Interfaces
{
    public interface IProtectionService
    {
        byte[] InstanceKey { get; set; }

        string Protect(string plainText);
        string UnProtect(string plainText);
    }
}
