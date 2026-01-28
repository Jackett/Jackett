declare global {
    interface Capability {
        ID: string;
        Name: string;
    }

    interface Indexer {
        id: string;
        name: string;
        description: string;
        type: 'public' | 'private' | 'semi-private';
        configured: boolean;
        site_link: string;
        alternativesitelinks: string[];
        language: string;
        tags: string[];
        last_error: string;
        potatoenabled: boolean;
        caps: Capability[];
        rss_host?: string;
        torznab_host?: string;
        potato_host?: string;
        state?: string;
        type_label?: string;
        mains_cats?: string;
    }

    interface Filter {
        id: string;
        apply: (indexer: Indexer) => boolean;
        value: string;
    }

    interface ConfigItem {
        id: string;
        value?: string | number | boolean;
        values?: string[];
    }

    interface ServerConfig {
        api_key: string;
        app_version?: string;
        port?: number | string;
        proxy_type?: string;
        proxy_url?: string;
        proxy_port?: number | string;
        proxy_username?: string;
        proxy_password?: string;
        basepathoverride?: string | null;
        baseurloverride?: string | null;
        blackholedir?: string;
        external?: boolean;
        local_bind_address?: string;
        cors?: boolean;
        updatedisabled?: boolean;
        prerelease?: boolean;
        logging?: boolean;
        cache_enabled?: boolean;
        cache_ttl?: number;
        cache_max_results_per_indexer?: number;
        flaresolverrurl?: string;
        flaresolverr_maxtimeout?: number;
        omdbkey?: string;
        omdburl?: string;
        password?: string;
        can_run_netcore?: boolean;
        notices?: string[];
    }

    interface SearchQuery {
        Query?: string;
        Category?: string[] | string;
        Tracker?: string[] | string;
        [key: string]: any;
    }

    interface IndexerSearchInfo {
        Name: string;
        Error?: string;
        Results?: number;
        ElapsedTime?: number;
    }

    interface SearchResult {
        Imdb?: string | number;
        TMDb?: string | number;
        TMDBId?: number;
        TVDBId?: number;
        TVMazeId?: number;
        TraktId?: number;
        DoubanId?: number;
        Poster?: string;
        Description?: string;
        PublishDate?: string;
        Tracker?: string;
        Title: string;
        Details?: string;
        Size?: string;
        SizeBytes?: number;
        Files?: number;
        CategoryDesc?: string;
        Grabs?: number;
        Seeders?: number;
        Peers?: number;
        DownloadVolumeFactor?: number;
        UploadVolumeFactor?: number;
        Link?: string;
        MagnetUri?: string;
        BlackholeLink?: string;
    }

    interface SearchResponse {
        Results: SearchResult[];
        Indexers?: IndexerSearchInfo[];
    }

    interface ErrorResponse {
        result: 'error';
        error: string;
        config?: ConfigItem[];
    }
}

export { };
