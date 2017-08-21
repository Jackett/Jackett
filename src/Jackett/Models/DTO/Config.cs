using System;
namespace Jackett.Models.DTO
{
    public class ConfigItem
    {
        public string id { get; set; }
        public string value { get; set; }
        public string cookie { get; set; } // for cookie alternative login (captcha needed + remote host)
        public string challenge { get; set; } // for reCaptcha V1 compatibility
        public string version { get; set; } // for reCaptcha V1 compatibility
    }
}
