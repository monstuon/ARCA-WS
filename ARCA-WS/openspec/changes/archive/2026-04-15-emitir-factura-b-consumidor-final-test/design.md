## Context

`AuthorizeVoucherAsync` is exposed in the public API, but the WSFE infrastructure client still returns a `NOT_IMPLEMENTED` placeholder and does not call `FECAESolicitar`. The sample consumer currently validates connectivity by querying the last authorized voucher only, which does not provide evidence that voucher authorization works with the configured certificate and CUIT in homologation.

This change introduces an executable homologation scenario (Factura B, consumidor final, ARS 1000) and completes the missing SOAP mapping path so operators can run one command and obtain CAE evidence.

## Goals / Non-Goals

**Goals:**
- Implement end-to-end WSFE voucher authorization against homologation using `FECAESolicitar`.
- Keep the existing public method signatures unchanged.
- Provide a deterministic sample payload for Factura B to consumidor final for ARS 1000.
- Surface clear operation output (approved/rejected, CAE, CAE expiration, voucher number).
- Preserve current resilience and typed error behavior.

**Non-Goals:**
- Expanding scope to all voucher types and edge cases beyond the target sample.
- Introducing a new domain model for tributes/VAT breakdown variants not needed by the scenario.
- Building a UI or orchestration layer beyond console sample execution.

## Decisions

1. Implement `FECAESolicitar` XML mapping inside `WsfeSoapClient`.
Rationale: This keeps transport concerns isolated in infrastructure, matching the existing architecture used by `FECompUltimoAutorizado`.
Alternative considered: Build XML in application service. Rejected because it mixes protocol details with orchestration logic.

2. Reuse `VoucherRequest` as the input contract and map it to one-detail request (`CantReg = 1`).
Rationale: Public API remains stable and tests can reuse existing validators.
Alternative considered: Add a scenario-specific DTO. Rejected to avoid unnecessary API surface growth.

3. Add a sample execution branch in `samples/PilotConsumer` to authorize the target voucher after fetching the last number.
Rationale: Sample already wires certificate/CUIT/environment and is the closest place for operator validation.
Alternative considered: Separate sample project. Rejected to avoid duplicated setup and maintenance overhead.

4. Parse WSFE response for both business errors (`Errors`) and observations while preserving functional/infrastructure exception mapping.
Rationale: Maintains consistent error semantics with existing service layer expectations.
Alternative considered: Return raw SOAP elements. Rejected because it leaks protocol details to consumers.

5. Add focused automated tests for SOAP request generation and response parsing paths.
Rationale: Most risk is in XML shape and parsing robustness, so tests should lock behavior there.
Alternative considered: Only integration tests with fake client. Rejected because they do not validate real SOAP mapping.

## Risks / Trade-offs

- [Risk] ARCA homologation can reject document data combinations not covered by current validator. -> Mitigation: Use known-safe consumer-final defaults in sample and surface explicit WSFE codes/messages.
- [Risk] Manual XML building may drift from WSFE schema over time. -> Mitigation: Keep mapping centralized and cover with request/response fixture tests.
- [Risk] Reusing current `VoucherRequest` may limit advanced fiscal fields in future. -> Mitigation: Keep this implementation minimal and add optional model extensions in a future change when required.
- [Risk] Homologation endpoint instability may produce transient failures during sample runs. -> Mitigation: Retain existing timeout/retry policies and clear operator logs.
