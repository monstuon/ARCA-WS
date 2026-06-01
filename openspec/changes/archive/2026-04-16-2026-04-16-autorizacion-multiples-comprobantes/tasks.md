## 1. Dominio — Eliminar CantReg de VoucherRequest

- [x] 1.1 En `Domain/Wsfe/WsfeModels.cs`, eliminar el parámetro `int CantReg = 1` de `VoucherRequest`. Es el último parámetro del record; quitarlo implica también eliminar el comentario asociado `// Cantidad de registros en el lote`. A partir de este cambio, `CantReg` es un detalle de implementación del SOAP client.

## 2. Interfaz SOAP — Nueva Firma de AuthorizeVoucherAsync

- [x] 2.1 En `Infrastructure/Wsfe/IWsfeSoapClient.cs`, cambiar la firma de `AuthorizeVoucherAsync` de `(string endpoint, string token, string sign, long taxpayerId, VoucherRequest request, CancellationToken cancellationToken)` → `Task<VoucherAuthorizationResult>` a `(string endpoint, string token, string sign, long taxpayerId, IReadOnlyList<VoucherRequest> requests, CancellationToken cancellationToken)` → `Task<IReadOnlyList<VoucherAuthorizationResult>>`.

## 3. SOAP Client — Soporte de Lotes

- [x] 3.1 En `WsfeSoapClient.cs`, cambiar la firma del método público `AuthorizeVoucherAsync` para recibir `IReadOnlyList<VoucherRequest> requests` y devolver `Task<IReadOnlyList<VoucherAuthorizationResult>>`. Actualizar el cuerpo para llamar a `BuildAuthorizeVoucherEnvelope(token, sign, taxpayerId, requests)` y parsear con `ParseAuthorizeVoucherResponse(body)`.

- [x] 3.2 Refactorizar `BuildAuthorizeVoucherEnvelope` para recibir `IReadOnlyList<VoucherRequest> requests` en lugar de un único `VoucherRequest`. El método toma `requests[0]` para los campos de cabecera `PointOfSale` y `VoucherType`. Establece `CantReg = requests.Count`. Genera el bloque `<ar:FeDetReq>` concatenando el resultado de un nuevo método privado `BuildDetRequest(VoucherRequest)` por cada elemento del lote. El método `BuildDetRequest` extrae la lógica de construcción del bloque `<ar:FECAEDetRequest>...</ar:FECAEDetRequest>` que actualmente está inline en `BuildAuthorizeVoucherEnvelope` (campos individuales: Concepto, DocTipo, DocNro, CondicionIVAReceptorId, CbteDesde, CbteHasta, CbteFch, ImpTotal, ImpTotConc, ImpNeto, ImpOpEx, ImpTrib, ImpIVA, fechas de servicio, MonId, MonCotiz, CanMisMonExt, CbtesAsoc, Iva, Tributos, Opcionales). Cada llamada a `BuildDetRequest` recibe un único `VoucherRequest` y devuelve el string XML del bloque completo.

- [x] 3.3 Refactorizar `ParseAuthorizeVoucherResponse` para devolver `IReadOnlyList<VoucherAuthorizationResult>`:
  - **Paso 1 — SOAP Fault**: si existe un nodo `Fault`, lanzar `ArcaFunctionalException` igual que antes.
  - **Paso 2 — Resultado de cabecera**: buscar `FECAESolicitarResult`. Si no existe, lanzar `ArcaInfrastructureException`.
  - **Paso 3 — Errores de cabecera**: parsear todos los nodos `Err` hijos directos de `FECAESolicitarResult` (o de su hijo `Errors`). Si la lista de errores de cabecera no es vacía, lanzar `ArcaFunctionalException` con el primer error. Estos errores afectan al lote completo (credenciales, punto de venta, tipo inválido, etc.).
  - **Paso 4 — Resultados de detalle**: seleccionar todos los elementos `FECAEDetResponse` dentro de `FeDetResp`. Por cada uno, extraer:
    - `Resultado` → `bool Approved = result == "A"`.
    - `CAE` (null si vacío o ausente).
    - `CAEFchVto` → `DateOnly?` con formato `yyyyMMdd`.
    - Nodos `Obs` dentro del propio elemento de detalle → lista de `WsfeError`. Si `Approved` es false y la lista de observaciones está vacía, agregar un `WsfeError` sintético con código `"WSFE_REJECTED_{Resultado}"`.
  - Devolver la lista de `VoucherAuthorizationResult` en el mismo orden que los elementos `FECAEDetResponse` del XML. Si la lista de detalles parseados está vacía (respuesta sin `FeDetResp`), lanzar `ArcaInfrastructureException`.

