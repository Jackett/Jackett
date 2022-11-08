import { post, useAPI } from "./api";

export type ServerConfig = {
	notices: string[];
	port: number;
	external: boolean;
	cors: boolean;
	api_key: string;
	blackholedir: string | null;
	updatedisabled: boolean;
	prerelease: boolean;
	logging: boolean;
	basepathoverride: string | null;
	baseurloverride: string | null;
	cache_enabled: boolean;
	cache_ttl: number;
	cache_max_results_per_indexer: number;
	flaresolverrurl: string | null;
	flaresolverr_maxtimeout: number;
	omdbkey: string | null;
	omdburl: string | null;
	app_version: string;
	can_run_netcore: boolean;
	proxy_type: number;
	proxy_url: string | null;
	proxy_port: number | null;
	proxy_username: string | null;
	proxy_password: string | null;
};

export function useServerConfig(update: boolean) {
	return useAPI<ServerConfig>("/server/config", update);
}

export function saveServerConfig(config: ServerConfig) {
	return post("/server/config", config);
}

export function saveAdminPassword(password: string) {
	return post("/server/adminpassword", password);
}
