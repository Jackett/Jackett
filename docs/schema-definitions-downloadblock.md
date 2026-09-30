# DownloadBlock Schema

```txt
Cardigann#/definitions/DownloadBlock
```

Needed only when the download link cannot be taken directly from the search results, the download must be a POST, or another page must be requested first.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## DownloadBlock Type

`object` ([DownloadBlock](schema-definitions-downloadblock.md))

# DownloadBlock Properties

| Property                | Type     | Required | Nullable       | Defined by                                                                                                                                                     |
| :---------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [method](#method)       | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-method.md "Cardigann#/definitions/DownloadBlock/properties/method")                 |
| [before](#before)       | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock.md "Cardigann#/definitions/DownloadBlock/properties/before")                                     |
| [selectors](#selectors) | `array`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-selectors.md "Cardigann#/definitions/DownloadBlock/properties/selectors")           |
| [infohash](#infohash)   | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-infohashblock.md "Cardigann#/definitions/DownloadBlock/properties/infohash")                                 |
| [headers](#headers)     | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-downloadblock-properties-headers-downloadblock.md "Cardigann#/definitions/DownloadBlock/properties/headers") |

## method

HTTP method for the download: get or post. Default: get.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-downloadblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-method.md "Cardigann#/definitions/DownloadBlock/properties/method")

### method Type

`string` ([Method](schema-definitions-downloadblock-properties-method.md))

## before

HTTP request sent before the download (for example, a 'thank you' page).

`before`

* is optional

* Type: `object` ([BeforeBlock](schema-definitions-beforeblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock.md "Cardigann#/definitions/DownloadBlock/properties/before")

### before Type

`object` ([BeforeBlock](schema-definitions-beforeblock.md))

one (and only one) of

* [Fixed path](schema-definitions-beforeblock-oneof-fixed-path.md "check type definition")

* [Path from selector](schema-definitions-beforeblock-oneof-path-from-selector.md "check type definition")

## selectors

If set, the search result download URL is fetched and parsed as HTML, and the first selector gives the actual download URL. Later selectors are fallbacks.

`selectors`

* is optional

* Type: `array` ([Selectors](schema-definitions-downloadblock-properties-selectors.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-selectors.md "Cardigann#/definitions/DownloadBlock/properties/selectors")

### selectors Type

`array` ([Selectors](schema-definitions-downloadblock-properties-selectors.md))

## infohash

Builds a magnet URI from an infohash and title. Public and semi-private indexers only: private sites usually require their own tracker and have DHT disabled.

`infohash`

* is optional

* Type: `object` ([InfoHashBlock](schema-definitions-infohashblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-infohashblock.md "Cardigann#/definitions/DownloadBlock/properties/infohash")

### infohash Type

`object` ([InfoHashBlock](schema-definitions-infohashblock.md))

## headers

Extra HTTP headers sent with download requests.

`headers`

* is optional

* Type: `object` ([Headers (DownloadBlock)](schema-definitions-downloadblock-properties-headers-downloadblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-downloadblock-properties-headers-downloadblock.md "Cardigann#/definitions/DownloadBlock/properties/headers")

### headers Type

`object` ([Headers (DownloadBlock)](schema-definitions-downloadblock-properties-headers-downloadblock.md))
