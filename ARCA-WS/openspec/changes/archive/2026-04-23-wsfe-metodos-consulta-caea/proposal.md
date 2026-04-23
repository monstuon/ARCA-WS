## Why

La libreria ya cubre el flujo principal de autorizacion WSFE, pero aun no expone varios metodos operativos clave del manual oficial ARCA COMPG v4.1 que son necesarios para operar en escenarios reales de consulta de comprobantes, administracion de CAEA y parametrizacion comercial.

Sin estos metodos, los consumidores deben resolver integraciones SOAP por fuera de la libreria, rompiendo la propuesta de API unificada y tipada.

## What Changes

- Incorporar en la capa de aplicacion y en el cliente SOAP WSFE los siguientes metodos faltantes, reutilizando el esquema actual de autenticacion con `Token`, `Sign` y `Cuit`:
  - `FEParamGetPtosVenta` (PuntosHabilitadosCaea)
  - `FECompConsultar` (ConsultarComprobante)
  - `FECAEAConsultar` (CAEAConsultar)
  - `FECAEASolicitar` (CAEASolicitar)
  - `FECAEARegInformativo` (CAEARegInformativo)
- Definir modelos de request/response tipados para cada operacion, alineados al contrato SOAP del manual COMPG v4.1.
- Extender validaciones de entrada para las operaciones de consulta y CAEA, con errores funcionales consistentes con el estilo actual de la libreria.
- Agregar pruebas unitarias de mapeo SOAP y de parseo de respuestas (exito, rechazo de negocio y errores de infraestructura).
- Actualizar documentacion tecnica con ejemplos minimos de uso para cada nuevo metodo.

## Capabilities

### Modified Capabilities
- `arca-wsfev1-invoicing`: se amplia el alcance de la capacidad para incluir operaciones de consulta de comprobantes, consulta/solicitud CAEA y registro informativo CAEA, manteniendo autenticacion WSAA/credenciales externas bajo el patron ya vigente.

## Impact

- Affected code:
  - `Application/Wsfe/Wsfev1InvoicingService.cs`
  - `Infrastructure/Wsfe/IWsfeSoapClient.cs`
  - `Infrastructure/Wsfe/WsfeSoapClient.cs`
  - `Domain/Wsfe/WsfeModels.cs`
  - `tests/ARCA-WS.Tests/Wsfe/*`
  - documentacion en `Documentacion/*` (si aplica)
- Public API impact: se agregan nuevas operaciones publicas para consultas WSFE y administracion CAEA.
- Operational impact: los consumidores podran centralizar en la libreria todo el set operativo WSFE indicado, sin implementar SOAP manual externo.