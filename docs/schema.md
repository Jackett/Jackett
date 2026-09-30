# Cardigann indexer definition Schema

```txt
Cardigann
```

Schema for Jackett Cardigann YAML indexer definitions. See the Definition-format page on the Jackett wiki for examples.

| Abstract               | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                          |
| :--------------------- | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :---------------------------------------------------------------------------------- |
| Cannot be instantiated | Yes        | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## Cardigann indexer definition Type

`object` ([Cardigann indexer definition](schema.md))

# Cardigann indexer definition Definitions

## Definitions group SchemaRoot

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/SchemaRoot"}
```

| Property                            | Type      | Required | Nullable       | Defined by                                                                                                                                                   |
| :---------------------------------- | :-------- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [id](#id)                           | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-id.md "Cardigann#/definitions/SchemaRoot/properties/id")                             |
| [replaces](#replaces)               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-replaces.md "Cardigann#/definitions/SchemaRoot/properties/replaces")                 |
| [name](#name)                       | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-name.md "Cardigann#/definitions/SchemaRoot/properties/name")                         |
| [description](#description)         | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-description.md "Cardigann#/definitions/SchemaRoot/properties/description")           |
| [language](#language)               | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-language.md "Cardigann#/definitions/SchemaRoot/properties/language")                 |
| [type](#type)                       | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-type.md "Cardigann#/definitions/SchemaRoot/properties/type")                         |
| [encoding](#encoding)               | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-encoding.md "Cardigann#/definitions/SchemaRoot/properties/encoding")                 |
| [followredirect](#followredirect)   | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-follow-redirect.md "Cardigann#/definitions/SchemaRoot/properties/followredirect")    |
| [testlinktorrent](#testlinktorrent) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-test-link-torrent.md "Cardigann#/definitions/SchemaRoot/properties/testlinktorrent") |
| [requestDelay](#requestdelay)       | `number`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-request-delay.md "Cardigann#/definitions/SchemaRoot/properties/requestDelay")        |
| [links](#links)                     | `array`   | Required | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-links.md "Cardigann#/definitions/SchemaRoot/properties/links")                       |
| [legacylinks](#legacylinks)         | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-legacy-links.md "Cardigann#/definitions/SchemaRoot/properties/legacylinks")          |
| [certificates](#certificates)       | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-certificates.md "Cardigann#/definitions/SchemaRoot/properties/certificates")         |
| [caps](#caps)                       | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-caps.md "Cardigann#/definitions/SchemaRoot/properties/caps")                                               |
| [settings](#settings)               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-schemaroot-properties-settings.md "Cardigann#/definitions/SchemaRoot/properties/settings")                 |
| [login](#login)                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login.md "Cardigann#/definitions/SchemaRoot/properties/login")                                             |
| [search](#search)                   | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-search.md "Cardigann#/definitions/SchemaRoot/properties/search")                                           |
| [download](#download)               | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock.md "Cardigann#/definitions/SchemaRoot/properties/download")                                  |

### id

Internal name of the indexer. Must be unique. Usually the site name in lower case without special characters or spaces. Used in the Torznab, download and search URLs and in the indexer config file name.

`id`

* is required

* Type: `string` ([ID](schema-definitions-schemaroot-properties-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-id.md "Cardigann#/definitions/SchemaRoot/properties/id")

#### id Type

`string` ([ID](schema-definitions-schemaroot-properties-id.md))

### replaces

Old indexer IDs this definition replaces. Keeps existing configs working after an ID rename. Maintainer use only.

`replaces`

* is optional

* Type: `string[]` ([Replaced ID](schema-definitions-schemaroot-properties-replaces-replaced-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-replaces.md "Cardigann#/definitions/SchemaRoot/properties/replaces")

#### replaces Type

`string[]` ([Replaced ID](schema-definitions-schemaroot-properties-replaces-replaced-id.md))

#### replaces Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### name

Display name of the tracker.

`name`

* is required

* Type: `string` ([Name](schema-definitions-schemaroot-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-name.md "Cardigann#/definitions/SchemaRoot/properties/name")

#### name Type

`string` ([Name](schema-definitions-schemaroot-properties-name.md))

### description

Short description shown in the tooltip on the add-indexer page and in the config panel. Usually sourced from opentrackers.org when available.

`description`

* is required

* Type: `string` ([Description](schema-definitions-schemaroot-properties-description.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-description.md "Cardigann#/definitions/SchemaRoot/properties/description")

#### description Type

`string` ([Description](schema-definitions-schemaroot-properties-description.md))

### language

Language code of the main language used on the tracker, usually from the site's <html lang> attribute.

`language`

* is required

* Type: `string` ([Language](schema-definitions-schemaroot-properties-language.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-language.md "Cardigann#/definitions/SchemaRoot/properties/language")

#### language Type

`string` ([Language](schema-definitions-schemaroot-properties-language.md))

#### language Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value      | Explanation |
| :--------- | :---------- |
| `"af"`     |             |
| `"af-ZA"`  |             |
| `"ar"`     |             |
| `"ar-AE"`  |             |
| `"ar-BH"`  |             |
| `"ar-DZ"`  |             |
| `"ar-EG"`  |             |
| `"ar-IQ"`  |             |
| `"ar-JO"`  |             |
| `"ar-KW"`  |             |
| `"ar-LB"`  |             |
| `"ar-LY"`  |             |
| `"ar-MA"`  |             |
| `"ar-OM"`  |             |
| `"ar-QA"`  |             |
| `"ar-SA"`  |             |
| `"ar-SY"`  |             |
| `"ar-TN"`  |             |
| `"ar-YE"`  |             |
| `"az"`     |             |
| `"az-AZ"`  |             |
| `"be"`     |             |
| `"be-BY"`  |             |
| `"bg"`     |             |
| `"bg-BG"`  |             |
| `"bs-BA"`  |             |
| `"ca"`     |             |
| `"ca-ES"`  |             |
| `"cs"`     |             |
| `"cs-CZ"`  |             |
| `"cy"`     |             |
| `"cy-GB"`  |             |
| `"da"`     |             |
| `"da-DK"`  |             |
| `"de"`     |             |
| `"de-AT"`  |             |
| `"de-CH"`  |             |
| `"de-DE"`  |             |
| `"de-LI"`  |             |
| `"de-LU"`  |             |
| `"dv"`     |             |
| `"dv-MV"`  |             |
| `"el"`     |             |
| `"el-GR"`  |             |
| `"en"`     |             |
| `"en-AU"`  |             |
| `"en-BZ"`  |             |
| `"en-CA"`  |             |
| `"en-CB"`  |             |
| `"en-GB"`  |             |
| `"en-IE"`  |             |
| `"en-JM"`  |             |
| `"en-NZ"`  |             |
| `"en-PH"`  |             |
| `"en-TT"`  |             |
| `"en-US"`  |             |
| `"en-ZA"`  |             |
| `"en-ZW"`  |             |
| `"eo"`     |             |
| `"es"`     |             |
| `"es-AR"`  |             |
| `"es-BO"`  |             |
| `"es-CL"`  |             |
| `"es-CO"`  |             |
| `"es-CR"`  |             |
| `"es-DO"`  |             |
| `"es-EC"`  |             |
| `"es-ES"`  |             |
| `"es-GT"`  |             |
| `"es-HN"`  |             |
| `"es-MX"`  |             |
| `"es-NI"`  |             |
| `"es-PA"`  |             |
| `"es-PE"`  |             |
| `"es-PR"`  |             |
| `"es-PY"`  |             |
| `"es-SV"`  |             |
| `"es-UY"`  |             |
| `"es-VE"`  |             |
| `"et"`     |             |
| `"et-EE"`  |             |
| `"eu"`     |             |
| `"eu-ES"`  |             |
| `"fa"`     |             |
| `"fa-IR"`  |             |
| `"fi"`     |             |
| `"fi-FI"`  |             |
| `"fo"`     |             |
| `"fo-FO"`  |             |
| `"fr"`     |             |
| `"fr-BE"`  |             |
| `"fr-CA"`  |             |
| `"fr-CH"`  |             |
| `"fr-FR"`  |             |
| `"fr-LU"`  |             |
| `"fr-MC"`  |             |
| `"gl"`     |             |
| `"gl-ES"`  |             |
| `"gu"`     |             |
| `"gu-IN"`  |             |
| `"he"`     |             |
| `"he-IL"`  |             |
| `"hi"`     |             |
| `"hi-IN"`  |             |
| `"hr"`     |             |
| `"hr-BA"`  |             |
| `"hr-HR"`  |             |
| `"hu"`     |             |
| `"hu-HU"`  |             |
| `"hy"`     |             |
| `"hy-AM"`  |             |
| `"id"`     |             |
| `"id-ID"`  |             |
| `"is"`     |             |
| `"is-IS"`  |             |
| `"it"`     |             |
| `"it-CH"`  |             |
| `"it-IT"`  |             |
| `"ja"`     |             |
| `"ja-JP"`  |             |
| `"ka"`     |             |
| `"ka-GE"`  |             |
| `"kk"`     |             |
| `"kk-KZ"`  |             |
| `"kn"`     |             |
| `"kn-IN"`  |             |
| `"ko"`     |             |
| `"ko-KR"`  |             |
| `"kok"`    |             |
| `"kok-IN"` |             |
| `"ky"`     |             |
| `"ky-KG"`  |             |
| `"lt"`     |             |
| `"lt-LT"`  |             |
| `"lv"`     |             |
| `"lv-LV"`  |             |
| `"mi"`     |             |
| `"mi-NZ"`  |             |
| `"mk"`     |             |
| `"mk-MK"`  |             |
| `"mn"`     |             |
| `"mn-MN"`  |             |
| `"mr"`     |             |
| `"mr-IN"`  |             |
| `"ms"`     |             |
| `"ms-BN"`  |             |
| `"ms-MY"`  |             |
| `"mt"`     |             |
| `"mt-MT"`  |             |
| `"nb"`     |             |
| `"nb-NO"`  |             |
| `"nl"`     |             |
| `"nl-BE"`  |             |
| `"nl-NL"`  |             |
| `"nn-NO"`  |             |
| `"ns"`     |             |
| `"ns-ZA"`  |             |
| `"pa"`     |             |
| `"pa-IN"`  |             |
| `"pl"`     |             |
| `"pl-PL"`  |             |
| `"ps"`     |             |
| `"ps-AR"`  |             |
| `"pt"`     |             |
| `"pt-BR"`  |             |
| `"pt-PT"`  |             |
| `"qu"`     |             |
| `"qu-BO"`  |             |
| `"qu-EC"`  |             |
| `"qu-PE"`  |             |
| `"ro"`     |             |
| `"ro-RO"`  |             |
| `"ru"`     |             |
| `"ru-RU"`  |             |
| `"sa"`     |             |
| `"sa-IN"`  |             |
| `"se"`     |             |
| `"se-FI"`  |             |
| `"se-NO"`  |             |
| `"se-SE"`  |             |
| `"sk"`     |             |
| `"sk-SK"`  |             |
| `"sl"`     |             |
| `"sl-SI"`  |             |
| `"sq"`     |             |
| `"sq-AL"`  |             |
| `"sr-BA"`  |             |
| `"sr-SP"`  |             |
| `"sv"`     |             |
| `"sv-FI"`  |             |
| `"sv-SE"`  |             |
| `"sw"`     |             |
| `"sw-KE"`  |             |
| `"syr"`    |             |
| `"syr-SY"` |             |
| `"ta"`     |             |
| `"ta-IN"`  |             |
| `"te"`     |             |
| `"te-IN"`  |             |
| `"th"`     |             |
| `"th-TH"`  |             |
| `"tl"`     |             |
| `"tl-PH"`  |             |
| `"tn"`     |             |
| `"tn-ZA"`  |             |
| `"tr"`     |             |
| `"tr-TR"`  |             |
| `"tt"`     |             |
| `"tt-RU"`  |             |
| `"ts"`     |             |
| `"uk"`     |             |
| `"uk-UA"`  |             |
| `"ur"`     |             |
| `"ur-PK"`  |             |
| `"uz"`     |             |
| `"uz-UZ"`  |             |
| `"vi"`     |             |
| `"vi-VN"`  |             |
| `"xh"`     |             |
| `"xh-ZA"`  |             |
| `"zh"`     |             |
| `"zh-CN"`  |             |
| `"zh-HK"`  |             |
| `"zh-MO"`  |             |
| `"zh-SG"`  |             |
| `"zh-TW"`  |             |
| `"zu"`     |             |
| `"zu-ZA"`  |             |

### type

public: no registration required. semi-private: registration required but always open. private: invite or application needed.

`type`

* is required

* Type: `string` ([Type](schema-definitions-schemaroot-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-type.md "Cardigann#/definitions/SchemaRoot/properties/type")

#### type Type

`string` ([Type](schema-definitions-schemaroot-properties-type.md))

#### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value            | Explanation |
| :--------------- | :---------- |
| `"public"`       |             |
| `"semi-private"` |             |
| `"private"`      |             |

### encoding

Character encoding of the site, usually from its <meta charset> tag.

`encoding`

* is required

* Type: `string` ([Encoding](schema-definitions-schemaroot-properties-encoding.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-encoding.md "Cardigann#/definitions/SchemaRoot/properties/encoding")

#### encoding Type

`string` ([Encoding](schema-definitions-schemaroot-properties-encoding.md))

### followredirect

Automatically update the site URL when the site redirects to a different domain. Default: false.

`followredirect`

* is optional

* Type: `boolean` ([Follow redirect](schema-definitions-schemaroot-properties-follow-redirect.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-follow-redirect.md "Cardigann#/definitions/SchemaRoot/properties/followredirect")

#### followredirect Type

`boolean` ([Follow redirect](schema-definitions-schemaroot-properties-follow-redirect.md))

### testlinktorrent

Pre-test the .torrent download when grabbing. Set to false for sites that reject two GET requests for the same .torrent in sequence. Indexers that support fallback downloading need true. Default: true.

`testlinktorrent`

* is optional

* Type: `boolean` ([Test link torrent](schema-definitions-schemaroot-properties-test-link-torrent.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-test-link-torrent.md "Cardigann#/definitions/SchemaRoot/properties/testlinktorrent")

#### testlinktorrent Type

`boolean` ([Test link torrent](schema-definitions-schemaroot-properties-test-link-torrent.md))

### requestDelay

Seconds to wait between requests to the site. Use for sites that temporarily block clients that exceed a request rate.

`requestDelay`

* is optional

* Type: `number` ([Request delay](schema-definitions-schemaroot-properties-request-delay.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-request-delay.md "Cardigann#/definitions/SchemaRoot/properties/requestDelay")

#### requestDelay Type

`number` ([Request delay](schema-definitions-schemaroot-properties-request-delay.md))

### links

Known site URLs. The first is the default. Each must end with /.

`links`

* is required

* Type: `string[]` ([Link](schema-definitions-schemaroot-properties-links-link.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-links.md "Cardigann#/definitions/SchemaRoot/properties/links")

#### links Type

`string[]` ([Link](schema-definitions-schemaroot-properties-links-link.md))

#### links Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### legacylinks

Old site URLs that no longer work. A configured legacy URL is replaced automatically with the first entry in links.

`legacylinks`

* is optional

* Type: `string[]` ([Legacy link](schema-definitions-schemaroot-properties-legacy-links-legacy-link.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-legacy-links.md "Cardigann#/definitions/SchemaRoot/properties/legacylinks")

#### legacylinks Type

`string[]` ([Legacy link](schema-definitions-schemaroot-properties-legacy-links-legacy-link.md))

#### legacylinks Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### certificates

SHA-1 fingerprints of untrusted HTTPS certificates (self-signed, expired, etc.) to accept anyway. Rarely needed.

`certificates`

* is optional

* Type: `string[]` ([Certificate fingerprint](schema-definitions-schemaroot-properties-certificates-certificate-fingerprint.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-certificates.md "Cardigann#/definitions/SchemaRoot/properties/certificates")

#### certificates Type

`string[]` ([Certificate fingerprint](schema-definitions-schemaroot-properties-certificates-certificate-fingerprint.md))

#### certificates Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### caps

Capabilities of the indexer: category mappings and supported Torznab search modes.

`caps`

* is required

* Type: `object` ([Caps](schema-definitions-caps.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps.md "Cardigann#/definitions/SchemaRoot/properties/caps")

#### caps Type

`object` ([Caps](schema-definitions-caps.md))

one (and only one) of

* [Using categories](schema-definitions-caps-oneof-using-categories.md "check type definition")

* [Using categorymappings](schema-definitions-caps-oneof-using-categorymappings.md "check type definition")

### settings

Config options shown for the indexer. If omitted, username and password are used. Set to \[] for public trackers that need no settings.

`settings`

* is optional

* Type: `object[]` ([SettingsField](schema-definitions-settingsfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-settings.md "Cardigann#/definitions/SchemaRoot/properties/settings")

#### settings Type

`object[]` ([SettingsField](schema-definitions-settingsfield.md))

### login

How Jackett logs in to the tracker. Omit for sites that need no login.

`login`

* is optional

* Type: `object` ([Login](schema-definitions-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login.md "Cardigann#/definitions/SchemaRoot/properties/login")

#### login Type

`object` ([Login](schema-definitions-login.md))

### search

How to build search requests and parse torrent results from the response.

`search`

* is required

* Type: `object` ([Search](schema-definitions-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search.md "Cardigann#/definitions/SchemaRoot/properties/search")

#### search Type

`object` ([Search](schema-definitions-search.md))

one (and only one) of

* [Multiple paths](schema-definitions-search-oneof-multiple-paths.md "check type definition")

* [Single path](schema-definitions-search-oneof-single-path.md "check type definition")

### download

Needed only when the download link cannot be taken directly from the search results, the download must be a POST, or another page must be requested first.

`download`

* is optional

* Type: `object` ([DownloadBlock](schema-definitions-downloadblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock.md "Cardigann#/definitions/SchemaRoot/properties/download")

#### download Type

`object` ([DownloadBlock](schema-definitions-downloadblock.md))

## Definitions group Caps

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/Caps"}
```

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                                  |
| :-------------------------------------- | :-------- | :------- | :------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [categories](#categories)               | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-categories-caps.md "Cardigann#/definitions/Caps/properties/categories")                   |
| [categorymappings](#categorymappings)   | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-category-mappings.md "Cardigann#/definitions/Caps/properties/categorymappings")           |
| [modes](#modes)                         | `object`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-modes.md "Cardigann#/definitions/Caps/properties/modes")                                                  |
| [allowrawsearch](#allowrawsearch)       | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-allow-raw-search.md "Cardigann#/definitions/Caps/properties/allowrawsearch")              |
| [allowtvsearchimdb](#allowtvsearchimdb) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md "Cardigann#/definitions/Caps/properties/allowtvsearchimdb") |

### categories

Simple mapping of tracker category ID to Newznab category. Use either this or categorymappings.

`categories`

* is optional

* Type: `object` ([Categories (Caps)](schema-definitions-caps-properties-categories-caps.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-categories-caps.md "Cardigann#/definitions/Caps/properties/categories")

#### categories Type

`object` ([Categories (Caps)](schema-definitions-caps-properties-categories-caps.md))

### categorymappings

List of tracker-to-Newznab category mappings. Use either this or categories.

`categorymappings`

* is optional

* Type: `object[]` ([CategoryMapping](schema-definitions-categorymapping.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-category-mappings.md "Cardigann#/definitions/Caps/properties/categorymappings")

#### categorymappings Type

`object[]` ([CategoryMapping](schema-definitions-categorymapping.md))

#### categorymappings Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### modes

Torznab search modes and the query parameters the tracker supports for each. Most apps calling Jackett depend on these being correct.

`modes`

* is required

* Type: `object` ([Modes](schema-definitions-modes.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes.md "Cardigann#/definitions/Caps/properties/modes")

#### modes Type

`object` ([Modes](schema-definitions-modes.md))

### allowrawsearch

Enable raw search. See Jackett issue 8246.

`allowrawsearch`

* is optional

* Type: `boolean` ([Allow raw search](schema-definitions-caps-properties-allow-raw-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-allow-raw-search.md "Cardigann#/definitions/Caps/properties/allowrawsearch")

#### allowrawsearch Type

`boolean` ([Allow raw search](schema-definitions-caps-properties-allow-raw-search.md))

### allowtvsearchimdb

Allow imdbid in tv-search. See Jackett issue 16412.

`allowtvsearchimdb`

* is optional

* Type: `boolean` ([Allow TV search by IMDb ID](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md "Cardigann#/definitions/Caps/properties/allowtvsearchimdb")

#### allowtvsearchimdb Type

`boolean` ([Allow TV search by IMDb ID](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md))

## Definitions group CategoryMapping

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/CategoryMapping"}
```

| Property            | Type      | Required | Nullable       | Defined by                                                                                                                                                 |
| :------------------ | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [id](#id-1)         | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-id.md "Cardigann#/definitions/CategoryMapping/properties/id")                 |
| [cat](#cat)         | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-indexercategories.md "Cardigann#/definitions/CategoryMapping/properties/cat") |
| [desc](#desc)       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-description.md "Cardigann#/definitions/CategoryMapping/properties/desc")      |
| [default](#default) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-categorymapping-properties-default.md "Cardigann#/definitions/CategoryMapping/properties/default")       |

### id

Tracker-specific category ID. Can be a number or a string.

`id`

* is required

* Type: merged type ([ID](schema-definitions-categorymapping-properties-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-id.md "Cardigann#/definitions/CategoryMapping/properties/id")

#### id Type

merged type ([ID](schema-definitions-categorymapping-properties-id.md))

one (and only one) of

* [ID as integer](schema-definitions-categorymapping-properties-id-oneof-id-as-integer.md "check type definition")

* [ID as string](schema-definitions-categorymapping-properties-id-oneof-id-as-string.md "check type definition")

### cat

Predefined Newznab/Torznab category name. See the Jackett-Categories wiki page.

`cat`

* is required

* Type: `string` ([IndexerCategories](schema-definitions-categorymapping-properties-indexercategories.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-indexercategories.md "Cardigann#/definitions/CategoryMapping/properties/cat")

#### cat Type

`string` ([IndexerCategories](schema-definitions-categorymapping-properties-indexercategories.md))

#### cat Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value                    | Explanation |
| :----------------------- | :---------- |
| `"Console"`              |             |
| `"Console/NDS"`          |             |
| `"Console/PSP"`          |             |
| `"Console/Wii"`          |             |
| `"Console/XBox"`         |             |
| `"Console/XBox 360"`     |             |
| `"Console/Wiiware"`      |             |
| `"Console/XBox 360 DLC"` |             |
| `"Console/PS3"`          |             |
| `"Console/Other"`        |             |
| `"Console/3DS"`          |             |
| `"Console/PS Vita"`      |             |
| `"Console/WiiU"`         |             |
| `"Console/XBox One"`     |             |
| `"Console/PS4"`          |             |
| `"Movies"`               |             |
| `"Movies/Foreign"`       |             |
| `"Movies/Other"`         |             |
| `"Movies/SD"`            |             |
| `"Movies/HD"`            |             |
| `"Movies/UHD"`           |             |
| `"Movies/BluRay"`        |             |
| `"Movies/3D"`            |             |
| `"Movies/DVD"`           |             |
| `"Movies/WEB-DL"`        |             |
| `"Audio"`                |             |
| `"Audio/MP3"`            |             |
| `"Audio/Video"`          |             |
| `"Audio/Audiobook"`      |             |
| `"Audio/Lossless"`       |             |
| `"Audio/Other"`          |             |
| `"Audio/Foreign"`        |             |
| `"PC"`                   |             |
| `"PC/0day"`              |             |
| `"PC/ISO"`               |             |
| `"PC/Mac"`               |             |
| `"PC/Mobile-Other"`      |             |
| `"PC/Games"`             |             |
| `"PC/Mobile-iOS"`        |             |
| `"PC/Mobile-Android"`    |             |
| `"TV"`                   |             |
| `"TV/WEB-DL"`            |             |
| `"TV/Foreign"`           |             |
| `"TV/SD"`                |             |
| `"TV/HD"`                |             |
| `"TV/UHD"`               |             |
| `"TV/Other"`             |             |
| `"TV/Sport"`             |             |
| `"TV/Anime"`             |             |
| `"TV/Documentary"`       |             |
| `"XXX"`                  |             |
| `"XXX/DVD"`              |             |
| `"XXX/WMV"`              |             |
| `"XXX/XviD"`             |             |
| `"XXX/x264"`             |             |
| `"XXX/UHD"`              |             |
| `"XXX/Pack"`             |             |
| `"XXX/ImageSet"`         |             |
| `"XXX/Other"`            |             |
| `"XXX/SD"`               |             |
| `"XXX/WEB-DL"`           |             |
| `"Books"`                |             |
| `"Books/Mags"`           |             |
| `"Books/EBook"`          |             |
| `"Books/Comics"`         |             |
| `"Books/Technical"`      |             |
| `"Books/Other"`          |             |
| `"Books/Foreign"`        |             |
| `"Other"`                |             |
| `"Other/Misc"`           |             |
| `"Other/Hashed"`         |             |

### desc

Tracker category name. If set, it is used for a 1:1 mapping between tracker and Newznab categories.

`desc`

* is optional

* Type: `string` ([Description](schema-definitions-categorymapping-properties-description.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-description.md "Cardigann#/definitions/CategoryMapping/properties/desc")

#### desc Type

`string` ([Description](schema-definitions-categorymapping-properties-description.md))

### default

Use this category when the search query has no categories. Default: false.

`default`

* is optional

* Type: `boolean` ([Default](schema-definitions-categorymapping-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-categorymapping-properties-default.md "Cardigann#/definitions/CategoryMapping/properties/default")

#### default Type

`boolean` ([Default](schema-definitions-categorymapping-properties-default.md))

## Definitions group Modes

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/Modes"}
```

| Property                      | Type    | Required | Nullable       | Defined by                                                                                                                                 |
| :---------------------------- | :------ | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------- |
| [search](#search-1)           | `array` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-search-modes.md "Cardigann#/definitions/Modes/properties/search")       |
| [tv-search](#tv-search)       | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-tv-search.md "Cardigann#/definitions/Modes/properties/tv-search")       |
| [movie-search](#movie-search) | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-movie-search.md "Cardigann#/definitions/Modes/properties/movie-search") |
| [music-search](#music-search) | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-music-search.md "Cardigann#/definitions/Modes/properties/music-search") |
| [book-search](#book-search)   | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-book-search.md "Cardigann#/definitions/Modes/properties/book-search")   |

### search

Parameters for basic search. q is the minimum and is required.

`search`

* is required

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-search-modes-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-search-modes.md "Cardigann#/definitions/Modes/properties/search")

#### search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-search-modes-search-parameter.md))

#### search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### tv-search

Parameters supported for TV search. Only list those the tracker can search with.

`tv-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-tv-search.md "Cardigann#/definitions/Modes/properties/tv-search")

#### tv-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

#### tv-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### movie-search

Parameters supported for movie search. Only list those the tracker can search with.

`movie-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-movie-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-movie-search.md "Cardigann#/definitions/Modes/properties/movie-search")

#### movie-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-movie-search-search-parameter.md))

#### movie-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### music-search

Parameters supported for music search. Only list those the tracker can search with.

`music-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-music-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-music-search.md "Cardigann#/definitions/Modes/properties/music-search")

#### music-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-music-search-search-parameter.md))

#### music-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

### book-search

Parameters supported for book search. Only list those the tracker can search with.

`book-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-book-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-book-search.md "Cardigann#/definitions/Modes/properties/book-search")

#### book-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-book-search-search-parameter.md))

#### book-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## Definitions group SettingsField

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/SettingsField"}
```

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                         |
| :-------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------- |
| [name](#name-1)       | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-name.md "Cardigann#/definitions/SettingsField/properties/name")         |
| [label](#label)       | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-label.md "Cardigann#/definitions/SettingsField/properties/label")       |
| [type](#type-1)       | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-type.md "Cardigann#/definitions/SettingsField/properties/type")         |
| [default](#default-1) | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-default.md "Cardigann#/definitions/SettingsField/properties/default")   |
| [options](#options)   | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-options.md "Cardigann#/definitions/SettingsField/properties/options")   |
| [defaults](#defaults) | `array`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-settingsfield-properties-defaults.md "Cardigann#/definitions/SettingsField/properties/defaults") |

### name

Internal variable name. Referenced in templates as .Config.<name>.

`name`

* is required

* Type: `string` ([Name](schema-definitions-settingsfield-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-name.md "Cardigann#/definitions/SettingsField/properties/name")

#### name Type

`string` ([Name](schema-definitions-settingsfield-properties-name.md))

### label

Display name shown on the config page.

`label`

* is optional

* Type: `string` ([Label](schema-definitions-settingsfield-properties-label.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-label.md "Cardigann#/definitions/SettingsField/properties/label")

#### label Type

`string` ([Label](schema-definitions-settingsfield-properties-label.md))

### type

Input type. password masks the input. The info\_\* types show a predefined info box (category_8000, cookie, FlareSolverr, useragent) and need no label.

`type`

* is required

* Type: `string` ([Type](schema-definitions-settingsfield-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-type.md "Cardigann#/definitions/SettingsField/properties/type")

#### type Type

`string` ([Type](schema-definitions-settingsfield-properties-type.md))

#### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value                  | Explanation |
| :--------------------- | :---------- |
| `"info"`               |             |
| `"text"`               |             |
| `"password"`           |             |
| `"checkbox"`           |             |
| `"select"`             |             |
| `"multi-select"`       |             |
| `"info_category_8000"` |             |
| `"info_cookie"`        |             |
| `"info_flaresolverr"`  |             |
| `"info_useragent"`     |             |

### default

Default value. For checkbox, true ticks it (unticked by default). For info, the text to display. For select, the default option key.

`default`

* is optional

* Type: merged type ([Default](schema-definitions-settingsfield-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-default.md "Cardigann#/definitions/SettingsField/properties/default")

#### default Type

merged type ([Default](schema-definitions-settingsfield-properties-default.md))

one (and only one) of

* [Default as string](schema-definitions-settingsfield-properties-default-oneof-default-as-string.md "check type definition")

* [Default as integer](schema-definitions-settingsfield-properties-default-oneof-default-as-integer.md "check type definition")

* [Default as boolean](schema-definitions-settingsfield-properties-default-oneof-default-as-boolean.md "check type definition")

### options

Select options as key: display-name pairs.

`options`

* is optional

* Type: `object` ([Options](schema-definitions-settingsfield-properties-options.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-options.md "Cardigann#/definitions/SettingsField/properties/options")

#### options Type

`object` ([Options](schema-definitions-settingsfield-properties-options.md))

### defaults

Default selected option keys for a multi-select.

`defaults`

* is optional

* Type: `string[]` ([Option key](schema-definitions-settingsfield-properties-defaults-option-key.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-settingsfield-properties-defaults.md "Cardigann#/definitions/SettingsField/properties/defaults")

#### defaults Type

`string[]` ([Option key](schema-definitions-settingsfield-properties-defaults-option-key.md))

## Definitions group Login

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/Login"}
```

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                             |
| :-------------------------------------- | :-------- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------- |
| [method](#method)                       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-method.md "Cardigann#/definitions/Login/properties/method")                         |
| [cookies](#cookies)                     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-cookies.md "Cardigann#/definitions/Login/properties/cookies")                       |
| [path](#path)                           | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-path.md "Cardigann#/definitions/Login/properties/path")                             |
| [submitpath](#submitpath)               | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-submit-path.md "Cardigann#/definitions/Login/properties/submitpath")                |
| [form](#form)                           | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-form.md "Cardigann#/definitions/Login/properties/form")                             |
| [captcha](#captcha)                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock.md "Cardigann#/definitions/Login/properties/captcha")                                   |
| [inputs](#inputs)                       | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-inputs-login.md "Cardigann#/definitions/Login/properties/inputs")                   |
| [selectors](#selectors)                 | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-selectors.md "Cardigann#/definitions/Login/properties/selectors")                   |
| [selectorinputs](#selectorinputs)       | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-selector-inputs.md "Cardigann#/definitions/Login/properties/selectorinputs")        |
| [getselectorinputs](#getselectorinputs) | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-get-selector-inputs.md "Cardigann#/definitions/Login/properties/getselectorinputs") |
| [error](#error)                         | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-error-login.md "Cardigann#/definitions/Login/properties/error")                     |
| [test](#test)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock.md "Cardigann#/definitions/Login/properties/test")                                     |
| [headers](#headers)                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-headers-login.md "Cardigann#/definitions/Login/properties/headers")                 |

### method

post: send inputs as an HTTP POST. get: same, using HTTP GET. form: fetch path, extract the HTML form (handles CSRF tokens and captchas), then POST it. cookie: use the configured cookie. oneurl: legacy, added for the removed beyond-hd-oneurl indexer; no definition uses it.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-login-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-method.md "Cardigann#/definitions/Login/properties/method")

#### method Type

`string` ([Method](schema-definitions-login-properties-method.md))

#### method Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value      | Explanation |
| :--------- | :---------- |
| `"form"`   |             |
| `"post"`   |             |
| `"cookie"` |             |
| `"get"`    |             |
| `"oneurl"` |             |

### cookies

Cookies sent with the post login request.

`cookies`

* is optional

* Type: `string[]` ([Cookie](schema-definitions-login-properties-cookies-cookie.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-cookies.md "Cardigann#/definitions/Login/properties/cookies")

#### cookies Type

`string[]` ([Cookie](schema-definitions-login-properties-cookies-cookie.md))

### path

Login target. For post/get it is the request URL. For form it is the page containing the login form.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-login-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-path.md "Cardigann#/definitions/Login/properties/path")

#### path Type

`string` ([Path](schema-definitions-login-properties-path.md))

### submitpath

Form login only. URL to POST to, when it differs from the form's action attribute.

`submitpath`

* is optional

* Type: `string` ([Submit path](schema-definitions-login-properties-submit-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-submit-path.md "Cardigann#/definitions/Login/properties/submitpath")

#### submitpath Type

`string` ([Submit path](schema-definitions-login-properties-submit-path.md))

### form

Form login only. Selector for the HTML form element. Default: form.

`form`

* is optional

* Type: `string` ([Form](schema-definitions-login-properties-form.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-form.md "Cardigann#/definitions/Login/properties/form")

#### form Type

`string` ([Form](schema-definitions-login-properties-form.md))

### captcha

Captcha handling for form logins. Google ReCaptcha and simplecaptcha are detected automatically and need no captcha block.

`captcha`

* is optional

* Type: `object` ([CaptchaBlock](schema-definitions-captchablock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock.md "Cardigann#/definitions/Login/properties/captcha")

#### captcha Type

`object` ([CaptchaBlock](schema-definitions-captchablock.md))

### inputs

Login parameters, for example, username: "{{ .Config.username }}". Fixed values are allowed.

`inputs`

* is optional

* Type: `object` ([Inputs (Login)](schema-definitions-login-properties-inputs-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-inputs-login.md "Cardigann#/definitions/Login/properties/inputs")

#### inputs Type

`object` ([Inputs (Login)](schema-definitions-login-properties-inputs-login.md))

### selectors

Form login only. If true, the keys in inputs are treated as CSS selectors. Only needed for dynamic input names (very rare).

`selectors`

* is optional

* Type: `boolean` ([Selectors](schema-definitions-login-properties-selectors.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-selectors.md "Cardigann#/definitions/Login/properties/selectors")

#### selectors Type

`boolean` ([Selectors](schema-definitions-login-properties-selectors.md))

### selectorinputs

Form login only. Input values taken from the login page with selectors, for example, a CSRF token hidden in JavaScript.

`selectorinputs`

* is optional

* Type: `object` ([Selector inputs](schema-definitions-login-properties-selector-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-selector-inputs.md "Cardigann#/definitions/Login/properties/selectorinputs")

#### selectorinputs Type

`object` ([Selector inputs](schema-definitions-login-properties-selector-inputs.md))

### getselectorinputs

Form login only. Like selectorinputs, but sent in the query string instead of the POST body.

`getselectorinputs`

* is optional

* Type: `object` ([GET selector inputs](schema-definitions-login-properties-get-selector-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-get-selector-inputs.md "Cardigann#/definitions/Login/properties/getselectorinputs")

#### getselectorinputs Type

`object` ([GET selector inputs](schema-definitions-login-properties-get-selector-inputs.md))

### error

Selectors checked on the login response. If one matches, the login failed and the matched text is shown as the error.

`error`

* is optional

* Type: `object[]` ([ErrorBlock](schema-definitions-errorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-error-login.md "Cardigann#/definitions/Login/properties/error")

#### error Type

`object[]` ([ErrorBlock](schema-definitions-errorblock.md))

### test

Page requested after login to confirm the session is valid. A redirect, or no selector match, means the login failed. Also used during searches to detect an expired session and re-login.

`test`

* is optional

* Type: `object` ([PageTestBlock](schema-definitions-pagetestblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock.md "Cardigann#/definitions/Login/properties/test")

#### test Type

`object` ([PageTestBlock](schema-definitions-pagetestblock.md))

### headers

Extra HTTP headers sent with login requests. If omitted, search headers are used.

`headers`

* is optional

* Type: `object` ([Headers (Login)](schema-definitions-login-properties-headers-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-headers-login.md "Cardigann#/definitions/Login/properties/headers")

#### headers Type

`object` ([Headers (Login)](schema-definitions-login-properties-headers-login.md))

## Definitions group PageTestBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/PageTestBlock"}
```

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                         |
| :-------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path-1)       | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock-properties-path.md "Cardigann#/definitions/PageTestBlock/properties/path")         |
| [selector](#selector) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-pagetestblock-properties-selector.md "Cardigann#/definitions/PageTestBlock/properties/selector") |

### path

Page to request, typically the home page or the search page.

`path`

* is required

* Type: `string` ([Path](schema-definitions-pagetestblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock-properties-path.md "Cardigann#/definitions/PageTestBlock/properties/path")

#### path Type

`string` ([Path](schema-definitions-pagetestblock-properties-path.md))

### selector

Selector that must match on a logged-in page. Required if the site shows a login form instead of redirecting.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-pagetestblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-pagetestblock-properties-selector.md "Cardigann#/definitions/PageTestBlock/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-pagetestblock-properties-selector.md))

## Definitions group CaptchaBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/CaptchaBlock"}
```

| Property                | Type     | Required | Nullable       | Defined by                                                                                                                                       |
| :---------------------- | :------- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------- |
| [type](#type-2)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-type.md "Cardigann#/definitions/CaptchaBlock/properties/type")         |
| [selector](#selector-1) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-selector.md "Cardigann#/definitions/CaptchaBlock/properties/selector") |
| [input](#input)         | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-captchablock-properties-input.md "Cardigann#/definitions/CaptchaBlock/properties/input")       |

### type

Captcha type: image or text.

`type`

* is required

* Type: `string` ([Type](schema-definitions-captchablock-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-type.md "Cardigann#/definitions/CaptchaBlock/properties/type")

#### type Type

`string` ([Type](schema-definitions-captchablock-properties-type.md))

#### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value     | Explanation |
| :-------- | :---------- |
| `"image"` |             |
| `"text"`  |             |

### selector

Selector for the captcha HTML element.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-captchablock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-selector.md "Cardigann#/definitions/CaptchaBlock/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-captchablock-properties-selector.md))

### input

Name of the form field that receives the captcha value.

`input`

* is required

* Type: `string` ([Input](schema-definitions-captchablock-properties-input.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-captchablock-properties-input.md "Cardigann#/definitions/CaptchaBlock/properties/input")

#### input Type

`string` ([Input](schema-definitions-captchablock-properties-input.md))

## Definitions group ErrorBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/ErrorBlock"}
```

| Property                | Type     | Required | Nullable       | Defined by                                                                                                                                   |
| :---------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path-2)         | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-errorblock-properties-path.md "Cardigann#/definitions/ErrorBlock/properties/path")         |
| [selector](#selector-2) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-errorblock-properties-selector.md "Cardigann#/definitions/ErrorBlock/properties/selector") |
| [message](#message)     | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/ErrorBlock/properties/message")                   |

### path

Only check responses from this path.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-errorblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-errorblock-properties-path.md "Cardigann#/definitions/ErrorBlock/properties/path")

#### path Type

`string` ([Path](schema-definitions-errorblock-properties-path.md))

### selector

If this selector matches, the request failed.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-errorblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-errorblock-properties-selector.md "Cardigann#/definitions/ErrorBlock/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-errorblock-properties-selector.md))

### message

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`message`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/ErrorBlock/properties/message")

#### message Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Definitions group SelectorBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/SelectorBlock"}
```

| Property                | Type      | Required | Nullable       | Defined by                                                                                                                                                     |
| :---------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [selector](#selector-3) | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-selector.md "Cardigann#/definitions/SelectorBlock/properties/selector")             |
| [attribute](#attribute) | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-attribute.md "Cardigann#/definitions/SelectorBlock/properties/attribute")           |
| [optional](#optional)   | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-optional.md "Cardigann#/definitions/SelectorBlock/properties/optional")             |
| [default](#default-2)   | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-default.md "Cardigann#/definitions/SelectorBlock/properties/default")               |
| [case](#case)           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/case")       |
| [remove](#remove)       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-remove.md "Cardigann#/definitions/SelectorBlock/properties/remove")                 |
| [text](#text)           | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-text.md "Cardigann#/definitions/SelectorBlock/properties/text")                     |
| [filters](#filters)     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-filters-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/filters") |

### selector

CSS selector for HTML, or path for JSON/XML. :has(), :not() and :contains() are supported. In JSON, $ refers to the root object and a .. prefix reads from the row instead of the rows attribute subset.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-selectorblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-selector.md "Cardigann#/definitions/SelectorBlock/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-selectorblock-properties-selector.md))

### attribute

Take the value of this attribute (for example, href) instead of the element text.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-selectorblock-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-attribute.md "Cardigann#/definitions/SelectorBlock/properties/attribute")

#### attribute Type

`string` ([Attribute](schema-definitions-selectorblock-properties-attribute.md))

### optional

Do not fail if the selector does not match. Use with default.

`optional`

* is optional

* Type: `boolean` ([Optional](schema-definitions-selectorblock-properties-optional.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-optional.md "Cardigann#/definitions/SelectorBlock/properties/optional")

#### optional Type

`boolean` ([Optional](schema-definitions-selectorblock-properties-optional.md))

### default

Value used when an optional selector does not match. Templates are allowed, for example, "{{ .Result.title_default }}".

`default`

* is optional

* Type: merged type ([Default](schema-definitions-selectorblock-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-default.md "Cardigann#/definitions/SelectorBlock/properties/default")

#### default Type

merged type ([Default](schema-definitions-selectorblock-properties-default.md))

one (and only one) of

* [Default as string](schema-definitions-selectorblock-properties-default-oneof-default-as-string.md "check type definition")

* [Default as number](schema-definitions-selectorblock-properties-default-oneof-default-as-number.md "check type definition")

### case

Selector: value pairs. The value of the first matching selector is used. "\*" matches anything. Common for downloadvolumefactor and uploadvolumefactor.

`case`

* is optional

* Type: `object` ([Case (SelectorBlock)](schema-definitions-selectorblock-properties-case-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/case")

#### case Type

`object` ([Case (SelectorBlock)](schema-definitions-selectorblock-properties-case-selectorblock.md))

### remove

Selector for elements to remove before reading the text. Removed elements are gone for all later fields, so put fields using remove last.

`remove`

* is optional

* Type: `string` ([Remove](schema-definitions-selectorblock-properties-remove.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-remove.md "Cardigann#/definitions/SelectorBlock/properties/remove")

#### remove Type

`string` ([Remove](schema-definitions-selectorblock-properties-remove.md))

### text

Fixed value, or a template. Used instead of selector, such as for minimumratio and minimumseedtime.

`text`

* is optional

* Type: merged type ([Text](schema-definitions-selectorblock-properties-text.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-text.md "Cardigann#/definitions/SelectorBlock/properties/text")

#### text Type

merged type ([Text](schema-definitions-selectorblock-properties-text.md))

one (and only one) of

* [Text as string](schema-definitions-selectorblock-properties-text-oneof-text-as-string.md "check type definition")

* [Text as number](schema-definitions-selectorblock-properties-text-oneof-text-as-number.md "check type definition")

### filters

Filters applied to the extracted value, in order.

`filters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-filters-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/filters")

#### filters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

## Definitions group Search

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/Search"}
```

| Property                                      | Type      | Required | Nullable       | Defined by                                                                                                                                                    |
| :-------------------------------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [path](#path-3)                               | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-path.md "Cardigann#/definitions/Search/properties/path")                                  |
| [paths](#paths)                               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-paths.md "Cardigann#/definitions/Search/properties/paths")                                |
| [allowEmptyInputs](#allowemptyinputs)         | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-allow-empty-inputs.md "Cardigann#/definitions/Search/properties/allowEmptyInputs")        |
| [inputs](#inputs-1)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-inputs-search.md "Cardigann#/definitions/Search/properties/inputs")                       |
| [headers](#headers-1)                         | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-headers-search.md "Cardigann#/definitions/Search/properties/headers")                     |
| [keywordsfilters](#keywordsfilters)           | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-keywords-filters.md "Cardigann#/definitions/Search/properties/keywordsfilters")           |
| [error](#error-1)                             | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-error-search.md "Cardigann#/definitions/Search/properties/error")                         |
| [preprocessingfilters](#preprocessingfilters) | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-preprocessing-filters.md "Cardigann#/definitions/Search/properties/preprocessingfilters") |
| [rows](#rows)                                 | `object`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock.md "Cardigann#/definitions/Search/properties/rows")                                               |
| [fields](#fields)                             | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-fieldsblock.md "Cardigann#/definitions/Search/properties/fields")                                           |

### path

Single search path. Shorthand for paths with one entry.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-search-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-path.md "Cardigann#/definitions/Search/properties/path")

#### path Type

`string` ([Path](schema-definitions-search-properties-path.md))

### paths

Search requests. Usually one. Some trackers need separate pages, for example, for porn or scene releases.

`paths`

* is optional

* Type: `object[]` ([SearchPathBlock](schema-definitions-searchpathblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-paths.md "Cardigann#/definitions/Search/properties/paths")

#### paths Type

`object[]` ([SearchPathBlock](schema-definitions-searchpathblock.md))

### allowEmptyInputs

Send inputs whose value resolves to empty. By default such key/value pairs are dropped. Default: false.

`allowEmptyInputs`

* is optional

* Type: `boolean` ([Allow empty inputs](schema-definitions-search-properties-allow-empty-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-allow-empty-inputs.md "Cardigann#/definitions/Search/properties/allowEmptyInputs")

#### allowEmptyInputs Type

`boolean` ([Allow empty inputs](schema-definitions-search-properties-allow-empty-inputs.md))

### inputs

HTTP arguments used by all paths. $raw is appended to the argument list without escaping (only variables are escaped).

`inputs`

* is optional

* Type: `object` ([Inputs (Search)](schema-definitions-search-properties-inputs-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-inputs-search.md "Cardigann#/definitions/Search/properties/inputs")

#### inputs Type

`object` ([Inputs (Search)](schema-definitions-search-properties-inputs-search.md))

### headers

Extra HTTP headers sent with search requests.

`headers`

* is optional

* Type: `object` ([Headers (Search)](schema-definitions-search-properties-headers-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-headers-search.md "Cardigann#/definitions/Search/properties/headers")

#### headers Type

`object` ([Headers (Search)](schema-definitions-search-properties-headers-search.md))

### keywordsfilters

Filters applied to the search keywords. The result is available as .Keywords.

`keywordsfilters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-keywords-filters.md "Cardigann#/definitions/Search/properties/keywordsfilters")

#### keywordsfilters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

### error

Selectors checked on the search response. Same syntax as the login error block.

`error`

* is optional

* Type: `object[]` ([ErrorBlock](schema-definitions-errorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-error-search.md "Cardigann#/definitions/Search/properties/error")

#### error Type

`object[]` ([ErrorBlock](schema-definitions-errorblock.md))

### preprocessingfilters

Filters applied to the raw response before parsing.

`preprocessingfilters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-preprocessing-filters.md "Cardigann#/definitions/Search/properties/preprocessingfilters")

#### preprocessingfilters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

### rows

Selects the result rows. Each row is then parsed with the fields block.

`rows`

* is required

* Type: `object` ([RowsBlock](schema-definitions-rowsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock.md "Cardigann#/definitions/Search/properties/rows")

#### rows Type

`object` ([RowsBlock](schema-definitions-rowsblock.md))

### fields

Fields extracted from each result row. Names starting with \_ are working fields for use in templates via .Result.<name>.

`fields`

* is required

* Type: `object` ([FieldsBlock](schema-definitions-fieldsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-fieldsblock.md "Cardigann#/definitions/Search/properties/fields")

#### fields Type

`object` ([FieldsBlock](schema-definitions-fieldsblock.md))

one (and only one) of

* [Category by ID](schema-definitions-fieldsblock-oneof-category-by-id.md "check type definition")

* [Category by description](schema-definitions-fieldsblock-oneof-category-by-description.md "check type definition")

## Definitions group SearchPathBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/SearchPathBlock"}
```

| Property                            | Type      | Required | Nullable       | Defined by                                                                                                                                                                 |
| :---------------------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path-4)                     | `string`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-path.md "Cardigann#/definitions/SearchPathBlock/properties/path")                             |
| [method](#method-1)                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-method.md "Cardigann#/definitions/SearchPathBlock/properties/method")                         |
| [followredirect](#followredirect-1) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-follow-redirect.md "Cardigann#/definitions/SearchPathBlock/properties/followredirect")        |
| [categories](#categories-1)         | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-categories-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/categories") |
| [inputs](#inputs-2)                 | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/inputs")         |
| [inheritinputs](#inheritinputs)     | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inherit-inputs.md "Cardigann#/definitions/SearchPathBlock/properties/inheritinputs")          |
| [queryseparator](#queryseparator)   | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-query-separator.md "Cardigann#/definitions/SearchPathBlock/properties/queryseparator")        |
| [response](#response)               | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock.md "Cardigann#/definitions/SearchPathBlock/properties/response")                                           |

### path

Search URL path. Templates and conditionals are allowed.

`path`

* is required

* Type: `string` ([Path](schema-definitions-searchpathblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-path.md "Cardigann#/definitions/SearchPathBlock/properties/path")

#### path Type

`string` ([Path](schema-definitions-searchpathblock-properties-path.md))

### method

HTTP method: get or post. Templates are allowed. Default: get.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-searchpathblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-method.md "Cardigann#/definitions/SearchPathBlock/properties/method")

#### method Type

`string` ([Method](schema-definitions-searchpathblock-properties-method.md))

### followredirect

Follow redirects on search requests. If true, set a selector in login.test. Default: false.

`followredirect`

* is optional

* Type: `boolean` ([Follow redirect](schema-definitions-searchpathblock-properties-follow-redirect.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-follow-redirect.md "Cardigann#/definitions/SearchPathBlock/properties/followredirect")

#### followredirect Type

`boolean` ([Follow redirect](schema-definitions-searchpathblock-properties-follow-redirect.md))

### categories

Only use this path if the search includes at least one of these tracker categories. A leading "!" inverts the match.

`categories`

* is optional

* Type: an array of merged types ([Tracker category](schema-definitions-searchpathblock-properties-categories-searchpathblock-tracker-category.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-categories-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/categories")

#### categories Type

an array of merged types ([Tracker category](schema-definitions-searchpathblock-properties-categories-searchpathblock-tracker-category.md))

### inputs

Extra HTTP arguments for this path only.

`inputs`

* is optional

* Type: `object` ([Inputs (SearchPathBlock)](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md "Cardigann#/definitions/SearchPathBlock/properties/inputs")

#### inputs Type

`object` ([Inputs (SearchPathBlock)](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md))

### inheritinputs

Use the search-level inputs as the base for this path's inputs. Default: true.

`inheritinputs`

* is optional

* Type: `boolean` ([Inherit inputs](schema-definitions-searchpathblock-properties-inherit-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inherit-inputs.md "Cardigann#/definitions/SearchPathBlock/properties/inheritinputs")

#### inheritinputs Type

`boolean` ([Inherit inputs](schema-definitions-searchpathblock-properties-inherit-inputs.md))

### queryseparator

Query string separator. Default: &.

`queryseparator`

* is optional

* Type: `string` ([Query separator](schema-definitions-searchpathblock-properties-query-separator.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-query-separator.md "Cardigann#/definitions/SearchPathBlock/properties/queryseparator")

#### queryseparator Type

`string` ([Query separator](schema-definitions-searchpathblock-properties-query-separator.md))

### response

Response type for non-HTML search results. Omit for HTML, which is the default.

`response`

* is optional

* Type: `object` ([ResponseBlock](schema-definitions-responseblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock.md "Cardigann#/definitions/SearchPathBlock/properties/response")

#### response Type

`object` ([ResponseBlock](schema-definitions-responseblock.md))

## Definitions group ResponseBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/ResponseBlock"}
```

| Property                              | Type     | Required | Nullable       | Defined by                                                                                                                                                           |
| :------------------------------------ | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [type](#type-3)                       | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock-properties-type.md "Cardigann#/definitions/ResponseBlock/properties/type")                           |
| [noResultsMessage](#noresultsmessage) | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-responseblock-properties-no-results-message.md "Cardigann#/definitions/ResponseBlock/properties/noResultsMessage") |

### type

json or xml.

`type`

* is required

* Type: `string` ([Type](schema-definitions-responseblock-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock-properties-type.md "Cardigann#/definitions/ResponseBlock/properties/type")

#### type Type

`string` ([Type](schema-definitions-responseblock-properties-type.md))

#### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value    | Explanation |
| :------- | :---------- |
| `"json"` |             |
| `"xml"`  |             |

### noResultsMessage

If the response contains this text, or is empty and this is an empty string, return zero results instead of an error. For sites that do not return an empty object or a zero count.

`noResultsMessage`

* is optional

* Type: `string` ([No results message](schema-definitions-responseblock-properties-no-results-message.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-responseblock-properties-no-results-message.md "Cardigann#/definitions/ResponseBlock/properties/noResultsMessage")

#### noResultsMessage Type

`string` ([No results message](schema-definitions-responseblock-properties-no-results-message.md))

## Definitions group RowsBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/RowsBlock"}
```

| Property                                                            | Type      | Required | Nullable       | Defined by                                                                                                                                                                                   |
| :------------------------------------------------------------------ | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [after](#after)                                                     | `integer` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-after.md "Cardigann#/definitions/RowsBlock/properties/after")                                                         |
| [dateheaders](#dateheaders)                                         | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/dateheaders")                                                                |
| [selector](#selector-4)                                             | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-selector.md "Cardigann#/definitions/RowsBlock/properties/selector")                                                   |
| [attribute](#attribute-1)                                           | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-attribute.md "Cardigann#/definitions/RowsBlock/properties/attribute")                                                 |
| [optional](#optional-1)                                             | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-optional.md "Cardigann#/definitions/RowsBlock/properties/optional")                                                   |
| [multiple](#multiple)                                               | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-multiple.md "Cardigann#/definitions/RowsBlock/properties/multiple")                                                   |
| [missingAttributeEqualsNoResults](#missingattributeequalsnoresults) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md "Cardigann#/definitions/RowsBlock/properties/missingAttributeEqualsNoResults") |
| [case](#case-1)                                                     | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-case-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/case")                                                 |
| [remove](#remove-1)                                                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-remove.md "Cardigann#/definitions/RowsBlock/properties/remove")                                                       |
| [text](#text-1)                                                     | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-text.md "Cardigann#/definitions/RowsBlock/properties/text")                                                           |
| [filters](#filters-1)                                               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-filters-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/filters")                                           |
| [count](#count)                                                     | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/count")                                                                      |

### after

Row merging. Merge this many following elements into each row, for sites that use several elements per torrent (for example, collapsed rows).

`after`

* is optional

* Type: `integer` ([After](schema-definitions-rowsblock-properties-after.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-after.md "Cardigann#/definitions/RowsBlock/properties/after")

#### after Type

`integer` ([After](schema-definitions-rowsblock-properties-after.md))

### dateheaders

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`dateheaders`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/dateheaders")

#### dateheaders Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

### selector

Selector for the result rows. For JSON, $ refers to the root object.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-rowsblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-selector.md "Cardigann#/definitions/RowsBlock/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-rowsblock-properties-selector.md))

### attribute

JSON/XML only. Sub-element holding the torrents, when they are nested in each row.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-rowsblock-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-attribute.md "Cardigann#/definitions/RowsBlock/properties/attribute")

#### attribute Type

`string` ([Attribute](schema-definitions-rowsblock-properties-attribute.md))

### optional

Do not fail if the selector does not match.

`optional`

* is optional

* Type: `boolean` ([Optional](schema-definitions-rowsblock-properties-optional.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-optional.md "Cardigann#/definitions/RowsBlock/properties/optional")

#### optional Type

`boolean` ([Optional](schema-definitions-rowsblock-properties-optional.md))

### multiple

JSON/XML only. Each row holds several torrents (for example, one title with several releases).

`multiple`

* is optional

* Type: `boolean` ([Multiple](schema-definitions-rowsblock-properties-multiple.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-multiple.md "Cardigann#/definitions/RowsBlock/properties/multiple")

#### multiple Type

`boolean` ([Multiple](schema-definitions-rowsblock-properties-multiple.md))

### missingAttributeEqualsNoResults

JSON/XML only. Return zero results instead of an error when attribute is missing.

`missingAttributeEqualsNoResults`

* is optional

* Type: `boolean` ([Missing attribute equals no results](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md "Cardigann#/definitions/RowsBlock/properties/missingAttributeEqualsNoResults")

#### missingAttributeEqualsNoResults Type

`boolean` ([Missing attribute equals no results](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md))

### case

Selector: value pairs. The value of the first matching selector is used.

`case`

* is optional

* Type: `object` ([Case (RowsBlock)](schema-definitions-rowsblock-properties-case-rowsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-case-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/case")

#### case Type

`object` ([Case (RowsBlock)](schema-definitions-rowsblock-properties-case-rowsblock.md))

### remove

Selector for elements to remove from each row.

`remove`

* is optional

* Type: `string` ([Remove](schema-definitions-rowsblock-properties-remove.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-remove.md "Cardigann#/definitions/RowsBlock/properties/remove")

#### remove Type

`string` ([Remove](schema-definitions-rowsblock-properties-remove.md))

### text

Fixed value, or a template.

`text`

* is optional

* Type: merged type ([Text](schema-definitions-rowsblock-properties-text.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-text.md "Cardigann#/definitions/RowsBlock/properties/text")

#### text Type

merged type ([Text](schema-definitions-rowsblock-properties-text.md))

one (and only one) of

* [Text as string](schema-definitions-rowsblock-properties-text-oneof-text-as-string.md "check type definition")

* [Text as number](schema-definitions-rowsblock-properties-text-oneof-text-as-number.md "check type definition")

### filters

Row filters, for example, andmatch or strdump.

`filters`

* is optional

* Type: `object[]` ([RowFilterBlock](schema-definitions-rowfilterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-filters-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/filters")

#### filters Type

`object[]` ([RowFilterBlock](schema-definitions-rowfilterblock.md))

### count

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`count`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/count")

#### count Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Definitions group FieldsBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/FieldsBlock"}
```

| Property                                                                                                                                                                                                                                                                                                                                                                                    | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :----- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^_[A-Za-z0-9-_]*$`                                                                                                                                                                                                                                                                                                                                                                         | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^_\[A-Za-z0-9-_]*$")                                                                                                                                                                                                                                                                                                                                                                         |
| `^((title\|description)\\|(append))?$`                                                                                                                                                                                                                                                                                                                                                      | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((title\|description)\\\|(append))?$")                                                                                                                                                                                                                                                                                                                                                      |
| `^((category\|categorydesc)\\|(noappend\|append))?$`                                                                                                                                                                                                                                                                                                                                        | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((category\|categorydesc)\\\|(noappend\|append))?$")                                                                                                                                                                                                                                                                                                                                        |
| `^(download\|magnet\|infohash\|details\|comments\|title\|description\|category\|categorydesc\|size\|leechers\|seeders\|date\|files\|grabs\|downloadvolumefactor\|uploadvolumefactor\|minimumratio\|minimumseedtime\|imdb\|imdbid\|tmdbid\|rageid\|tvdbid\|tvmazeid\|traktid\|doubanid\|poster\|genre\|year\|author\|booktitle\|publisher\|album\|artist\|label\|track)(_([A-Za-z0-9_])*)?$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^(download\|magnet\|infohash\|details\|comments\|title\|description\|category\|categorydesc\|size\|leechers\|seeders\|date\|files\|grabs\|downloadvolumefactor\|uploadvolumefactor\|minimumratio\|minimumseedtime\|imdb\|imdbid\|tmdbid\|rageid\|tvdbid\|tvmazeid\|traktid\|doubanid\|poster\|genre\|year\|author\|booktitle\|publisher\|album\|artist\|label\|track)(_(\[A-Za-z0-9_])*)?$") |

### Pattern: `^_[A-Za-z0-9-_]*$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^_[A-Za-z0-9-_]*$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^_\[A-Za-z0-9-_]*$")

#### ^\_\[A-Za-z0-9-\_]\*$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

### Pattern: `^((title|description)\|(append))?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^((title|description)\|(append))?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((title|description)\\|(append))?$")

#### ^((title|description)\\|(append))?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

### Pattern: `^((category|categorydesc)\|(noappend|append))?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^((category|categorydesc)\|(noappend|append))?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((category|categorydesc)\\|(noappend|append))?$")

#### ^((category|categorydesc)\\|(noappend|append))?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

### Pattern: `^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_([A-Za-z0-9_])*)?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_([A-Za-z0-9_])*)?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_(\[A-Za-z0-9_])*)?$")

#### ^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(\_(\[A-Za-z0-9\_])\*)?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Definitions group DownloadBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/DownloadBlock"}
```

| Property                  | Type     | Required | Nullable       | Defined by                                                                                                                                                     |
| :------------------------ | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [method](#method-2)       | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-method.md "Cardigann#/definitions/DownloadBlock/properties/method")                 |
| [before](#before)         | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock.md "Cardigann#/definitions/DownloadBlock/properties/before")                                     |
| [selectors](#selectors-1) | `array`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-selectors.md "Cardigann#/definitions/DownloadBlock/properties/selectors")           |
| [infohash](#infohash)     | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-infohashblock.md "Cardigann#/definitions/DownloadBlock/properties/infohash")                                 |
| [headers](#headers-2)     | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-headers-downloadblock.md "Cardigann#/definitions/DownloadBlock/properties/headers") |

### method

HTTP method for the download: get or post. Default: get.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-downloadblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-method.md "Cardigann#/definitions/DownloadBlock/properties/method")

#### method Type

`string` ([Method](schema-definitions-downloadblock-properties-method.md))

### before

HTTP request sent before the download (for example, a 'thank you' page).

`before`

* is optional

* Type: `object` ([BeforeBlock](schema-definitions-beforeblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock.md "Cardigann#/definitions/DownloadBlock/properties/before")

#### before Type

`object` ([BeforeBlock](schema-definitions-beforeblock.md))

one (and only one) of

* [Fixed path](schema-definitions-beforeblock-oneof-fixed-path.md "check type definition")

* [Path from selector](schema-definitions-beforeblock-oneof-path-from-selector.md "check type definition")

### selectors

If set, the search result download URL is fetched and parsed as HTML, and the first selector gives the actual download URL. Later selectors are fallbacks.

`selectors`

* is optional

* Type: `array` ([Selectors](schema-definitions-downloadblock-properties-selectors.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-selectors.md "Cardigann#/definitions/DownloadBlock/properties/selectors")

#### selectors Type

`array` ([Selectors](schema-definitions-downloadblock-properties-selectors.md))

### infohash

Builds a magnet URI from an infohash and title. Public and semi-private indexers only: private sites usually require their own tracker and have DHT disabled.

`infohash`

* is optional

* Type: `object` ([InfoHashBlock](schema-definitions-infohashblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-infohashblock.md "Cardigann#/definitions/DownloadBlock/properties/infohash")

#### infohash Type

`object` ([InfoHashBlock](schema-definitions-infohashblock.md))

### headers

Extra HTTP headers sent with download requests.

`headers`

* is optional

* Type: `object` ([Headers (DownloadBlock)](schema-definitions-downloadblock-properties-headers-downloadblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-headers-downloadblock.md "Cardigann#/definitions/DownloadBlock/properties/headers")

#### headers Type

`object` ([Headers (DownloadBlock)](schema-definitions-downloadblock-properties-headers-downloadblock.md))

## Definitions group BeforeBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/BeforeBlock"}
```

| Property                            | Type     | Required | Nullable       | Defined by                                                                                                                                                  |
| :---------------------------------- | :------- | :------- | :------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path-5)                     | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-path.md "Cardigann#/definitions/BeforeBlock/properties/path")                      |
| [pathselector](#pathselector)       | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/BeforeBlock/properties/pathselector")                            |
| [method](#method-3)                 | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-method.md "Cardigann#/definitions/BeforeBlock/properties/method")                  |
| [inputs](#inputs-3)                 | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock.md "Cardigann#/definitions/BeforeBlock/properties/inputs")      |
| [queryseparator](#queryseparator-1) | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-query-separator.md "Cardigann#/definitions/BeforeBlock/properties/queryseparator") |

### path

Request target.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-beforeblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-path.md "Cardigann#/definitions/BeforeBlock/properties/path")

#### path Type

`string` ([Path](schema-definitions-beforeblock-properties-path.md))

### pathselector

Selector applied to the download page to get the download URL, hash or title.

`pathselector`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/BeforeBlock/properties/pathselector")

#### pathselector Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

### method

HTTP method: get or post.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-beforeblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-method.md "Cardigann#/definitions/BeforeBlock/properties/method")

#### method Type

`string` ([Method](schema-definitions-beforeblock-properties-method.md))

### inputs

HTTP arguments sent with the request, for example, id: "{{ .DownloadUri.Query.id }}".

`inputs`

* is optional

* Type: `object` ([Inputs (BeforeBlock)](schema-definitions-beforeblock-properties-inputs-beforeblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock.md "Cardigann#/definitions/BeforeBlock/properties/inputs")

#### inputs Type

`object` ([Inputs (BeforeBlock)](schema-definitions-beforeblock-properties-inputs-beforeblock.md))

### queryseparator

Query string separator. Default: &.

`queryseparator`

* is optional

* Type: `string` ([Query separator](schema-definitions-beforeblock-properties-query-separator.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-query-separator.md "Cardigann#/definitions/BeforeBlock/properties/queryseparator")

#### queryseparator Type

`string` ([Query separator](schema-definitions-beforeblock-properties-query-separator.md))

## Definitions group InfoHashBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/InfoHashBlock"}
```

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                                             |
| :-------------------------------------- | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [hash](#hash)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/hash")                                             |
| [title](#title)                         | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/title")                                            |
| [usebeforeresponse](#usebeforeresponse) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-infohashblock-properties-use-before-response.md "Cardigann#/definitions/InfoHashBlock/properties/usebeforeresponse") |

### hash

Selector applied to the download page to get the download URL, hash or title.

`hash`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/hash")

#### hash Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

### title

Selector applied to the download page to get the download URL, hash or title.

`title`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/InfoHashBlock/properties/title")

#### title Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

### usebeforeresponse

Take hash and title from the page returned by the before request instead of the search result download link. Default: false.

`usebeforeresponse`

* is optional

* Type: `boolean` ([Use before response](schema-definitions-infohashblock-properties-use-before-response.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-infohashblock-properties-use-before-response.md "Cardigann#/definitions/InfoHashBlock/properties/usebeforeresponse")

#### usebeforeresponse Type

`boolean` ([Use before response](schema-definitions-infohashblock-properties-use-before-response.md))

## Definitions group SelectorField

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/SelectorField"}
```

| Property                                  | Type      | Required | Nullable       | Defined by                                                                                                                                                             |
| :---------------------------------------- | :-------- | :------- | :------------- | :--------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [selector](#selector-5)                   | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-selector.md "Cardigann#/definitions/SelectorField/properties/selector")                     |
| [attribute](#attribute-2)                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-attribute.md "Cardigann#/definitions/SelectorField/properties/attribute")                   |
| [usebeforeresponse](#usebeforeresponse-1) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-use-before-response.md "Cardigann#/definitions/SelectorField/properties/usebeforeresponse") |
| [filters](#filters-2)                     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield-properties-filters-selectorfield.md "Cardigann#/definitions/SelectorField/properties/filters")         |

### selector

Selector for the value.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-selectorfield-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-selector.md "Cardigann#/definitions/SelectorField/properties/selector")

#### selector Type

`string` ([Selector](schema-definitions-selectorfield-properties-selector.md))

### attribute

Take the value of this attribute (for example, href) instead of the element text.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-selectorfield-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-attribute.md "Cardigann#/definitions/SelectorField/properties/attribute")

#### attribute Type

`string` ([Attribute](schema-definitions-selectorfield-properties-attribute.md))

### usebeforeresponse

Apply the selector to the page returned by the before request instead of the search result download link. Default: false.

`usebeforeresponse`

* is optional

* Type: `boolean` ([Use before response](schema-definitions-selectorfield-properties-use-before-response.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-use-before-response.md "Cardigann#/definitions/SelectorField/properties/usebeforeresponse")

#### usebeforeresponse Type

`boolean` ([Use before response](schema-definitions-selectorfield-properties-use-before-response.md))

### filters

Filters applied to the extracted value, in order.

`filters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield-properties-filters-selectorfield.md "Cardigann#/definitions/SelectorField/properties/filters")

#### filters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

## Definitions group RowFilterBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/RowFilterBlock"}
```

| Property        | Type     | Required | Nullable       | Defined by                                                                                                                                        |
| :-------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------ |
| [name](#name-2) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-name.md "Cardigann#/definitions/RowFilterBlock/properties/name")      |
| [args](#args)   | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-arguments.md "Cardigann#/definitions/RowFilterBlock/properties/args") |

### name

andmatch: only return rows containing all search words. strdump: log each row's HTML (debugging).

`name`

* is required

* Type: `string` ([Name](schema-definitions-rowfilterblock-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-name.md "Cardigann#/definitions/RowFilterBlock/properties/name")

#### name Type

`string` ([Name](schema-definitions-rowfilterblock-properties-name.md))

#### name Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value        | Explanation |
| :----------- | :---------- |
| `"andmatch"` |             |
| `"strdump"`  |             |

### args

Filter arguments. For andmatch, an optional maximum title length to compare, for sites that truncate names.

`args`

* is optional

* Type: merged type ([Arguments](schema-definitions-rowfilterblock-properties-arguments.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-arguments.md "Cardigann#/definitions/RowFilterBlock/properties/args")

#### args Type

merged type ([Arguments](schema-definitions-rowfilterblock-properties-arguments.md))

one (and only one) of

* [Arguments as array (RowFilterBlock)](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-array-rowfilterblock.md "check type definition")

* [Arguments as string](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-string.md "check type definition")

* [Arguments as integer](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-integer.md "check type definition")

## Definitions group FilterBlock

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/FilterBlock"}
```

| Property        | Type     | Required | Nullable       | Defined by                                                                                                                                  |
| :-------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------ |
| [name](#name-3) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-filterblock-properties-name.md "Cardigann#/definitions/FilterBlock/properties/name")      |
| [args](#args-1) | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-filterblock-properties-arguments.md "Cardigann#/definitions/FilterBlock/properties/args") |

### name

Filter name.

`name`

* is required

* Type: `string` ([Name](schema-definitions-filterblock-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-filterblock-properties-name.md "Cardigann#/definitions/FilterBlock/properties/name")

#### name Type

`string` ([Name](schema-definitions-filterblock-properties-name.md))

#### name Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value             | Explanation |
| :---------------- | :---------- |
| `"querystring"`   |             |
| `"timeparse"`     |             |
| `"dateparse"`     |             |
| `"regexp"`        |             |
| `"re_replace"`    |             |
| `"split"`         |             |
| `"replace"`       |             |
| `"trim"`          |             |
| `"prepend"`       |             |
| `"append"`        |             |
| `"tolower"`       |             |
| `"toupper"`       |             |
| `"urldecode"`     |             |
| `"urlencode"`     |             |
| `"htmldecode"`    |             |
| `"htmlencode"`    |             |
| `"timeago"`       |             |
| `"reltime"`       |             |
| `"fuzzytime"`     |             |
| `"validfilename"` |             |
| `"diacritics"`    |             |
| `"jsonjoinarray"` |             |
| `"hexdump"`       |             |
| `"strdump"`       |             |
| `"validate"`      |             |

### args

Filter arguments: a single value or a list, depending on the filter.

`args`

* is optional

* Type: merged type ([Arguments](schema-definitions-filterblock-properties-arguments.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-filterblock-properties-arguments.md "Cardigann#/definitions/FilterBlock/properties/args")

#### args Type

merged type ([Arguments](schema-definitions-filterblock-properties-arguments.md))

one (and only one) of

* [Arguments as array (FilterBlock)](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-array-filterblock.md "check type definition")

* [Arguments as string](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-string.md "check type definition")

* [Arguments as integer](schema-definitions-filterblock-properties-arguments-oneof-arguments-as-integer.md "check type definition")

## Definitions group IndexerCategories

Reference this group by using

```json
{"$ref":"Cardigann#/definitions/IndexerCategories"}
```

| Property | Type | Required | Nullable | Defined by |
| :------- | :--- | :------- | :------- | :--------- |
