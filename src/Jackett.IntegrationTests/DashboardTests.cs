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
            var pageTitle = s_WebDriver.Title;
            Assert.AreEqual("Jackett", pageTitle);
        }

        [TestMethod]
        public void DefaultPortShown()
        {
            var port = s_WebDriver.FindElement(By.CssSelector("#jackett-port")).GetAttribute("value");
            Assert.AreEqual("9117", port);
        }

        [TestMethod]
        public void IndexerTableIsPresent()
        {
            var table = s_WebDriver.FindElement(By.CssSelector("#configured-indexer-datatable"));
            Assert.IsNotNull(table);
        }

        [TestMethod]
        public void AddIndexerOpens()
        {
            s_WebDriver.FindElement(By.CssSelector("#jackett-add-indexer")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal h4");
            var modalHeading = s_WebDriver.FindElement(By.CssSelector("#select-indexer-modal h4")).Text;
            Assert.AreEqual("Select an indexer to setup", modalHeading);
        }

        [TestMethod]
        public void CheckIndexersAreAvailableToAdd()
        {
            s_WebDriver.FindElement(By.CssSelector("#jackett-add-indexer")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal h4");
            var indexerCount = s_WebDriver.FindElements(By.CssSelector("#unconfigured-indexer-datatable tbody tr")).Count;
            Assert.IsTrue(indexerCount > 400);
        }

        [TestMethod]
        public void ManualSearchOpens()
        {
            s_WebDriver.FindElement(By.CssSelector("#jackett-show-search")).Click();
            WaitUntilModalIsDisplayed("#select-indexer-modal div.modal-body p");
            var modalDescription = s_WebDriver.FindElement(By.CssSelector("#select-indexer-modal div.modal-body p")).Text;
            Assert.AreEqual("You can search all configured indexers from this screen.", modalDescription);
        }

        [ClassInitialize]
        public static void StartBrowser(TestContext testContext)
        {
            s_WebDriver = WebDriverFactory.CreateFromEnvironmentVariableSettings();
            s_WebDriver.Manage().Timeouts().AsynchronousJavaScript = TimeSpan.FromSeconds(20);
            s_WebDriver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(20);
            s_WebDriver.Manage().Timeouts().PageLoad = TimeSpan.FromSeconds(20);
        }

        [ClassCleanup]
        public static void StopBrowser() => s_WebDriver?.Quit();

        [TestInitialize]
        public void LoadDashboard()
        {
            var url = "http://localhost:9117/UI/Dashboard";
            Console.WriteLine($"Url for test: {url}");
            s_WebDriver.Navigate().GoToUrl(url);
            var wait = new WebDriverWait(s_WebDriver, TimeSpan.FromSeconds(20));
            var element = wait.Until(x => x.FindElement(By.CssSelector("#configured-indexer-datatable")));
        }

        private bool WaitUntilModalIsDisplayed(string cssSelector)
        {
            var wait = new WebDriverWait(s_WebDriver, TimeSpan.FromSeconds(5));
            var element = wait.Until(
                condition =>
                {
                    try
                    {
                        var elementToBeDisplayed = s_WebDriver.FindElement(By.CssSelector(cssSelector));
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

        private static IWebDriver s_WebDriver;
    }
}
