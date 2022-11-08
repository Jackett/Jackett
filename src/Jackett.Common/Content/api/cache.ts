import { useAPI } from "./api";
import { ReleaseInfo } from "./release";

export type CacheResult = {
	FirstSeen: string;
	Tracker: string;
	TrackerId: string;
	TrackerType: string;
	CategoryDesc: string;
	BlackholeLink?: string;
} & ReleaseInfo;

export function useCache() {
	return useAPI<CacheResult[]>("/indexers/cache", true);
}
