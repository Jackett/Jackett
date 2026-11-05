# README

## Top-level Schemas

* [Cardigann indexer definition](./schema.md "Schema for Jackett Cardigann YAML indexer definitions") – `Cardigann`

## Other Schemas

### Objects

* [BeforeBlock](./schema-definitions-beforeblock.md "HTTP request sent before the download (for example, a 'thank you' page)") – `Cardigann#/definitions/BeforeBlock`

* [Caps](./schema-definitions-caps.md "Capabilities of the indexer: category mappings and supported Torznab search modes") – `Cardigann#/definitions/Caps`

* [CaptchaBlock](./schema-definitions-captchablock.md "Captcha handling for form logins") – `Cardigann#/definitions/CaptchaBlock`

* [Case (RowsBlock)](./schema-definitions-rowsblock-properties-case-rowsblock.md "Selector: value pairs") – `Cardigann#/definitions/RowsBlock/properties/case`

* [Case (SelectorBlock)](./schema-definitions-selectorblock-properties-case-selectorblock.md "Selector: value pairs") – `Cardigann#/definitions/SelectorBlock/properties/case`

* [Categories (Caps)](./schema-definitions-caps-properties-categories-caps.md "Simple mapping of tracker category ID to Newznab category") – `Cardigann#/definitions/Caps/properties/categories`

* [CategoryMapping](./schema-definitions-categorymapping.md "Maps one tracker category to a Newznab/Torznab category") – `Cardigann#/definitions/CategoryMapping`

* [DownloadBlock](./schema-definitions-downloadblock.md "Needed only when the download link cannot be taken directly from the search results, the download must be a POST, or another page must be requested first") – `Cardigann#/definitions/DownloadBlock`

* [ErrorBlock](./schema-definitions-errorblock.md "Selector that detects an error on a login or search response") – `Cardigann#/definitions/ErrorBlock`

* [FieldsBlock](./schema-definitions-fieldsblock.md "Fields extracted from each result row") – `Cardigann#/definitions/FieldsBlock`

* [FilterBlock](./schema-definitions-filterblock.md "Filter applied to an extracted value") – `Cardigann#/definitions/FilterBlock`

* [GET selector inputs](./schema-definitions-login-properties-get-selector-inputs.md "Form login only") – `Cardigann#/definitions/Login/properties/getselectorinputs`

* [Headers (DownloadBlock)](./schema-definitions-downloadblock-properties-headers-downloadblock.md "Extra HTTP headers sent with download requests") – `Cardigann#/definitions/DownloadBlock/properties/headers`

* [Headers (Login)](./schema-definitions-login-properties-headers-login.md "Extra HTTP headers sent with login requests") – `Cardigann#/definitions/Login/properties/headers`

* [Headers (Search)](./schema-definitions-search-properties-headers-search.md "Extra HTTP headers sent with search requests") – `Cardigann#/definitions/Search/properties/headers`

* [InfoHashBlock](./schema-definitions-infohashblock.md "Builds a magnet URI from an infohash and title") – `Cardigann#/definitions/InfoHashBlock`

* [Inputs (BeforeBlock)](./schema-definitions-beforeblock-properties-inputs-beforeblock.md "HTTP arguments sent with the request, for example, id: \"{{ ") – `Cardigann#/definitions/BeforeBlock/properties/inputs`

* [Inputs (Login)](./schema-definitions-login-properties-inputs-login.md "Login parameters, for example, username: \"{{ ") – `Cardigann#/definitions/Login/properties/inputs`

* [Inputs (Search)](./schema-definitions-search-properties-inputs-search.md "HTTP arguments used by all paths") – `Cardigann#/definitions/Search/properties/inputs`

* [Inputs (SearchPathBlock)](./schema-definitions-searchpathblock-properties-inputs-searchpathblock.md "Extra HTTP arguments for this path only") – `Cardigann#/definitions/SearchPathBlock/properties/inputs`

* [Login](./schema-definitions-login.md "How Jackett logs in to the tracker") – `Cardigann#/definitions/Login`

* [Modes](./schema-definitions-modes.md "Torznab search modes and the query parameters the tracker supports for each") – `Cardigann#/definitions/Modes`

* [Options](./schema-definitions-settingsfield-properties-options.md "Select options as key: display-name pairs") – `Cardigann#/definitions/SettingsField/properties/options`

* [PageTestBlock](./schema-definitions-pagetestblock.md "Page requested after login to confirm the session is valid") – `Cardigann#/definitions/PageTestBlock`

* [ResponseBlock](./schema-definitions-responseblock.md "Response type for non-HTML search results") – `Cardigann#/definitions/ResponseBlock`

* [RowFilterBlock](./schema-definitions-rowfilterblock.md "Filter applied to the result rows") – `Cardigann#/definitions/RowFilterBlock`

* [RowsBlock](./schema-definitions-rowsblock.md "Selects the result rows") – `Cardigann#/definitions/RowsBlock`

* [SchemaRoot](./schema-definitions-schemaroot.md "Root of a Cardigann YAML indexer definition: header, caps, settings, login, search and download blocks") – `Cardigann#/definitions/SchemaRoot`

* [Search](./schema-definitions-search.md "How to build search requests and parse torrent results from the response") – `Cardigann#/definitions/Search`

* [SearchPathBlock](./schema-definitions-searchpathblock.md "One search request") – `Cardigann#/definitions/SearchPathBlock`

* [Selector inputs](./schema-definitions-login-properties-selector-inputs.md "Form login only") – `Cardigann#/definitions/Login/properties/selectorinputs`

* [SelectorBlock](./schema-definitions-selectorblock.md "Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters") – `Cardigann#/definitions/SelectorBlock`

