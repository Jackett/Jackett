# Inputs (Search) Schema

```txt
Cardigann#/definitions/Search/properties/inputs
```

HTTP arguments used by all paths. $raw is appended to the argument list without escaping (only variables are escaped).

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## inputs Type

`object` ([Inputs (Search)](schema-definitions-search-properties-inputs-search.md))

# inputs Properties

| Property                           | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                                     |
| :--------------------------------- | :----- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^((\$raw)\|[A-Za-z0-9.\-_[\]]*)$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value.md "Cardigann#/definitions/Search/properties/inputs/patternProperties/^((\\$raw)\|\[A-Za-z0-9.\\-_\[\\]]*)$") |

## Pattern: `^((\$raw)|[A-Za-z0-9.\-_[\]]*)$`



`^((\$raw)|[A-Za-z0-9.\-_[\]]*)$`

* is optional

* Type: merged type ([Inputs value](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value.md "Cardigann#/definitions/Search/properties/inputs/patternProperties/^((\\$raw)|\[A-Za-z0-9.\\-_\[\\]]*)$")

### ^((\\$raw)|\[A-Za-z0-9.\\-\_\[\\]]\*)$ Type

merged type ([Inputs value](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value.md))

one (and only one) of

* [Inputs value as number](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value-oneof-inputs-value-as-number.md "check type definition")

* [Inputs value as string](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value-oneof-inputs-value-as-string.md "check type definition")

* [Inputs value as boolean](schema-definitions-search-properties-inputs-search-patternproperties-inputs-value-oneof-inputs-value-as-boolean.md "check type definition")
