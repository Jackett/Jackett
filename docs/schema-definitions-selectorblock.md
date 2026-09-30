# SelectorBlock Schema

```txt
Cardigann#/definitions/SelectorBlock
```

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## SelectorBlock Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")

# SelectorBlock Properties

| Property                | Type      | Required | Nullable       | Defined by                                                                                                                                                     |
| :---------------------- | :-------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [selector](#selector)   | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-selector.md "Cardigann#/definitions/SelectorBlock/properties/selector")             |
| [attribute](#attribute) | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-attribute.md "Cardigann#/definitions/SelectorBlock/properties/attribute")           |
| [optional](#optional)   | `boolean` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-optional.md "Cardigann#/definitions/SelectorBlock/properties/optional")             |
| [default](#default)     | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-default.md "Cardigann#/definitions/SelectorBlock/properties/default")               |
| [case](#case)           | `object`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/case")       |
| [remove](#remove)       | `string`  | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-remove.md "Cardigann#/definitions/SelectorBlock/properties/remove")                 |
| [text](#text)           | Merged    | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-text.md "Cardigann#/definitions/SelectorBlock/properties/text")                     |
| [filters](#filters)     | `array`   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-filters-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/filters") |

## selector

CSS selector for HTML, or path for JSON/XML. :has(), :not() and :contains() are supported. In JSON, $ refers to the root object and a .. prefix reads from the row instead of the rows attribute subset.

`selector`

* is optional

* Type: `string` ([Selector](schema-definitions-selectorblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-selector.md "Cardigann#/definitions/SelectorBlock/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-selectorblock-properties-selector.md))

## attribute

Take the value of this attribute (for example, href) instead of the element text.

`attribute`

* is optional

* Type: `string` ([Attribute](schema-definitions-selectorblock-properties-attribute.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-attribute.md "Cardigann#/definitions/SelectorBlock/properties/attribute")

### attribute Type

`string` ([Attribute](schema-definitions-selectorblock-properties-attribute.md))

## optional

Do not fail if the selector does not match. Use with default.

`optional`

* is optional

* Type: `boolean` ([Optional](schema-definitions-selectorblock-properties-optional.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-optional.md "Cardigann#/definitions/SelectorBlock/properties/optional")

### optional Type

`boolean` ([Optional](schema-definitions-selectorblock-properties-optional.md))

## default

Value used when an optional selector does not match. Templates are allowed, for example, "{{ .Result.title_default }}".

`default`

* is optional

* Type: merged type ([Default](schema-definitions-selectorblock-properties-default.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-default.md "Cardigann#/definitions/SelectorBlock/properties/default")

### default Type

merged type ([Default](schema-definitions-selectorblock-properties-default.md))

one (and only one) of

* [Default as string](schema-definitions-selectorblock-properties-default-oneof-default-as-string.md "check type definition")

* [Default as number](schema-definitions-selectorblock-properties-default-oneof-default-as-number.md "check type definition")

## case

Selector: value pairs. The value of the first matching selector is used. "\*" matches anything. Common for downloadvolumefactor and uploadvolumefactor.

`case`

* is optional

* Type: `object` ([Case (SelectorBlock)](schema-definitions-selectorblock-properties-case-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/case")

### case Type

`object` ([Case (SelectorBlock)](schema-definitions-selectorblock-properties-case-selectorblock.md))

## remove

Selector for elements to remove before reading the text. Removed elements are gone for all later fields, so put fields using remove last.

`remove`

* is optional

* Type: `string` ([Remove](schema-definitions-selectorblock-properties-remove.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-remove.md "Cardigann#/definitions/SelectorBlock/properties/remove")

### remove Type

`string` ([Remove](schema-definitions-selectorblock-properties-remove.md))

## text

Fixed value, or a template. Used instead of selector, such as for minimumratio and minimumseedtime.

`text`

* is optional

* Type: merged type ([Text](schema-definitions-selectorblock-properties-text.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-text.md "Cardigann#/definitions/SelectorBlock/properties/text")

### text Type

merged type ([Text](schema-definitions-selectorblock-properties-text.md))

one (and only one) of

* [Text as string](schema-definitions-selectorblock-properties-text-oneof-text-as-string.md "check type definition")

* [Text as number](schema-definitions-selectorblock-properties-text-oneof-text-as-number.md "check type definition")

## filters

Filters applied to the extracted value, in order.

`filters`

* is optional

* Type: `object[]` ([FilterBlock](schema-definitions-filterblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-filters-selectorblock.md "Cardigann#/definitions/SelectorBlock/properties/filters")

### filters Type

`object[]` ([FilterBlock](schema-definitions-filterblock.md))
