# RowFilterBlock Schema

```txt
Cardigann#/definitions/RowFilterBlock
```

Filter applied to the result rows.

| Abstract            | Extensible | Status         | Identifiable | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :----------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | No           | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## RowFilterBlock Type

`object` ([RowFilterBlock](schema-definitions-rowfilterblock.md))

# RowFilterBlock Properties

| Property      | Type     | Required | Nullable       | Defined by                                                                                                                                        |
| :------------ | :------- | :------- | :------------- | :------------------------------------------------------------------------------------------------------------------------------------------------ |
| [name](#name) | `string` | Required | cannot be null | [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-name.md "Cardigann#/definitions/RowFilterBlock/properties/name")      |
| [args](#args) | Merged   | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-arguments.md "Cardigann#/definitions/RowFilterBlock/properties/args") |

## name

andmatch: only return rows containing all search words. strdump: log each row's HTML (debugging).

`name`

* is required

* Type: `string` ([Name](schema-definitions-rowfilterblock-properties-name.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-name.md "Cardigann#/definitions/RowFilterBlock/properties/name")

### name Type

`string` ([Name](schema-definitions-rowfilterblock-properties-name.md))

### name Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value        | Explanation |
| :----------- | :---------- |
| `"andmatch"` |             |
| `"strdump"`  |             |

## args

Filter arguments. For andmatch, an optional maximum title length to compare, for sites that truncate names.

`args`

* is optional

* Type: merged type ([Arguments](schema-definitions-rowfilterblock-properties-arguments.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-rowfilterblock-properties-arguments.md "Cardigann#/definitions/RowFilterBlock/properties/args")

### args Type

merged type ([Arguments](schema-definitions-rowfilterblock-properties-arguments.md))

one (and only one) of

* [Arguments as array (RowFilterBlock)](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-array-rowfilterblock.md "check type definition")

* [Arguments as string](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-string.md "check type definition")

* [Arguments as integer](schema-definitions-rowfilterblock-properties-arguments-oneof-arguments-as-integer.md "check type definition")
