# Headers (Login) Schema

```txt
Cardigann#/definitions/Login/properties/headers
```

Extra HTTP headers sent with login requests. If omitted, search headers are used.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                 |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :--------------------------------------------------------- |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Forbidden             | none                | [schema.json\*](../out/schema.json "open original schema") |

## headers Type

`object` ([Headers (Login)](schema-definitions-login-properties-headers-login.md))

# headers Properties

| Property          | Type    | Required | Nullable       | Defined by                                                                                                                                                                                                      |
| :---------------- | :------ | :------- | :------------- | :-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `^[A-Za-z0-9-]*$` | `array` | Optional | cannot be null | [Cardigann indexer definition](schema-definitions-login-properties-headers-login-patternproperties-headers-value-login.md "Cardigann#/definitions/Login/properties/headers/patternProperties/^\[A-Za-z0-9-]*$") |

## Pattern: `^[A-Za-z0-9-]*$`



`^[A-Za-z0-9-]*$`

* is optional

* Type: `string[]` ([Headers value item](schema-definitions-login-properties-headers-login-patternproperties-headers-value-login-headers-value-item.md))

* cannot be null

* defined in: [Cardigann indexer definition](schema-definitions-login-properties-headers-login-patternproperties-headers-value-login.md "Cardigann#/definitions/Login/properties/headers/patternProperties/^\[A-Za-z0-9-]*$")

### ^\[A-Za-z0-9-]\*$ Type

`string[]` ([Headers value item](schema-definitions-login-properties-headers-login-patternproperties-headers-value-login-headers-value-item.md))
