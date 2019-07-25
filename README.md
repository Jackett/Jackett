# Jackett

[![GitHub issues](https://img.shields.io/github/issues/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/pulls)
[![Build status](https://ci.appveyor.com/api/projects/status/gaybh5mvyx418nsp/branch/master?svg=true)](https://ci.appveyor.com/project/Jackett/jackett)
[![Github Releases](https://img.shields.io/github/downloads/Jackett/Jackett/total.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/linuxserver/jackett.svg?maxAge=60&style=flat-square)](https://hub.docker.com/r/linuxserver/jackett/)
[![Discord](https://img.shields.io/badge/discord-chat-7289DA.svg?maxAge=60&style=flat-square)](https://discord.gg/J865QuA)

This project is a new fork and is recruiting development help.  If you are able to help out please contact us.

Jackett works as a proxy server: it translates queries from apps ([Sonarr](https://github.com/Sonarr/Sonarr), [Radarr](https://github.com/Radarr/Radarr), [SickRage](https://sickrage.github.io/), [CouchPotato](https://couchpota.to/), [Mylar](https://github.com/evilhero/mylar), [Lidarr](https://github.com/lidarr/lidarr), [DuckieTV](https://github.com/SchizoDuckie/DuckieTV), [qBittorrent](https://www.qbittorrent.org/), [Nefarious](https://github.com/lardbit/nefarious) etc) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps.

Developer note: The software implements the [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/dev/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) APIs.



#### Supported Systems
* Windows 7SP1 or greater using .NET 4.6.1 or above [Download here](https://www.microsoft.com/net/framework/versions/net461)
* Linux ([supported operating systems here](https://github.com/dotnet/core/blob/master/release-notes/2.1/2.1-supported-os.md))
* macOS 10.12 or greater

### Supported Public Trackers
 * 1337x
 * ACG.RIP
 * Anidex
 * Anime Tosho
 * AniRena
 * AudioBook Bay (ABB)
 * btbit
 * BTDB
 * BTDigg
 * BTKitty
 * ConCen
 * cpasbien
 * cpasbienClone
 * Demonoid
 * DIGBT
 * ETTV
 * EliteTorrent.biz
 * ExtraTorrent.ag
 * EZTV
 * Frozen Layer
 * GkTorrent
 * Hon3yHD.net
 * Horrible Subs
 * Il Corsaro Nero <!-- maintained by bonny1992 -->
 * Il Corsaro Blu
 * Isohunt2
 * iTorrent
 * KATcrs
 * KickAssTorrent (KATcr)
 * KickAssTorrent (thekat.se clone)
 * KikiBT
 * LePorno
 * LimeTorrents
 * MacTorrents
 * MagnetDL
 * MejorTorrent <!-- maintained by ivandelabeldad -->
 * MkvCage
 * Monova
 * MovCr
 * Newpct (aka: tvsinpagar, descargas2020, torrentlocura, torrentrapid, tumejortorrent, pctnew, etc)
 * Nyaa.si
 * Nyaa-Pantsu
 * OxTorrent
 * ProStyleX
 * QXR
 * RARBG
 * RuTor
 * shokweb
 * ShowRSS
 * SkyTorrentsClone
 * SolidTorrents
 * sukebei.Nyaa.si
 * sukebei-Pantsu
 * The Pirate Bay (TPB)
 * TNTVillage <!-- maintained by bonny1992 -->
 * Tokyo Tosho
 * Torlock
 * Torrent Downloads (TD)
 * TorrentFunk
 * TorrentGalaxy.org (TGx)
 * TorrentKitty
 * TorrentProject2
 * TorrentQuest
 * Torrents.csv
 * Torrent9
 * Torernt9 clone (torrents9.ch)
 * Torrentz2
 * World Wide Torrents
 * YourBittorrent
 * YTS.ag
 * Zooqle

### Supported Semi-Private Trackers
 * 7tor
 * Alein
 * AniDUB
 * ArenaBG
 * BaibaKo
 * Crazy's Corner
 * CzTorrent
 * Deildu
 * Film-Paleis
 * Gay-Torrents.net
 * Gay-Torrents.org
 * GDF76
 * HamsterStudio
 * Kinozal
 * LostFilm.tv
 * Metal Tracker
 * MVGroup Forum
 * MVGroup Main
 * Newstudio
 * NetHD (VietTorrent)
 * NoName Club (NNM-Club)
 * RockBox
 * RuTracker
 * Sharewood
 * SkTorrent
 * SoundPark
 * Toloka.to
 * Torrents-Local
 * Union Fansub
 * Vanila
 * YggTorrent (YGG)
 * Ztracker

### Supported Private Trackers
 * 2 Fast 4 You
 * 3D Torrents (3DT) 
 * 3evils
 * 720pier
 * Abnormal
 * Acid Lounge (A-L)
 * Aftershock
 * AlphaRatio (AR)
 * AmigosShareClub
 * AnimeBytes (AB)
 * AnimeTorrents (AnT)
 * Anthelion
 * AOX (Chippu)
 * Araba Fenice (Phoenix)
 * Asgaard (AG)
 * AsianCinema
 * AsianDVDClub
 * AST4u
 * Audiobook Torrents (ABT)
 * AudioNews (AN)
 * Awesome-HD (AHD)
 * AVG (Audio Video Games)
 * Avistaz (AsiaTorrents)
 * Back-ups
 * BakaBT
 * BaconBits (bB)
 * BeyondHD (BHD)
 * BIGTorrent
 * BigTower
 * Bit-City Reloaded
 * BIT-HDTV
 * BiT-TiTAN
 * Bithorlo (BHO)
 * BitHUmen
 * BitMe
 * BitMeTV
 * BitsPiracy
 * Bitspyder
 * BitTorrentFiles
 * BitTurk
 * BJ-Share (BJ)
 * BlueBird
 * Blutopia (BLU)
 * BroadcastTheNet (BTN)
 * BrokenStones
 * BTGigs (TG)
 * BTNext (BTNT)
 * Carpathians
 * CartoonChaos (CC)
 * CasaTorrent
 * CasStudioTV
 * CCFBits
 * CGPeers
 * CHDBits
 * ChannelX
 * Cinemageddon
 * Cinematik
 * CinemaZ (EuTorrents)
 * Classix
 * CrazySpirits
 * CrnaBerza
 * DanishBits (DB)
 * Dark-Shadow
 * Das Unerwartete
 * DataScene (DS)
 * DesiReleasers
 * DesiTorrents
 * Diablo Torrent
 * DigitalCore
 * DigitalHive
 * DivTeam
 * DocumentaryTorrents (DT)
 * Downloadville
 * Dragonworld Reloaded
 * Dream Team
 * DXDHD
 * EbookParadijs
 * Ebooks-Shares
 * EfectoDoppler
 * EliteHD (HDClub) [![(invite needed)][inviteneeded]](#)
 * Elit Tracker (ET)
 * Elite-Tracker
 * Empornium (EMP)
 * eShareNet
 * eStone (XiDER, BeLoad)
 * Ethor.net (Thor's Land)
 * EvolutionPalace
 * FANO.IN
 * FileList (FL)
 * Femdomcult
 * FocusX
 * FreeTorrent
 * FullMixMusic
 * FunFile (FF)
 * FunkyTorrents (FT)
 * Fuzer (FZ)
 * GAYtorrent.ru
 * GazelleGames (GGn)
 * Generation-Free
 * GFXNews
 * GFXPeers
 * GigaTorrents
 * GimmePeers (formerly ILT) <!-- maintained by jamesb2147 -->
 * GiroTorrent
 * Greek Team
 * HacheDe
 * Hardbay
 * HD4Free (HD4)
 * HD-Forever (HDF)
 * HD-Only (HDO)
 * HD-Space (HDS)
 * HD-Spain
 * HD-Torrents (HDT)
 * HD-Bits.com
 * HDArea (HDA)
 * HDBits
 * HDCenter
 * HDChina (HDWing)
 * HDCity
 * HDHome (HDBigger)
 * HDME
 * HDSky
 * HDTorrents.it
 * Hebits
 * Hon3y HD
 * HQSource (HQS)
 * HuSh 
 * Hyperay
 * ICE Torrent
 * iLoveClassics (iLC)
 * ImmortalSeed (iS)
 * inPeril
 * Insane Tracker
 * IPTorrents (IPT)
 * JPopsuki
 * Kapaki
 * Karagarga
 * LaPauseTorrents
 * Le Chaudron
 * Le Saloon
 * LearnFlakes
 * LibraNet (LN)
 * LinkoManija
 * LosslessClub
 * M-Team TP (MTTP)
 * Magico (Trellas)
 * Majomparádé (TurkDepo)
 * Mega-Bliz
 * Mononoké-BT
 * MoreThanTV (MTV)
 * Music-Master
 * MyAnonamouse (MAM)
 * myAmity
 * MySpleen
 * NCore
 * NBTorrents
 * Nebulance (NBL) (TransmiTheNet)
 * New Real World
 * Norbits
 * NordicBits (NB)
 * Nostalgic (The Archive)
 * notwhat.cd
 * Orpheus
 * Ourbits (HDPter)
 * P2PBG
 * Passione Torrent <!-- maintained by bonny1992 -->
 * PassThePopcorn (PTP)
 * Peers.FM
 * PiratBit
 * PirateTheNet (PTN)
 * PiXELHD (PxHD)
 * Pleasuredome
 * PolishSource (PS)
 * PolishTracker
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
 * Redacted (PassTheHeadphones)
 * Red Star Torrent (RST)
 * RetroFlix
 * RevolutionTT
 * RGU
 * RocketHD
 * RoDVD (Cinefiles)
 * Romanian Metal Torrent (RMT)
 * RPTorrents
 * SceneFZ
 * SceneHD
 * ScenePalace (SP)
 * SceneReactor
 * SceneRush
 * SceneTime
 * SDBits
 * Secret Cinema
 * SeedFile (SF)
 * Shareisland
 * ShareSpaceDB
 * Shazbat
 * Shellife (SL)
 * SiamBIT
 * SpaceTorrent
 * SpeedCD
 * SpeedTorrent Reloaded
 * SportHD
 * SportsCult
 * SuperBits (SBS)
 * TakeaByte
 * Tapochek
 * Tasmanit
 * TeamHD
 * TeamOS
 * TellyTorrent
 * TenYardTorrents (TYT)
 * TheAudioScene
 * TheEmpire (TE)
 * The Geeks
 * The Horror Charnel (THC)
 * The Movie Cave
 * The New Retro
 * The Occult
 * The Place
 * The Shinning (TsH)
 * The Show
 * The-Torrents
 * The Vault
 * Tigers-dl
 * Torrent Network (TN)
 * Torrent Sector Crew (TSC)
 * Torrent.LT
 * TorrentBD
 * TorrentBytes (TBy)
 * TorrentCCF (TCCF)
 * TorrentDay (TD)
 * Torrentech (TTH)
 * TorrentHeaven
 * TorrentHR
 * Torrenting (TT)
 * Torrentland
 * TorrentLeech (TL)
 * TorrentSeeds (TS)
 * Torrent-Syndikat
 * TOrrent-tuRK (TORK)
 * TorViet  (HDVNBits)
 * TotallyKids (TK)
 * ToTheGlory
 * TranceTraffic
 * Trezzor
 * TurkTorrent (TT)
 * TV Chaos UK (TVCUK)
 * TV-Vault
 * TVstore
 * Twilight Torrents
 * u-torrents (SceneFZ)
 * UHDBits
 * Ultimate Gamer Club (UGC)
 * UnionGang
 * Vizuk
 * Waffles
 * World-In-HD
 * World-of-Tomorrow
 * WorldOfP2P (WOP)
 * x-ite.me (XM)
 * xBytesV2
 * XSpeeds (XS)
 * XKTorrent
 * XWTorrents (XWT)
 * Xthor
 * XtremeFile
 * XtreMeZone (MYXZ)
 * ExoticaZ (YourExotic)
 * Zamunda.net
 * Zelka.org

Trackers marked with  [![(invite needed)][inviteneeded]](#) have no active maintainer and are missing features or are broken. If you have an invite for them please send it to kaso1717 -at- gmail.com to get them fixed/improved.

### Aggregate indexers

A special "all" indexer is available at `/api/v2.0/indexers/all/results/torznab`.
It will query all configured indexers and return the combined results.

If your client supports multiple feeds it's recommended to add each indexer directly instead of using the all indexer.
Using the all indexer has no advantages (besides reduced management overhead), only disadvantages:
* you lose control over indexer specific settings (categories, search modes, etc.)
* mixing search modes (IMDB, query, etc.) might cause low quality results
* indexer specific categories (>= 100000) can't be used.
* slow indexers will slow down the overall result
* total results are limited to 1000

To get all Jackett indexers including their capabilities you can use `t=indexers` on the all indexer. To get only configured/unconfigured indexers you can also add `configured=true/false` as query parameter.


## Installation on Windows
We recommend you install Jackett as a Windows service using the supplied installer. You may also download the zipped version if you would like to configure everything manually.

To get started with using the installer for Jackett, follow the steps below:

1. Download the latest version of the Windows installer, "Jackett.Installer.Windows.exe" from the [releases](https://github.com/Jackett/Jackett/releases/latest) page.
2. When prompted if you would like this app to make changes to your computer, select "yes".
3. If you would like to install Jackett as a Windows Service, make sure the "Install as Windows Service" checkbox is filled.
4. Once the installation has finished, check the "Launch Jackett" box to get started.
5. Navigate your web browser to: http://127.0.0.1:9117
6. You're now ready to begin adding your trackers and using Jackett.

When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line if you would like to see log messages (Ensure the server isn't already running from the tray/service). This can be done by using "JackettConsole.exe" (for Command Prompt), found in the Jackett data folder: "%ProgramData%\Jackett".


## Install on Linux (AMDx64)
On most operating systems all the required dependencies will already be present. In case they are not, you can refer to this page https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x#linux-distribution-dependencies

### Install as service
1. Download and extract the latest `Jackett.Binaries.LinuxAMDx64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases)
2. To install Jackett as a service, open the Terminal and run `sudo ./install_service_systemd.sh` You need root permissions to install the service. The service will start on each logon. You can always stop it by running `systemctl stop jackett.service` from Terminal. You can start it again it using `systemctl start jackett.service`. Logs are stored as usual under `~/.config/Jackett/log.txt` and also in `journalctl -u jackett.service`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.LinuxAMDx64.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett with the command `./jackett`

### home directory
If you want to run it with a user without a /home directory you need to add `Environment=XDG_CONFIG_HOME=/path/to/folder` to your systemd file, this folder will be used to store your config files.  


## Install on Linux (ARMv7 or above)
On most operating systems all the required dependencies will already be present. In case they are not, you can refer to this page https://docs.microsoft.com/en-us/dotnet/core/linux-prerequisites?tabs=netcore2x#linux-distribution-dependencies

### Install as service
1. Download and extract the latest `Jackett.Binaries.LinuxARM32.tar.gz` or `Jackett.Binaries.LinuxARM64.tar.gz` (32 bit is the most common on ARM) release from the [releases page](https://github.com/Jackett/Jackett/releases) 
2. To install Jackett as a service, open the Terminal and run `sudo ./install_service_systemd.sh` You need root permissions to install the service. The service will start on each logon. You can always stop it by running `systemctl stop jackett.service` from Terminal. You can start it again it using `systemctl start jackett.service`. Logs are stored as usual under `~/.config/Jackett/log.txt` and also in `journalctl -u jackett.service`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.LinuxARM32.tar.gz` or `Jackett.Binaries.LinuxARM64.tar.gz` (32 bit is the most common on ARM) release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett with the command `./jackett`

### home directory
If you want to run it with a user without a /home directory you need to add `Environment=XDG_CONFIG_HOME=/path/to/folder` to your systemd file, this folder will be used to store your config files.  


## Installation on Linux (ARMv6 or below)
 1. Install [Mono 5.8](http://www.mono-project.com/download/#download-lin) or better (using the latest stable release is recommended)
       * Follow the instructions on the mono website and install the `mono-devel` and the `ca-certificates-mono` packages.
       * On Red Hat/CentOS/openSUSE/Fedora the `mono-locale-extras` package is also required.
 2. Install  libcurl:
       * Debian/Ubunutu: `apt-get install libcurl4-openssl-dev`
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
macOS 10.12 or greater

### Install as service
1. Download and extract the latest `Jackett.Binaries.macOS.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases).
2. Open the extracted folder and double-click on `install_service_macos`.
3. If the installation was a success, you can close the Terminal window.

The service will start on each logon. You can always stop it by running `launchctl unload ~/Library/LaunchAgents/org.user.Jackett.plist` from Terminal. You can start it again it using `launchctl load ~/Library/LaunchAgents/org.user.Jackett.plist`.
Logs are stored as usual under `~/.config/Jackett/log.txt`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.macOS.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett with the command `./jackett`.

### upgrading from mono
If you were previously using the Mono flavour of Jackett then you should shutdown the service from a terminal with with the command `systemctl stop jackett.service` and then remove the startup script at `/etc/systemd/system/jackett.service` and delete the content of the `/Applications/Jackett` folder, prior to performing this install.


## Installation using Docker
Detailed instructions are available at [LinuxServer.io Jackett Docker](https://hub.docker.com/r/linuxserver/jackett/). The Jackett Docker is highly recommended, especially if you are having Mono stability issues or having issues running Mono on your system eg. QNAP, Synology. Thanks to [LinuxServer.io](https://linuxserver.io)


## Installation on Synology
Jackett is available as beta package from [SynoCommunity](https://synocommunity.com/)


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

Example config for nginx:
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

## Troubleshooting

* __Command line switches__

  You can pass various options when running via the command line, see --help for details.

* __Error "An error occurred while sending the request: Error: TrustFailure (A call to SSPI failed, see inner exception.)"__

  This is often caused by missing CA certificates.
  Try reimporting the certificates in this case:
   - On Linux (as user root): `wget -O - https://curl.haxx.se/ca/cacert.pem | cert-sync /dev/stdin`
   - On macOS: `curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin`

*  __Enable enhanced logging__

  You can get *enhanced* logging with the command line switches `-t -l` or by enabling `Enhanced logging` via the web interface (followed by clicking on the `Apply Server Settings` button).
  Please post logs if you are unable to resolve your issue with these switches ensuring to remove your username/password/cookies.
  The logfiles (log.txt/updater.txt) are stored in `%ProgramData%\Jackett` on Windows and `~/.config/Jackett/` on Linux/macOS.

## Configuring OMDb
This feature is used as a fallback (when using the aggregate Indexer) to get the movie/series title if only the IMDB ID is provided in the request.
To use it, please just request a free API key on [OMDb](http://www.omdbapi.com/apikey.aspx) (1,000 daily requests limit) and paste the key in Jackett

## Creating an issue
Please supply as much information about the problem you are experiencing as possible. Your issue has a much greater chance of being resolved if logs are supplied so that we can see what is going on. Creating an issue with '### isn't working' doesn't help anyone to fix the problem.

## Contributing

Jackett's framework typically allows our team and volunteering developers to implement new trackers in a couple of hours

Depending on logic complexity, there are two common ways new trackers are implemented:

1. simple [definitions](http://github.com/Jackett/Jackett/tree/master/src/Jackett.Common/Definitions) (.yml / YAML), and;
2. advanced (native) [indexers](http://github.com/Jackett/Jackett/tree/master/src/Jackett.Common/Indexers) (.cs / C#)

Read more about the [simple definition format](https://github.com/Jackett/Jackett/wiki/Definition-format).

If you are a developer then it's recommended to download the free community version of [Visual Studio](http://visualstudio.com)

If you are not a developer and would like a (new) tracker supported then feel free to leave an [issue](https://github.com/Jackett/Jackett/issues) request.

All contributions are welcome just send a pull request.

## Building from source

### Windows
* Install the .NET Core [SDK](https://www.microsoft.com/net/download/windows)
* Clone Jackett
* Open Powershell and from the `src` directory, run `dotnet restore`
* Open the Jackett solution in Visual Studio 2017 (version 15.9 or above)
* Right click on the Jackett solution and click 'Rebuild Solution' to restore nuget packages
* Select Jackett.Server as startup project
* In the drop down menu of the run button select "Jackett.Server" instead of "IIS Express"
* Build/Start the project

### OSX


```bash
# manually install osx dotnet via: 
https://dotnet.microsoft.com/download?initial-os=macos
# then: 
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f netcoreapp2.2 --self-contained -r osx-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/netcoreapp2.2/osx-x64/jackett # run jackett
```

### Linux


```bash
sudo apt install mono-complete nuget msbuild dotnet-sdk-2.2 # install build tools (debian/ubuntu)
git clone https://github.com/Jackett/Jackett.git
cd Jackett/src

# dotnet core version
dotnet publish Jackett.Server -f netcoreapp2.2 --self-contained -r linux-x64 -c Debug # takes care of everything
./Jackett.Server/bin/Debug/netcoreapp2.2/linux-x64/jackett # run jackett
```

## Screenshots

![screenshot](https://i.imgur.com/0d1nl7g.png "screenshot")

[inviteneeded]: https://raw.githubusercontent.com/Jackett/Jackett/master/.github/label-inviteneeded.png
