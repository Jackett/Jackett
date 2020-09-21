namespace Jackett.Common.Utils.Clients
{
    public class WebClientStringResult : BaseWebResult
    {
        public static implicit operator WebClientStringResult(WebClientByteResult br) => new WebClientStringResult()
        {
            ContentBytes = br.ContentBytes,
            Cookies = br.Cookies,
            Encoding = br.Encoding,
            Headers = br.Headers,
            RedirectingTo = br.RedirectingTo,
            Request = br.Request,
            Status = br.Status
        };

    }
}
