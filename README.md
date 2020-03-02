# Jackett

[![GitHub issues](https://img.shields.io/github/issues/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/pulls)
[![Build Status](https://dev.azure.com/Jackett-project/Jackett/_apis/build/status/Jackett.Jackett?branchName=master)](https://dev.azure.com/Jackett-project/Jackett/_build/latest?definitionId=1&branchName=master)
[![GitHub Releases](https://img.shields.io/github/downloads/Jackett/Jackett/total.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/linuxserver/jackett.svg?maxAge=60&style=flat-square)](https://hub.docker.com/r/linuxserver/jackett/)
[![Discord](https://img.shields.io/badge/discord-chat-7289DA.svg?maxAge=60&style=flat-square)](https://discord.gg/J865QuA)

This project is a new fork and is recruiting development help.  If you are able to help out please contact us.

Please see our [troubleshooting and contributing guidelines](CONTRIBUTING.md) before submitting any issues or pull requests

Jackett works as a proxy server: it translates queries from apps ([Sonarr](https://github.com/Sonarr/Sonarr), [Radarr](https://github.com/Radarr/Radarr), [SickRage](https://sickrage.github.io/), [CouchPotato](https://couchpota.to/), [Mylar](https://github.com/evilhero/mylar), [Lidarr](https://github.com/lidarr/lidarr), [DuckieTV](https://github.com/SchizoDuckie/DuckieTV), [qBittorrent](https://www.qbittorrent.org/), [Nefarious](https://github.com/lardbit/nefarious) etc.) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps.

Developer note: The software implements the [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/dev/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) APIs.



#### Supported Systems
* Windows 7SP1 or greater using .NET 4.6.1 or above [Download here](https://www.microsoft.com/net/framework/versions/net461)
* Linux [supported operating systems here](https://github.com/dotnet/core/blob/master/release-notes/2.1/2.1-supported-os.md#linux)
* macOS 10.13 or greater

<details> <summary> <b> Supported Public Trackers </b> </summary>

 * 1337x
 * 7torrents
 * AcademicTorrents
 * ACG.RIP
 * ACGsou
 * Anidex
 * Anime Tosho
 * AniRena
 * AudioBook Bay (ABB)
 * Badass Torrents
 * BigFANGroup
 * BitRu
 * BitTorrent.AM
 * BTDB
 * BTDIGG
 * BTeye
 * BT.etree
 * BTSOW
 * Cili180
 * ConCen
 * Corsaro.red
 * cpasbien
 * cpasbienClone
 * Demonoid
 * dmhy
 * ETTV
 * EliteTorrent.biz
 * EstrenosDTL
 * ExtraTorrent.cd
 * EXT Torrents
 * EZTV
 * Filebase
 * FireBit
 * Frozen Layer
 * GamesTorrents
 * GkTorrent
 * GloDLS
 * HDReactor
 * Horrible Subs
 * IBit
 * Idope
 * Il Corsaro Nero <!-- maintained by bonny1992 -->
 * Il Corsaro Blu
 * Internet Archive (archive.org)
 * Isohunt2
 * iTorrent
 * KickAssTorrent (KATcr)
 * KickAssTorrent (kat.li)
 * Legit Torrents
 * LePorno
 * LimeTorrents
 * MacTorrents
 * Magnet4You
 * MagnetDL
 * MejorTorrent <!-- maintained by ivandelabeldad -->
 * Monova
 * MovCr
 * MoviesDVDR
 * MyPornClub
 * Newpct (aka: tvsinpagar, descargas2020, torrentlocura, torrentrapid, tumejortorrent, pctnew, etc)
 * Newstudio
 * Nitro
 * NNTT
 * NoName Club (NNM-Club)
 * Nyaa.si
 * Nyaa-Pantsu
 * OneJAV
 * OxTorrent
 * PiratBit
 * Pirateiro
 * PornLeech
 * ProStyleX
 * RARBG
 * Rus-media
 * RuTor
 * Seedpeer
 * shokweb
 * ShowRSS
 * SkyTorrentsClone
 * SolidTorrents
 * sukebei.Nyaa.si
 * sukebei-Pantsu
 * TFile
 * The Pirate Bay (TPB)
 * Tokyo Tosho
 * TopNow
 * Torlock
 * TOROS
 * Torrent Downloads (TD)
 * Torrent4You
 * Torrent9
 * Torrent9Clone
 * TorrentDownload
 * TorrentFunk
 * TorrentGalaxy (TGx)
 * TorrentKitty
 * TorrentParadise
 * TorrentProject2
 * TorrentQuest
 * Torrents.csv
 * TorrentView
 * TorrentWal
 * Torrentz2
 * Underverse
 * YourBittorrent
 * YTS.ag
 * Zooqle
</details>

<details> <summary> <b> Supported Semi-Private Trackers </b> </summary>

 * Alein
 * AlexFilm
 * AniDUB
 * ArenaBG
 * BaibaKo
 * BookTracker
 * CasStudioTV
 * Crazy's Corner
 * CzTorrent
 * Deildu
 * DXP (Deaf Experts)
 * EniaHD
 * Erzsebet
 * ExKinoRay
 * ExtremlymTorrents
 * FilmsClub
 * Gay-Torrents.net
 * Gay-Torrents.org
 * HamsterStudio
 * HD Dolby
 * Kinozal
 * Korsar
 * LostFilm.tv
 * Marine Tracker
 * Metal Tracker
 * MuziekFrabriek
 * MVGroup Forum
 * MVGroup Main
 * NetHD (VietTorrent)
 * Pornolab
 * RiperAM
 * RockBox
 * Rustorka
 * RuTracker
 * Sharewood
 * SkTorrent
 * SoundPark
 * Toloka.to
 * Torrent-Explosiv
 * Torrents-Local
 * TribalMixes
 * Union Fansub
 * YggTorrent (YGG)
 * Ztracker
</details>

<details> <summary> <b> Supported Private Trackers </b> </summary>

 * 0day.kiev
 * 2 Fast 4 You
 * 3D Torrents (3DT)
 * 3evils
 * 4thD (4th Dimension)
 * 52PT
 * 720pier
 * Abnormal
 * Acid Lounge (A-L)
 * Aftershock
 * AlphaRatio (AR)
 * AmigosShareClub
 * AnimeBytes (AB)
 * AnimeTorrents (AnT)
 * Anthelion
 * Araba Fenice (Phoenix)
 * Asgaard (AG)
 * AsianCinema
 * AST4u
 * Audiobook Torrents (ABT)
 * AudioNews (AN)
 * Awesome-HD (AHD)
 * Avistaz (AsiaTorrents)
 * Back-ups
 * BakaBT
 * BaconBits (bB)
 * BeiTai
 * BeyondHD (BHD)
 * Bibliotik
 * BIGTorrent
 * BigTower
 * Bit-City Reloaded
 * BIT-HDTV
 * BiT-TiTAN
 * Bithorlo (BHO)
 * BitHUmen
 * Bitspyder
 * BitTorrentFiles
 * BitTurk
 * BJ-Share (BJ)
 * BlueBird
 * Blutopia (BLU)
 * Boxing Torrents
 * Brasil Tracker
 * BroadcastTheNet (BTN)
 * BroadCity
 * BRObits
 * BrokenStones
 * BTGigs (TG)
 * BTNext (BTNT)
 * BTSCHOOL
 * Carpathians
 * CartoonChaos (CC)
 * CasaTorrent
 * CCFBits
 * CGPeers
 * CHDBits
 * ChannelX
 * Cinemageddon
 * CinemaMovies
 * Cinematik
 * CinemaZ (EuTorrents)
 * Classix
 * Concertos
 * CrazyHD
 * CrazySpirits
 * CrnaBerza
 * DanishBits (DB)
 * Das Unerwartete
 * DataScene (DS)
 * DesiReleasers
 * DesiTorrents
 * Diablo Torrent
 * DigitalCore
 * DigitalHive
 * DivTeam
 * DivxTotal
 * DocumentaryTorrents (DT)
 * Downloadville
 * Dragonworld Reloaded
 * DXDHD
 * EbookParadijs
 * Ebooks-Shares
 * EfectoDoppler
 * EggMeOn
 * Elite-Tracker
 * Empornium (EMP)
 * eShareNet
 * eStone (XiDER, BeLoad)
 * Ethor.net (Thor's Land)
 * ExtremeTorrents
 * FANO.IN
 * FeedUrNeed (FuN)
 * Femdomcult
 * FileList (FL)
 * Film-Paleis
 * FinVip
 * FocusX
 * FreeTorrent
 * FullMixMusic
 * FunFile (FF)
 * FunkyTorrents (FT)
 * Fuzer (FZ)
 * Galeriens (LaPauseTorrents)
 * GAYtorrent.ru
 * GazelleGames (GGn)
 * Generation-Free
 * GFXPeers
 * GigaTorrents
 * GimmePeers (formerly ILT) <!-- maintained by jamesb2147 -->
 * GiroTorrent
 * Greek Legends
 * Greek Team
 * HacheDe
 * HD-Forever (HDF)
 * HD-Olimpo
 * HD-Only (HDO)
 * HD-Space (HDS)
 * HD-Spain
 * HD-Torrents (HDT)
 * HD-Bits.com
 * HD4FANS
 * HDArea (HDA)
 * HDBits
 * HDCenter
 * HDChina (HDWing)
 * HDCity
 * HDDisk (HDD)
 * HDHome (HDBigger)
 * HDME
 * HDRoute
 * HDSky
 * HDTime
 * HDTorrents.it
 * HDTurk
 * HDU
 * HDZone
 * Hebits
 * Hon3y HD
 * HQSource (HQS)
 * HuSh
 * ICE Torrent
 * ImmortalSeed (iS)
 * Immortuos
 * inPeril
 * Insane Tracker
 * IPTorrents (IPT)
 * JPopsuki
 * Kapaki
 * Karagarga
 * LegacyHD (HD4Free)
 * Le Saloon
 * LeagueHD
 * LearnFlakes
 * LibraNet (LN)
 * LinkoManija
 * LosslessClub
 * M-Team TP (MTTP)
 * Magico (Trellas)
 * Majomparádé (TurkDepo)
 * MicroBit (µBit)
 * MoeCat
 * Mononoké-BT
 * MoreThanTV (MTV)
 * MyAnonamouse (MAM)
 * myAmity
 * MySpleen
 * NBTorrents
 * NCore
 * Nebulance (NBL) (TransmiTheNet)
 * NetCosmo
 * NetLab
 * New Real World
 * Norbits
 * NordicBits (NB)
 * Nostalgic (The Archive)
 * notwhat.cd
 * OnlineSelfEducation
 * Orpheus
 * Ourbits (HDPter)
 * P2PBG
 * P2PElite
 * Partis
 * PassThePopcorn (PTP)
 * Peers.FM
 * PirateTheNet (PTN)
 * PixelCove (Ultimate Gamer)
 * PiXELHD (PxHD)
 * Pleasuredome
 * PolishSource (PS)
 * PolishTracker
 * Pornbay
 * PornBits (PB)
 * Pretome
 * PrivateHD (PHD)
 * ProAudioTorrents (PAT)
 * Psytorrents
 * PT99
 * PTFiles (PTF)
 * PThome
 * PuntoTorrent
 * PWTorrents (PWT)
 * R3V WTF!
 * Racing4Everyone (R4E)
 * RacingForMe (RFM)
 * RainbowNation Sharing (RNS)
 * Redacted (PassTheHeadphones)
 * Red Star Torrent (RST)
 * RetroFlix
 * RevolutionTT
 * RoDVD (Cinefiles)
 * Romanian Metal Torrent (RMT)
 * RPTorrents
 * SceneFZ
 * SceneHD
 * ScenePalace (SP)
 * SceneRush
 * SceneTime
 * SDBits
 * Secret Cinema
 * SeedFile (SF)
 * Shareisland
 * ShareSpaceDB
 * ShareUniversity
 * Shazbat
 * Shellife (SL)
 * SiamBIT
 * SnowPT (SSPT)
 * SpaceTorrent
 * SpeedCD
 * SpeedTorrent Reloaded
 * SportHD
 * SportsCult
 * SpringSunday
 * SuperBits (SBS)
 * TakeaByte
 * Tapochek
 * Tasmanit
 * TeamHD
 * TeamOS
 * TEKNO3D
 * TellyTorrent
 * TenYardTorrents (TYT)
 * TheAudioScene
 * TheEmpire (TE)
 * The Falling Angels (TFA)
 * The Geeks
 * The Horror Charnel (THC)
 * The New Retro
 * The Occult
 * The Place
 * The Shinning (TsH)
 * The Show
 * The-Madhouse
 * The Vault
 * TLFBits
 * Torrent Network (TN)
 * Torrent Sector Crew (TSC)
 * Torrent.LT
 * TorrentBD
 * TorrentBytes (TBy)
 * TorrentCCF (TCCF)
 * TorrentDay (TD)
 * Torrentech (TTH)
 * TorrentFactory
 * TorrentHeaven
 * TorrentHR
 * Torrenting (TT)
 * Torrentland
 * TorrentLeech (TL)
 * TorrentLeech.pl
 * TorrentSeeds (TS)
 * Torrent-Syndikat
 * TOrrent-tuRK (TORK)
 * TotallyKids (TK)
 * ToTheGlory
 * TranceTraffic
 * Trezzor
 * TTsWEB
 * TurkTorrent (TT)
 * TV Chaos UK (TVCUK)
 * TV-Vault
 * TVstore
 * Twilight Torrents
 * Twilights Zoom
 * u-torrents (SceneFZ)
 * U2 (U2分享園@動漫花園)
 * UHDBits
 * UnionGang
 * UnlimitZ
 * Vizuk
 * WDT (Wrestling Desires Torrents / Ultimate Wrestling Torrents)
 * World-In-HD
 * World-of-Tomorrow
 * x-ite.me (XM)
 * xBytesV2
 * XSpeeds (XS)
 * XWTorrents (XWT)
 * XWT-Classics
 * Xthor
 * XtremeFile
 * XtreMeZone (MYXZ)
 * ExoticaZ (YourExotic)
 * Zamunda.net
 * Zelka.org
</details>

Trackers marked with  [![(invite needed)][inviteneeded]](#) have no active maintainer and are missing features or are broken. If you have an invite for them please send it to garfieldsixtynine -at- gmail.com to get them fixed/improved.

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


## Installation on Windows
We recommend you install Jackett as a Windows service using the supplied installer. You may also download the zipped version if you would like to configure everything manually.

To get started with using the installer for Jackett, follow the steps below:

1. Download the latest version of the Windows installer, "Jackett.Installer.Windows.exe" from the [releases](https://github.com/Jackett/Jackett/releases/latest) page.
2. When prompted if you would like this app to make changes to your computer, select "yes".
3. If you would like to install Jackett as a Windows Service, make sure the "Install as Windows Service" checkbox is filled.
4. Once the installation has finished, check the "Launch Jackett" box to get started.
5. Navigate your web browser to http://127.0.0.1:9117
6. You're now ready to begin adding your trackers and using Jackett.

When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line if you would like to see log messages (Ensure the server isn't already running from the tray/service). This can be done by using "JackettConsole.exe" (for Command Prompt), found in the Jackett data folder: "%ProgramData%\Jackett".


## Install on Linux (AMDx64)
On most operating systems all the required dependencies will already be present. In case they are not, you can refer to this page https://github.com/dotnet/core/blob/master/Documentation/linux-prereqs.md

### Install as service
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
macOS 10.13 or greater

### Install as service
1. Download and extract the latest `Jackett.Binaries.macOS.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases).
2. Open the extracted folder and double-click on `install_service_macos`.
3. If the installation was a success, you can close the Terminal window.

The service will start on each logon. You can always stop it by running `launchctl unload ~/Library/LaunchAgents/org.user.Jackett.plist` from Terminal. You can start it again it using `launchctl load ~/Library/LaunchAgents/org.user.Jackett.plist`.
Logs are stored as usual under `~/.config/Jackett/log.txt`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.macOS.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett with the command `./jackett`.


## Installation using Docker
Detailed instructions are available at [LinuxServer.io Jackett Docker](https://hub.docker.com/r/linuxserver/jackett/). The Jackett Docker is highly recommended, especially if you are having Mono stability issues or having issues running Mono on your system e.g. QNAP, Synology. Thanks to [LinuxServer.io](https://linuxserver.io)


## Installation on Synology
Jackett is available as a beta package from [SynoCommunity](https://synocommunity.com/)


## Running Jackett behind a reverse proxy
When running jackett behind a reverse proxy make sure that the original hostname of the request is passed to Jackett. If HTTPS is used also set the X-Forwarded-Proto header to "https". Don't forget to adjust the "Base Path Override" Jackett option accordingly.

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

-   `-j, --ProxyConnection`    use proxy - e.g. 127.0.0.1:8888


-   `-x, --ListenPublic`       Listen publicly

-   `-z, --ListenPrivate`      Only allow local access

-   `-p, --Port`               Web server port

-   `-m, --MigrateSettings`    Migrate settings manually (Must be an admin on Windows)

-   `-n, --IgnoreSslErrors`    [true/false] Ignores invalid SSL certificates

-   `-d, --DataFolder`         Specify the location of the data folder (Must be an admin on Windows)
    - e.g. --DataFolder="D:\Your Data\Jackett\".
    - Don't use this on Unix (mono) systems. On Unix just adjust the HOME directory of the user to the datadir or set the XDG_CONFIG_HOME environment variable.

-   `--NoRestart`              Don't restart after update

-   `--PIDFile`                Specify the location of PID file

-   `--NoUpdates`              Disable automatic updates

-   `--help`                   Display this help screen.

-   `--version`                Display version information.
</details>

## Building from source

### Windows
* Install the .NET Core [SDK](https://www.microsoft.com/net/download/windows)
* Clone Jackett
* Open PowerShell and from the `src` directory, run `dotnet restore`
* Open the Jackett solution in Visual Studio 2019 (version 16.4 or above)
* Right-click on the Jackett solution and click 'Rebuild Solution' to restore NuGet packages
* Select Jackett.Server as the startup project
* In the drop-down menu of the run button select "Jackett.Server" instead of "IIS Express"
* Build/Start the project

### OSX


```bash
# manually install osx dotnet via:
https://dotnet.microsoft.com/download?initial-os=macos
# then:
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f netcoreapp3.1 --self-contained -r osx-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/netcoreapp3.1/osx-x64/jackett # run jackett
```

### Linux


```bash
sudo apt install mono-complete nuget msbuild dotnet-sdk-3.1 # install build tools (Debian/ubuntu)
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f netcoreapp3.1 --self-contained -r linux-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/netcoreapp3.1/linux-x64/jackett # run jackett
```

## Screenshots

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot1.png)

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot2.png)

![screenshot](https://raw.githubusercontent.com/Jackett/Jackett/master/.github/jackett-screenshot3.png)

[inviteneeded]: https://raw.githubusercontent.com/Jackett/Jackett/master/.github/label-inviteneeded.png
