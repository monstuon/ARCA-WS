## Por qué

El WSFE de ARCA/AFIP soporta autorizar un lote de comprobantes en una única llamada SOAP (`FECAESolicitar`). El cabezal del lote (`FeCabReq`) lleva `CantReg` (cantidad de registros), `PtoVta` y `CbteTipo`, que son compartidos por todos los comprobantes del lote. Cada comprobante individual ocupa su propio bloque `<FECAEDetRequest>` dentro de `<FeDetReq>`.

La implementación actual solo admite un único `<FECAEDetRequest>` por llamada. El campo `CantReg` está expuesto como parámetro del caller en `VoucherRequest`, lo que significa que el caller puede declarar `CantReg=2` pero enviar un solo bloque de detalle, generando una inconsistencia que AFIP rechazará con error técnico.

Los escenarios que motivan el cambio son:

- **Eficiencia operativa**: emitir múltiples facturas (por ejemplo, dos líneas de un mismo punto de venta y tipo de comprobante) en un solo round-trip HTTP, reduciendo latencia total y consumo de tokens WSAA.
- **Coherencia con el protocolo WSFE**: `CantReg` es un campo del cabezal del lote, no un dato del comprobante individual. Exponerlo en `VoucherRequest` da al caller control sobre un campo que solo tiene sentido como derivación del tamaño del lote, lo que abre la puerta a inconsistencias irrecuperables en tiempo de integración.
- **Modelo correcto**: el tamaño del lote debe ser computado automáticamente por la librería como la cantidad de `VoucherRequest` enviados.

## Qué Cambia

- **Dominio** (`WsfeModels.cs`): eliminar el parámetro `CantReg` de `VoucherRequest`. A partir de este cambio, `CantReg` es un detalle de implementación calculado como `requests.Count` en el SOAP client; ningún caller necesita informarlo. Es un breaking change mínimo: los callers que no usaban el campo siguen compilando con `default`; los que lo usaban deben eliminarlo.

- **Interfaz SOAP** (`IWsfeSoapClient`): cambiar la firma de `AuthorizeVoucherAsync` para recibir `IReadOnlyList<VoucherRequest>` en lugar de un único `VoucherRequest`, y devolver `Task<IReadOnlyList<VoucherAuthorizationResult>>`. Un lote de un solo elemento equivale al comportamiento anterior.

- **SOAP client** (`WsfeSoapClient`): refactorizar `BuildAuthorizeVoucherEnvelope` para:
  1. Tomar el `PointOfSale` y `VoucherType` del primer elemento del lote (ya validados como homogéneos antes de llegar al SOAP client).
  2. Calcular `CantReg = requests.Count`.
  3. Generar un bloque `<ar:FECAEDetRequest>` por cada `VoucherRequest` del lote, concatenados dentro de `<ar:FeDetReq>`.
  Refactorizar `ParseAuthorizeVoucherResponse` para:
  1. Parsear errores de cabecera (`Err` en raíz de `FECAESolicitarResult`) y lanzar `ArcaFunctionalException` si los hay, ya que indican fallo completo del lote (credenciales, punto de venta inválido, etc.).
  2. Parsear todos los elementos `FECAEDetResponse` (uno por comprobante enviado), cada uno con su propio `Resultado`, `CAE`, `CAEFchVto` y lista de `Obs` (observaciones individuales).
  3. Devolver `IReadOnlyList<VoucherAuthorizationResult>` con un elemento por comprobante.

- **Validador** (`WsfeRequestValidator`): agregar método `ValidateBatch(IReadOnlyList<VoucherRequest>)` que:
  1. Lanza si la lista está vacía.
  2. Lanza si no todos los comprobantes tienen el mismo `PointOfSale`.
  3. Lanza si no todos los comprobantes tienen el mismo `VoucherType`.
  4. Valida cada comprobante individualmente con el `Validate` existente.

