# Inputs (SearchPathBlock) Schema

```txt
Cardigann#/definitions/SearchPathBlock/properties/inputs
```

Extra HTTP arguments for this path only.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## inputs Type

`object` ([Inputs (SearchPathBlock)](schema-definitions-searchpathblock-properties-inputs-searchpathblock.md))

# inputs Properties

| Property                           | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                                                                |
| :--------------------------------- | :----- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^((\$raw)\|[A-Za-z0-9.\-_[\]]*)$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value.md "Cardigann#/definitions/SearchPathBlock/properties/inputs/patternProperties/^((\\$raw)\|\[A-Za-z0-9.\\-_\[\\]]*)$") |

## Pattern: `^((\$raw)|[A-Za-z0-9.\-_[\]]*)$`



`^((\$raw)|[A-Za-z0-9.\-_[\]]*)$`

* is optional

* Type: merged type ([Inputs value](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value.md "Cardigann#/definitions/SearchPathBlock/properties/inputs/patternProperties/^((\\$raw)|\[A-Za-z0-9.\\-_\[\\]]*)$")

### ^((\\$raw)|\[A-Za-z0-9.\\-\_\[\\]]\*)$ Type

merged type ([Inputs value](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value.md))

one (and only one) of

* [Inputs value as integer](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value-oneof-inputs-value-as-integer.md "check type definition")

* [Inputs value as string](schema-definitions-searchpathblock-properties-inputs-searchpathblock-patternproperties-inputs-value-oneof-inputs-value-as-string.md "check type definition")
