# TV search Schema

```txt
Cardigann#/definitions/Modes/properties/tv-search
```

Parameters supported for TV search. Only list those the tracker can search with.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## tv-search Type

`string[]` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

## tv-search Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
