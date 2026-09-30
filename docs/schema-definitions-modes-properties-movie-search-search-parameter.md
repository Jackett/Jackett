# Search parameter Schema

```txt
Cardigann#/definitions/Modes/properties/movie-search/items
```



| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../out/schema.json "open original schema") |

## items Type

`string` ([Search parameter](schema-definitions-modes-properties-movie-search-search-parameter.md))

## items Constraints

**enum**: the value of this property must be equal to one of the following values:

| Value        | Explanation |
| :----------- | :---------- |
| `"q"`        |             |
| `"imdbid"`   |             |
| `"tmdbid"`   |             |
| `"traktid"`  |             |
| `"doubanid"` |             |
| `"year"`     |             |
| `"genre"`    |             |
