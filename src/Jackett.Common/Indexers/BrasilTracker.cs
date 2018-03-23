using System.Collections.Generic;
using Jackett.Common.Indexers.Abstract;
using Jackett.Common.Models;
using Jackett.Common.Services.Interfaces;
using Jackett.Common.Utils.Clients;
using NLog;

namespace Jackett.Common.Indexers
{
    public class BrasilTracker : GazelleTracker
    {
        public BrasilTracker(IIndexerConfigurationService configService, WebClient webClient, Logger logger, IProtectionService protectionService)
            : base(name: "Brasil Tracker",
                desc: "A Brasil tracker",
                link: "https://brasiltracker.org/",
                configService: configService,
                logger: logger,
                protectionService: protectionService,
                webClient: webClient,
                supportsFreeleechTokens: true
                )
        {
            Language = "pt-br";
            Type = "private";
            TorznabCaps.SupportedMusicSearchParamsList = new List<string>() { "q", "album", "artist", "label", "year" };

            AddCategoryMapping(1, TorznabCatType.Movies, "Filmes");
            AddCategoryMapping(2, TorznabCatType.TV, "Series");
            AddCategoryMapping(3, TorznabCatType.XXX, "Filmes XXX");
            AddCategoryMapping(4, TorznabCatType.Audio, "Música");
            AddCategoryMapping(5, TorznabCatType.TV, "Show");
            AddCategoryMapping(6, TorznabCatType.TVAnime, "Animes");
            AddCategoryMapping(7, TorznabCatType.TV, "Televisão");
            AddCategoryMapping(8, TorznabCatType.TVDocumentary, "Documentário");
            AddCategoryMapping(9, TorznabCatType.PCGames, "Jogos");
            AddCategoryMapping(10, TorznabCatType.BooksMagazines, "Revistas");
            AddCategoryMapping(11, TorznabCatType.PC0day, "Aplicativos");
            AddCategoryMapping(12, TorznabCatType.BooksComics, "Histórias em Quadrinhos");
            AddCategoryMapping(13, TorznabCatType.Books, "Livros");
            AddCategoryMapping(14, TorznabCatType.TVSport, "Esportes");
            AddCategoryMapping(15, TorznabCatType.Other, "Vídeo Aula");
        }
    }
}