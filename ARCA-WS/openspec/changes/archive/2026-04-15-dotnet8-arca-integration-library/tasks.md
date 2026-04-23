## 1. Solution and Project Setup

- [x] 1.1 Create .NET 8 class library project structure for ARCA integration with clear folders for Public API, Application, Domain, and Infrastructure
- [x] 1.2 Add required dependencies for SOAP transport, certificate handling, logging abstractions, and test projects
- [x] 1.3 Define strongly typed configuration models for endpoints, service names, certificate source, timeout, and retry settings
- [x] 1.4 Add CI-ready build and test configuration for the new library and associated test projects

## 2. WSAA Authentication Capability

- [x] 2.1 Implement TRA builder with configurable service identifier and timestamp tolerance
- [x] 2.2 Implement certificate provider abstraction supporting file-based and store-based resolution
- [x] 2.3 Implement WSAA client adapter for loginCms invocation and response mapping
- [x] 2.4 Implement Token/Sign cache with proactive renewal window and atomic refresh behavior
- [x] 2.5 Add unit and integration tests for successful login, authentication failures, cache hit, and renewal scenarios

## 3. WSFEv1 Invoicing Capability

- [x] 3.1 Implement typed request/response contracts for last voucher query, voucher authorization, and parameter catalog operations
- [x] 3.2 Implement pre-call validation rules for mandatory fields and amount consistency
- [x] 3.3 Implement WSFEv1 client adapter with authenticated invocation flow using WSAA credentials
- [x] 3.4 Implement result mapping for approved vouchers (including CAE data) and rejected vouchers (codes and messages)
- [x] 3.5 Add unit and integration tests for successful authorization, rejection handling, and validation short-circuit behavior

## 4. Resilience, Error Model, and Observability

- [x] 4.1 Implement bounded retry and timeout policies with retryable/non-retryable classification
- [x] 4.2 Implement unified error taxonomy for validation, functional rejection, authentication, and infrastructure failures
- [x] 4.3 Implement structured logging with correlation id, operation name, duration, and outcome
- [x] 4.4 Implement metrics hooks/counters for latency, error rate, and retry count per operation type
- [x] 4.5 Add tests verifying retry policy behavior, error classification consistency, and telemetry emission points

## 5. Consumer Integration and Rollout

- [x] 5.1 Package and publish initial library version for internal consumption (NuGet internal feed or project reference strategy)
- [x] 5.2 Integrate the library in a pilot consumer and validate parity against legacy behavior in homologation
- [x] 5.3 Document migration guide including configuration examples, common error handling, and rollback procedure
- [x] 5.4 Execute progressive migration plan for prioritized consumers with operational monitoring during stabilization
