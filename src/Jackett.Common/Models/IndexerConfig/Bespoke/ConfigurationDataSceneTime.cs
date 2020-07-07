namespace Jackett.Common.Models.IndexerConfig.Bespoke
{
    public class ConfigurationDataSceneTime : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public RecaptchaItem Captcha { get; private set; }
        public BoolItem Freeleech { get; private set; }
        public DisplayItem Instructions { get; private set; }

        public ConfigurationDataSceneTime()
            : base()
        {
            Username = new StringItem { Name = "Username" };
            Password = new StringItem { Name = "Password" };
            Captcha = new RecaptchaItem() { Name = "Recaptcha" };
            Freeleech = new BoolItem() { Name = "Freeleech Only (Optional)", Value = false };
            Instructions = new DisplayItem("For best results, change the 'Torrents per page' setting to the maximum in your profile on the SceneTime webpage.") { Name = "" };
        }
    }
}
