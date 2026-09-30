# Caps Schema

```txt
Cardigann#/definitions/Caps
```

Capabilities of the indexer: category mappings and supported Torznab search modes.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## Caps Type

`object` ([Caps](schema-definitions-caps.md))

one (and only one) of

* [Using categories](schema-definitions-caps-oneof-using-categories.md "check type definition")

* [Using categorymappings](schema-definitions-caps-oneof-using-categorymappings.md "check type definition")

# Caps Properties

| Property                                | Type      | Required | Nullable       | Defined by                                                                                                                                                  |
| :-------------------------------------- | :-------- | :------- | :------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [categories](#categories)               | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-categories-caps.md "Cardigann#/definitions/Caps/properties/categories")                   |
| [categorymappings](#categorymappings)   | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-category-mappings.md "Cardigann#/definitions/Caps/properties/categorymappings")           |
| [modes](#modes)                         | `object`  | Required | cannot be null | [Cardigann indexer definition](schema-definitions-modes.md "Cardigann#/definitions/Caps/properties/modes")                                                  |
| [allowrawsearch](#allowrawsearch)       | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-allow-raw-search.md "Cardigann#/definitions/Caps/properties/allowrawsearch")              |
| [allowtvsearchimdb](#allowtvsearchimdb) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md "Cardigann#/definitions/Caps/properties/allowtvsearchimdb") |

## categories

Simple mapping of tracker category ID to Newznab category. Use either this or categorymappings.

`categories`

* is optional

* Type: `object` ([Categories (Caps)](schema-definitions-caps-properties-categories-caps.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-categories-caps.md "Cardigann#/definitions/Caps/properties/categories")

### categories Type

`object` ([Categories (Caps)](schema-definitions-caps-properties-categories-caps.md))

## categorymappings

List of tracker-to-Newznab category mappings. Use either this or categories.

`categorymappings`

* is optional

* Type: `object[]` ([CategoryMapping](schema-definitions-categorymapping.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-category-mappings.md "Cardigann#/definitions/Caps/properties/categorymappings")

### categorymappings Type

`object[]` ([CategoryMapping](schema-definitions-categorymapping.md))

### categorymappings Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.

## modes

Torznab search modes and the query parameters the tracker supports for each. Most apps calling Jackett depend on these being correct.

`modes`

* is required

* Type: `object` ([Modes](schema-definitions-modes.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-modes.md "Cardigann#/definitions/Caps/properties/modes")

### modes Type

`object` ([Modes](schema-definitions-modes.md))

## allowrawsearch

Enable raw search. See Jackett issue 8246.

`allowrawsearch`

* is optional

* Type: `boolean` ([Allow raw search](schema-definitions-caps-properties-allow-raw-search.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-allow-raw-search.md "Cardigann#/definitions/Caps/properties/allowrawsearch")

### allowrawsearch Type

`boolean` ([Allow raw search](schema-definitions-caps-properties-allow-raw-search.md))

## allowtvsearchimdb

Allow imdbid in tv-search. See Jackett issue 16412.

`allowtvsearchimdb`

* is optional

* Type: `boolean` ([Allow TV search by IMDb ID](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md "Cardigann#/definitions/Caps/properties/allowtvsearchimdb")

### allowtvsearchimdb Type

`boolean` ([Allow TV search by IMDb ID](schema-definitions-caps-properties-allow-tv-search-by-imdb-id.md))
