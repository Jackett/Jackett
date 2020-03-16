namespace Jackett.Common.Utils.Clients
{
    public class WebClientStringResult : BaseWebResult
    {
        public string ContentString { get; set; }

        public static implicit operator WebClientStringResult(WebClientByteResult br) => new WebClientStringResult()
        {
            ContentString = br.Encoding.GetString(br.ContentBytes),
            Cookies = br.Cookies,
            Encoding = br.Encoding,
            Headers = br.Headers,
            RedirectingTo = br.RedirectingTo,
            Request = br.Request,
            Status = br.Status
        };

    }
}
