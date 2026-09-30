# Modes Schema

```txt
Cardigann#/definitions/Modes
```

Torznab search modes and the query parameters the tracker supports for each. Most apps calling Jackett depend on these being correct.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## Modes Type

`object` ([Modes](schema-definitions-modes.md))

# Modes Properties

| Property                      | Type    | Required | Nullable       | Defined by                                                                                                                                 |
| :---------------------------- | :------ | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------- |
| [search](#search)             | `array` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-search-modes.md "Cardigann#/definitions/Modes/properties/search")       |
| [tv-search](#tv-search)       | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-tv-search.md "Cardigann#/definitions/Modes/properties/tv-search")       |
| [movie-search](#movie-search) | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-movie-search.md "Cardigann#/definitions/Modes/properties/movie-search") |
| [music-search](#music-search) | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-music-search.md "Cardigann#/definitions/Modes/properties/music-search") |
| [book-search](#book-search)   | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-modes-properties-book-search.md "Cardigann#/definitions/Modes/properties/book-search")   |

## search

Parameters for basic search. q is the minimum and is required.

`search`

* is required

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-search-modes-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-search-modes.md "Cardigann#/definitions/Modes/properties/search")

### search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-search-modes-search-parameter.md))

### search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## tv-search

Parameters supported for TV search. Only list those the tracker can search with.

`tv-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-tv-search.md "Cardigann#/definitions/Modes/properties/tv-search")

### tv-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

### tv-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## movie-search

Parameters supported for movie search. Only list those the tracker can search with.

`movie-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-movie-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-movie-search.md "Cardigann#/definitions/Modes/properties/movie-search")

### movie-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-movie-search-search-parameter.md))

### movie-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## music-search

Parameters supported for music search. Only list those the tracker can search with.

`music-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-music-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-music-search.md "Cardigann#/definitions/Modes/properties/music-search")

### music-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-music-search-search-parameter.md))

### music-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## book-search

Parameters supported for book search. Only list those the tracker can search with.

`book-search`

* is optional

* Type: `string[]` ([Search parameter](schema-definitions-modes-properties-book-search-search-parameter.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes-properties-book-search.md "Cardigann#/definitions/Modes/properties/book-search")

### book-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-book-search-search-parameter.md))

### book-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
