## Jackett

This project is a new fork and is recruiting development help.  If you are able to help out please contact us.

Jackett works as a proxy server: it translates queries from apps (Sonarr, SickRage, CouchPotato, Mylar, etc) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps. 

Developer note: The software implements the [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/master/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) APIs.



#### Supported Systems
* Windows using .NET 4.5
* Linux and OSX using Mono 4 (v3 should work but you may experience crashes).


#### Supported Private Trackers
 * Abnormal
 * AlphaRatio
 * AnimeBytes
 * Avistaz
 * bB
 * BeyondHD
 * BIT-HDTV
 * BitMeTV
 * BitSoup
 * BlueTigers
 * BTN
 * DanishBits
 * Demonoid
 * EuTorrents
 * FileList
 * French-ADN
 * Freshon
 * Fuzer
 * HD-Space
 * HD-Torrents
 * Hounddawgs
 * ILoveTorrents
 * Immortalseed
 * IPTorrents
 * MoreThanTV
 * MyAnonamouse
 * NCore
 * NextGen
 * PhxBit
 * Pretome
 * PrivateHD
 * RevolutionTT
 * SceneAccess
 * SceneFZ
 * SceneTime
 * Shazbat
 * SpeedCD
 * TehConnection
 * TorrentBytes
 * TorrentDay
 * TorrentLeech
 * TorrentShack
 * TransmitheNet
 * TV Chaos UK
 * World-In-HD
 * XSpeeds
 * Xthor

#### Installation on Windows

We recommend you install Jackett as a Windows service using the supplied installer.  When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line using JackettConsole.exe if you would like to see log messages (Ensure the server isn't already running from the tray/service).

#### Installation on Linux/OSX
 1. Install [Mono 4](http://www.mono-project.com/download/) or better
 2. Install  libcurl:
       * Debian/Ubunutu: apt-get install libcurl-dev
       * Redhat/Fedora: yum install libcurl-devel
       * For other distros see the  [Curl docs](http://curl.haxx.se/dlwiz/?type=devel).
 3. Download and extract the latest ```.tar.gz``` release from the [releases page](https://github.com/Jackett/Jackett/releases) and run Jackett using mono with the command "mono JackettConsole.exe".
 
Detailed instructions for [Ubuntu 14.x](http://www.htpcguides.com/install-jackett-on-ubuntu-14-x-for-custom-torrents-in-sonarr/) and [Ubuntu 15.x](http://www.htpcguides.com/install-jackett-ubuntu-15-x-for-custom-torrents-in-sonarr/)

#### Installation on Synology
Jackett is available as beta package from [SynoCommuniy](https://synocommunity.com/)

#### Troubleshooting

* Command line switches

You can pass various options when running via the command line, see --help for details.

* Unable to  connect to certain trackers on Linux

Try running with the "--SSLFix true" if you are on Redhat/Fedora/NNS based libcurl.  If the tracker is currently configured try removing it and adding it again. Alternatively try running with a different client via --UseClient (Warning: safecurl just executes curl and your details may be seen from the process list). You can also try running with the "--IgnoreSslErrors true" option which is useful if the site has an invalid SSL certificate.

*  Enable logging

You can get additional logging with the switches "-t -l".  Please post logs if you are unable to resolve your issue with these switches ensuring to remove your username/password/cookies.

#### Creating an issue
Please supply as much information about the problem you are experiencing as possible. Your issue has a much greater chance of being resolved if logs are supplied so that we can see what is going on. Creating an issue with '### isn't working' doesn't help anyone to fix the problem.

### Contributing
All contributions are welcome just send a pull request.  Jackett's framework allows our team (and any other volunteering dev) to implement new trackers in an hour or two. If you'd like support for a new tracker but are not a developer then feel free to leave a request on the [issues page](https://github.com/Jackett/Jackett/issues).  It is recommended to use Visual studio 2015 when making code changes in this project.  We currently only support private trackers.


### Screenshots

![screenshot](http://i.imgur.com/t1sVva6.png "screenshot")
