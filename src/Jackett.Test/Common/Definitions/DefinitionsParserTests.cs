using System;
using System.IO;
using System.Reflection;
using Jackett.Common.Models;
using NUnit.Framework;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using Assert = NUnit.Framework.Assert;

namespace Jackett.Test.Common.Definitions
{
    [TestFixture]
    public class DefinitionsParserTests
    {
        [Test]
        public void LoadAndParseAllCardigannDefinitions()
        {
            var applicationFolder = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? "";
            var definitionsFolder = Path.Combine(applicationFolder, "Definitions");
            var deserializer = new DeserializerBuilder()
                               .WithNamingConvention(CamelCaseNamingConvention.Instance)
                               .Build();
            var files = new DirectoryInfo(definitionsFolder).GetFiles("*.yml");
            Assert.True(files.Length > 0);
            foreach (var file in files)
                try
                {
                    var definitionString = File.ReadAllText(file.FullName);
                    deserializer.Deserialize<IndexerDefinition>(definitionString);
                }
                catch (Exception ex)
                {
                    Assert.Fail($"Error while parsing Cardigann definition {file.Name}\n{ex}");
                }
        }
    }
}
