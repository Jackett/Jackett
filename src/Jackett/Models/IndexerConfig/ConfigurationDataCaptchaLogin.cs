using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    class ConfigurationDataCaptchaLogin : ConfigurationData
    {
        public StringItem Username { get; private set; }

        public StringItem Password { get; private set; }

        public ImageItem CaptchaImage { get; private set; }

        public StringItem CaptchaText { get; private set; }

        public HiddenItem CaptchaCookie { get; private set; }

        public ConfigurationDataCaptchaLogin()
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            CaptchaImage = new ImageItem { Name = "Captcha Image" };
            CaptchaText = new StringItem { Name = "Captcha Text" };
            CaptchaCookie = new HiddenItem("") { Name = "Captcha Cookie" };
        }
    }
}
