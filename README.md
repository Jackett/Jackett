# Jackett

[![GitHub issues](https://img.shields.io/github/issues/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/pulls)
[![Build Status](https://dev.azure.com/Jackett/Jackett/_apis/build/status/Jackett.Jackett?branchName=master)](https://dev.azure.com/jackett/jackett/_build/latest?definitionId=1&branchName=master)
[![GitHub Releases](https://img.shields.io/github/downloads/Jackett/Jackett/total.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/linuxserver/jackett.svg?maxAge=60&style=flat-square)](https://hub.docker.com/r/linuxserver/jackett/)

_Our [![Discord](https://img.shields.io/badge/discord-chat-7289DA.svg?maxAge=60&style=flat-square)](https://discord.gg/J865QuA) server is no longer maintained. If you have a problem, request, or question then please open a new issue here._

This project is a new fork and is recruiting development help.  If you are able to help out please [contact us](https://github.com/Jackett/Jackett/issues/8180).

Please see our [troubleshooting and contributing guidelines](CONTRIBUTING.md) before submitting any issues or pull requests

Jackett works as a proxy server: it translates queries from apps ([Sonarr](https://github.com/Sonarr/Sonarr), [Radarr](https://github.com/Radarr/Radarr), [SickRage](https://sickrage.github.io/), [CouchPotato](https://couchpota.to/), [Mylar3](https://github.com/mylar3/mylar3), [Lidarr](https://github.com/lidarr/lidarr), [DuckieTV](https://github.com/SchizoDuckie/DuckieTV), [qBittorrent](https://www.qbittorrent.org/), [Nefarious](https://github.com/lardbit/nefarious) etc.) into tracker-site-specific http queries, parses the html or json response, and then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps.

Developer note: The software implements the [Torznab](https://web.archive.org/web/20220517013745/https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with hybrid [nZEDb](https://github.com/nZEDb/nZEDb/blob/b485fa326a0ff1f47ce144164eb1f070e406b555/resources/db/schema/data/10-categories.tsv)/[Newznab](https://newznab.readthedocs.io/en/latest/misc/api/#predefined-categories) [category numbering](https://github.com/Jackett/Jackett/wiki/Jackett-Categories)) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) APIs.

A third-party Golang SDK for Jackett is available from [webtor-io/go-jackett](https://github.com/webtor-io/go-jackett)

#### Supported Systems
* Windows 7 SP1 or greater
* Linux [supported operating systems here](https://github.com/dotnet/core/blob/main/release-notes/6.0/supported-os.md#linux)
* macOS 10.15+ or greater

<details> <summary> <b> Supported Public Trackers </b> </summary>

 * ØMagnet
 * 1337x
 * ACG.RIP
 * ACGsou (36DM)
 * Anidex
 * AniLibria
 * Anime Tosho
 * AniRena
 * AniSource
 * AudioBook Bay (ABB)
 * Badass Torrents
 * Bangumi Moe
 * BigFANGroup
 * BitRu
 * BitSearch
 * BT.etree
 * BT4G
 * BTDIGG
 * BTMET
 * BTSOW
 * BullTorrent
 * Byrutor
 * Cinecalidad
 * comicat
 * ConCen
 * cpasbien
 * cpasbienClone
 * CrackingPatching
 * Demonoid
 * DivxTotal
 * dmhy
 * DonTorrent
 * E-Hentai
 * EBook Bay (EBB)
 * EpubLibre
 * EXT Torrents
 * ExtraTorrent.st
 * EZTV
 * FileListing
 * FireBit
 * freshMeat
 * Frozen Layer
 * FTUApps
 * GamesTorrents
 * GkTorrent
 * GloDLS
 * GTorrent.pro
 * Idope
 * Il Corsaro Nero
 * Internet Archive (archive.org)
 * Isohunt2
 * iTorrent
 * JAV-Torrent
 * kickasstorrents.to
 * kickasstorrents.ws
 * Knaben
 * LePorno.info
 * LimeTorrents
 * LimeTorrents clone
 * LinuxTracker
 * Mac Torrents Download
 * MegaPeer
 * MejorTorrent
 * Mikan
 * MioBT
 * MixTapeTorrent
 * MoviesDVDR
 * MovieTorrent
 * MyPornClub
 * Newstudio
 * Nipponsei
 * NNTT
 * NoNaMe Club (NNM-Club)
 * Nyaa.si
 * OneJAV
 * ParnuXi
 * PC-torrent
 * PiratBit
 * PornosLab
 * PornoTorrent
 * ProPorn
 * Rapidzona
 * RARBG
 * RinTorNeT
 * Rus-media
 * RuTor
 * RuTracker.RU
 * Seedoff
 * Sexy-Pics
 * Shana Project
 * ShowRSS
 * Solid Torrents
 * sosulki
 * SubsPlease
 * sukebei.Nyaa.si
 * The Pirate Bay (TPB)
 * Tokyo Tosho
 * Torlock
 * Torrent[CORE]
 * Torrent Downloads (TD)
 * Torrent Oyun indir
 * torrent.by
 * torrent-pirat
 * Torrent9
 * Torrent9 clone
 * Torrent911
 * TorrentFunk
 * TorrentGalaxy (TGx)
 * TorrentKitty
 * TorrentProject2
 * TorrentQQ (토렌트큐큐)
 * Torrents.csv
 * TorrentSir (토렌트썰)
 * Torrentv
 * TorrentView (토렌트뷰)
 * TorrentWhiz ( 토렌트위즈)
 * Torrentz2eu
 * Torrentz2nz
 * TrahT
 * truPornolabs
 * UnionDHT
 * VSTHouse
 * VST Torrents
 * xxxAdultTorrent
 * XXXClub
 * xxxtor
 * xxxtorrents
 * YourBittorrent
 * YTS.ag
 * zetorrents
</details>

<details> <summary> <b> Supported Semi-Private Trackers </b> </summary>

 * AniDUB
 * Anime-Free
 * AnimeLayer
 * ArenaBG
 * BitNova
 * BookTracker
 * BootyTape
 * Catorrent
 * Darmowe torrenty
 * Deildu
 * Demonoid
 * Devil-Torrents
 * DimeADozen (EzTorrent)
 * DXP (Deaf Experts)
 * Electro-Torrent
 * EliteTorrent-pl
 * EniaHD
 * Erai-Raws
 * Ex-torrenty
 * ExKinoRay
 * ExtremlymTorrents
 * Fenyarnyek-Tracker
 * File-Tracker
 * Gay-Torrents.net
 * Genesis-Movement
 * HD-CzTorrent
 * HDGalaKtik
 * HellTorrents
 * HunTorrent
 * Il CorSaRo Blu
 * Kinorun
 * Kinozal
 * Lightning-Tor
 * LostFilm.tv
 * Magnetico (Local DHT) [[site](https://github.com/boramalper/magnetico)]
 * Marine Tracker
 * Mazepa
 * Metal Tracker
 * Moviesite
 * MVGroup Forum
 * MVGroup Main
 * NetHD (VietTorrent)
 * Newstudio (login)
 * NoNaMe Club (NNM-Club) (login)
 * PiratBit (login)
 * PornoLab
 * PussyTorrents
 * Rainbow Tracker
 * RGFootball
 * RinTor
 * RiperAM
 * RockBox
 * RUDUB (ex-BaibaKoTV)
 * Rustorka
 * RuTracker
 * SATClubbing
 * seleZen
 * Sharewood
 * SkTorrent
 * SkTorrent-org
 * themixingbowl (TMB)
 * Toloka.to
 * Torrents-Local
 * TorrentDownload
 * TribalMixes
 * Union Fansub
 * UniOtaku
 * vTorrent
 * xTorrenty
 * YggTorrent (YGG)
 * ZOMB
 * Ztracker
</details>

<details> <summary> <b> Supported Private Trackers </b> </summary>

 * 0day.kiev
 * 1ptbar
 * 2 Fast 4 You [![(invite needed)][inviteneeded]](#)
 * 2xFree
 * 3ChangTrai (3CT) [![(invite needed)][inviteneeded]](#)
 * 3D Torrents (3DT)
 * 4thD (4th Dimension)
 * 52PT
 * 720pier
 * Abnormal
 * ABtorrents (ABT + RNS)
 * Acervos [![(invite needed)][inviteneeded]](#)
 * Acid Lounge (A-L)
 * AcrossTheTasman [![(invite needed)][inviteneeded]](#)
 * Aftershock
 * Aidoru!Online
 * Aither
 * AlphaRatio (AR)
 * AmigosShareClub
 * AnimeBytes (AB)
 * AnimeTorrents (AnT)
 * AnimeTracker
 * AnimeWorld
 * Anthelion
 * Araba Fenice (Phoenix) [![(invite needed)][inviteneeded]](#)
 * ArabP2P
 * ArabTorrents [![(invite needed)][inviteneeded]](#)
 * aro.lol
 * AsianCinema
 * AsianDVDClub
 * Audiences
 * AudioNews (AN)
 * Aussierul.es [![(invite needed)][inviteneeded]](#)
 * AvistaZ (AsiaTorrents)
 * Back-ups [![(invite needed)][inviteneeded]](#)
 * BakaBT
 * BeiTai
 * Best-Core
 * BeyondHD (BHD)
 * Bibliotik
 * Bit-Bázis
 * BIT-HDTV
 * Bitded
 * Bithorlo (BHO)
 * BitHUmen [![(invite needed)][inviteneeded]](#)
 * Bit Of Valor
 * BitSexy
 * Bitspyder
 * BitTorrentFiles
 * BiTTuRK
 * BJ-Share (BJ)
 * BlueBird [![(invite needed)][inviteneeded]](#)
 * Blutopia (BLU)
 * Borgzelle [![(invite needed)][inviteneeded]](#)
 * Boxing Torrents
 * Brasil Tracker
 * BroadcasTheNet (BTN)
 * BroadCity
 * BrokenStones [![(invite needed)][inviteneeded]](#)
 * BrSociety [![(invite needed)][inviteneeded]](#)
 * BTNext (BTNT) [![(invite needed)][inviteneeded]](#)
 * BTSCHOOL
 * BWTorrents
 * BYRBT
 * Carp-Hunter
 * Carpathians
 * CarPT
 * CartoonChaos (CC)
 * Cathode-Ray.Tube (CRT)
 * CCFBits [![(invite needed)][inviteneeded]](#)
 * CeskeForum
 * CGPeers [![(invite needed)][inviteneeded]](#)
 * CHDBits [![(invite needed)][inviteneeded]](#)
 * ChileBT
 * Cinemageddon [![(invite needed)][inviteneeded]](#)
 * CinemaMovieS_ZT
 * Cinematik [![(invite needed)][inviteneeded]](#)
 * CinemaZ (EuTorrents)
 * Classix [![(invite needed)][inviteneeded]](#)
 * Coastal-Crew
 * Concertos
 * CrazyHD
 * CrazySpirits
 * CrnaBerza
 * DANISH BYTES
 * DataScene (DS)
 * DataTalli
 * DesiTorrents
 * Diablo Torrent [![(invite needed)][inviteneeded]](#)
 * DICMusic
 * DigitalCore
 * DiscFan
 * DivTeam
 * DocsPedia
 * Dream Tracker
 * Ebooks-Shares
 * Empornium (EMP) [![(invite needed)][inviteneeded]](#)
 * eShareNet
 * eStone (XiDER, BeLoad, BigTorrent)
 * ExoticaZ (YourExotic)
 * ExtremeBits
 * ExtremeTorrents
 * Falkon Vision Team
 * FANO.IN [![(invite needed)][inviteneeded]](#)
 * Fantastic Heaven
 * Fantastiko
 * Femdomcult
 * FileList (FL)
 * FinElite (FE)
 * FinVip
 * Flux-Zone
 * FunFile (FF)
 * FunkyTorrents (FT)
 * Fuzer (FZ)
 * GainBound [![(invite needed)][inviteneeded]](#)
 * Gay-Torrents.org
 * GAYtorrent.ru
 * GazelleGames (GGn) [![(invite needed)][inviteneeded]](#)
 * Generation-Free
 * GigaTorrents
 * GimmePeers (formerly ILT)
 * GiroTorrent
 * GreatPosterWall (GPW)
 * GreekDiamond
 * Greek Team [![(invite needed)][inviteneeded]](#)
 * HaiDan
 * Haitang
 * HappyFappy
 * Hawke-uno
 * HD Dolby
 * HD-Forever (HDF)
 * HD-Olimpo
 * HD-Only (HDO) [![(invite needed)][inviteneeded]](#)
 * HD-Space (HDS)
 * HD-Torrents (HDT)
 * HD-UNiT3D
 * HD4FANS [![(invite needed)][inviteneeded]](#)
 * HDArea (HDA)
 * HDAtmos
 * HDBits [![(invite needed)][inviteneeded]](#)
 * HDC (HDCiTY) [![(invite needed)][inviteneeded]](#)
 * HDChina (HDWing) [![(invite needed)][inviteneeded]](#)
 * HDFans
 * HDHome (HDBigger) [![(invite needed)][inviteneeded]](#)
 * HDMaYi
 * HDMonkey
 * HDRoute [![(invite needed)][inviteneeded]](#)
 * HDSky [![(invite needed)][inviteneeded]](#)
 * HDTime
 * HDTorrents.it [![(invite needed)][inviteneeded]](#)
 * HDTurk
 * HDU
 * HDZone
 * Hebits
 * HellasHut
 * HHanClub
 * House of Devil
 * HQMusic
 * iAnon
 * ICC2022
 * ImmortalSeed (iS)
 * Immortuos
 * Indietorrents [![(invite needed)][inviteneeded]](#)
 * Insane Tracker
 * IPTorrents (IPT)
 * JME-REUNIT3D
 * JoyHD [![(invite needed)][inviteneeded]](#)
 * JPopsuki
 * JPTV
 * KamePT [![(invite needed)][inviteneeded]](#)
 * Karagarga
 * Keep Friends [![(invite needed)][inviteneeded]](#)
 * Korsar
 * KrazyZone
 * Kufirc
 * LastFiles
 * Le Saloon
 * Lat-Team [![(invite needed)][inviteneeded]](#)
 * LearnBits
 * LearnFlakes
 * leech24
 * LemonHD
 * Les-Cinephiles
 * Libble
 * LibraNet (LN)
 * LinkoManija [![(invite needed)][inviteneeded]](#)
 * Locadora
 * LosslessClub [![(invite needed)][inviteneeded]](#)
 * LST
 * M-Team TP (MTTP) [![(invite needed)][inviteneeded]](#)
 * MaDs Revolution
 * magic-heaven
 * Magico (Trellas) [![(invite needed)][inviteneeded]](#)
 * Majomparádé (TurkDepo)
 * MegamixTracker
 * MeseVilág (Fairytale World)
 * Milkie
 * MIRcrew
 * MMA-Torrents [![(invite needed)][inviteneeded]](#)
 * MNV (Max-New-Vision)
 * MOJBLiNK
 * MoreThanTV (MTV)
 * MouseBits
 * Movie-Torrentz
 * MuziekFabriek
 * MyAnonamouse (MAM)
 * MySpleen [![(invite needed)][inviteneeded]](#)
 * NCore
 * Nebulance (NBL) (TransmiTheNet)
 * NicePT
 * NorBits
 * Old Toons World
 * OpenCD [![(invite needed)][inviteneeded]](#)
 * Orpheus
 * OshenPT
 * OurBits (HDPter)
 * P2PBG
 * Partis
 * PassThePopcorn (PTP)
 * Peeratiko
 * Peers.FM
 * PigNetwork
 * PirateTheNet (PTN)
 * PixelCove (Ultimate Gamer)
 * PiXELHD (PxHD) [![(invite needed)][inviteneeded]](#)
 * PolishSource (PS)
 * PolishTracker
 * Pornbay [![(invite needed)][inviteneeded]](#)
 * Portugas
 * PotUK
 * Pretome
 * PrivateHD (PHD)
 * ProAudioTorrents (PAT)
 * PTChina (铂金学院)
 * PTerClub
 * PTFiles (PTF)
 * PThome [![(invite needed)][inviteneeded]](#)
 * PTSBAO
 * PTtime
 * Punk's Horror Tracker
 * PuntoTorrent
 * PuTao
 * Puur-Hollands
 * PWTorrents (PWT)
 * R3V WTF! [![(invite needed)][inviteneeded]](#)
 * Racing4Everyone (R4E)
 * RacingForMe (RFM)
 * Red Star Torrent (RST) [![(invite needed)][inviteneeded]](#)
 * Redacted (PassTheHeadphones)
 * RedBits
 * ReelFlix
 * Resurrect The Net
 * RetroFlix
 * RevolutionTT
 * Romanian Metal Torrents (RMT) [![(invite needed)][inviteneeded]](#)
 * RPTorrents
 * SceneHD  [![(invite needed)][inviteneeded]](#)
 * SceneRush
 * SceneTime
 * SDBits [![(invite needed)][inviteneeded]](#)
 * Secret Cinema
 * SeedFile
 * Shareisland
 * SharkPT
 * Shazbat [![(invite needed)][inviteneeded]](#)
 * SiamBIT
 * SkipTheCommercials
 * SkipTheTrailers
 * slosoul
 * SnowPT (SSPT)
 * SoulVoice
 * SpeedApp (SceneFZ, XtreMeZone / MYXZ, ICE Torrent)
 * SpeedCD
 * Speedmaster HD [![(invite needed)][inviteneeded]](#)
 * SpeedTorrent Reloaded
 * SpiderTK
 * Spirit of Revolution [![(invite needed)][inviteneeded]](#)
 * SportHD
 * SportsCult
 * SpringSunday [![(invite needed)][inviteneeded]](#)
 * SugoiMusic
 * Superbits (SBS)
 * Swarmazon
 * Tapochek
 * Tasmanit
 * Team CT Game (TCTG)
 * TeamHD
 * TeamOS
 * TEKNO3D [![(invite needed)][inviteneeded]](#)
 * TellyTorrent
 * teracod (Movie Zone)
 * The Falling Angels (TFA)
 * The Geeks
 * The New Retro
 * The Occult [![(invite needed)][inviteneeded]](#)
 * The Old School
 * The Place [![(invite needed)][inviteneeded]](#)
 * The Shinning (TsH)
 * The Show
 * The Vault [![(invite needed)][inviteneeded]](#)
 * The-Crazy-Ones
 * The-New-Fun
 * TheDarkCommunity (TDC)
 * TheEmpire (TE)
 * TheLeachZone (TLZ)
 * TheScenePlace (TSP)
 * Thor's Land
 * TJUPT
 * TLFBits [![(invite needed)][inviteneeded]](#)
 * TmGHuB
 * Tornado
 * Torrent Network (TN)
 * Torrent Sector Crew (TSC)
 * Torrent-Explosiv
 * Torrent-Syndikat [![(invite needed)][inviteneeded]](#)
 * TOrrent-tuRK (TORK)
 * Torrent.LT
 * TorrentBD
 * TorrentBytes (TBy)
 * TorrentCCF (TCCF)
 * TorrentDay (TD)
 * TorrentHeaven [![(invite needed)][inviteneeded]](#)
 * TorrentHR [![(invite needed)][inviteneeded]](#)
 * Torrenting (TT)
 * Torrentland
 * TorrentLeech (TL)
 * TorrentLeech.pl
 * TorrentMasters
 * TorrentSeeds (TS)
 * TotallyKids (TK)
 * ToTheGlory (TTG) [![(invite needed)][inviteneeded]](#)
 * TranceTraffic [![(invite needed)][inviteneeded]](#)
 * Trezzor [![(invite needed)][inviteneeded]](#)
 * TurkSeed
 * TurkTorrent (TT)
 * TV Chaos UK (TVCUK)
 * TvRoad
 * TVstore
 * Twilight Torrents
 * U2 (U2分享園@動漫花園) [![(invite needed)][inviteneeded]](#)
 * UHDBits
 * UnionGang
 * UnleashTheCartoons
 * UnlimitZ
 * White Angel
 * World-In-HD [![(invite needed)][inviteneeded]](#)
 * World-of-Tomorrow
 * x-ite.me (XM)
 * xBytesV2
 * XSpeeds (XS)
 * Xthor
 * XtremeBytes
 * XWT-Classics
 * XWTorrents (XWT)
 * YDYPT
 * Zamunda.net
 * Zelka.org
 * ZmPT (织梦)
 * ZonaQ
</details>

Trackers marked with [![(invite needed)][inviteneeded]](#) have no active maintainer and may be missing features or be broken. If you have an invite for them please send it to garfieldsixtynine -at- gmail.com to get them fixed/improved.

### Aggregate indexers

A special "all" indexer is available at `/api/v2.0/indexers/all/results/torznab`.
It will query all configured indexers and return the combined results.

If your client supports multiple feeds it's recommended to add each indexer directly instead of using the all indexer.
Using the all indexer has no advantages (besides reduced management overhead), only disadvantages:
* you lose control over indexer specific settings (categories, search modes, etc.)
* mixing search modes (IMDB, query, etc.) might cause low-quality results
* indexer specific categories (>= 100000) can't be used.
* slow indexers will slow down the overall result
* total results are limited to 1000

To get all Jackett indexers including their capabilities you can use `t=indexers` on the all indexer. To get only configured/unconfigured indexers you can also add `configured=true/false` as a query parameter.

### Filter indexers

Another special "filter" indexer is available at `/api/v2.0/indexers/<filter>/results/torznab`
It will query the configured indexers that match the `<filter>` expression criteria and return the combined results as "all".

Supported filters
Filter | Condition
-|-
`type:<type>` | where the indexer type is equal to `<type>`
`tag:<tag>` | where the indexer tags contains `<tag>`
`lang:<tag>` | where the indexer language start with `<lang>`
`test:{passed\|failed}` | where the last indexer test performed `passed` or `failed`
`status:{healthy\|failing\|unknown}` | where the indexer state is `healthy` (successfully operates in the last minutes), `failing` (generates errors in the recent call) or `unknown` (unused for a while)

Supported operators
Operator | Condition
-|-
`!<expr>` | where not `<expr>`
`<expr1>+<expr2>[+<expr3>...]` | where `<expr1>` and `<expr2>` [and `<expr3>`...]
`<expr1>,<expr2>[,<expr3>...]` | where `<expr1>` or `<expr2>` [or `<expr3>`...]

Example 1:
The "filter" indexer at `/api/v2.0/indexers/tag:group1,!type:private+lang:en/results/torznab` will query all the configured indexers tagged with `group1` or all the indexers not private and with `en` language (`en-en`,`en-us`,...)

Example 2:
The "filter" indexer at `/api/v2.0/indexers/!status:failing,test:passed` will query all the configured indexers not `failing` or which `passed` its last test.

### Search modes and parameters

A list of supported API search modes and parameters:

```
t=search:
   params  : q
t=tvsearch:
   params  : q, season, ep, imdbid, tvdbid, rid, tmdbid, tvmazeid, traktid, doubanid, year, genre
t=movie:
   params  : q, imdbid, tmdbid, traktid, doubanid, year, genre
t=music:
   params  : q, album, artist, label, track, year, genre
t=book:
   params  : q, title, author, publisher, year, genre
```

Examples:

```
.../api?apikey=APIKEY&t=search&cat=1,3&q=Show+Title+S01E02

.../api?apikey=APIKEY&t=tvsearch&cat=1,3&q=Show+Title&season=1&ep=2
.../api?apikey=APIKEY&t=tvsearch&cat=1,3&genre=comedy&season=2023&ep=02/13

.../api?apikey=APIKEY&t=movie&cat=2&q=Movie+Title&year=2023
.../api?apikey=APIKEY&t=movie&cat=2&imdbid=tt1234567

.../api?apikey=APIKEY&t=music&cat=4&album=Title&artist=Name

.../api?apikey=APIKEY&t=book&cat=5,6&genre=horror&publisher=Stuff
```

## Installation on Windows
We recommend you install Jackett as a Windows service using the supplied installer. You may also download the zipped version if you would like to configure everything manually.

To get started with using the installer for Jackett, follow the steps below:

1. Check if you need any .NET prerequisites installed, see https://docs.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60#dependencies
2. Download the latest version of the Windows installer, "Jackett.Installer.Windows.exe" from the [releases](https://github.com/Jackett/Jackett/releases/latest) page.
3. When prompted if you would like this app to make changes to your computer, select "yes".
4. If you would like to install Jackett as a Windows Service, make sure the "Install as Windows Service" checkbox is filled.
5. Once the installation has finished, check the "Launch Jackett" box to get started.
6. Navigate your web browser to http://127.0.0.1:9117
7. You're now ready to begin adding your trackers and using Jackett.

When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line if you would like to see log messages (Ensure the server isn't already running from the tray/service). This can be done by using "JackettConsole.exe" (for Command Prompt), found in the Jackett data folder: "%ProgramData%\Jackett".


## Install on Linux (AMDx64)
On most operating systems all the required dependencies will already be present. In case they are not, you can refer to this page https://github.com/dotnet/core/blob/master/Documentation/linux-prereqs.md

### Install as service
A) Command to download and install latest package and run the Jackett service:

`cd /opt && f=Jackett.Binaries.LinuxAMDx64.tar.gz && release=$(wget -q https://github.com/Jackett/Jackett/releases/latest -O - | grep "title>Release" | cut -d " " -f 4) && sudo wget -Nc https://github.com/Jackett/Jackett/releases/download/$release/"$f" && sudo tar -xzf "$f" && sudo rm -f "$f" && cd Jackett* && sudo ./install_service_systemd.sh && systemctl status jackett.service && cd - && echo -e "\nVisit http://127.0.0.1:9117"`

B) Or manually:

1. Download and extract the latest `Jackett.Binaries.LinuxAMDx64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases)
2. To install Jackett as a service, open a Terminal, cd to the jackett folder and run `sudo ./install_service_systemd.sh` You need root permissions to install the service. The service will start on each logon. You can always stop it by running `systemctl stop jackett.service` from Terminal. You can start it again it using `systemctl start jackett.service`. Logs are stored as usual under `~/.config/Jackett/log.txt` and also in `journalctl -u jackett.service`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.LinuxAMDx64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases), open a Terminal, cd to the jackett folder and run Jackett with the command `./jackett`

### home directory
If you want to run it with a user without a /home directory you need to add `Environment=XDG_CONFIG_HOME=/path/to/folder` to your systemd file, this folder will be used to store your config files.


## Install on Linux (ARMv7 or above)
On most operating systems all the required dependencies will already be present. In case they are not, you can refer to this page https://github.com/dotnet/core/blob/master/Documentation/linux-prereqs.md

### Install as service
1. Download and extract the latest `Jackett.Binaries.LinuxARM32.tar.gz` or `Jackett.Binaries.LinuxARM64.tar.gz` (32 bit is the most common on ARM) release from the [releases page](https://github.com/Jackett/Jackett/releases)
2. To install Jackett as a service, open a Terminal, cd to the jackett folder and run `sudo ./install_service_systemd.sh` You need root permissions to install the service. The service will start on each logon. You can always stop it by running `systemctl stop jackett.service` from Terminal. You can start it again it using `systemctl start jackett.service`. Logs are stored as usual under `~/.config/Jackett/log.txt` and also in `journalctl -u jackett.service`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.LinuxARM32.tar.gz` or `Jackett.Binaries.LinuxARM64.tar.gz` (32 bit is the most common on ARM) release from the [releases page](https://github.com/Jackett/Jackett/releases), open a Terminal, cd to the jackett folder and run Jackett with the command `./jackett`

### home directory
If you want to run it with a user without a /home directory you need to add `Environment=XDG_CONFIG_HOME=/path/to/folder` to your systemd file, this folder will be used to store your config files.


## Installation on Linux (ARMv6 or below)
 1. Install [Mono 5.8](http://www.mono-project.com/download/#download-lin) or better (using the latest stable release is recommended)
       * Follow the instructions on the mono website and install the `mono-devel` and the `ca-certificates-mono` packages.
       * On Red Hat/CentOS/openSUSE/Fedora the `mono-locale-extras` package is also required.
 2. Install  libcurl:
       * Debian/Ubuntu: `apt-get install libcurl4-openssl-dev`
       * Redhat/Fedora: `yum install libcurl-devel`
       * For other distros see the  [Curl docs](http://curl.haxx.se/dlwiz/?type=devel).
 3. Download and extract the latest `Jackett.Binaries.Mono.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett using mono with the command `mono --debug JackettConsole.exe`.
 4. (Optional) To install Jackett as a service, open the Terminal and run `sudo ./install_service_systemd_mono.sh` You need root permissions to install the service. The service will start on each logon. You can always stop it by running `systemctl stop jackett.service` from Terminal. You can start it again it using `systemctl start jackett.service`. Logs are stored as usual under `~/.config/Jackett/log.txt` and also in `journalctl -u jackett.service`.

If you want to run it with a user without a /home directory you need to add `Environment=XDG_CONFIG_HOME=/path/to/folder` to your systemd file, this folder will be used to store your config files.

Mono must be compiled with the Roslyn compiler (default), using MCS will cause "An error has occurred." errors (See https://github.com/Jackett/Jackett/issues/2704).


### Installation on Linux via Ansible

On a CentOS/RedHat 7 system: [jewflix.jackett](https://galaxy.ansible.com/jewflix/jackett)

On an Ubuntu 16 system: [chrisjohnson00.jackett](https://galaxy.ansible.com/chrisjohnson00/jackett)


## Installation on macOS

### Prerequisites
macOS 10.15+ or greater

### Install as service
1. Download and extract the latest `Jackett.Binaries.macOS.tar.gz` or `Jackett.Binaries.macOSARM64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases).
2. Open the extracted folder and double-click on `install_service_macos`.
3. If the installation was a success, you can close the Terminal window.

The service will start on each logon. You can always stop it by running `launchctl unload ~/Library/LaunchAgents/org.user.Jackett.plist` from Terminal. You can start it again it using `launchctl load ~/Library/LaunchAgents/org.user.Jackett.plist`.
Logs are stored as usual under `~/.config/Jackett/log.txt`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.macOS.tar.gz` or `Jackett.Binaries.macOSARM64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett with the command `./jackett`.


## Installation using Docker
Detailed instructions are available at [LinuxServer.io Jackett Docker](https://hub.docker.com/r/linuxserver/jackett/). The Jackett Docker is highly recommended, especially if you are having Mono stability issues or having issues running Mono on your system e.g. QNAP, Synology. Thanks to [LinuxServer.io](https://linuxserver.io)


## Installation on Alpine Linux
Detailed instructions are available at [Jackett's Wiki](https://github.com/Jackett/Jackett/wiki/Installation-on-Alpine-Linux).


## Installation on Synology
Jackett is available as a [beta package](https://synocommunity.com/package/jackett) from [SynoCommunity](https://synocommunity.com/)


## Installation on OpenWrt
Detailed instructions are available at [Jackett's Wiki](https://github.com/Jackett/Jackett/wiki/Installation-on-OpenWrt).


## Running Jackett behind a reverse proxy
When running jackett behind a reverse proxy make sure that the original hostname of the request is passed to Jackett. If HTTPS is used also set the X-Forwarded-Proto header to "https". Don't forget to adjust the "Base path override" Jackett option accordingly.

Example config for apache:
```
<Location /jackett>
    ProxyPreserveHost On
    RequestHeader set X-Forwarded-Proto expr=%{REQUEST_SCHEME}
    ProxyPass http://127.0.0.1:9117
    ProxyPassReverse http://127.0.0.1:9117
</Location>
```

Example config for Nginx:
```
location /jackett {
	proxy_pass http://127.0.0.1:9117;
	proxy_set_header X-Real-IP $remote_addr;
	proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
	proxy_set_header X-Forwarded-Proto $scheme;
	proxy_set_header X-Forwarded-Host $http_host;
	proxy_redirect off;
}
```

## Search Cache
Jackett has an internal cache to increase search speed and to reduce the number of requests to the torrent sites.
The default values should be good for most users. If you have problems, you can reduce the TTL value in the
configuration or even disable the cache. Keep in mind that you can be banned by the sites if you make a lot of requests.
* **Cache TTL (seconds)**: (default 2100 / 35 minutes) It indicates how long the results can remain in the cache.
* **Cache max results per indexer**: (default 1000) How many results are kept in cache for each indexer. This limit is used to limit the use of RAM. If you make many requests and you have enough memory, increase this number.

## Torznab cache
If you have enabled the Jackett internal cache, but have an indexer for which you would prefer to fetch fresh results (thus ignoring the internal cache) then add the **&cache=false** parameter to your torznab query.

## Configuring FlareSolverr
Some indexers are protected by CloudFlare or similar services and Jackett is not able to solve the challenges.
For these cases, [FlareSolverr](https://github.com/FlareSolverr/FlareSolverr) has been integrated into Jackett. This service is in charge of solving the challenges and configuring Jackett with the necessary cookies.
Setting up this service is optional, most indexers don't need it.
* Install FlareSolverr service (following their instructions)
* Configure **FlareSolverr API URL** in Jackett. For example: http://172.17.0.2:8191
* It is recommended to keep the default value in **FlareSolverr Max Timeout (ms)**

## Configuring OMDb
This feature is used as a fallback (when using the aggregate Indexer) to get the movie/series title if only the IMDB ID is provided in the request.
To use it, please just request a free API key on [OMDb](http://www.omdbapi.com/apikey.aspx) (1,000 daily requests limit) and paste the key in Jackett

## Command line switches

  You can pass various options when running via the command line:

<details> <summary> Command Line Switches </summary>

-   `-i, --Install`            Install Jackett windows service (Must be admin)
-   `-s, --Start`              Start the Jacket Windows service (Must be admin)
-   `-k, --Stop`               Stop the Jacket Windows service (Must be admin)
-   `-u, --Uninstall`          Uninstall Jackett windows service (Must be admin).

-   `-r, --ReserveUrls`        (Re)Register windows port reservations (Required for
                            listening on all interfaces).

-   `-l, --Logging`            Log all requests/responses to Jackett

-   `-t, --Tracing`            Enable tracing

-   `-c, --UseClient`          Override web client selection.
                            [automatic(Default)/httpclient/httpclient2]

-   `-x, --ListenPublic`       Listen publicly

-   `-z, --ListenPrivate`      Only allow local access

-   `-p, --Port`               Web server port

-   `-n, --IgnoreSslErrors`    [true/false] Ignores invalid SSL certificates

-   `-d, --DataFolder`         Specify the location of the data folder (Must be an admin on Windows)
    - e.g. --DataFolder="D:\Your Data\Jackett\".
    - Don't use this on Unix (mono) systems. On Unix just adjust the HOME directory of the user to the data folder or set the XDG_CONFIG_HOME environment variable.

-   `--NoRestart`              Don't restart after update

-   `--PIDFile`                Specify the location of PID file

-   `--NoUpdates`              Disable automatic updates

-   `--help`                   Display this help screen.

-   `--version`                Display version information.
</details>

## Building from source

### Windows
* Install the .NET 6 [SDK](https://www.microsoft.com/net/download/windows)
* Clone Jackett
* Open PowerShell and from the `src` directory:
* - run `dotnet msbuild /restore`
* - then run `dotnet restore`
* - and run `dotnet build`
* Open the Jackett solution in Visual Studio 2019 (version 16.9 or above)
* Select **Jackett.Server** as the startup project
* In the drop-down menu of the run button select **Jackett.Server** instead of _IIS Express_
* Build/Start the project

### OSX


```bash
# manually install osx dotnet via:
https://dotnet.microsoft.com/download?initial-os=macos
# then:
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f net6.0 --self-contained -r osx-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/net6.0/osx-x64/jackett # run jackett
```

### Linux


```bash
sudo apt install nuget msbuild dotnet-sdk-6.0 # install build tools (Debian/ubuntu)
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f net6.0 --self-contained -r linux-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/net6.0/linux-x64/jackett # run jackett
```

## Screenshots

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot1.png)

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot2.png)

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot3.png)

[inviteneeded]: https://raw.githubusercontent.com/Jackett/Jackett/master/.github/label-inviteneeded.png
