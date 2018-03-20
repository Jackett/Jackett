using System.Net.Http;

namespace Jackett.Common.Services.Interfaces
{
    public interface ISecuityService
    {
        bool CheckAuthorised(HttpRequestMessage request);
        string HashPassword(string input);
        void Login(HttpResponseMessage request);
        void Logout(HttpResponseMessage request);
    }
}