* [SelectorField](./schema-definitions-selectorfield.md "Selector applied to the download page to get the download URL, hash or title") – `Cardigann#/definitions/SelectorField`

* [SettingsField](./schema-definitions-settingsfield.md "One config option shown on the indexer config page") – `Cardigann#/definitions/SettingsField`

### Arrays

* [Arguments as array (FilterBlock)](./schema-definitions-filterblock-properties-arguments-oneof-arguments-as-array-filterblock.md) – `Cardigann#/definitions/FilterBlock/properties/args/oneOf/0`

* [Arguments as array (RowFilterBlock)](./schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-array-rowfilterblock.md) – `Cardigann#/definitions/RowFilterBlock/properties/args/oneOf/0`

* [Book search](./schema-definitions-modes-properties-book-search.md "Parameters supported for book search") – `Cardigann#/definitions/Modes/properties/book-search`

* [Categories (SearchPathBlock)](./schema-definitions-searchpathblock-properties-categories-searchpathblock.md "Only use this path if the search includes at least one of these tracker categories") – `Cardigann#/definitions/SearchPathBlock/properties/categories`

* [Category mappings](./schema-definitions-caps-properties-category-mappings.md "List of tracker-to-Newznab category mappings") – `Cardigann#/definitions/Caps/properties/categorymappings`

* [Certificates](./schema-definitions-schemaroot-properties-certificates.md "SHA-1 fingerprints of untrusted HTTPS certificates (self-signed, expired, etc") – `Cardigann#/definitions/SchemaRoot/properties/certificates`

* [Cookies](./schema-definitions-login-properties-cookies.md "Cookies sent with the post login request") – `Cardigann#/definitions/Login/properties/cookies`

* [Defaults](./schema-definitions-settingsfield-properties-defaults.md "Default selected option keys for a multi-select") – `Cardigann#/definitions/SettingsField/properties/defaults`

* [Error (Login)](./schema-definitions-login-properties-error-login.md "Selectors checked on the login response") – `Cardigann#/definitions/Login/properties/error`

* [Error (Search)](./schema-definitions-search-properties-error-search.md "Selectors checked on the search response") – `Cardigann#/definitions/Search/properties/error`

* [Filters (RowsBlock)](./schema-definitions-rowsblock-properties-filters-rowsblock.md "Row filters, for example, andmatch or strdump") – `Cardigann#/definitions/RowsBlock/properties/filters`

* [Filters (SelectorBlock)](./schema-definitions-selectorblock-properties-filters-selectorblock.md "Filters applied to the extracted value, in order") – `Cardigann#/definitions/SelectorBlock/properties/filters`

* [Filters (SelectorField)](./schema-definitions-selectorfield-properties-filters-selectorfield.md "Filters applied to the extracted value, in order") – `Cardigann#/definitions/SelectorField/properties/filters`

* [Headers value (DownloadBlock)](./schema-definitions-downloadblock-properties-headers-downloadblock-patternproperties-headers-value-downloadblock.md) – `Cardigann#/definitions/DownloadBlock/properties/headers/patternProperties/^[A-Za-z0-9-]*$`

* [Headers value (Login)](./schema-definitions-login-properties-headers-login-patternproperties-headers-value-login.md) – `Cardigann#/definitions/Login/properties/headers/patternProperties/^[A-Za-z0-9-]*$`

* [Headers value (Search)](./schema-definitions-search-properties-headers-search-patternproperties-headers-value-search.md) – `Cardigann#/definitions/Search/properties/headers/patternProperties/^[A-Za-z0-9-]*$`

* [Keywords filters](./schema-definitions-search-properties-keywords-filters.md "Filters applied to the search keywords") – `Cardigann#/definitions/Search/properties/keywordsfilters`

* [Legacy links](./schema-definitions-schemaroot-properties-legacy-links.md "Old site URLs that no longer work") – `Cardigann#/definitions/SchemaRoot/properties/legacylinks`

* [Links](./schema-definitions-schemaroot-properties-links.md "Known site URLs") – `Cardigann#/definitions/SchemaRoot/properties/links`

* [Movie search](./schema-definitions-modes-properties-movie-search.md "Parameters supported for movie search") – `Cardigann#/definitions/Modes/properties/movie-search`

* [Music search](./schema-definitions-modes-properties-music-search.md "Parameters supported for music search") – `Cardigann#/definitions/Modes/properties/music-search`

* [Paths](./schema-definitions-search-properties-paths.md "Search requests") – `Cardigann#/definitions/Search/properties/paths`

* [Preprocessing filters](./schema-definitions-search-properties-preprocessing-filters.md "Filters applied to the raw response before parsing") – `Cardigann#/definitions/Search/properties/preprocessingfilters`

* [Replaces](./schema-definitions-schemaroot-properties-replaces.md "Old indexer IDs this definition replaces") – `Cardigann#/definitions/SchemaRoot/properties/replaces`

* [Search (Modes)](./schema-definitions-modes-properties-search-modes.md "Parameters for basic search") – `Cardigann#/definitions/Modes/properties/search`

* [Selectors](./schema-definitions-downloadblock-properties-selectors.md "If set, the search result download URL is fetched and parsed as HTML, and the first selector gives the actual download URL") – `Cardigann#/definitions/DownloadBlock/properties/selectors`

* [Settings](./schema-definitions-schemaroot-properties-settings.md "Config options shown for the indexer") – `Cardigann#/definitions/SchemaRoot/properties/settings`

* [TV search](./schema-definitions-modes-properties-tv-search.md "Parameters supported for TV search") – `Cardigann#/definitions/Modes/properties/tv-search`

## Version Note

The schemas linked above follow the JSON Schema Spec version: `https://json-schema.org/draft/2019-09/schema`
