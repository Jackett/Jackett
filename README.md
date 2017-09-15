# Jackett

[![GitHub issues](https://img.shields.io/github/issues/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/issues)
[![GitHub pull requests](https://img.shields.io/github/issues-pr/Jackett/Jackett.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/pulls)
[![Bountysource](https://img.shields.io/bountysource/team/jackett/activity.svg?style=flat-square)](https://www.bountysource.com/teams/jackett)
[![Build status](https://ci.appveyor.com/api/projects/status/gaybh5mvyx418nsp/branch/master?svg=true)](https://ci.appveyor.com/project/camjac251/jackett)
[![Github Releases](https://img.shields.io/github/downloads/Jackett/Jackett/total.svg?maxAge=60&style=flat-square)](https://github.com/Jackett/Jackett/releases/latest)
[![Docker Pulls](https://img.shields.io/docker/pulls/linuxserver/jackett.svg?maxAge=60&style=flat-square)](https://hub.docker.com/r/linuxserver/jackett/)
[![Discord](https://img.shields.io/badge/discord-chat-7289DA.svg?maxAge=60&style=flat-square)](https://discord.gg/J865QuA)

This project is a new fork and is recruiting development help.  If you are able to help out please contact us.

Jackett works as a proxy server: it translates queries from apps ([Sonarr](https://github.com/Sonarr/Sonarr), [Radarr](https://github.com/Radarr/Radarr), [SickRage](https://sickrage.github.io/), [CouchPotato](https://couchpota.to/), [Mylar](https://github.com/evilhero/mylar), etc) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps.

Developer note: The software implements the [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/dev/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) APIs.



#### Supported Systems
* Windows using .NET 4.5
* Linux and macOS using Mono 4 (mono 3 is no longer supported).

### Supported Public Trackers
 * Anidex
 * Anime Tosho
 * AniRena
 * cpasbien
 * EZTV
 * GkTorrent
 * Horrible Subs
 * Il Corsaro Nero <!-- maintained by bonny1992 -->
 * Isohunt
 * KickAssTorrent
 * KickAssTorrent (thekat.se clone)
 * LimeTorrents
 * NextTorrent
 * Nyaa.si
 * Nyaa-Pantsu
 * Nyoo
 * RARBG
 * ShowRSS
 * Sky torrents
 * T411 v2
 * The Pirate Bay
 * TNTVillage <!-- maintained by bonny1992 -->
 * Tokyo Toshokan
 * Torlock
 * Torrent Downloads
 * Torrent9
 * TorrentProject
 * Torrentz2
 * YTS.ag
 * Zooqle
 
### Supported Semi-Private Trackers
 * 7tor
 * CzTorrent
 * Deildu
 * Gay-Torrents.net
 * NetHD
 * RuTracker
 * SkTorrent
 * TorrentBytes
 * Xtreme Zone
 * YggTorrent
 * Ztracker

### Supported Private Trackers
 * 2 Fast 4 You
 * 3D Torrents
 * Abnormal
 * Acid-Lounge
 * AlphaRatio
 * Andraste
 * AnimeBytes
 * AnimeTorrents
 * AOX
 * Apollo (XANAX)
 * ArabaFenice
 * Arche Torrent
 * AsianDVDClub
 * AST4u
 * Audiobook Torrents
 * Awesome-HD
 * Avistaz
 * B2S-Share
 * Back-ups
 * BakaBT  [![(invite needed)][inviteneeded]](#)
 * bB
 * BeyondHD
 * BIGTorrent
 * Bit-City Reloaded
 * BIT-HDTV
 * BitHQ
 * BitHUmen
 * BitMeTV
 * BitSoup  [![(invite needed)][inviteneeded]](#)
 * Bitspyder
 * BJ-Share
 * Blu-bits
 * BlueBird
 * Blutopia
 * BroadcastTheNet
 * BrokenStones
 * BTNext
 * Carpathians
 * CGPeers
 * CHDBits
 * Cinematik
 * Cinemageddon
 * CinemaZ
 * Classix
 * CZTeam
 * DanishBits
 * DataScene
 * Demonoid
 * Diablo Torrent
 * DigitalHive
 * Dragonworld Reloaded
 * Dream Team
 * Elite-Tracker
 * EoT-Forum
 * eStone
 * Ethor.net (Thor's Land)
 * FANO.IN
 * FileList
 * Freedom-HD
 * FullMixMusic
 * FunFile
 * FunkyTorrents
 * Fuzer
 * GayTorrent.ru
 * GFTracker
 * GFXPeers
 * Ghost City
 * GigaTorrents  [![(invite needed)][inviteneeded]](#)
 * GimmePeers <!-- maintained by jamesb2147 -->
 * GODS  [![(invite needed)][inviteneeded]](#)
 * Gormogon
 * Greek Team
 * Hardbay
 * HD-Forever
 * HD-Only
 * HD-Space
 * HD-Torrents
 * HD-Bits.com
 * HD4Free
 * HDBits
 * HDChina
 * HDHome
 * HDME
 * HDSky
 * HDTorrents.it
 * Hebits
 * Hon3y HD
 * Hounddawgs
 * House-of-Torrents
 * Hyperay
 * ICE Torrent
 * I Love Classics
 * Immortalseed
 * Infinity-T
 * inPeril
 * Insane Tracker
 * IPTorrents
 * JPopsuki
 * Kapaki
 * Karagarga
 * LinkoManija
 * LosslessClub
 * M-Team - TP
 * Magico
 * Majomparádé
 * Manicomio Share
 * Mononoké-BT
 * MoreThanTV
 * MyAnonamouse
 * myAmity
 * MySpleen
 * NCore
 * Nebulance
 * New Real World
 * Norbits  [![(invite needed)][inviteneeded]](#) <!-- added by DiseaseNO but no longer maintained? -->
 * notwhat.cd
 * Ourbits
 * Passione Torrent <!-- maintained by bonny1992 -->
 * PassThePopcorn  [![(invite needed)][inviteneeded]](#)
 * PirateTheNet
 * PiXELHD
 * PolishSource
 * PolishTracker
 * Pretome
 * PrivateHD
 * Psytorrents
 * PTFiles
 * Redacted (PassTheHeadphones)
 * RevolutionTT
 * Rockhard Lossless
 * RoDVD
 * SceneFZ
 * SceneTime
 * SDBits
 * Secret Cinema
 * Shareisland
 * ShareSpaceDB
 * Shazbat
 * Shellife
 * SpeedCD
 * SpeedTorrent Reloaded
 * SportsCult
 * SportHD
 * Superbits
 * Synthesiz3r
 * Tasmanit
 * The Empire
 * The Geeks
 * The Horror Charnel
 * The Occult
 * The New Retro
 * The Place
 * The Shinning
 * The Show
 * The Vault
 * The-Torrents
 * TehConnection
 * TenYardTracker
 * Torrent Network
 * Torrent Sector Crew
 * TorrentBD
 * TorrentCCF  [![(invite needed)][inviteneeded]](#)
 * TorrentDay
 * Torrentech
 * TorrentHeaven
 * TorrentHR
 * Torrenting
 * TorrentLeech
 * Torrents.Md
 * TorrentVault
 * Torrent-Syndikat
 * TorViet
 * ToTheGlory
 * TranceTraffic
 * Trezzor
 * TV Chaos UK
 * TV-Vault
 * u-Torrent
 * UHDBits
 * Ultimate Gamer Club
 * ULTRAHDCLUB
 * Waffles
 * World-In-HD
 * WorldOfP2P
 * x264
 * XSpeeds
 * Xthor
 * Zamunda.net
 * Zelka.org

Trackers marked with  [![(invite needed)][inviteneeded]](#) have no active maintainer and are missing features or are broken. If you have an invite for them please send it to kaso1717 -at- gmail.com to get them fixed/improved.

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

## Installation on Linux
 1. Install [Mono 4](http://www.mono-project.com/download/#download-lin) or better (version 4.8 is recommended)
       * Follow the instructions on the mono website and install the `mono-devel` and the `ca-certificates-mono` packages.
       * On Red Hat/CentOS/openSUSE/Fedora the `mono-locale-extras` package is also required.
 2. Install  libcurl:
       * Debian/Ubunutu: `apt-get install libcurl4-openssl-dev`
       * Redhat/Fedora: `yum install libcurl-devel`
       * For other distros see the  [Curl docs](http://curl.haxx.se/dlwiz/?type=devel).
 3. Download and extract the latest `Jackett.Binaries.Mono.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett using mono with the command `mono --debug JackettConsole.exe`.

Detailed instructions for [Ubuntu 14.x](http://www.htpcguides.com/install-jackett-on-ubuntu-14-x-for-custom-torrents-in-sonarr/) and [Ubuntu 15.x](http://www.htpcguides.com/install-jackett-ubuntu-15-x-for-custom-torrents-in-sonarr/)

## Installation on macOS

### Prerequisites
Install [Mono 4](http://www.mono-project.com/download/#download-mac) or better (version 4.8 is recommended).
 * Setup ssl support by running `curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin`

### Install as service
1. Download and extract the latest `Jackett.Binaries.Mono.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases).
2. Open the extracted folder and double-click on `install_service_macos`.
3. If the installation was a success, you can close the Terminal window.
       
The service will start on each logon. You can always stop it by running `launchctl unload ~/Library/LaunchAgents/org.user.Jackett.plist` from Terminal. You can start it again it using `launchctl load ~/Library/LaunchAgents/org.user.Jackett.plist`.
Logs are stored as usual under `~/.config/Jackett/log.txt`.

### Run without installing as a service
Download and extract the latest `Jackett.Binaries.Mono.tar.gz` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett using mono with the command `mono --debug JackettConsole.exe`.

## Installation using Docker
Detailed instructions are available at [LinuxServer.io Jackett Docker](https://hub.docker.com/r/linuxserver/jackett/). The Jackett Docker is highly recommended, especially if you are having Mono stability issues or having issues running Mono on your system eg. QNAP, Synology. Thanks to [LinuxServer.io](https://linuxserver.io)

## Installation on Synology
Jackett is available as beta package from [SynoCommunity](https://synocommunity.com/)

## Troubleshooting

* __Command line switches__

  You can pass various options when running via the command line, see --help for details.

* __Error "The underlying connection was closed: Could not establish trust relationship for the SSL/TLS secure channel."__

  This is often caused by missing CA certificates.
  Try reimporting the certificates in this case:
   - On Linux (as user root): `wget -O - https://curl.haxx.se/ca/cacert.pem | cert-sync /dev/stdin`
   - On macOS: `curl -sS https://curl.haxx.se/ca/cacert.pem | cert-sync --user /dev/stdin`

*  __Enable logging__

  You can get additional logging with the command line switches `-t -l` or by enabling `Enhanced logging` via the web interface.
  Please post logs if you are unable to resolve your issue with these switches ensuring to remove your username/password/cookies.
  The logfiles (log.txt/updater.txt) are stored in `%ProgramData%\Jackett` on Windows and `~/.config/Jackett/` on Linux/macOS.

## Creating an issue
Please supply as much information about the problem you are experiencing as possible. Your issue has a much greater chance of being resolved if logs are supplied so that we can see what is going on. Creating an issue with '### isn't working' doesn't help anyone to fix the problem.

## Contributing
All contributions are welcome just send a pull request.  Jackett's framework allows our team (and any other volunteering dev) to implement new trackers in an hour or two. If you'd like support for a new tracker but are not a developer then feel free to leave a request on the [issues page](https://github.com/Jackett/Jackett/issues).  It is recommended to use Visual studio 2015 when making code changes in this project.


## Screenshots

![screenshot](https://i.imgur.com/0d1nl7g.png "screenshot")

[inviteneeded]: https://raw.githubusercontent.com/Jackett/Jackett/master/.github/label-inviteneeded.png
