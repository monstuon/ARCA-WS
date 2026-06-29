## ADDED Requirements

### Requirement: PersonaTaxData MUST include enriched datosGenerales fields
The system MUST map and return in `PersonaTaxData` the additional `datosGenerales` fields defined for this change from `getPersona_v2`, preserving all currently exposed fields.

#### Scenario: Existing fields remain available
- **WHEN** a consumer calls the persona query after the change
- **THEN** all previously available `PersonaTaxData` fields MUST keep their existing behavior
- **THEN** additional configured `datosGenerales` fields MUST be present when returned by ARCA

---

### Requirement: PersonaTaxData MUST map condition-specific taxpayer data
The system MUST populate condition-specific fields according to taxpayer type, differentiating at least Responsable Inscripto and Monotributista.

#### Scenario: Responsable Inscripto mapping
- **WHEN** ARCA indicates the taxpayer is Responsable Inscripto
- **THEN** `PersonaTaxData` MUST populate the fields defined for Responsable Inscripto
- **THEN** Monotributo-only fields MUST remain null or absent

#### Scenario: Monotributista mapping
- **WHEN** ARCA indicates the taxpayer is Monotributista
- **THEN** `PersonaTaxData` MUST populate the fields defined for Monotributista
- **THEN** Responsable Inscripto-only fields MUST remain null or absent

---

### Requirement: Mapping MUST be null-safe for missing optional data
The system MUST tolerate partial `datosGenerales` payloads without failing the operation.

#### Scenario: Optional node not provided by ARCA
- **WHEN** one or more optional `datosGenerales` fields are missing in SOAP response
- **THEN** the query MUST still succeed
- **THEN** missing values MUST be represented as null/empty according to the destination type