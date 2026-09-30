# SchemaRoot Schema

```txt
Cardigann#/definitions/SchemaRoot
```

Root of a Cardigann YAML indexer definition: header, caps, settings, login, search and download blocks.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## SchemaRoot Type

`object` ([SchemaRoot](schema-definitions-schemaroot.md))

# SchemaRoot Properties

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

## id

Internal name of the indexer. Must be unique. Usually the site name in lower case without special characters or spaces. Used in the Torznab, download and search URLs and in the indexer config file name.

`id`

* is required

* Type: `string` ([ID](schema-definitions-schemaroot-properties-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-id.md "Cardigann#/definitions/SchemaRoot/properties/id")

### id Type

`string` ([ID](schema-definitions-schemaroot-properties-id.md))

## replaces

Old indexer IDs this definition replaces. Keeps existing configs working after an ID rename. Maintainer use only.

`replaces`

* is optional

* Type: `string[]` ([Replaced ID](schema-definitions-schemaroot-properties-replaces-replaced-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-replaces.md "Cardigann#/definitions/SchemaRoot/properties/replaces")

### replaces Type

`string[]` ([Replaced ID](schema-definitions-schemaroot-properties-replaces-replaced-id.md))

### replaces Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## name

Display name of the tracker.

`name`

* is required

* Type: `string` ([Name](schema-definitions-schemaroot-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-name.md "Cardigann#/definitions/SchemaRoot/properties/name")

### name Type

`string` ([Name](schema-definitions-schemaroot-properties-name.md))

## description

Short description shown in the tooltip on the add-indexer page and in the config panel. Usually sourced from opentrackers.org when available.

`description`

* is required

* Type: `string` ([Description](schema-definitions-schemaroot-properties-description.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-description.md "Cardigann#/definitions/SchemaRoot/properties/description")

### description Type

`string` ([Description](schema-definitions-schemaroot-properties-description.md))

## language

Language code of the main language used on the tracker, usually from the site's <html lang> attribute.

`language`

* is required

* Type: `string` ([Language](schema-definitions-schemaroot-properties-language.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-language.md "Cardigann#/definitions/SchemaRoot/properties/language")

### language Type

`string` ([Language](schema-definitions-schemaroot-properties-language.md))

### language Constraints

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

## type

public: no registration required. semi-private: registration required but always open. private: invite or application needed.

`type`

* is required

* Type: `string` ([Type](schema-definitions-schemaroot-properties-type.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-type.md "Cardigann#/definitions/SchemaRoot/properties/type")

### type Type

`string` ([Type](schema-definitions-schemaroot-properties-type.md))

### type Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value            | Explanation |
| :--------------- | :---------- |
| `"public"`       |             |
| `"semi-private"` |             |
| `"private"`      |             |

## encoding

Character encoding of the site, usually from its <meta charset> tag.

`encoding`

* is required

* Type: `string` ([Encoding](schema-definitions-schemaroot-properties-encoding.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-encoding.md "Cardigann#/definitions/SchemaRoot/properties/encoding")

### encoding Type

`string` ([Encoding](schema-definitions-schemaroot-properties-encoding.md))

## followredirect

Automatically update the site URL when the site redirects to a different domain. Default: false.

`followredirect`

* is optional

* Type: `boolean` ([Follow redirect](schema-definitions-schemaroot-properties-follow-redirect.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-follow-redirect.md "Cardigann#/definitions/SchemaRoot/properties/followredirect")

### followredirect Type

`boolean` ([Follow redirect](schema-definitions-schemaroot-properties-follow-redirect.md))

## testlinktorrent

Pre-test the .torrent download when grabbing. Set to false for sites that reject two GET requests for the same .torrent in sequence. Indexers that support fallback downloading need true. Default: true.

`testlinktorrent`

* is optional

* Type: `boolean` ([Test link torrent](schema-definitions-schemaroot-properties-test-link-torrent.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-test-link-torrent.md "Cardigann#/definitions/SchemaRoot/properties/testlinktorrent")

### testlinktorrent Type

`boolean` ([Test link torrent](schema-definitions-schemaroot-properties-test-link-torrent.md))

## requestDelay

Seconds to wait between requests to the site. Use for sites that temporarily block clients that exceed a request rate.

`requestDelay`

* is optional

* Type: `number` ([Request delay](schema-definitions-schemaroot-properties-request-delay.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-request-delay.md "Cardigann#/definitions/SchemaRoot/properties/requestDelay")

### requestDelay Type

`number` ([Request delay](schema-definitions-schemaroot-properties-request-delay.md))

## links

Known site URLs. The first is the default. Each must end with /.

`links`

* is required

* Type: `string[]` ([Link](schema-definitions-schemaroot-properties-links-link.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-links.md "Cardigann#/definitions/SchemaRoot/properties/links")

### links Type

`string[]` ([Link](schema-definitions-schemaroot-properties-links-link.md))

### links Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## legacylinks

Old site URLs that no longer work. A configured legacy URL is replaced automatically with the first entry in links.

`legacylinks`

* is optional

* Type: `string[]` ([Legacy link](schema-definitions-schemaroot-properties-legacy-links-legacy-link.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-legacy-links.md "Cardigann#/definitions/SchemaRoot/properties/legacylinks")

### legacylinks Type

`string[]` ([Legacy link](schema-definitions-schemaroot-properties-legacy-links-legacy-link.md))

### legacylinks Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## certificates

SHA-1 fingerprints of untrusted HTTPS certificates (self-signed, expired, etc.) to accept anyway. Rarely needed.

`certificates`

* is optional

* Type: `string[]` ([Certificate fingerprint](schema-definitions-schemaroot-properties-certificates-certificate-fingerprint.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-certificates.md "Cardigann#/definitions/SchemaRoot/properties/certificates")

### certificates Type

`string[]` ([Certificate fingerprint](schema-definitions-schemaroot-properties-certificates-certificate-fingerprint.md))

### certificates Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## caps

Capabilities of the indexer: category mappings and supported Torznab search modes.

`caps`

* is required

* Type: `object` ([Caps](schema-definitions-caps.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps.md "Cardigann#/definitions/SchemaRoot/properties/caps")

### caps Type

`object` ([Caps](schema-definitions-caps.md))

one (and only one) of

* [Using categories](schema-definitions-caps-oneof-using-categories.md "check type definition")

* [Using categorymappings](schema-definitions-caps-oneof-using-categorymappings.md "check type definition")

## settings

Config options shown for the indexer. If omitted, username and password are used. Set to \[] for public trackers that need no settings.

`settings`

* is optional

* Type: `object[]` ([SettingsField](schema-definitions-settingsfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-schemaroot-properties-settings.md "Cardigann#/definitions/SchemaRoot/properties/settings")

### settings Type

`object[]` ([SettingsField](schema-definitions-settingsfield.md))

## login

How Jackett logs in to the tracker. Omit for sites that need no login.

`login`

* is optional

* Type: `object` ([Login](schema-definitions-login.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login.md "Cardigann#/definitions/SchemaRoot/properties/login")

### login Type

`object` ([Login](schema-definitions-login.md))

## search

How to build search requests and parse torrent results from the response.

`search`

* is required

* Type: `object` ([Search](schema-definitions-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search.md "Cardigann#/definitions/SchemaRoot/properties/search")

### search Type

`object` ([Search](schema-definitions-search.md))

one (and only one) of

* [Multiple paths](schema-definitions-search-oneof-multiple-paths.md "check type definition")

* [Single path](schema-definitions-search-oneof-single-path.md "check type definition")

## download

Needed only when the download link cannot be taken directly from the search results, the download must be a POST, or another page must be requested first.

`download`

* is optional

* Type: `object` ([DownloadBlock](schema-definitions-downloadblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock.md "Cardigann#/definitions/SchemaRoot/properties/download")

### download Type

`object` ([DownloadBlock](schema-definitions-downloadblock.md))
