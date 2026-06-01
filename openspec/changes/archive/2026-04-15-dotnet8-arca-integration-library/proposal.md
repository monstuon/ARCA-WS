## Why

La integracion de facturacion electronica con ARCA (WSAA y WSFEv1) esta distribuida en varios proyectos legacy de .NET Framework, con duplicacion de logica, dependencias fragiles y problemas de despliegue (incluyendo errores de carga de assemblies de WSAA). Esta situacion incrementa riesgo operativo y costo de mantenimiento, por lo que se requiere una base unica, moderna y reusable en .NET 8.

## What Changes

- Crear una biblioteca de clases en .NET 8 desacoplada de aplicaciones host, que encapsule de forma completa la autenticacion WSAA y las operaciones WSFEv1.
- Exponer una API publica simple y estable para:
  - Solicitar y renovar ticket de acceso WSAA.
  - Consultar ultimo comprobante autorizado.
  - Autorizar comprobantes electronicos con validaciones previas.
  - Consultar parametros y catalogos relevantes de WSFEv1.
- Centralizar configuracion, manejo de certificados y politicas de reintento/timeout en componentes internos reutilizables.
- Definir contratos de errores consistentes (tecnicos y funcionales) para facilitar observabilidad e integracion entre proyectos.
- Incorporar pruebas unitarias y de integracion para escenarios criticos de autenticacion y autorizacion de comprobantes.
- Preparar empaquetado como dependencia reusable (NuGet interno o referencia de proyecto) para adopcion incremental.

## Capabilities

### New Capabilities
- `arca-wsaa-authentication`: Gestion integral de autenticacion WSAA (TRA, firma, login CMS, cache y renovacion de token/sign).
- `arca-wsfev1-invoicing`: Operaciones de facturacion WSFEv1 para consulta y autorizacion de comprobantes con contratos tipados.
- `arca-integration-configuration-and-resilience`: Configuracion tipada, manejo de certificados, timeouts, reintentos y trazabilidad para invocaciones SOAP.
- `arca-integration-errors-and-observability`: Modelo de errores de dominio/infraestructura y puntos de observabilidad para diagnostico y soporte.

### Modified Capabilities
- Ninguna. No existen capacidades previas en `openspec/specs` para modificar.

## Impact

- Codigo afectado: nueva solucion/biblioteca .NET 8 y proyecto(s) de pruebas asociados.
- APIs: nueva API publica para consumo por aplicaciones internas; no se define compatibilidad binaria con codigo legacy.
- Dependencias: cliente SOAP para WSAA/WSFEv1, librerias de criptografia/certificados compatibles con .NET 8, utilidades de resiliencia y logging.
- Sistemas: proyectos consumidores actuales de facturacion deberan migrar gradualmente para usar la nueva biblioteca.
