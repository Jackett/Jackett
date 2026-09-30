# Category mappings Schema

```txt
Cardigann#/definitions/Caps/properties/categorymappings
```

List of tracker-to-Newznab category mappings. Use either this or categories.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## categorymappings Type

`object[]` ([CategoryMapping](schema-definitions-categorymapping.md))

## categorymappings Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
