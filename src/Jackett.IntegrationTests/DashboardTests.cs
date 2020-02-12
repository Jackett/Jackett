using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;

namespace Jackett.IntegrationTests
{
    [TestClass]
    public class DashboardTests
    {
        [TestMethod]
        public void CheckTitle()
        {
            var pageTitle = _webDriver.Title;
            Assert.AreEqual("Jackett", pageTitle);
        }

        [TestMethod]
        public void DefaultPortShown()
        {
            var port = _webDriver.FindElement(By.CssSelector("#jackett-port")).GetAttribute("value");
            Assert.AreEqual("9117", port);
        }

        [TestMethod]
        public void IndexerTableIsPresent()
        {
            var table = _webDriver.FindElement(By.CssSelector("#configured-indexer-datatable"));
            Assert.IsNotNull(table);
        }

        [TestMethod]
        public void AddIndexerOpens()
        {
            _webDriver.FindElement(By.CssSelector("#jackett-add-indexer")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal h4");
            var modalHeading = _webDriver.FindElement(By.CssSelector("#select-indexer-modal h4")).Text;
            Assert.AreEqual("Select an indexer to setup", modalHeading);
        }

        [TestMethod]
        public void CheckIndexersAreAvailableToAdd()
        {
            _webDriver.FindElement(By.CssSelector("#jackett-add-indexer")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal h4");
            var indexerCount = _webDriver.FindElements(By.CssSelector("#unconfigured-indexer-datatable tbody tr")).Count;
            Assert.IsTrue(indexerCount > 400);
        }

        [TestMethod]
        public void ManualSearchOpens()
        {
            _webDriver.FindElement(By.CssSelector("#jackett-show-search")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal div.modal-body p");
            var modalDescription = _webDriver.FindElement(By.CssSelector("#select-indexer-modal div.modal-body p")).Text;
            Assert.AreEqual("You can search all configured indexers from this screen.", modalDescription);
        }


        [ClassInitialize]
        public static void StartBrowser(TestContext testContext)
        {
            _webDriver = WebDriverFactory.CreateFromEnvironmentVariableSettings();
            _webDriver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(20);
            _webDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);
            _webDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
        }

        [ClassCleanup]
        public static void StopBrowser() => _webDriver?.Quit();

        [TestInitialize]
        public void LoadDashboard()
        {
            var url = "http://localhost:9117/UI/Dashboard";
            Console.WriteLine($"Url for test: {url}");
            _webDriver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(20));
            var element = wait.Until(x => x.FindElement(By.CssSelector("#configured-indexer-datatable")));
        }

        private bool WaitUntilModalIsDisplayed(string cssSelector)
        {
            var wait = new WebDriverWait(_webDriver, TimeSpan.FromSeconds(5));
            var element = wait.Until(condition =>
            {
                try
                {
                    var elementToBeDisplayed = _webDriver.FindElement(By.CssSelector(cssSelector));
                    return elementToBeDisplayed.Displayed;
                }
                catch (StaleElementReferenceException)
                {
                    return false;
                }
                catch (NoSuchElementException)
                {
                    return false;
                }
            });
            return false;
        }

        private static IWebDriver _webDriver;
    }
}
