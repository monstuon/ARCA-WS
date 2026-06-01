## ADDED Requirements

### Requirement: Unified error taxonomy
The library SHALL expose a unified error taxonomy separating validation errors, functional ARCA rejections, authentication failures, and technical infrastructure failures.

#### Scenario: Validation error classification
- **WHEN** input validation fails before remote invocation
- **THEN** the library returns a validation error classification with field-level diagnostics where available

#### Scenario: ARCA functional rejection classification
- **WHEN** WSFEv1 returns business rejection codes for a voucher request
- **THEN** the library returns a functional rejection classification preserving external codes and messages

### Requirement: Structured logging with correlation
The library MUST emit structured logs for outbound WSAA/WSFEv1 operations including correlation identifiers, operation name, duration, and outcome.

#### Scenario: Successful operation logging
- **WHEN** an outbound operation completes successfully
- **THEN** the library emits an informational structured log event with correlation id and latency

#### Scenario: Failed operation logging
- **WHEN** an outbound operation fails
- **THEN** the library emits an error structured log event with correlation id, failure classification, and retry metadata

### Requirement: Minimal operational metrics
The library SHALL expose metrics hooks or counters for request latency, error rate, and retry count per operation type.

#### Scenario: Metrics on successful request
- **WHEN** a WSAA or WSFEv1 request succeeds
- **THEN** the library increments success metrics and records request duration for the operation type

#### Scenario: Metrics on retried failure
- **WHEN** a request triggers retries and eventually fails
- **THEN** the library records retry count, failure classification, and final failed duration metrics
