# BeforeBlock Schema

```txt
Cardigann#/definitions/BeforeBlock
```

HTTP request sent before the download (for example, a 'thank you' page).

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## BeforeBlock Type

`object` ([BeforeBlock](schema-definitions-beforeblock.md))

one (and only one) of

* [Fixed path](schema-definitions-beforeblock-oneof-fixed-path.md "check type definition")

* [Path from selector](schema-definitions-beforeblock-oneof-path-from-selector.md "check type definition")

# BeforeBlock Properties

| Property                          | Type     | Required | Nullable       | Defined by                                                                                                                                                  |
| :-------------------------------- | :------- | :------- | :------------- | :---------------------------------------------------------------------------------------------------------------------------------------------------------- |
| [path](#path)                     | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-path.md "Cardigann#/definitions/BeforeBlock/properties/path")                      |
| [pathselector](#pathselector)     | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/BeforeBlock/properties/pathselector")                            |
| [method](#method)                 | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-method.md "Cardigann#/definitions/BeforeBlock/properties/method")                  |
| [inputs](#inputs)                 | `object` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock.md "Cardigann#/definitions/BeforeBlock/properties/inputs")      |
| [queryseparator](#queryseparator) | `string` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-query-separator.md "Cardigann#/definitions/BeforeBlock/properties/queryseparator") |

## path

Request target.

`path`

* is optional

* Type: `string` ([Path](schema-definitions-beforeblock-properties-path.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-path.md "Cardigann#/definitions/BeforeBlock/properties/path")

### path Type

`string` ([Path](schema-definitions-beforeblock-properties-path.md))

## pathselector

Selector applied to the download page to get the download URL, hash or title.

`pathselector`

* is optional

* Type: `object` ([SelectorField](schema-definitions-selectorfield.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorfield.md "Cardigann#/definitions/BeforeBlock/properties/pathselector")

### pathselector Type

`object` ([SelectorField](schema-definitions-selectorfield.md))

## method

HTTP method: get or post.

`method`

* is optional

* Type: `string` ([Method](schema-definitions-beforeblock-properties-method.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-method.md "Cardigann#/definitions/BeforeBlock/properties/method")

### method Type

`string` ([Method](schema-definitions-beforeblock-properties-method.md))

## inputs

HTTP arguments sent with the request, for example, id: "{{ .DownloadUri.Query.id }}".

`inputs`

* is optional

* Type: `object` ([Inputs (BeforeBlock)](schema-definitions-beforeblock-properties-inputs-beforeblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock.md "Cardigann#/definitions/BeforeBlock/properties/inputs")

### inputs Type

`object` ([Inputs (BeforeBlock)](schema-definitions-beforeblock-properties-inputs-beforeblock.md))

## queryseparator

Query string separator. Default: &.

`queryseparator`

* is optional

* Type: `string` ([Query separator](schema-definitions-beforeblock-properties-query-separator.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-query-separator.md "Cardigann#/definitions/BeforeBlock/properties/queryseparator")

### queryseparator Type

`string` ([Query separator](schema-definitions-beforeblock-properties-query-separator.md))
