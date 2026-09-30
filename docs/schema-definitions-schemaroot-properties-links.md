# Links Schema

```txt
Cardigann#/definitions/SchemaRoot/properties/links
```

Known site URLs. The first is the default. Each must end with /.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## links Type

`string[]` ([Link](schema-definitions-schemaroot-properties-links-link.md))

## links Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
