# RowsBlock Schema

```txt
Cardigann#/definitions/RowsBlock
```

Selects the result rows. Each row is then parsed with the fields block.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## RowsBlock Type

`object` ([RowsBlock](schema-definitions-rowsblock.md))

# RowsBlock Properties

| Property                                                            | Type      | Required | Nullable       | Defined by                                                                                                                                                                                   |
| :------------------------------------------------------------------ | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [after](#after)                                                     | `integer` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-after.md "Cardigann#/definitions/RowsBlock/properties/after")                                                         |
| [dateheaders](#dateheaders)                                         | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/dateheaders")                                                                |
| [selector](#selector)                                               | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-selector.md "Cardigann#/definitions/RowsBlock/properties/selector")                                                   |
| [attribute](#attribute)                                             | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-attribute.md "Cardigann#/definitions/RowsBlock/properties/attribute")                                                 |
| [optional](#optional)                                               | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-optional.md "Cardigann#/definitions/RowsBlock/properties/optional")                                                   |
| [multiple](#multiple)                                               | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-multiple.md "Cardigann#/definitions/RowsBlock/properties/multiple")                                                   |
| [missingAttributeEqualsNoResults](#missingattributeequalsnoresults) | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md "Cardigann#/definitions/RowsBlock/properties/missingAttributeEqualsNoResults") |
| [case](#case)                                                       | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-case-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/case")                                                 |
| [remove](#remove)                                                   | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-remove.md "Cardigann#/definitions/RowsBlock/properties/remove")                                                       |
| [text](#text)                                                       | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-text.md "Cardigann#/definitions/RowsBlock/properties/text")                                                           |
| [filters](#filters)                                                 | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowsblock-properties-filters-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/filters")                                           |
| [count](#count)                                                     | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/count")                                                                      |

## after

Row merging. Merge this many following elements into each row, for sites that use several elements per torrent (e.g. collapsed rows).

`after`

* is optional

* Type: `integer` ([After](schema-definitions-rowsblock-properties-after.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-after.md "Cardigann#/definitions/RowsBlock/properties/after")

### after Type

`integer` ([After](schema-definitions-rowsblock-properties-after.md))

## dateheaders

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`dateheaders`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/dateheaders")

### dateheaders Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

## selector

Selector for the result rows. For JSON, $ refers to the root object.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-rowsblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-selector.md "Cardigann#/definitions/RowsBlock/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-rowsblock-properties-selector.md))

## attribute

JSON/XML only. Sub-element holding the torrents, when they are nested in each row.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-rowsblock-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-attribute.md "Cardigann#/definitions/RowsBlock/properties/attribute")

### attribute Type

`string` ([Attribute](schema-definitions-rowsblock-properties-attribute.md))

## optional

Do not fail if the selector does not match.

`optional`

* is optional

* Type: `boolean` ([Optional](schema-definitions-rowsblock-properties-optional.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-optional.md "Cardigann#/definitions/RowsBlock/properties/optional")

### optional Type

`boolean` ([Optional](schema-definitions-rowsblock-properties-optional.md))

## multiple

JSON/XML only. Each row holds several torrents (e.g. one title with several releases).

`multiple`

* is optional

* Type: `boolean` ([Multiple](schema-definitions-rowsblock-properties-multiple.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-multiple.md "Cardigann#/definitions/RowsBlock/properties/multiple")

### multiple Type

`boolean` ([Multiple](schema-definitions-rowsblock-properties-multiple.md))

## missingAttributeEqualsNoResults

JSON/XML only. Return zero results instead of an error when attribute is missing.

`missingAttributeEqualsNoResults`

* is optional

* Type: `boolean` ([Missing attribute equals no results](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md "Cardigann#/definitions/RowsBlock/properties/missingAttributeEqualsNoResults")

### missingAttributeEqualsNoResults Type

`boolean` ([Missing attribute equals no results](schema-definitions-rowsblock-properties-missing-attribute-equals-no-results.md))

## case

Selector: value pairs. The value of the first matching selector is used.

`case`

* is optional

* Type: `object` ([Case (RowsBlock)](schema-definitions-rowsblock-properties-case-rowsblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-case-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/case")

### case Type

`object` ([Case (RowsBlock)](schema-definitions-rowsblock-properties-case-rowsblock.md))

## remove

Selector for elements to remove from each row.

`remove`

* is optional

* Type: `string` ([Remove](schema-definitions-rowsblock-properties-remove.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-remove.md "Cardigann#/definitions/RowsBlock/properties/remove")

### remove Type

`string` ([Remove](schema-definitions-rowsblock-properties-remove.md))

## text

Fixed value, or a template.

`text`

* is optional

* Type: merged type ([Text](schema-definitions-rowsblock-properties-text.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-text.md "Cardigann#/definitions/RowsBlock/properties/text")

### text Type

merged type ([Text](schema-definitions-rowsblock-properties-text.md))

one (and only one) of

* [Text as string](schema-definitions-rowsblock-properties-text-oneof-text-as-string.md "check type definition")

* [Text as number](schema-definitions-rowsblock-properties-text-oneof-text-as-number.md "check type definition")

## filters

Row filters, e.g. andmatch or strdump.

`filters`

* is optional

* Type: `object[]` ([RowFilterBlock](schema-definitions-rowfilterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowsblock-properties-filters-rowsblock.md "Cardigann#/definitions/RowsBlock/properties/filters")

### filters Type

`object[]` ([RowFilterBlock](schema-definitions-rowfilterblock.md))

## count

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`count`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/RowsBlock/properties/count")

### count Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")
