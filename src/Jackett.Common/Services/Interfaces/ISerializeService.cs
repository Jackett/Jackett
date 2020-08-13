namespace Jackett.Common.Services.Interfaces
{
    public interface ISerializeService
    {
        string Serialise(object obj);
        T DeSerialise<T>(string json);
    }
}
