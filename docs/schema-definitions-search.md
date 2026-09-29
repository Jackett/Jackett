# Search Schema

```txt
Cardigann#/definitions/Search
```

How to build search requests and parse torrent results from the response.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## Search Type

`object` ([Search](schema-definitions-search.md))

one (and only one) of

* [Multiple paths](schema-definitions-search-oneof-multiple-paths.md "check type definition")

* [Single path](schema-definitions-search-oneof-single-path.md "check type definition")

# Search Properties

| Property                                      | Type      | Required | Nullable       | Defined by                                                                                                                                                    |
| :-------------------------------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------ |
| [path](#path)                                 | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-path.md "Cardigann#/definitions/Search/properties/path")                                  |
| [paths](#paths)                               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-paths.md "Cardigann#/definitions/Search/properties/paths")                                |
| [allowEmptyInputs](#allowemptyinputs)         | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-allow-empty-inputs.md "Cardigann#/definitions/Search/properties/allowEmptyInputs")        |
| [inputs](#inputs)                             | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-inputs-search.md "Cardigann#/definitions/Search/properties/inputs")                       |
| [headers](#headers)                           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-headers-search.md "Cardigann#/definitions/Search/properties/headers")                     |
| [keywordsfilters](#keywordsfilters)           | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-keywords-filters.md "Cardigann#/definitions/Search/properties/keywordsfilters")           |
| [error](#error)                               | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-error-search.md "Cardigann#/definitions/Search/properties/error")                         |
| [preprocessingfilters](#preprocessingfilters) | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-preprocessing-filters.md "Cardigann#/definitions/Search/properties/preprocessingfilters") |
| [rows](#rows)                                 | `object`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock.md "Cardigann#/definitions/Search/properties/rows")                                               |
| [fields](#fields)                             | Merged    | Required | cannot be null | [Cardigann indexer definition](schema-definitions-fieldsblock.md "Cardigann#/definitions/Search/properties/fields")                                           |

## path

Single search path. Shorthand for paths with one entry.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-search-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-path.md "Cardigann#/definitions/Search/properties/path")

### path Type

`string` ([Path](schema-definitions-search-properties-path.md))

## paths

Search requests. Usually one. Some trackers need separate pages for e.g. porn or scene releases.

`paths`

* is optional

* Type: `object[]` ([SearchPathBlock](schema-definitions-searchpathblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-paths.md "Cardigann#/definitions/Search/properties/paths")

### paths Type

`object[]` ([SearchPathBlock](schema-definitions-searchpathblock.md))

## allowEmptyInputs

Send inputs whose value resolves to empty. By default such key/value pairs are dropped. Default: false.

`allowEmptyInputs`

* is optional

* Type: `boolean` ([Allow empty inputs](schema-definitions-search-properties-allow-empty-inputs.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-allow-empty-inputs.md "Cardigann#/definitions/Search/properties/allowEmptyInputs")

### allowEmptyInputs Type

`boolean` ([Allow empty inputs](schema-definitions-search-properties-allow-empty-inputs.md))

## inputs

HTTP arguments used by all paths. $raw is appended to the argument list without escaping (only variables are escaped).

`inputs`

* is optional

* Type: `object` ([Inputs (Search)](schema-definitions-search-properties-inputs-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-inputs-search.md "Cardigann#/definitions/Search/properties/inputs")

### inputs Type

`object` ([Inputs (Search)](schema-definitions-search-properties-inputs-search.md))

## headers

Extra HTTP headers sent with search requests.

`headers`

* is optional

* Type: `object` ([Headers (Search)](schema-definitions-search-properties-headers-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-headers-search.md "Cardigann#/definitions/Search/properties/headers")

### headers Type

`object` ([Headers (Search)](schema-definitions-search-properties-headers-search.md))

## keywordsfilters

Filters applied to the search keywords. The result is available as .Keywords.

`keywordsfilters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-keywords-filters.md "Cardigann#/definitions/Search/properties/keywordsfilters")

### keywordsfilters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

## error

Selectors checked on the search response. Same syntax as the login error block.

`error`

* is optional

* Type: `object[]` ([ErrorBlock](schema-definitions-errorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-error-search.md "Cardigann#/definitions/Search/properties/error")

### error Type

`object[]` ([ErrorBlock](schema-definitions-errorblock.md))

## preprocessingfilters

Filters applied to the raw response before parsing.

`preprocessingfilters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-preprocessing-filters.md "Cardigann#/definitions/Search/properties/preprocessingfilters")

### preprocessingfilters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))

## rows

Selects the result rows. Each row is then parsed with the fields block.

`rows`

* is required

* Type: `object` ([RowsBlock](schema-definitions-rowsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock.md "Cardigann#/definitions/Search/properties/rows")

### rows Type

`object` ([RowsBlock](schema-definitions-rowsblock.md))

## fields

Fields extracted from each result row. Names starting with \_ are working fields for use in templates via .Result.<name>.

`fields`

* is required

* Type: `object` ([FieldsBlock](schema-definitions-fieldsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-fieldsblock.md "Cardigann#/definitions/Search/properties/fields")

### fields Type

`object` ([FieldsBlock](schema-definitions-fieldsblock.md))

one (and only one) of

* [Category by ID](schema-definitions-fieldsblock-oneof-category-by-id.md "check type definition")

* [Category by description](schema-definitions-fieldsblock-oneof-category-by-description.md "check type definition")
