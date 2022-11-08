import { base, post, remove, useAPI } from "./api";

export type Capability = {
	ID: string;
	Name: string;
};

export type Indexer = {
	id: string;
	name: string;
	description: string;
	type: "private" | "public" | "semi-private";
	configured: boolean;
	site_link: string;
	alternativesitelinks: string[];
	language: string;
	tags: string[];
	last_error: string;
	potatoenabled: boolean;
	caps: Capability[];
};

export type IndexerSettingType =
	| "displayinfo"
	| "inputstring"
	| "inputbool"
	| "inputselect"
	| "inputcheckbox"
	| "inputtags";

export type IndexerSetting = {
	id: string;
	type: IndexerSettingType;
	name: string;
	value: any;
	options?: any;
	separator?: string;
	delimiters?: string;
	pattern?: string;
};

export type IndexerTestResult = {
	result: string;
	error: string;
	stacktrace: string;
	innerstacktrace: string;
};

export function useIndexers() {
	return useAPI<Indexer[]>("/indexers", true);
}

export function useIndexerConfig(id: string) {
	return useAPI<IndexerSetting[]>(`/indexers/${id}/config`, false);
}

export function saveIndexerConfig(id: string, settings: IndexerSetting[]) {
	return post(`/indexers/${id}/config`, settings);
}

export function deleteIndexer(id: string) {
	return remove(`/indexers/${id}`);
}

export function testIndexer(id: string) {
	return fetch(`${base}/indexers/${id}/test`, { method: "POST" })
		.then((res) =>
			Promise.resolve<IndexerTestResult>(
				res.status >= 200 && res.status <= 299
					? ({ result: "success" } as IndexerTestResult)
					: res.json()
			)
		)
		.catch((err) =>
			Promise.resolve({
				result: "error",
				error: err,
			} as IndexerTestResult)
		);
}
