## Jackett

#### Download
Downloads on the [Releases page](https://github.com/zone117x/Jackett/releases)


#### Overview
This software creates a [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) (with [nZEDb](https://github.com/nZEDb/nZEDb/blob/master/docs/newznab_api_specification.txt) category numbering) and [TorrentPotato](https://github.com/RuudBurger/CouchPotatoServer/wiki/Couchpotato-torrent-provider) API server on your machine.  Torznab enables software such as [Sonarr](https://sonarr.tv) to access data from your favorite indexers in a similar fashion to rss but with added features such as searching.  TorrentPotato is an interface accessible to [CouchPotato](https://couchpota.to/).

Jackett works as a proxy server: it translates queries from apps (Sonarr, SickRage, CouchPotato, Mylar, etc) into tracker-site-specific http queries, parses the html response, then sends results back to the requesting software. This allows for getting recent uploads (like RSS) and performing searches. Jackett is a single repository of maintained indexer scraping & translation logic - removing the burden from other apps. 

We were previously focused on TV but are working on extending searches to allow for searching other items such as movies, comics, and music.


#### Supported Systems
* Windows using .NET 4.5
* Linux and OSX using Mono 4 (v3 should work but you may experience crashes).


#### Supported Trackers
 * [AlphaRatio](https://alpharatio.cc/)
 * [AnimeBytes](https://animebytes.tv/)
 * [AnimeTorrents](http://animetorrents.me/)
 * [Avistaz](https://avistaz.to/)
 * [BakaBT](http://bakabt.me/)
 * [bB](http://reddit.com/r/baconbits)
 * [BeyondHD](https://beyondhd.me/)
 * [BIT-HDTV](https://www.bit-hdtv.com)
 * [BitMeTV](http://www.bitmetv.org/)
 * [BTN](http://broadcasthe.net)
 * [Demonoid](http://www.demonoid.pw/)
 * [EuTorrents](https://eutorrents.to/)
 * [FileList](http://filelist.ro/)
 * [FrenchTorrentDb](http://www.frenchtorrentdb.com/)
 * [Freshon](https://freshon.tv/)
 * [HD-Space](https://hd-space.org/)
 * [HD-Torrents.org](https://hd-torrents.org/)
 * [Immortalseed.me](http://immortalseed.me)
 * [IPTorrents](https://iptorrents.com/)
 * [MoreThan.tv](https://morethan.tv/)
 * [NextGen](https://nxtgn.org/)
 * [pretome](https://pretome.info)
 * [PrivateHD](https://privatehd.to/)
 * [RARBG](https://rarbg.to/)
 * [RuTor](http://rutor.org/)
 * [SceneAccess](https://sceneaccess.eu/login)
 * [SceneTime](https://www.scenetime.com/)
 * [Shazbat](www.shazbat.tv/login)
 * [ShowRSS](https://showrss.info/)
 * [Strike](https://getstrike.net/)
 * [T411](http://www.t411.io/)
 * [TehConnection](https://tehconnection.eu/) 
 * [The Pirate Bay](https://thepiratebay.se/)
 * [TorrentBytes](https://www.torrentbytes.net/)
 * [TorrentDay](https://torrentday.eu/)
 * [TorrentLeech](http://www.torrentleech.org/)
 * [TorrentShack](http://torrentshack.me/)
 * [Torrentz](https://torrentz.eu/)
 * [TV Chaos UK](https://tvchaosuk.com/)

#### Installation on Windows

Grab the latest release from the [website](http://jackett.net/Download).

We recommend you install Jackett as a Windows service using the supplied installer.  When installed as a service the tray icon acts as a way to open/start/stop Jackett. If you opted to not install it as a service then Jackett will run its web server from the tray tool.

Jackett can also be run from the command line using JackettConsole.exe if you would like to see log messages (Ensure the server isn't already running from the tray/service).

#### Installation on Linux/OSX
 1. Install [Mono 4](http://www.mono-project.com/download/) or better
 2. Install  libcurl:
       * Debian/Ubunutu: apt-get install libcurl-dev
       * Redhat/Fedora: yum install libcurl-devel
       * For other distros see the  [Curl docs](http://curl.haxx.se/dlwiz/?type=devel).
 3. Download and extract the latest ```.tar.bz2``` release from the [website](http://jackett.net/Download)  and run Jackett using mono with the command "mono JackettConsole.exe".

#### Installation on Synology
1. Install Sonarr & Mono 3.10 from synocommunity.
2. Install Mono beta 3.12 from the main Synology repo (Or newer if available).
3. Download jackett and place it in /opt/Jackett
4. cd /opt
5. chown -R {user who will run jackett} Jackett
6. Copy Upstart.config to /etc/init/jackett.conf and replace the braces {} in the script with the username that you wish to run Jackett with.
9. From anywhere on command line type "start jackett" . You should see output telling you that Jackett is running and you should be able to browse to {IP Address}:9117 . If not you should check /var/log/upstart/jackett.log and see what that says.

#### Troubleshooting

* Command line switches

You can pass various options when running via the command line, see --help for details.

* Unable to  connect to certain trackers on Linux

Try running with the "--SSLFix true" if you are on Redhat/Fedora/NNS based libcurl.  If the tracker is currently configured try removing it and adding it again. Alternatively try running with a different client via --UseClient (Warning: safecurl just executes curl and your details may be seen from the process list).

*  Enable logging

You can get additional logging with the switches "-t -l".  Please post logs if you are unable to resolve your issue with these switches ensuring to remove your username/password/cookies.


### Contributing
All contributions are welcome just send a pull request.  Jackett's framework allows our team (and any other volunteering dev) to implement new trackers in an hour or two. If you'd like support for a new tracker but are not a developer then feel free to leave a request on the [issues page](https://github.com/zone117x/Jackett/issues).  It is recommended to use Visual studio 2015 when making code changes in this project.

### Contact & Support
Use the github issues pages or talk to us directly at: [irc.freenode.net#jackett](http://webchat.freenode.net/?channels=#jackett).

### Screenshots

![screenshot](http://i.imgur.com/t1sVva6.png "screenshot")