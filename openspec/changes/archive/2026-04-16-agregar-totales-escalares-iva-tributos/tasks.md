## 1. Dominio — Nuevos Campos en VoucherRequest

- [x] 1.1 Agregar `decimal? VatTotal = null` a `VoucherRequest` en `Domain/Wsfe/WsfeModels.cs`, después de `TaxBreakdown`. Representa el ImpIVA de cabecera cuando el caller necesita informarlo explícitamente.
- [x] 1.2 Agregar `decimal? TaxTotal = null` a `VoucherRequest` en `Domain/Wsfe/WsfeModels.cs`, inmediatamente después de `VatTotal`. Representa el ImpTrib de cabecera cuando el caller necesita informarlo explícitamente.
- [x] 1.3 Agregar el record `public sealed record OptionalItem(int Id, string Value);` en `Domain/Wsfe/WsfeModels.cs`. Representa un ítem del array `Opcionales` del WSFE (e.g., Id=2911 → CBU del emisor para FCE MiPyME).
- [x] 1.4 Agregar `IReadOnlyList<OptionalItem>? Optionals = null` a `VoucherRequest` en `Domain/Wsfe/WsfeModels.cs`, inmediatamente después de `TaxTotal`. Permite informar datos opcionales requeridos por ciertos tipos de comprobante (obligatorio para FCE tipos 201, 202, 203, 206, 207, 208).

## 2. Validación — Precedencia y Validación Cruzada

- [x] 2.1 Actualizar la suma de `expected` en `WsfeRequestValidator.Validate` para usar `request.VatTotal ?? request.VatBreakdown?.Sum(v => v.Amount) ?? 0m` y `request.TaxTotal ?? request.TaxBreakdown?.Sum(t => t.Amount) ?? 0m` en lugar de los cálculos actuales de sólo array.
- [x] 2.2 Agregar test: `VatTotal` informado con `VatBreakdown` nula → `expected` usa `VatTotal` y la validación de totales pasa correctamente.
- [x] 2.3 Agregar test: `TaxTotal` informado con `TaxBreakdown` nula → `expected` usa `TaxTotal` y la validación de totales pasa correctamente.
- [x] 2.4 Agregar test: `VatTotal` informado con `VatBreakdown` también informada → la validación de totales usa el escalar sin error (no se valida coherencia entre ambos; eso lo hace AFIP).

## 3. SOAP Client — Uso del Escalar con Fallback al Array + Mapeo de Opcionales

- [x] 3.1 En `BuildAuthorizeVoucherEnvelope`, reemplazar `var vatTotal = request.VatBreakdown?.Sum(v => v.Amount) ?? 0m;` por `var vatTotal = request.VatTotal ?? request.VatBreakdown?.Sum(v => v.Amount) ?? 0m;`.
- [x] 3.2 Reemplazar `var taxTotal = request.TaxBreakdown?.Sum(t => t.Amount) ?? 0m;` por `var taxTotal = request.TaxTotal ?? request.TaxBreakdown?.Sum(t => t.Amount) ?? 0m;`.
- [x] 3.3 Generar el bloque `<ar:Opcionales>` iterando `Optionals`: por cada `OptionalItem` emitir `<ar:Opcional><ar:Id>{Id}</ar:Id><ar:Valor>{Value}</ar:Valor></ar:Opcional>`. Omitir el bloque si `Optionals` es nula o vacía. Ubicarlo en el envelope inmediatamente después del bloque `<ar:Tributos>` (antes del cierre de `</ar:FECAEDetRequest>`).
- [x] 3.4 Agregar test: `VatTotal = 210m` con `VatBreakdown` nula → `<ar:ImpIVA>210</ar:ImpIVA>` aparece en el envelope.
- [x] 3.5 Agregar test: `TaxTotal = 15m` con `TaxBreakdown` nula → `<ar:ImpTrib>15</ar:ImpTrib>` aparece en el envelope.
- [x] 3.6 Agregar test: `VatTotal` informado y `VatBreakdown` también informada → el envelope usa `VatTotal` en `<ar:ImpIVA>` y mantiene los nodos `<ar:AlicIva>` del array.
- [x] 3.7 Agregar test: `Optionals = [OptionalItem(2911, "0000003100012345678901")]` → envelope contiene `<ar:Opcionales>` con un nodo `<ar:Opcional>` con Id=2911 y el CBU informado.
- [x] 3.8 Agregar test: `Optionals = null` → envelope no contiene el bloque `<ar:Opcionales>`.

