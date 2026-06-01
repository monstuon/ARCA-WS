## ADDED Requirements

### Requirement: Linear service execution without executors
The library MUST execute WSAA and WSFE application flows through explicit linear service logic and MUST NOT depend on executor/orchestrator abstractions for request processing.

#### Scenario: WSFE authorization runs through linear application flow
- **WHEN** a consumer invokes voucher authorization
- **THEN** the application layer MUST execute a direct ordered flow (input validation, credential resolution, WSFE invocation, response mapping)
- **THEN** no executor component MUST be required to coordinate those stages

#### Scenario: WSAA credential issuance runs through linear application flow
- **WHEN** the system needs to issue or renew credentials through WSAA
- **THEN** the authentication service MUST execute the operation with direct service logic
- **THEN** no executor component MUST be required in the authentication path

### Requirement: Dependency registration excludes executor pattern
The library MUST remove executor-based registrations from dependency injection and MUST wire direct services as runtime entry points.

#### Scenario: Runtime wiring uses direct services
- **WHEN** the application configures service dependencies
- **THEN** dependency registration MUST expose direct WSAA/WSFE services for operations
- **THEN** registration MUST NOT include executor/orchestrator types as required runtime dependencies