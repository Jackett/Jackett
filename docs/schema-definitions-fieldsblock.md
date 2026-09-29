# FieldsBlock Schema

```txt
Cardigann#/definitions/FieldsBlock
```

Fields extracted from each result row. Names starting with \_ are working fields for use in templates via .Result.<name>.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## FieldsBlock Type

`object` ([FieldsBlock](schema-definitions-fieldsblock.md))

one (and only one) of

* [Category by ID](schema-definitions-fieldsblock-oneof-category-by-id.md "check type definition")

* [Category by description](schema-definitions-fieldsblock-oneof-category-by-description.md "check type definition")

# FieldsBlock Properties

| Property                                                                                                                                                                                                                                                                                                                                                                                    | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            |
| :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------ | :----- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^_[A-Za-z0-9-_]*$`                                                                                                                                                                                                                                                                                                                                                                         | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^_\[A-Za-z0-9-_]*$")                                                                                                                                                                                                                                                                                                                                                                         |
| `^((title\|description)\\|(append))?$`                                                                                                                                                                                                                                                                                                                                                      | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((title\|description)\\\|(append))?$")                                                                                                                                                                                                                                                                                                                                                      |
| `^((category\|categorydesc)\\|(noappend\|append))?$`                                                                                                                                                                                                                                                                                                                                        | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((category\|categorydesc)\\\|(noappend\|append))?$")                                                                                                                                                                                                                                                                                                                                        |
| `^(download\|magnet\|infohash\|details\|comments\|title\|description\|category\|categorydesc\|size\|leechers\|seeders\|date\|files\|grabs\|downloadvolumefactor\|uploadvolumefactor\|minimumratio\|minimumseedtime\|imdb\|imdbid\|tmdbid\|rageid\|tvdbid\|tvmazeid\|traktid\|doubanid\|poster\|genre\|year\|author\|booktitle\|publisher\|album\|artist\|label\|track)(_([A-Za-z0-9_])*)?$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^(download\|magnet\|infohash\|details\|comments\|title\|description\|category\|categorydesc\|size\|leechers\|seeders\|date\|files\|grabs\|downloadvolumefactor\|uploadvolumefactor\|minimumratio\|minimumseedtime\|imdb\|imdbid\|tmdbid\|rageid\|tvdbid\|tvmazeid\|traktid\|doubanid\|poster\|genre\|year\|author\|booktitle\|publisher\|album\|artist\|label\|track)(_(\[A-Za-z0-9_])*)?$") |

## Pattern: `^_[A-Za-z0-9-_]*$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^_[A-Za-z0-9-_]*$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^_\[A-Za-z0-9-_]*$")

### ^\_\[A-Za-z0-9-\_]\*$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Pattern: `^((title|description)\|(append))?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^((title|description)\|(append))?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((title|description)\\|(append))?$")

### ^((title|description)\\|(append))?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Pattern: `^((category|categorydesc)\|(noappend|append))?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^((category|categorydesc)\|(noappend|append))?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^((category|categorydesc)\\|(noappend|append))?$")

### ^((category|categorydesc)\\|(noappend|append))?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## Pattern: `^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_([A-Za-z0-9_])*)?$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_([A-Za-z0-9_])*)?$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/FieldsBlock/patternProperties/^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(_(\[A-Za-z0-9_])*)?$")

### ^(download|magnet|infohash|details|comments|title|description|category|categorydesc|size|leechers|seeders|date|files|grabs|downloadvolumefactor|uploadvolumefactor|minimumratio|minimumseedtime|imdb|imdbid|tmdbid|rageid|tvdbid|tvmazeid|traktid|doubanid|poster|genre|year|author|booktitle|publisher|album|artist|label|track)(\_(\[A-Za-z0-9\_])\*)?$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")
