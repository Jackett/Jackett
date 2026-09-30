# Case (SelectorBlock) Schema

```txt
Cardigann#/definitions/SelectorBlock/properties/case
```

Selector: value pairs. The value of the first matching selector is used. "\*" matches anything. Common for downloadvolumefactor and uploadvolumefactor.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## case Type

`object` ([Case (SelectorBlock)](schema-definitions-selectorblock-properties-case-selectorblock.md))

# case Properties

| Property | Type   | Required | Nullable       | Defined by                                                                                                                                                                                                   |
| :------- | :----- | :------- | :------------- | :----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^.*$`   | Merged | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value.md "Cardigann#/definitions/SelectorBlock/properties/case/patternProperties/^.*$") |

## Pattern: `^.*$`



`^.*$`

* is optional

* Type: merged type ([Case value](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value.md "Cardigann#/definitions/SelectorBlock/properties/case/patternProperties/^.*$")

### ^.\*$ Type

merged type ([Case value](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value.md))

one (and only one) of

* [Case value as string](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value-oneof-case-value-as-string.md "check type definition")

* [Case value as number](schema-definitions-selectorblock-properties-case-selectorblock-patternproperties-case-value-oneof-case-value-as-number.md "check type definition")
