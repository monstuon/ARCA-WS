## ADDED Requirements

### Requirement: Typed WSFEv1 operations API
The library SHALL expose strongly typed methods for WSFEv1 operations including last authorized voucher query, voucher authorization request, and parameter catalog queries.

#### Scenario: Last voucher query
- **WHEN** a consumer requests the last authorized voucher for a point-of-sale and voucher type
- **THEN** the library invokes WSFEv1 with valid authentication and returns a typed response with the last voucher number

#### Scenario: Parameter catalog query
- **WHEN** a consumer requests a WSFEv1 parameter catalog
- **THEN** the library invokes the corresponding WSFEv1 operation and returns normalized typed catalog items

### Requirement: Voucher authorization flow
The library SHALL implement voucher authorization against WSFEv1 and MUST return the authorization outcome with CAE data when approved.

#### Scenario: Successful voucher authorization
- **WHEN** a consumer submits a valid voucher authorization request
- **THEN** the library sends the request to WSFEv1 and returns approval data including CAE and CAE expiration date

#### Scenario: Rejected voucher authorization
- **WHEN** WSFEv1 rejects the voucher request
- **THEN** the library returns a typed functional error result containing rejection codes and descriptions

### Requirement: Request validation before WSFEv1 call
The library MUST validate required request fields and business-consistency invariants before invoking WSFEv1.

#### Scenario: Missing mandatory data
- **WHEN** a voucher authorization request omits mandatory fields
- **THEN** the library MUST fail fast with a typed validation error and MUST NOT call WSFEv1

#### Scenario: Inconsistent totals
- **WHEN** the voucher amounts are inconsistent with configured validation rules
- **THEN** the library MUST reject the request with a typed validation error and MUST NOT call WSFEv1
