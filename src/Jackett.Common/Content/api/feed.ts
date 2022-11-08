import { ServerConfig } from "./config";
import { Indexer } from "./indexers";

export function getRSSFeed(indexerConfig: Indexer, serverConfig: ServerConfig) {
	return `${window.location.origin}/api/v2.0/indexers/${indexerConfig.id}/results/torznab/api?apikey=${serverConfig.api_key}&t=search&cat=&q=`;
}

export function getTorznabFeed(indexerConfig: Indexer) {
	return `${window.location.origin}/api/v2.0/indexers/${indexerConfig.id}/results/torznab/`;
}

export function getPotatoFeed(indexerConfig: Indexer) {
	return `${window.location.origin}/api/v2.0/indexers/${indexerConfig.id}/results/potato/`;
}
