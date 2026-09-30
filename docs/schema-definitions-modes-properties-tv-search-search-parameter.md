# Search parameter Schema

```txt
Cardigann#/definitions/Modes/properties/tv-search/items
```



| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## items Type

`string` ([Search parameter](schema-definitions-modes-properties-tv-search-search-parameter.md))

## items Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value        | Explanation |
| :----------- | :---------- |
| `"q"`        |             |
| `"season"`   |             |
| `"ep"`       |             |
| `"imdbid"`   |             |
| `"tvdbid"`   |             |
| `"tmdbid"`   |             |
| `"tvmazeid"` |             |
| `"traktid"`  |             |
| `"doubanid"` |             |
| `"year"`     |             |
| `"genre"`    |             |
