## 1. Contratos y modelos de dominio

- [x] 1.1 Definir/ajustar modelos tipados en `Domain/Wsfe/WsfeModels.cs` para requests y responses de `FEParamGetPtosVenta`, `FECompConsultar`, `FECAEAConsultar`, `FECAEASolicitar` y `FECAEARegInformativo`.
- [x] 1.2 Asegurar consistencia de nombres con aliases funcionales de negocio: `PuntosHabilitadosCaea`, `ConsultarComprobante`, `CAEAConsultar`, `CAEASolicitar`, `CAEARegInformativo`.

## 2. Cliente SOAP WSFE

- [x] 2.1 Extender `Infrastructure/Wsfe/IWsfeSoapClient.cs` con las nuevas operaciones asincronas y sus contratos.
- [x] 2.2 Implementar en `Infrastructure/Wsfe/WsfeSoapClient.cs` el armado de envelopes SOAP y parseo de respuesta para cada metodo segun manual COMPG v4.1.
- [x] 2.3 Reutilizar autenticacion actual (`Token`, `Sign`, `Cuit`) en todas las llamadas nuevas.
- [x] 2.4 Mapear errores de negocio (`Errors`/`Observaciones`) y fallas de infraestructura al esquema de excepciones actual.

## 3. Capa de aplicacion

- [x] 3.1 Exponer las nuevas operaciones en `Application/Wsfe/Wsfev1InvoicingService.cs` manteniendo patrones actuales de ejecucion resiliente y telemetria.
- [x] 3.2 Agregar validaciones de entrada especificas para consultas y CAEA en `Application/Wsfe/WsfeRequestValidator.cs` cuando corresponda.

## 4. Pruebas

- [x] 4.1 Crear/actualizar tests unitarios de serializacion SOAP para los 5 metodos.
- [x] 4.2 Crear/actualizar tests de parseo para casos de exito y rechazo funcional en los 5 metodos.
- [x] 4.3 Verificar que el flujo de `FECAEARegInformativo` mantenga paridad estructural con el flujo actual de emision (detalle + autenticacion) adaptado a CAEA.
- [x] 4.4 Ejecutar la suite de pruebas de WSFE y corregir regresiones.

## 5. Documentacion

- [x] 5.1 Actualizar documentacion tecnica con ejemplo de invocacion por cada metodo nuevo.
- [x] 5.2 Documentar brevemente restricciones relevantes de homologacion/produccion para operaciones CAEA.