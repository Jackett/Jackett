namespace Jackett.Models.IndexerConfig.Bespoke
{
    class ConfigurationDataTorrenting : ConfigurationData
    {
        public StringItem Username { get; private set; }
        public StringItem Password { get; private set; }
        public HiddenItem RSSKey { get; private set; }
        //public DisplayItem AdvancedWarning { get; private set; }
        public BoolItem Browser { get; private set; }
        //public BoolItem UseRSS { get; private set; }
        public DisplayItem HeadersWarning { get; private set; }
        public StringItem HeaderAccept { get; private set; }
        public StringItem HeaderAcceptLang { get; private set; }
        public BoolItem HeaderDNT { get; private set; }
        public BoolItem HeaderUpgradeInsecure { get; private set; }
        public StringItem HeaderUserAgent { get; private set; }

        public DisplayItem DevWarning { get; private set; }
        public BoolItem DevMode { get; private set; }
        //public DisplayItem Nothing { get; set; }

        public ConfigurationDataTorrenting()
            : base()
        {
            Username = new StringItem { Name = "Username", Value = "" };
            Password = new StringItem { Name = "Password", Value = "" };
            //UseRSS = new BoolItem() { Name = "Use RSS when not searching", Value = true };
            //AdvancedWarning = new DisplayItem("<b>Advanced Configuration</b><input type=\"checkbox\" class=\"form - control\" id=\"advtoggle\">") { Name = "Advanced Configuration (optional)" };
            HeadersWarning = new DisplayItem("<b>Browser Simulation (Optional)</b> <br /> <ul><li>By filling these fields, <b>Jackett will inject headers</b> with your values <u>to simulate a real browser</u>.</li><li>You can get <b>your browser values</b> here: <a href='https://www.whatismybrowser.com/detect/what-http-headers-is-my-browser-sending' target='blank'>www.whatismybrowser.com</a></li></ul><br /><i><b>Note that</b> some headers are not necessary because they are injected automatically by this provider such as Accept_Encoding, Connection, Host or X-Requested-With</i>") { Name = "Injecting headers" };
            Browser = new BoolItem() { Name = "Enable Browser Simulation (Optional)", Value = true };
            HeaderAccept = new StringItem { Name = "Accept", Value = "text/html, application/xhtml+xml, image/jxr, */*" };
            HeaderAcceptLang = new StringItem { Name = "Accept-Language", Value = "en-US" };
            HeaderDNT = new BoolItem { Name = "DNT (Do Not Track)", Value = false };
            HeaderUpgradeInsecure = new BoolItem { Name = "Upgrade-Insecure-Requests", Value = false };
            HeaderUserAgent = new StringItem { Name = "User-Agent", Value = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko" };
            DevWarning = new DisplayItem("<b>Devlopement Facility</b> (<i>For Developers ONLY</i>),<br /><br /> <ul><li>By enabling devlopement mode, <b>Jackett will bypass his cache</b> and will <u>output debug messages to console</u> instead of his log file.</li></ul>") { Name = "Devlopement" };
            DevMode = new BoolItem { Name = "Enable DEV MODE (Developers ONLY)", Value = false };
            RSSKey = new HiddenItem { Name = "RSSKey" };
            //Nothing = new DisplayItem("<script language='javascript'>$( document ).ready(function() { $('#advtoggle').change(function () { $('#advdivcontent').toggle(this.checked); }).change(); var f=$('.config-setup-form').get(0);var n=undefined;n=f.childNodes;var found=0;var d=document.createElement('div');d.id='advdivcontent'; d.style.margin = '5px'; d.style.padding = '5px'; d.style.display = 'none'; var lastNode = undefined; for (var i = n.length; i > 0; i--) { var e=n[i-1]; var eid = '' ; if (found == 0 && e.attributes !== undefined && e.hasAttribute('data-id') && e.attributes['data-id'].value == 'advancedconfiguration(optional)') { e.id = 'advancedconfiguration(optional)'; found=1; e.appendChild(d);} ; if (found == 0) { if (lastNode === undefined) {d.appendChild(e);} else {d.insertBefore(e,d.firstChild);};lastNode=e}; };  });</script> xxxx") { Name = "Nothing"};
        }
    }
}