# Certificates Schema

```txt
Cardigann#/definitions/SchemaRoot/properties/certificates
```

SHA-1 fingerprints of untrusted HTTPS certificates (self-signed, expired, etc.) to accept anyway. Rarely needed.

| Abstract            | Extensible | Status         | Identifiable            | Custom Properties | Additional Properties | Access Restrictions | Defined In                                                                            |
| :------------------ | :--------- | :------------- | :---------------------- | :---------------- | :-------------------- | :------------------ | :------------------------------------------------------------------------------------ |
| Can be instantiated | No         | Unknown status | Unknown identifiability | Forbidden         | Allowed               | none                | [schema.json\*](../src/Jackett.Common/Definitions/schema.json "open original schema") |

## certificates Type

`string[]` ([Certificate fingerprint](schema-definitions-schemaroot-properties-certificates-certificate-fingerprint.md))

## certificates Constraints

**unique items**: all items in this array must be unique. Duplicates are not allowed.
