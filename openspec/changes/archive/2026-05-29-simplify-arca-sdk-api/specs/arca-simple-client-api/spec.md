## ADDED Requirements

### Requirement: Single public entrypoint for ARCA operations
The library SHALL expose a single concrete public client named `ArcaClient` as the primary API entrypoint for WSAA + WSFEv1 operations.

#### Scenario: Consumer initializes the client directly
- **WHEN** a .NET consumer configures the SDK with valid ARCA settings and certificates
- **THEN** the consumer MUST be able to instantiate `ArcaClient` directly without composing multiple service abstractions
- **THEN** the call flow MUST be traceable from `ArcaClient` method to SOAP invocation in a short, explicit path

### Requirement: Core WSFEv1 operations are available on ArcaClient
`ArcaClient` MUST provide explicit async methods for the core invoicing flows: `AutorizarFacturaAsync`, `ConsultarComprobanteAsync`, `ObtenerUltimoComprobanteAsync`, `SolicitarCAEAAsync`, and `InformarCAEAAsync`.

#### Scenario: Consumer executes an invoicing operation through the main client
- **WHEN** a consumer invokes one of the required operation methods on `ArcaClient`
- **THEN** the library MUST execute the corresponding WSFEv1 operation using typed request/response DTOs
- **THEN** the consumer MUST NOT need to orchestrate lower-level internal components

### Requirement: Public API remains explicit and minimal
The public surface MUST prioritize concrete types and explicit methods over enterprise-style indirection not required by current runtime needs.

#### Scenario: Public API avoids unnecessary abstraction layers
- **WHEN** the SDK exposes an operation to consumers
- **THEN** that operation MUST be reachable through concrete public API members on `ArcaClient` or clearly related configuration models
- **THEN** the SDK MUST NOT require consumer-side factories, provider chains, or handler pipelines to perform standard flows
