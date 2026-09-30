# Name Schema

```txt
Cardigann#/definitions/RowFilterBlock/properties/name
```

andmatch: only return rows containing all search words. strdump: log each row's HTML (debugging).

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## name Type

`string` ([Name](schema-definitions-rowfilterblock-properties-name.md))

## name Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value        | Explanation |
| :----------- | :---------- |
| `"andmatch"` |             |
| `"strdump"`  |             |