## 4. Validador — Nuevo Método ValidateBatch

- [x] 4.1 En `Application/Wsfe/WsfeRequestValidator.cs`, agregar el método público `void ValidateBatch(IReadOnlyList<VoucherRequest> requests)`:
  - Si `requests` es nulo o su `Count == 0`, lanzar `ArcaValidationException("The voucher batch must contain at least one request.")`.
  - Si no todos los elementos tienen el mismo `PointOfSale`, lanzar `ArcaValidationException("All vouchers in a batch must share the same PointOfSale.")`.
  - Si no todos los elementos tienen el mismo `VoucherType`, lanzar `ArcaValidationException("All vouchers in a batch must share the same VoucherType.")`.
  - Llamar a `Validate(request)` por cada elemento del lote (las excepciones individuales se propagan sin atrapar).

## 5. Servicio — Nuevo Método AuthorizeVouchersAsync

- [x] 5.1 En `Application/Wsfe/Wsfev1InvoicingService.cs`, agregar `AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)` a la interfaz `IWsfev1InvoicingService`, con tipo de retorno `Task<IReadOnlyList<VoucherAuthorizationResult>>`.

- [x] 5.2 Implementar `AuthorizeVouchersAsync` en `Wsfev1InvoicingService`:
  - Llamar a `validator.ValidateBatch(requests)` antes de iniciar la operación.
  - Dentro de `ExecuteOperationAsync("wsfe.authorize-vouchers", ...)`:
    - Obtener credenciales y endpoint igual que el método de un solo comprobante.
    - Llamar a `wsfeSoapClient.AuthorizeVoucherAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, requests, ct)`.
    - Devolver la lista de resultados tal cual; no lanzar por rechazos parciales.
  - Usar el string `"wsfe.authorize-vouchers"` para métricas y logging.

- [x] 5.3 Cambiar `AuthorizeVoucherAsync(VoucherRequest request, ...)` en `Wsfev1InvoicingService` para que delegue a `AuthorizeVouchersAsync`:
  - Llamar a `validator.Validate(request)` (mantener la validación individual existente antes de delegar).
  - Dentro de `ExecuteOperationAsync("wsfe.authorize-voucher", ...)`: llamar a `wsfeSoapClient.AuthorizeVoucherAsync(endpoint, auth.Token, auth.Sign, options.TaxpayerId, [request], ct)`, extraer `results[0]`, y aplicar la misma lógica de throw `ArcaFunctionalException` cuando `!result.Approved` que existe hoy. No usar `AuthorizeVouchersAsync` directamente para no duplicar el `ExecuteOperationAsync`; mantener el cuerpo actual cambiando solo la llamada al SOAP client.

## 6. API Pública — Exponer AuthorizeVouchersAsync

- [x] 6.1 En `PublicApi/ArcaIntegrationClient.cs`, agregar el método:
  ```csharp
  public Task<IReadOnlyList<VoucherAuthorizationResult>> AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest> requests, string correlationId, CancellationToken cancellationToken = default)
      => invoicingService.AuthorizeVouchersAsync(requests, correlationId, cancellationToken);
  ```

## 7. Tests — WsfeSoapClientTests

- [x] 7.1 Actualizar todas las llamadas existentes a `sut.AuthorizeVoucherAsync(...)` en `WsfeSoapClientTests.cs` para que pasen `[request]` (array de un elemento) en lugar de `request` como quinto argumento. El tipo de retorno cambia a `IReadOnlyList<VoucherAuthorizationResult>`; reemplazar `result.Approved` / `result.Errors` / etc. por `result[0].Approved` / `result[0].Errors` / etc. en los asserts de cada test existente.

