namespace Jackett.Common.Services.Interfaces
{
    public interface IFilePermissionService
    {
        void MakeFileExecutable(string path);
    }
}
