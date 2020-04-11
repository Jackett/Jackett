using System.Text;
using System.Web;
using Jackett.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Jackett.Test
{
    [TestClass]
    public class WebUtilityHelpersTests
    {
        private readonly Encoding[] _codePagesToTest;
        private readonly string[] _stringsToTest;

        public WebUtilityHelpersTests()
        {
            //https://docs.microsoft.com/en-us/dotnet/api/system.text.codepagesencodingprovider?view=netcore-2.0
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            _codePagesToTest = new Encoding[]{
                Encoding.UTF8,
                Encoding.ASCII,
                Encoding.GetEncoding("iso-8859-1"),
                Encoding.GetEncoding("windows-1255"),
                Encoding.GetEncoding("windows-1252"),
                Encoding.GetEncoding("windows-1251") }
            ;

            _stringsToTest = new string[]
            {
                "Test! אני לא יודע עברית, אבל אני מאמין שזה טקסט חוקי! $ # 2 אני תוהה אם אמוג'י יהיה נתמך 🐀.",
                "Å[ÉfÉBÉìÉOÇÕìÔÇµÇ≠Ç»Ç¢",
                "J͖ͥͨ̑͂̄̈́ḁ̹ͧͦ͡ͅc̲̗̮͍̻͓ͤk̳̥̖͗ͭ̾͌e̖̲̟̽ț̠͕͈͓͎̱t͕͕͓̹̹ͫͧ̆͑ͤ͝ ̼͓̟̣͔̇̒T̻̺̙̣̘͔̤̅͒̈̈͛̅e̥̗̍͟s̖̬̭͈̠t̫̩͙̯̩ͣ̏̕ ̸̰̬̄̀ͧ̀S̨̻̼̜̹̼͓̺ͨ̍ͦt͇̻̺̂́̄͌͗̕r̥͈̙͙̰͈̙͗̆̽̀i͉͔̖̻̹̗̣̍ͭ̒͗n̴̻͔̹̘̱̳͈͐ͦ̃̽͐̓̂g̴͚͙̲ͩ͌̆̉̀̾"
            };
        }



        [TestMethod]
        public void WebUtilityHelpers_UrlEncode_CorrectlyEncodes()
        {
            foreach (var encoding in _codePagesToTest)
            {
                foreach (var testString in _stringsToTest)
                {
                    //Check our implementation of Decode in .NET Standard Matches the .NET Framework Version                          
                    var NETString = HttpUtility.UrlEncode(testString, encoding);
                    var WebUtilityString = WebUtilityHelpers.UrlEncode(testString, encoding);
                    //Of note is that percent encoding gives lowercase values, where NET Native uses upper case this should be okay according to RFC3986 (https://tools.ietf.org/html/rfc3986#section-2.1)
                    var NETDecode = HttpUtility.UrlDecode(NETString);
                    var WebUtilityDecode = HttpUtility.UrlDecode(WebUtilityString);

                    Assert.AreEqual(NETDecode, WebUtilityDecode, $"{testString} did not match the expected decoded string with {encoding.EncodingName})");
                }
            }
        }

        [TestMethod]
        public void WebUtilityHelpers_UrlDecode_CorrectlyDecodes()
        {
            foreach (var encoding in _codePagesToTest)
            {
                foreach (var testString in _stringsToTest)
                {
                    //Check our implementation of Decode in .NET Standard Matches the .NET Framework Version      
                    var encodedString = HttpUtility.UrlEncode(testString, encoding);
                    var NETString = HttpUtility.UrlDecode(encodedString, encoding);
                    var WebUtilityString = WebUtilityHelpers.UrlDecode(encodedString, encoding);
                    Assert.AreEqual(NETString, WebUtilityString, $"{testString} did not match the expected decoded value after encoding with {encoding.EncodingName})");
                }
            }
        }
    }
}
