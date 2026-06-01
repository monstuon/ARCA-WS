## ADDED Requirements

### Requirement: Unified ARCA exception contract
The library SHALL expose a primary exception type named `ArcaException` for operational failures in authentication and invoicing flows.

#### Scenario: Operational failure raises unified exception
- **WHEN** an ARCA operation fails due to transport, SOAP, authentication, or functional processing errors
- **THEN** the library MUST raise `ArcaException` as the default exception contract for consumer handling

### Requirement: Exception metadata supports diagnosis and retry policies
`ArcaException` MUST include `Code`, `Message`, `IsRetryable`, and `InnerException` to preserve actionable diagnostics.

#### Scenario: Error information is surfaced with retry semantics
- **WHEN** the library maps an internal failure to `ArcaException`
- **THEN** `Code` and `Message` MUST identify the failure category and detail
- **THEN** `IsRetryable` MUST indicate whether automated retry is considered safe by the SDK
- **THEN** `InnerException` MUST preserve underlying technical context when available

### Requirement: Specialized exceptions are optional and value-driven
The SDK MUST keep specialized exception types to a minimal set and only when they add clear semantic value over `ArcaException`.

#### Scenario: No unnecessary exception hierarchy growth
- **WHEN** a new error case is introduced
- **THEN** the SDK MUST use `ArcaException` unless a specialized type materially improves consumer behavior or diagnostics