## 4. PilotConsumer — Agregar Escalares a Escenarios Existentes

Actualizar los escenarios que ya tienen `VatBreakdown` o `TaxBreakdown` para que también informen los escalares de cabecera correspondientes. No se agregan escenarios nuevos.

- [x] 4.1 En los escenarios que tienen `VatBreakdown`, agregar `VatTotal` con el mismo valor que la suma del array: escenario 1 (CF-Productos, VatTotal=173.55m), escenario 2 (CF-Servicios, VatTotal=173.55m), escenario 3 (RI-Productos, VatTotal=210m), escenario 4 (RI-Servicios, VatTotal=210m), escenario 7 (NC-B, VatTotal=173.55m), escenario 8 (USD-FC-B, VatTotal=17.36m), escenario 9 (IIBB-Percepcion, VatTotal=210m), escenario 10 (NC-Multi-Vinculo, VatTotal=173.55m).
- [x] 4.2 En el escenario 9 (IIBB-Percepcion), agregar además `TaxTotal = 15m` junto al `TaxBreakdown` ya existente.
- [x] 4.3 Actualizar el log de resultado de cada escenario modificado para imprimir `VatTotal` y, cuando corresponda, `TaxTotal`.

## 5. PilotConsumer — Escenarios FCE MiPyME (Factura de Crédito Electrónica)

Las FCE usan tipos de comprobante especiales (201/206) y sus NC asociadas (203/208). Requieren siempre CUIT del receptor (DocumentType=80 con CUIT de 11 dígitos), nunca consumidor final. Los escenarios 11 y 12 generan los comprobantes FCE; los escenarios 13 y 14 los anulan con NC FCE. Los dos últimos dependen del éxito de los dos primeros.

- [x] 5.1 **Escenario 11 — `FCE-A`**: Factura de Crédito Electrónica MiPyME A (VoucherType=201) a Responsable Inscripto (DocumentType=80, DocumentNumber=receiverCuit). Importes: NetAmount=1000, NonTaxableAmount=0, ExemptAmount=0, VatTotal=210, VatBreakdown=[VatItem(5,1000,210)], TotalAmount=1210, CurrencyId="PES", CurrencyRate=1, RecipientVatConditionId=1, Concept=1. Incluir `Optionals=[OptionalItem(2911, issuerCbu)]` donde `issuerCbu` es una constante de 22 dígitos de prueba para homologación. Capturar el número asignado en `fceAVoucherNum`.
- [x] 5.2 **Escenario 12 — `FCE-B`**: Factura de Crédito Electrónica MiPyME B (VoucherType=206) a Responsable Inscripto (DocumentType=80, DocumentNumber=receiverCuit). Mismos importes que FCE-A. Incluir `Optionals=[OptionalItem(2911, issuerCbu)]`. Capturar el número en `fceBVoucherNum`.
- [x] 5.3 **Escenario 13 — `NC-FCE-A`**: Nota de Crédito FCE A (VoucherType=203). Solo ejecutar si `fceAVoucherNum` no es null. AssociatedVouchers=[AssociatedVoucherInfo(201, pointOfSale, fceAVoucherNum.Value, issuerCuit)]. Mismos importes que la FCE-A. VatTotal=210, VatBreakdown=[VatItem(5,1000,210)], Optionals=[OptionalItem(2911, issuerCbu)], RecipientVatConditionId=1.
- [x] 5.4 **Escenario 14 — `NC-FCE-B`**: Nota de Crédito FCE B (VoucherType=208). Solo ejecutar si `fceBVoucherNum` no es null. AssociatedVouchers=[AssociatedVoucherInfo(206, pointOfSale, fceBVoucherNum.Value, issuerCuit)]. Mismos importes que la FCE-B. VatTotal=210, VatBreakdown=[VatItem(5,1000,210)], Optionals=[OptionalItem(2911, issuerCbu)], RecipientVatConditionId=1.
- [x] 5.5 Agregar la constante `issuerCbu` (string de 22 dígitos) junto a las otras constantes de configuración al inicio del `Program.cs`. Usada en todos los escenarios FCE como valor del opcional 2911.
- [x] 5.6 En los escenarios 13 y 14, si el comprobante padre no fue autorizado, loguear el motivo de omisión con `logger.LogWarning` y marcar `anyFailed = true`, igual que el patrón de NC-B y NC-Multi-Vinculo existentes.
