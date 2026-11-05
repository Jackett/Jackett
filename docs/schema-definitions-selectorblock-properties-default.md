# Default Schema

```txt
Cardigann#/definitions/SelectorBlock/properties/default
```

Value used when an optional selector does not match. Templates are allowed, for example, "{{ .Result.title_default }}".

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## default Type

merged type ([Default](schema-definitions-selectorblock-properties-default.md))

one (and only one) of

* [Default as string](schema-definitions-selectorblock-properties-default-oneof-default-as-string.md "check type definition")

* [Default as number](schema-definitions-selectorblock-properties-default-oneof-default-as-number.md "check type definition")
