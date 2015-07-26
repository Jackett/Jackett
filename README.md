# Jackett

Use just about any tracker with Sonarr

### API Access to your favorite trackers

This software creates a [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) API server on your machine that any Torznab enabled software can consume. Jackett works as a proxy server: it translates Torznab queries into tracker-site-specific http queries, parses the html response into Torznab results, then sends results back to the requesting software. 

Currently [Sonarr](https://sonarr.tv/) is the only software that uses Torznab. [Couchpotato](https://couchpota.to/) will hopefully get Torznab support in the future.

### Download
Download in the [Releases page](https://github.com/zone117x/Jackett/releases)

### Supported Systems
* Works on Windows by default
* Works on Linux and OS X using Mono. See instructions below...

### Instructions for Mono
 * Install Mono: http://www.mono-project.com/download/
 * *For MoreThanTV & ThePirateBay* install libcurl-dev for your system, [tutorial](http://curl.haxx.se/dlwiz/?type=devel)
   * For apt-get systems its simply: `apt-get install libcurl4-openssl-dev`

### Running Jackett

On Windows the recommened way of running Jackett is to install it as a windows service.  When installed as a service the tray icon acts as a way to open/start/stop Jackett.  If you opted to not install it as a service then Jacett will run it's web server from the tray tool.

Jackett can also be run from the command line (See --help for switches) using JackettConsole.exe if you would like to see log messages.  On Linux / OSX you would need to run the console using "mono JackettConsole.exe".


### Supported Trackers
 * [AlphaRatio](https://alpharatio.cc/)
 * [AnimeBytes](https://animebytes.tv/)
 * [BakaBT](http://bakabt.me/)
 * [bB](http://reddit.com/r/baconbits)
 * [BeyondHD](https://beyondhd.me/)
 * [BIT-HDTV](https://www.bit-hdtv.com)
 * [BitMeTV](http://www.bitmetv.org/)
 * [FrenchTorrentDb](http://www.frenchtorrentdb.com/)
 * [Freshon](https://freshon.tv/)
 * [HD-Space](https://hd-space.org/)
 * [HD-Torrents.org](https://hd-torrents.org/)
 * [IPTorrents](https://iptorrents.com/)
 * [MoreThan.tv](https://morethan.tv/)
 * [pretome](https://pretome.info)
 * [PrivateHD](https://privatehd.to/)
 * [RARBG](https://rarbg.com)
 * [SceneAccess](https://sceneaccess.eu/login)
 * [SceneTime](https://www.scenetime.com/)
 * [ShowRSS](https://showrss.info/)
 * [Strike](https://getstrike.net/)
 * [T411](http://www.t411.io/)
 * [The Pirate Bay](https://thepiratebay.se/)
 * [TorrentDay](https://torrentday.eu/)
 * [TorrentLeech](http://www.torrentleech.org/)
 * [TorrentShack](http://torrentshack.me/)
 * [Torrentz](https://torrentz.eu/)


### Additional Trackers
Jackett's framework allows me (and any other volunteering dev) to implement just about any new tracker in 15-60 minutes. If you'd like support for a new tracker then feel free to leave a request on the [issues page](https://github.com/zone117x/Jackett/issues) or contact me on IRC (see below).

### Contact & Support
I can be contact on IRC at [irc.freenode.net#jackett](http://webchat.freenode.net/?channels=#jackett) & [irc.freenode.net#sonarr](http://webchat.freenode.net/?channels=#sonarr)

### Screenshots

![screenshot](http://i.imgur.com/t1sVva6.png "screenshot")
