# ErrorBlock Schema

```txt
Cardigann#/definitions/ErrorBlock
```

Selector that detects an error on a login or search response. If it matches, the request is treated as failed.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## ErrorBlock Type

`object` ([ErrorBlock](schema-definitions-errorblock.md))

# ErrorBlock Properties

| Property              | Type     | Required | Nullable       | Defined by                                                                                                                                   |
| :-------------------- | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path)         | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-errorblock-properties-path.md "Cardigann#/definitions/ErrorBlock/properties/path")         |
| [selector](#selector) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-errorblock-properties-selector.md "Cardigann#/definitions/ErrorBlock/properties/selector") |
| [message](#message)   | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/ErrorBlock/properties/message")                   |

## path

Only check responses from this path.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-errorblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-errorblock-properties-path.md "Cardigann#/definitions/ErrorBlock/properties/path")

### path Type

`string` ([Path](schema-definitions-errorblock-properties-path.md))

## selector

If this selector matches, the request failed.

`selector`

* is required

* Type: `string` ([Selector](schema-definitions-errorblock-properties-selector.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-errorblock-properties-selector.md "Cardigann#/definitions/ErrorBlock/properties/selector")

### selector Type

`string` ([Selector](schema-definitions-errorblock-properties-selector.md))

## message

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`message`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/ErrorBlock/properties/message")

### message Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")
