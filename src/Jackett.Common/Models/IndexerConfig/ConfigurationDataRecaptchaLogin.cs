using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Jackett.Models.IndexerConfig
{
    public class ConfigurationDataRecaptchaLogin : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public RecaptchaItem Captcha { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataRecaptchaLogin(string instructionMessageOptional = null)
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Captcha = new RecaptchaItem() { Name = "Recaptcha" };
            Instructions = new DisplayItem(instructionMessageOptional) { Name = "" };
        }


    }
}
