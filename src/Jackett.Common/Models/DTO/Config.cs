namespace Jackett.Common.Models.DTO
{
    public class ConfigItem
    {
        public string id { get; set; }
        public string value { get; set; }
        public string[] values { get; set; } // for array data (e.g. checkboxes)
        public string cookie { get; set; } // for cookie alternative login (captcha needed + remote host)
    }
}
