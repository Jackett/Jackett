import { base } from "./api";
import { CacheResult } from "./cache";
import fetcher from "./swr";

export type SearchIndexerResult = {
	ID: string;
	Name: string;
	Status: number;
	Results: number;
	Error: string | null;
};

export type SearchResults = {
	Results: CacheResult[];
	Indexers: SearchIndexerResult[];
};

export function searchTrackers(query: string) {
	// TODO: bypass apikey requirement with cookie
	return fetcher<SearchResults>(
		`${base}/indexers/status:!failing/results?Query=${query}&_=${Date.now()}`
	);
}
