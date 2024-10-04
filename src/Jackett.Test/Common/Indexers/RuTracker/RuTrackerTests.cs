using System.Collections;
using System.Collections.Generic;
using Jackett.Common.Models;
using NUnit.Framework;

namespace Jackett.Test.Common.Indexers.RuTracker
{
    [TestFixture]
    public class RuTrackerTests
    {
        [TestCaseSource(typeof(TitleParserTestData), nameof(TitleParserTestData.TestCases))]
        public string TestTitleParsing(string title, ICollection<int> category, bool stripCyrillicLetters, bool moveAllTagsToEndOfReleaseTitle, bool moveFirstTagsToEndOfReleaseTitle)
        {
            var titleParser = new Jackett.Common.Indexers.Definitions.RuTracker.TitleParser();

            return titleParser.Parse(title, category, stripCyrillicLetters, moveAllTagsToEndOfReleaseTitle, moveFirstTagsToEndOfReleaseTitle);
        }
    }

    public class TitleParserTestData
    {
        public static IEnumerable TestCases
        {
            get
            {
                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVSD.ID }, false, false, false).Returns("Терапия / Shrinking / S1E1-2 of 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DL] Dub (Iyuno-SDI Group) + Original + Sub Rus");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / сезон: 5 / Серии: 1-14 из ?? (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVForeign.ID }, false, false, false).Returns("Новичок / Новобранец / The Rookie / S5E1-14 of ?? (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DL] MVO (LostFilm) + Original");
                yield return new TestCaseData("Красный яр / Сезон: 1-8 (Михаил Вассербаум) [2022, детектив, WEBRip-AVC]", new List<int> { TorznabCatType.TVOther.ID }, false, false, false).Returns("Красный яр / S1-8 (Михаил Вассербаум) [2022, детектив, WEBRip-AVC]");
                yield return new TestCaseData("Просто Михалыч / Эпизод: 1-5 из ХХ (Евгений Корчагин) [2022, комедия, WEBRip 720p]", new List<int> { TorznabCatType.TVHD.ID }, false, false, false).Returns("Просто Михалыч / E1-5 of XX (Евгений Корчагин) [2022, комедия, WEBRip 720p]");
                yield return new TestCaseData("Открывай, полиция! / Выпуски: 1,2 (Сергей Гинзбург) [2022, комедия, WEBRip]", new List<int> { TorznabCatType.TV.ID }, false, false, false).Returns("Открывай, полиция! / E1-2 (Сергей Гинзбург) [2022, комедия, WEBRip]");

                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVHD.ID }, true, false, false).Returns("Shrinking / S1E1-2 of 10 [2023, WEB-DL] Dub (Iyuno-SDI Group) + Original + Sub Rus");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / сезон: 5 / Серии: 1-14 из ?? (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVForeign.ID }, true, false, false).Returns("The Rookie / S5E1-14 of ?? [2022, WEB-DL] MVO (LostFilm) + Original");
                yield return new TestCaseData("Красный яр / Сезон: 1-8 (Михаил Вассербаум) [2022, детектив, WEBRip-AVC]", new List<int> { TorznabCatType.TVOther.ID }, true, false, false).Returns("S1-8 [2022, WEBRip-AVC]");
                yield return new TestCaseData("Просто Михалыч / Эпизод: 1-5 из ХХ (Евгений Корчагин) [2022, комедия, WEBRip 720p]", new List<int> { TorznabCatType.TVHD.ID }, true, false, false).Returns("E1-5 of XX [2022, WEBRip 720p]");
                yield return new TestCaseData("Открывай, полиция! / Выпуски: 1,2 (Сергей Гинзбург) [2022, комедия, WEBRip]", new List<int> { TorznabCatType.TV.ID }, true, false, false).Returns("E1-2 [2022, WEBRip]");

                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / Серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVUHD.ID }, true, false, true).Returns("Shrinking / S1E1-2 of 10 [2023, WEB-DL] Dub (Iyuno-SDI Group) + Original + Sub Rus");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / Сезон: 5 / Серии: 1-14 из ?? (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVSport.ID }, true, false, true).Returns("The Rookie / S5E1-14 of ?? [2022, WEB-DL] MVO (LostFilm) + Original");

                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / Серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVAnime.ID }, true, true, false).Returns("Shrinking / S1E1-2 of 10 Dub + Original + Sub Rus (Iyuno-SDI Group) [2023, WEB-DL]");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / Сезон: 5 / Серии: 1-14 из ХХ (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVDocumentary.ID }, true, true, false).Returns("The Rookie / S5E1-14 of XX MVO + Original (LostFilm) [2022, WEB-DL]");

                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / Серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVAnime.ID }, true, true, true).Returns("Shrinking / S1E1-2 of 10 Dub + Original + Sub Rus (Iyuno-SDI Group) [2023, WEB-DL]");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / Сезон: 5 / Серии: 1,14 из ?? (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVDocumentary.ID }, true, true, true).Returns("The Rookie / S5E1-14 of ?? MVO + Original (LostFilm) [2022, WEB-DL]");

                yield return new TestCaseData("Терапия / Shrinking / Сезон: 1 / Серии: 1-2 из 10 (Джеймс Понсольдт) [2023, США, комедия, WEB-DLRip] Dub (Iyuno-SDI Group) + Original + Sub Rus", new List<int> { TorznabCatType.TVHD.ID }, false, true, false).Returns("Терапия / Shrinking / S1E1-2 of 10 Dub + Original + Sub Rus (Джеймс Понсольдт) (Iyuno-SDI Group) [2023, США, комедия, WEB-DL]");
                yield return new TestCaseData("Новичок / Новобранец / The Rookie / Сезон: 5 / Серии: 1,14 из ХХ (Майкл Гои, Билл Роу) [2022, США, боевик, драма, криминал, WEB-DLRip] MVO (LostFilm) + Original", new List<int> { TorznabCatType.TVForeign.ID }, false, true, false).Returns("Новичок / Новобранец / The Rookie / S5E1-14 of XX MVO + Original (Майкл Гои, Билл Роу) (LostFilm) [2022, США, боевик, драма, криминал, WEB-DL]");

                yield return new TestCaseData("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [12+2 из 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]", new List<int> { TorznabCatType.TVAnime.ID }, true, false, true).Returns("Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [E12+2 of 12+2] [JAP+Sub] [2023, BDRip] [1080p]");
                yield return new TestCaseData("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [12+2 из 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]", new List<int> { TorznabCatType.TVAnime.ID }, true, true, false).Returns("Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [E12+2 of 12+2] [JAP+Sub] [2023, BDRip] [1080p]");
                yield return new TestCaseData("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [12+2 из 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]", new List<int> { TorznabCatType.TVAnime.ID }, true, true, true).Returns("Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [E12+2 of 12+2] [JAP+Sub] [2023, BDRip] [1080p]");
                yield return new TestCaseData("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [12+2 из 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]", new List<int> { TorznabCatType.TVAnime.ID }, false, true, false).Returns("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [E12+2 of 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]");
                yield return new TestCaseData("Стать любимой собачкой / Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [12+2 из 12+2] [JAP+Sub] [2023, комедия, этти, BDRip] [1080p]", new List<int> { TorznabCatType.TVAnime.ID }, true, false, false).Returns("Inu ni Nattara Suki na Hito ni Hirowareta / My Life as Inukai-san's Dog [TV+Special] [E12+2 of 12+2] [JAP+Sub] [2023, BDRip] [1080p]");
            }
        }
    }
}
