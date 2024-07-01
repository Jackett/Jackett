using System.Collections;
using System.Collections.Generic;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.Toloka
{
    [TestFixture]
    public class TolokaTests
    {
        [TestCaseSource(typeof(TitleParserTestData), nameof(TitleParserTestData.TestCases))]
        public string TestTitleParsing(string title, ICollection<int> category, bool stripCyrillicLetters)
        {
            var titleParser = new Jackett.Common.Indexers.Definitions.Toloka.TitleParser();

            return titleParser.Parse(title, category, stripCyrillicLetters);
        }
    }

    public class TitleParserTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("Правдива терапія (Сезон 1, серії 1-2) / Shrinking (Season 1, episodes 1-2) (2023) WEBRip 1080p Ukr/Eng", new List<int> { TorznabCatType.TV.ID }, true).Returns("Shrinking (S1E1-2) (2023) WEBRip 1080p Ukr/Eng (S1E1-2)");
                yield return new TestCaseData("Ші-Ра та принцеси могутності (сезон 1-2, серій 14 з 20) / She-Ra and the Princesses of Power (seasons 1-2, episodes 14 of 20) (2018) WEBRip 1080p", new List<int> { TorznabCatType.TVHD.ID }, true).Returns("She-Ra and the Princesses of Power (S1-2, E14 of 20) (2018) WEBRip 1080p (S1-2, E14 of 20)");
                yield return new TestCaseData("А інші сгорять у пеклі (Сезон 1, Серія 3) / Everyone Else Burns (Season 1, Episode 3) (2023) WEB-DL 1080p Ukr/Eng | Sub Ukr/Eng", new List<int> { TorznabCatType.TVOther.ID }, true).Returns("Everyone Else Burns (S1E3) (2023) WEB-DL 1080p Ukr/Eng | Sub Ukr/Eng (S1E3)");
                yield return new TestCaseData("У тілі (Сезон 2, Епізод 1,2 з ХХ) / In the flesh (Season 2, episodes 1,2 of XX) (2014) 1080p BDRip Eng | sub Ukr", new List<int> { TorznabCatType.TVSport.ID }, true).Returns("In the flesh (S2E1-2 of XX) (2014) 1080p BDRip Eng | sub Ukr (S2E1-2 of XX)");

                yield return new TestCaseData("Правдива терапія (Сезон 1, серії 1-2) / Shrinking (Season 1, episodes 1-2) (2023) WEBRip 1080p Ukr/Eng", new List<int> { TorznabCatType.TVHD.ID }, false).Returns("Правдива терапія (S1E1-2) / Shrinking (S1E1-2) (2023) WEBRip 1080p Ukr/Eng");
                yield return new TestCaseData("Ші-Ра та принцеси могутності (сезон 1-2, серій 14 з 20) / She-Ra and the Princesses of Power (seasons 1-2, episodes 14 of 20) (2018) WEBRip 1080p", new List<int> { TorznabCatType.TVAnime.ID }, false).Returns("Ші-Ра та принцеси могутності (S1-2, E14 of 20) / She-Ra and the Princesses of Power (S1-2, E14 of 20) (2018) WEBRip 1080p");
                yield return new TestCaseData("А інші сгорять у пеклі (Сезон 1, Серія 3) / Everyone Else Burns (Season 1, Episode 3) (2023) WEB-DL 1080p Ukr/Eng | Sub Ukr/Eng", new List<int> { TorznabCatType.TVDocumentary.ID }, false).Returns("А інші сгорять у пеклі (S1E3) / Everyone Else Burns (S1E3) (2023) WEB-DL 1080p Ukr/Eng | Sub Ukr/Eng");
                yield return new TestCaseData("У тілі (Сезон 2, Епізод 1,2 з ХХ) / In the flesh (Season 2, episodes 1,2 of XX) (2014) 1080p BDRip Eng | sub Ukr", new List<int> { TorznabCatType.TV.ID }, false).Returns("У тілі (S2E1-2 of XX) / In the flesh (S2E1-2 of XX) (2014) 1080p BDRip Eng | sub Ukr");
            }
        }
    }
}
