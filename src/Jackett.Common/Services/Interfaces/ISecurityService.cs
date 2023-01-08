namespace Jackett.Common.Services.Interfaces
{
    public interface ISecurityService
    {
        bool CheckAuthorised(string password);
        string HashPassword(string input);
    }
}
