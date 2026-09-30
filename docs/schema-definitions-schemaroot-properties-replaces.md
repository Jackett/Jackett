# Replaces Schema

```txt
Cardigann#/definitions/SchemaRoot/properties/replaces
```

Old indexer IDs this definition replaces. Keeps existing configs working after an ID rename. Maintainer use only.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../out/schema.json "open original schema") |

## replaces Type

`string[]` ([Replaced ID](schema-definitions-schemaroot-properties-replaces-replaced-id.md))

## replaces Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
