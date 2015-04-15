# Jackett

### API Access to your favorite trackers

This software creates a [Torznab](https://github.com/Sonarr/Sonarr/wiki/Implementing-a-Torznab-indexer) API server on your machine that any Torznab enabled software can consume. Jackett works as a proxy server: it translates Torznab queries into tracker-site-specific http queries, parses the html response into Torznab results, then sends results back to the requesting software. 

Currently [Sonarr](https://sonarr.tv/) is the only software that uses Torznab. [Couchpotato](https://couchpota.to/) will hopefully get Torznab support in the future.

### Download
Software is still in development, check back in a few days.

### Supported Trackers
 * [BitMeTV](http://www.bitmetv.org/)
 * [Freshon](https://freshon.tv/)
 * [The Pirate Bay](https://thepiratebay.se/)

### Additional Trackers
Jackett's framework allows me (and any other volunteering dev) to implement just about any new tracker in 15-60 minutes. If you'd like support for a new tracker then feel free to leave a request on the [issues page](https://github.com/zone117x/Jackett/issues) or contact me on IRC (see below).

### Contact & Support
I can be contact on IRC at [irc.freenode.net#sonarr](http://webchat.freenode.net/?channels=#sonarr)
