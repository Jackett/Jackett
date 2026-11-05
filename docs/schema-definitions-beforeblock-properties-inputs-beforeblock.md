# Inputs (BeforeBlock) Schema

```txt
Cardigann#/definitions/BeforeBlock/properties/inputs
```

HTTP arguments sent with the request, for example, id: "{{ .DownloadUri.Query.id }}".

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## inputs Type

`object` ([Inputs (BeforeBlock)](schema-definitions-beforeblock-properties-inputs-beforeblock.md))

# inputs Properties

| Property          | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                               |
| :---------------- | :----- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^[A-Za-z0-9_]*$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value.md "Cardigann#/definitions/BeforeBlock/properties/inputs/patternProperties/^\[A-Za-z0-9_]*$") |

## Pattern: `^[A-Za-z0-9_]*$`



`^[A-Za-z0-9_]*$`

* is optional

* Type: merged type ([Inputs value](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value.md "Cardigann#/definitions/BeforeBlock/properties/inputs/patternProperties/^\[A-Za-z0-9_]*$")

### ^\[A-Za-z0-9\_]\*$ Type

merged type ([Inputs value](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value.md))

one (and only one) of

* [Inputs value as integer](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value-oneof-inputs-value-as-integer.md "check type definition")

* [Inputs value as string](schema-definitions-beforeblock-properties-inputs-beforeblock-patternproperties-inputs-value-oneof-inputs-value-as-string.md "check type definition")
