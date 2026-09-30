# Selector inputs Schema

```txt
Cardigann#/definitions/Login/properties/selectorinputs
```

Form login only. Input values taken from the login page with selectors, for example, a CSRF token hidden in JavaScript.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## selectorinputs Type

`object` ([Selector inputs](schema-definitions-login-properties-selector-inputs.md))

# selectorinputs Properties

| Property          | Type   | Required | Nullable       | Defined by                                                                                                                                                      |
| :---------------- | :----- | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^[A-Za-z0-9_]*$` | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/Login/properties/selectorinputs/patternProperties/^\[A-Za-z0-9_]*$") |

## Pattern: `^[A-Za-z0-9_]*$`

Extracts a value from a response using a CSS selector (HTML) or path (JSON/XML), then applies case, remove and filters.

`^[A-Za-z0-9_]*$`

* is optional

* Type: `object` ([SelectorBlock](schema-definitions-selectorblock.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock.md "Cardigann#/definitions/Login/properties/selectorinputs/patternProperties/^\[A-Za-z0-9_]*$")

### ^\[A-Za-z0-9\_]\*$ Type

`object` ([SelectorBlock](schema-definitions-selectorblock.md))

all of

* [Default requires optional](schema-definitions-selectorblock-allof-default-requires-optional.md "check type definition")
