## Por que

La incorporacion previa de escenarios FCE dejo tres problemas abiertos que hoy impactan directamente en homologacion:

- **Validacion FCE incompleta**: el validador solo exige `Concept = 1` y acepta `ServicePaymentDueDate` si viene informado, pero no fuerza los campos obligatorios ni distingue entre factura/debito y nota de credito FCE con las reglas reales del WSFE.
- **Condicion IVA del receptor desacoplada del WSFE**: `RecipientVatConditionId` se valida solo como entero positivo. Para FCE-B esto permite combinaciones que luego el WSFE rechaza porque la condicion IVA del receptor debe pertenecer al catalogo oficial de `FEParamGetCondicionIvaReceptor` y ser compatible con el tipo de comprobante.
- **Ejemplos y piloto desalineados**: el cambio anterior documento `FCE-B` con una combinacion equivocada y el `PilotConsumer` sigue intentando emitir y anular comprobantes FCE sin chequear si el comprobante base quedo realmente disponible para asociar. Eso desplaza errores evitables al WSFE y hace que los tests no representen el comportamiento esperado.

El resultado es que el codigo parece "soportar FCE", pero en realidad delega al web service validaciones que deberian resolverse antes del envio. La propuesta corrige esa brecha y deja trazabilidad explicita de las reglas oficiales que deben gobernar FCE-A, FCE-B y sus notas de credito.

## Que cambia

- **Validacion basada en parametros oficiales**: introducir una capa de consulta/cache para catalogos WSFEParam, al menos para `FEParamGetCondicionIvaReceptor`, y usarla desde el flujo de autorizacion para validar que la condicion IVA informada exista y sea compatible con el tipo de comprobante FCE antes de construir el request SOAP.
- **Reglas FCE explicitadas en el validador**: separar las reglas generales de `Concept`/fechas de servicio de las reglas especificas de FCE. Para tipos FCE (201, 202, 203, 206, 207, 208) el sistema debe exigir `ServicePaymentDueDate` valido, documento identificatorio consistente y asociaciones validas segun tipo.
- **Saneamiento de NC-FCE**: cuando el piloto o una integracion no dispongan del comprobante FCE padre autorizado, la nota de credito correspondiente debe omitirse de forma controlada. Si existe el numero pero el tipo asociado no es valido para la NC FCE que se intenta emitir, la libreria debe rechazar el request localmente sin enviarlo a WSFE.
- **Alineacion de ejemplos, piloto y tests**: corregir `PilotConsumer`, los escenarios documentados y los tests para que FCE-B use una condicion IVA de receptor permitida por WSFE, que las NC-FCE solo se emitan si el padre existe y que cada error corregido quede cubierto por tests automatizados.

## Decisiones claves

- **No hardcodear el catalogo de condicion IVA**: la libreria puede mantener una cache en memoria por ambiente, pero la fuente de verdad debe seguir siendo WSFEParam. Si la consulta al catalogo falla, la autorizacion debe fallar con un error funcional claro o reutilizar una cache vigente conocida; no debe degradar a una validacion permisiva.
- **Mantener la validacion FCE cerca del flujo de autorizacion**: `WsfeRequestValidator` hoy no tiene dependencias externas. Para incorporar parametros oficiales sin romper la API publica, la propuesta admite introducir un validador enriquecido o una fase adicional previa al envio en `Wsfev1InvoicingService`.
- **Tratar FCE-B como escenario B2B y no consumidor final**: la propuesta reemplaza el antecedente archivado que describia `FCE-B` con supuestos inconsistentes. El piloto y la spec deben reflejar un receptor identificable y una condicion IVA compatible con catalogo oficial.

## Capacidades

### Capacidades modificadas

- `arca-wsfev1-invoicing`: agrega validacion previa al envio para escenarios FCE utilizando catalogos WSFEParam y reglas de asociacion por tipo de comprobante.
- `wsfe-homologation-b2c-invoice-example`: corrige y extiende el ejemplo operativo para que los escenarios FCE del piloto sean reproducibles y consistentes con las reglas reales del WSFE.

## Impacto

- **Archivos previstos**:
  - `Application/Wsfe/WsfeRequestValidator.cs` - reglas FCE completas y validacion de asociaciones NC-FCE.
  - `Application/Wsfe/Wsfev1InvoicingService.cs` - incorporacion de consulta/cache de parametros oficiales antes de autorizar.
  - `Infrastructure/Wsfe/WsfeSoapClient.cs` o nueva capa asociada - soporte para consultar catalogos WSFEParam requeridos por la validacion.
  - `PublicApi/ArcaIntegrationClient.cs` - sin breaking changes; reutiliza `GetParameterCatalogAsync` o expone helpers adicionales solo si hacen falta internamente.
  - `samples/PilotConsumer/Program.cs` - correccion de escenarios FCE-A, FCE-B, NC-FCE-A y NC-FCE-B.
  - `tests/ARCA-WS.Tests/Wsfe/*.cs` - cobertura de reglas FCE, catalogos oficiales y omision controlada.
- **Impacto operativo**: antes de emitir FCE la libreria pasara a depender de parametros WSFEParam vigentes o de una cache valida. Esto evita rechazos del WSFE a costa de una validacion previa mas estricta.
- **Riesgo principal**: definir exactamente la compatibilidad entre `VoucherType` FCE y `RecipientVatConditionId` a partir del catalogo real. La implementacion debe dejar esa matriz en un punto unico y testeado para no repetir la inconsistencia actual.