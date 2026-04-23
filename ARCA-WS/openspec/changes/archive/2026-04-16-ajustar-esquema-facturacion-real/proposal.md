## Por qué

La librería cubre los casos de homologación básicos pero no puede emitir comprobantes en varios escenarios reales habituales:

- **Multifactura / CantReg**: el campo `CantReg` se hardcodea a `1` en el envelope SOAP aunque el modelo ya lo expone. Sin mapearlo correctamente no se puede informar el tamaño real del lote.
- **Vínculos múltiples en NC**: `AssociatedVouchers` ya es una colección en el dominio, pero el SOAP client y el validador aún tratan el vínculo como un objeto singular. Una Nota de Crédito que anula tres facturas de distintos puntos de venta no puede expresarse.
- **Conceptos No Gravados — ImpTotConc**: el envelope SOAP emite siempre `<ar:ImpTotConc>0</ar:ImpTotConc>` ignorando `NonTaxableAmount`. Propinas, tasas de servicio o conceptos exentos de IVA pero gravados por otro tributo quedan sin informar y el total declarado no cierra con AFIP.
- **Desglose de IVA y Tributos**: el SOAP client genera un único nodo `<ar:AlicIva>` hardcodeado al 21% partiendo del escalar `VatAmount`. No puede representar una factura con bases imponibles a 10,5 % y 21 % simultáneamente ni incluir tributos provinciales (IIBB, percepciones). Los campos `VatBreakdown` y `TaxBreakdown` ya existen en el modelo pero no se mapean.
- **Moneda extranjera — CanMisMonExt**: el campo `SameCurrencyQuantity` existe en el modelo pero no se incluye en el envelope. AFIP lo requiere en ciertos escenarios de validación cruzada de tipo de cambio.
- **Rotura de contrato entre modelo y capa de aplicación**: `VoucherRequest` ya no tiene los escalares `VatAmount` y `TaxAmount`, pero el validador y el SOAP client aún los referencian, haciendo que el código no compile con el modelo actualizado.

Sin estos cambios la librería no puede cubrir ni el 30 % de los casos reales de una empresa que facture productos y servicios, tenga múltiples alícuotas de IVA y opere con percepciones provinciales.

## Qué Cambia

- **Dominio** (`WsfeModels.cs`): reemplazar los escalares `VatAmount` y `TaxAmount` por los arrays `VatBreakdown` y `TaxBreakdown`; exponer `NonTaxableAmount` en lugar del `ImpTotConc` hardcodeado; cambiar `AssociatedVoucher` (singular) a `AssociatedVouchers` (colección); agregar `CantReg` y `SameCurrencyQuantity`. *(Los campos ya están presentes en el modelo; este ítem confirma el contrato definitivo.)*
- **Validación** (`WsfeRequestValidator.cs`): actualizar la suma de totales para usar `VatBreakdown.Sum(Amount)` y `TaxBreakdown.Sum(Amount)` en lugar de escalares; ajustar la validación de NC para iterar `AssociatedVouchers`; agregar validaciones de consistencia para `VatBreakdown`, `TaxBreakdown` y `NonTaxableAmount`.
- **SOAP client** (`WsfeSoapClient.cs`): mapear `CantReg` al encabezado del lote; mapear `NonTaxableAmount` a `<ar:ImpTotConc>`; generar múltiples nodos `<ar:AlicIva>` desde `VatBreakdown`; generar `<ar:Tributos>` desde `TaxBreakdown`; calcular `ImpIVA` e `ImpTrib` como suma de los arrays; mapear `AssociatedVouchers` como múltiples `<ar:CbteAsoc>`; incluir `<ar:CanMisMonExt>` cuando `SameCurrencyQuantity` está presente.
- **Tests unitarios**: migrar constructores de `VoucherRequest` del contrato viejo al nuevo; agregar tests para IVA mixto, TaxBreakdown, múltiples `CbteAsoc`, `ImpTotConc` y `CanMisMonExt`.
- **PilotConsumer**: agregar escenario con percepción de Ingresos Brutos CABA (TaxBreakdown + ImpTotConc) y escenario de NC que anula dos facturas simultáneamente (AssociatedVouchers con dos elementos). Ambos muestran el retorno del comprobante autorizado con los nuevos campos.

## Capacidades

### Capacidades Modificadas

- `arca-wsfev1-invoicing`: Validación y mapeo SOAP extendidos de un modelo de escalares planos a un esquema con arrays de IVA, tributos y vínculos múltiples, campo de no gravados y cantidad en moneda extranjera.
- `wsfe-homologation-b2c-invoice-example`: Muestra extendida con dos escenarios adicionales: percepción de IIBB (demuestra `TaxBreakdown` + `NonTaxableAmount`) y Nota de Crédito multi-vínculo (demuestra `AssociatedVouchers` con dos entradas).

## Impacto

- **Archivos afectados**:
  - `Domain/Wsfe/WsfeModels.cs` — contrato definitivo confirmado
  - `Application/Wsfe/WsfeRequestValidator.cs` — lógica de validación actualizada
  - `Infrastructure/Wsfe/WsfeSoapClient.cs` — generación del envelope SOAP actualizada
  - `tests/ARCA-WS.Tests/Wsfe/WsfeRequestValidatorTests.cs` — migración y nuevos tests
  - `tests/ARCA-WS.Tests/Wsfe/WsfeSoapClientTests.cs` — migración y nuevos tests
  - `samples/PilotConsumer/Program.cs` — dos escenarios nuevos
- **Impacto en API pública**: `VoucherRequest` pierde los escalares `VatAmount` y `TaxAmount` (breaking change); los callers existentes deben migrar a `VatBreakdown` y `TaxBreakdown`. Los demás campos son aditivos o ya estaban presentes.
- **Impacto operativo**: los escenarios de homologación existentes deben actualizarse antes de ejecutarse nuevamente; se requieren tokens WSAA válidos para los dos nuevos escenarios del pilot.