- **Servicio** (`IWsfev1InvoicingService` y `Wsfev1InvoicingService`): agregar `AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest>, string, CancellationToken)` que devuelve `Task<IReadOnlyList<VoucherAuthorizationResult>>`. A diferencia del método de un solo comprobante, **no lanza** `ArcaFunctionalException` ante rechazos parciales; devuelve todos los resultados y deja que el caller evalúe cada uno. El método original `AuthorizeVoucherAsync(VoucherRequest)` delega internamente a `AuthorizeVouchersAsync` y extrae el único resultado; mantiene el comportamiento de lanzar si el resultado no fue aprobado.

- **API pública** (`ArcaIntegrationClient`): exponer `AuthorizeVouchersAsync` pasando la llamada al servicio. El método `AuthorizeVoucherAsync(VoucherRequest)` existente no cambia de firma.

- **Tests unitarios**: actualizar `WsfeSoapClientTests.cs` para que todas las llamadas existentes a `AuthorizeVoucherAsync` pasen `[request]` en lugar de `request`; agregar un test de lote de dos comprobantes. Actualizar `WsfeRequestValidatorTests.cs` para remover usos de `CantReg` y agregar los tests de `ValidateBatch`.

- **PilotConsumer**: eliminar `CantReg: 2` de los escenarios 3 y 4 (RI-Productos y RI-Servicios); agregar un escenario nuevo que ejercite `AuthorizeVouchersAsync` con dos comprobantes Factura B (Consumidor Final) en un único lote.

## Capacidades

### Capacidades Modificadas

- `arca-wsfev1-invoicing`: `AuthorizeVoucherAsync(VoucherRequest)` mantiene su firma pública y comportamiento (lanza si rechazado). La firma del `IWsfeSoapClient` interno cambia: recibe lista, devuelve lista.

### Capacidades Nuevas

- `arca-wsfev1-invoicing`: nuevo `AuthorizeVouchersAsync(IReadOnlyList<VoucherRequest>)` en `IWsfev1InvoicingService` y `ArcaIntegrationClient`. Permite enviar un lote de comprobantes del mismo `PointOfSale` y `VoucherType` en una sola llamada SOAP; devuelve un resultado por comprobante.

## Impacto

- **Archivos afectados**:
  - `Domain/Wsfe/WsfeModels.cs` — eliminar `CantReg` de `VoucherRequest`
  - `Infrastructure/Wsfe/IWsfeSoapClient.cs` — nueva firma de `AuthorizeVoucherAsync`
  - `Infrastructure/Wsfe/WsfeSoapClient.cs` — `BuildAuthorizeVoucherEnvelope` y `ParseAuthorizeVoucherResponse` para soportar lotes
  - `Application/Wsfe/WsfeRequestValidator.cs` — nuevo método `ValidateBatch`
  - `Application/Wsfe/Wsfev1InvoicingService.cs` — nuevo método `AuthorizeVouchersAsync`; `AuthorizeVoucherAsync` delega al batch
  - `PublicApi/ArcaIntegrationClient.cs` — nuevo `AuthorizeVouchersAsync`
  - `tests/ARCA-WS.Tests/Wsfe/WsfeSoapClientTests.cs` — actualizar calls existentes; agregar test de lote
  - `tests/ARCA-WS.Tests/Wsfe/WsfeRequestValidatorTests.cs` — remover `CantReg` en construcciones; agregar tests de `ValidateBatch`
  - `samples/PilotConsumer/Program.cs` — eliminar `CantReg: 2`; agregar escenario de lote

- **Impacto en API pública**: breaking change mínimo solo en `VoucherRequest` (eliminación de `CantReg`). Los callers que omitían el parámetro (usaban el default `CantReg = 1`) no necesitan ningún cambio. Los callers que lo seteaban explícitamente deben eliminarlo. La firma de `ArcaIntegrationClient.AuthorizeVoucherAsync` no cambia. Se agrega `AuthorizeVouchersAsync` como nuevo método aditivo.

- **Impacto operativo**: ninguno. El comportamiento de los lotes de un solo comprobante es funcionalmente idéntico al actual una vez corregido el `CantReg`.
