# Jackett

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
 * Install libcurl-dev for your system: http://curl.haxx.se/dlwiz/?type=devel
   * For apt-get systems its simply: `apt-get install libcurl4-openssl-dev`


### Supported Trackers
 * [BitMeTV](http://www.bitmetv.org/)
 * [MoreThan.tv](https://morethan.tv/)
 * [BIT-HDTV](https://www.bit-hdtv.com)
 * [Freshon](https://freshon.tv/)
 * [IPTorrents](https://iptorrents.com/)
 * [The Pirate Bay](https://thepiratebay.se/)


### Additional Trackers
Jackett's framework allows me (and any other volunteering dev) to implement just about any new tracker in 15-60 minutes. If you'd like support for a new tracker then feel free to leave a request on the [issues page](https://github.com/zone117x/Jackett/issues) or contact me on IRC (see below).

### Contact & Support
I can be contact on IRC at [irc.freenode.net#sonarr](http://webchat.freenode.net/?channels=#sonarr)

### Screenshots

![screenshot](http://i.imgur.com/ca3lKif.png "screenshot")
