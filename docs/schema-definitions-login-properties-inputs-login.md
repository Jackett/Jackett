# Inputs (Login) Schema

```txt
Cardigann#/definitions/Login/properties/inputs
```

Login parameters, e.g. username: "{{ .Config.username }}". Fixed values are allowed.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## inputs Type

`object` ([Inputs (Login)](schema-definitions-login-properties-inputs-login.md))

# inputs Properties

| Property | Type   | Required | Nullable       | Defined by                                                                                                                                                                                 |
| :------- | :----- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^.*$`   | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value.md "Cardigann#/definitions/Login/properties/inputs/patternProperties/^.*$") |

## Pattern: `^.*$`



`^.*$`

* is optional

* Type: merged type ([Inputs value](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value.md "Cardigann#/definitions/Login/properties/inputs/patternProperties/^.*$")

### ^.\*$ Type

merged type ([Inputs value](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value.md))

one (and only one) of

* [Inputs value as number](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value-oneof-inputs-value-as-number.md "check type definition")

* [Inputs value as string](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value-oneof-inputs-value-as-string.md "check type definition")

* [Inputs value as boolean](schema-definitions-login-properties-inputs-login-patternproperties-inputs-value-oneof-inputs-value-as-boolean.md "check type definition")