- [x] 7.2 Agregar test `AuthorizeVouchersAsync_ShouldGenerateTwoDetReqBlocks_AndParseBothResults_WhenTwoVouchersProvided`:
  - El handler devuelve un SOAP con dos `FECAEDetResponse` aprobados, con CAE `"11111111111111"` y `"22222222222222"` respectivamente.
  - El request contiene dos `VoucherRequest` iguales excepto por `VoucherNumberFrom`/`VoucherNumberTo` (p.ej., 40 y 41).
  - Asserts sobre el envelope: `<ar:CantReg>2</ar:CantReg>`; dos ocurrencias de `<ar:FECAEDetRequest>` en el body.
  - Asserts sobre los resultados: `results.Count == 2`; `results[0].Approved == true`; `results[0].Cae == "11111111111111"`; `results[1].Approved == true`; `results[1].Cae == "22222222222222"`.

- [x] 7.3 Agregar el método auxiliar privado `BuildApprovedBatchSoap()` en `WsfeSoapClientTests` que devuelve un SOAP con dos `FECAEDetResponse` aprobados, cada uno con su CAE (`"11111111111111"` y `"22222222222222"`) y `CAEFchVto` `"20260530"`. Estructura análoga a `BuildApprovedSoap()` pero con dos bloques de detalle.

## 8. Tests — WsfeRequestValidatorTests

- [x] 8.1 En los tests existentes de `WsfeRequestValidatorTests.cs` que construyen `VoucherRequest` con `CantReg:` explícito, eliminar ese argumento de la construcción del record. Verificar que no hay ninguna aparición de `CantReg` en el archivo después de la limpieza.

- [x] 8.2 Agregar test `ValidateBatch_ShouldThrow_WhenRequestsIsEmpty`: llama a `sut.ValidateBatch([])` y verifica que lanza `ArcaValidationException` con mensaje que contiene `"batch"`.

- [x] 8.3 Agregar test `ValidateBatch_ShouldThrow_WhenPointOfSalesDiffer`: construye dos `VoucherRequest` con `PointOfSale` distintos (1 y 2) y el mismo `VoucherType`. Verifica `ArcaValidationException` con mensaje que contiene `"PointOfSale"`.

- [x] 8.4 Agregar test `ValidateBatch_ShouldThrow_WhenVoucherTypesDiffer`: construye dos `VoucherRequest` con el mismo `PointOfSale` y distintos `VoucherType` (1 y 6). Verifica `ArcaValidationException` con mensaje que contiene `"VoucherType"`.

- [x] 8.5 Agregar test `ValidateBatch_ShouldPass_WhenTwoVouchersHaveSameHeaderFields`: construye dos `VoucherRequest` válidos con el mismo `PointOfSale` y `VoucherType`, números de comprobante distintos. Verifica que `sut.ValidateBatch(requests)` no lanza.

## 9. PilotConsumer — Eliminar CantReg y Agregar Escenario de Lote

- [x] 9.1 En `samples/PilotConsumer/Program.cs`, eliminar `CantReg: 2` de los escenarios 3 (RI-Productos) y 4 (RI-Servicios). El parámetro `CantReg` ya no existe en `VoucherRequest`.

- [x] 9.2 Agregar escenario nuevo **`CF-Lote-2`** inmediatamente antes del resumen final (`anyFailed ? ...`). El escenario llama a `client.AuthorizeVouchersAsync(requests, "cf-lote-2")` con dos `VoucherRequest` tipo Factura B (VoucherType=6), Consumidor Final (DocumentType=99, DocumentNumber=0), importes `NetAmount=826.45m, VatBreakdown=[VatItem(5, 826.45m, 173.55m)], VatTotal=173.55m, TotalAmount=1000m, CurrencyId="PES", CurrencyRate=1m, RecipientVatConditionId=5, Concept=1`. El primer comprobante usa el número siguiente al último autorizado de tipo 6; el segundo usa el número siguiente al primero (`lastNum + 1` y `lastNum + 2`). El escenario itera los resultados y loguea cada uno por separado, indicando número de comprobante, CAE y resultado. Marca `anyFailed = true` si algún resultado no está aprobado. No usa el helper `RunScenarioAsync` (que maneja un único resultado); implementa su propio bloque `try/catch` inline con el patrón `Console.WriteLine` / `logger.LogInformation` / `logger.LogError` existente en el archivo.
