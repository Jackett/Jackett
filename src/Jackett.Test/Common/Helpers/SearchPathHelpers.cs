using System.Collections.Generic;
using Jackett.Common.Helpers;
using NUnit.Framework;

namespace Jackett.Test.Common.Helpers
{
    [TestFixture]
    public class SearchPathHelpersTests
    {
        [Test]
        public void ExactMatch_ReturnsAll()
        {
            var searchPathCategories = new List<string> { "33", "66", "89" };
            var categories = new List<string> { "33", "89", "66" };

            var applicableCategories = SearchPathHelpers.GetApplicableCategories(searchPathCategories, categories);

            CollectionAssert.AreEquivalent(categories, applicableCategories);
        }

        [Test]
        public void AllCategoriesInSearchPath_ReturnsAll()
        {
            var searchPathCategories = new List<string> { "33", "66", "89" };
            var categories = new List<string> { "89", "66" };

            var applicableCategories = SearchPathHelpers.GetApplicableCategories(searchPathCategories, categories);

            CollectionAssert.AreEquivalent(categories, applicableCategories);
        }

        [Test]
        public void PartialNegated_ReturnsNonNegated()
        {
            var searchPathCategories = new List<string> { "!", "33", "66", "89" };
            var categories = new List<string> { "22", "58", "69", "68", "91", "33", "89", "66" };

            var applicableCategories = SearchPathHelpers.GetApplicableCategories(searchPathCategories, categories);

            CollectionAssert.AreEquivalent(new[] { "22", "58", "69", "68", "91" }, applicableCategories);
        }

        [Test]
        public void Partial_ReturnsOnlyPartialMatch()
        {
            var searchPathCategories = new List<string> { "33", "66", "89" };
            var categories = new List<string> { "22", "58", "69", "68", "91", "33", "89", "66" };

            var applicableCategories = SearchPathHelpers.GetApplicableCategories(searchPathCategories, categories);

            CollectionAssert.AreEquivalent(new[] { "33", "66", "89" }, applicableCategories);
        }

        [Test]
        public void AllNegated_NoResult()
        {
            var searchPathCategories = new List<string> { "!", "33", "66", "89" };
            var categories = new List<string> { "33", "89", "66" };

            var applicableCategories = SearchPathHelpers.GetApplicableCategories(searchPathCategories, categories);

            CollectionAssert.IsEmpty(applicableCategories);
        }
    }
}
