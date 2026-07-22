using System;
using System.IO;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;

namespace Jackett.IntegrationTests;

public static class WebDriverFactory
{
    public static IWebDriver CreateFromEnvironmentVariableSettings()
    {
        const int windowWidth = 1920;
        const int windowHeight = 1080;

        var chromeDriverDirectory = Environment.GetEnvironmentVariable("CHROMEWEBDRIVER") ?? Environment.CurrentDirectory;
        var chromeExecutable = Environment.GetEnvironmentVariable("CHROME_EXECUTABLE");
        var chromeOptions = new ChromeOptions();
        if (File.Exists(chromeExecutable))
        {
            chromeOptions.BinaryLocation = chromeExecutable;
        }
        chromeOptions.AddArgument("--headless");
        chromeOptions.AddArgument($"--window-size={windowWidth},{windowHeight}");

        return new ChromeDriver(chromeDriverDirectory, chromeOptions);
    }
}
