## Jackett

#### Download
Downloads on the [Releases page](https://github.com/zone117x/Jackett/releases)


#### Overview
This software creates a [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/master/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) API server on your machine.  Torznab enables software such as [Sonarr](https://sonarr.tv) to access data from your favorite indexers in a similar fashion to rss but with added features such as searching.  TorrentPotato is an interface accessible to [CouchPotato](https://couchpota.to/).

Jackett works as a proxy server: it translates queries from apps (Sonarr, SickRage, CouchPotato, Mylar, etc) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps. 

We were previously focused on TV but are working on extending searches to allow for searching other items such as movies, comics, and music.


#### Supported Systems
* Windows using .NET 4.5
* Linux and OSX using Mono 4


#### Supported Trackers
 * [AlphaRatio](https://alpharatio.cc/)
 * [AnimeBytes](https://animebytes.tv/)
 * [BakaBT](http://bakabt.me/)
 * [bB](http://reddit.com/r/baconbits)
 * [BeyondHD](https://beyondhd.me/)
 * [BIT-HDTV](https://www.bit-hdtv.com)
 * [BitMeTV](http://www.bitmetv.org/)
 * [Demonoid](http://www.demonoid.pw/)
 * [FileList](http://filelist.ro/)
 * [FrenchTorrentDb](http://www.frenchtorrentdb.com/)
 * [Freshon](https://freshon.tv/)
 * [HD-Space](https://hd-space.org/)
 * [HD-Torrents.org](https://hd-torrents.org/)
 * [Immortalseed.me](http://immortalseed.me)
 * [IPTorrents](https://iptorrents.com/)
 * [MoreThan.tv](https://morethan.tv/)
 * [pretome](https://pretome.info)
 * [PrivateHD](https://privatehd.to/)
 * [RARGB](https://rarbg.to/)
 * [RuTor](http://rutor.org/)
 * [SceneAccess](https://sceneaccess.eu/login)
 * [SceneTime](https://www.scenetime.com/)
 * [ShowRSS](https://showrss.info/)
 * [Strike](https://getstrike.net/)
 * [T411](http://www.t411.io/)
 * [The Pirate Bay](https://thepiratebay.se/)
 * [TorrentBytes](https://www.torrentbytes.net/)
 * [TorrentDay](https://torrentday.eu/)
 * [TorrentLeech](http://www.torrentleech.org/)
 * [TorrentShack](http://torrentshack.me/)
 * [Torrentz](https://torrentz.eu/)
 * [TV Chaos UK](https://tvchaosuk.com/)

#### Installation on Linux/OSX
 1. Install [Mono 4](http://www.mono-project.com/download/) or better
 2. Install  libcurl:
       * Debian/Ubunutu: apt-get install libcurl-dev
       * Redhat/Fedora: yum install libcurl-devel
       * Or see the [Curl docs](http://curl.haxx.se/dlwiz/?type=devel).



#### Installation on Windows

We recommend you install Jackett as a Windows service using the supplied installer.  When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line using JackettConsole.exe if you would like to see log messages (Ensure the server isn't already running from the tray/service).

#### Installation on Linux/OSX

Run Jackett using mono with the command "mono JackettConsole.exe".



#### Troubleshooting

* Command line switches

You can pass various options when running via the command line, see --help for details.

* Unable to  connect to certain trackers on Linux

Try running with the "--SSLFix true" if you are on Redhat/Fedora/NNS based libcurl.  If the tracker is currently configured try removing it and adding it again. Alternatively try running with a different client via --UseClient (Warning: safecurl just executes curl and your details may be seen from the process list).

*  Enable logging

You can get additional logging with the switches "-t -l".  Please post logs if you are unable to resolve your issue with these switches ensuring to remove your username/password/cookies.


### Additional Trackers
Jackett's framework allows our team (and any other volunteering dev) to implement new trackers in an hour or two. If you'd like support for a new tracker then feel free to leave a request on the [issues page](https://github.com/zone117x/Jackett/issues) or contact us on IRC (see below).

### Contact & Support
Use the github issues pages or talk to us directly at: [irc.freenode.net#jackett](http://webchat.freenode.net/?channels=#jackett).

### Screenshots

![screenshot](http://i.imgur.com/t1sVva6.png "screenshot")
