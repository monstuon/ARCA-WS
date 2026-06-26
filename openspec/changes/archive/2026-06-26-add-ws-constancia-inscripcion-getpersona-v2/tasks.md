## 1. Modelos y contratos de WS Constancia

- [x] 1.1 Crear modelos de dominio para la respuesta de `getPersona_v2` (datos fiscales + errores + metadata de credenciales).
- [x] 1.2 Definir interfaz del cliente SOAP de constancia e interfaz `IWSConstanciaInscripcionService`.

## 2. Integración SOAP y autenticación

- [x] 2.1 Implementar `WSConstanciaInscripcionSoapClient` con armado de envelope/SOAPAction para `getPersona_v2` y parseo de respuesta/fault.
- [x] 2.2 Reutilizar WSAA para múltiples service names (incluyendo `ws_sr_constancia_inscripcion`) con aislamiento de cache por servicio.
- [x] 2.3 Implementar `WSConstanciaInscripcionService` con resolución de token/sign, soporte de credenciales externas y fallback WSAA por error de autenticación.

## 3. Exposición en API pública y configuración

- [x] 3.1 Agregar endpoints de WS Constancia (homologación/producción) en `EndpointOptions` y validaciones asociadas.
- [x] 3.2 Registrar cliente SOAP y servicio de constancia en `ServiceCollectionExtensions`.
- [x] 3.3 Exponer método `GetPersona` en `ArcaIntegrationClient` con firma alineada a métodos WSFE.

## 4. Pruebas y validación

- [x] 4.1 Crear pruebas de `WSConstanciaInscripcionService` para flujo normal, credenciales externas y fallback.
- [x] 4.2 Agregar pruebas de delegación del método `GetPersona` en `ArcaIntegrationClient`.
- [x] 4.3 Incorporar casos de prueba para CUIT: `30-53331924-2`, `55-00000410-2`, `20-20490252-7`, `33-61307092-9`, `27-12619106-0`, `27-22630583-7`, `20-39083356-4`.
- [x] 4.4 Ejecutar build/tests relevantes y ajustar fallos antes de cerrar el change.
