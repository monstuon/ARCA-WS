## Why

La librería actual resuelve WSAA + WSFEv1, pero el recorrido técnico tiene demasiadas capas y abstracciones para un SDK que se depura principalmente en escenarios de certificados, XML/SOAP y expiración de token. Reducir complejidad ahora mejora mantenibilidad, trazabilidad y onboarding sin perder compatibilidad funcional en .NET 8 para Windows/Linux.

## What Changes

- Simplificar la estructura interna a un diseño lineal centrado en un cliente principal (`ArcaClient`) y componentes internos explícitos.
- Consolidar la autenticación WSAA (login, cache, renovación y expiración) en un único `WsaaTokenManager`.
- Reducir/eliminar interfaces, factories, providers y handlers sin múltiples implementaciones reales.
- Mantener DTOs tipados y flujo directo SOAP -> mapeo -> DTO para operaciones WSFEv1.
- Unificar el manejo de errores en `ArcaException` con `Code`, `Message`, `IsRetryable` e `InnerException`.
- Preservar manejo correcto de certificados, cache de token/sign y logging básico.
- **BREAKING**: reorganización de namespaces/estructura interna y posible retiro de APIs intermedias heredadas en favor de `ArcaClient`.

## Capabilities

### New Capabilities
- `arca-simple-client-api`: API pública unificada y lineal para operaciones WSFEv1 mediante un único cliente principal.
- `wsaa-token-manager`: administración centralizada de autenticación WSAA con cache y renovación automática.
- `arca-unified-error-model`: modelo de errores unificado y predecible orientado a debugging.

### Modified Capabilities
- `arca-wsaa-authentication`: simplificación de la implementación manteniendo comportamiento funcional de autenticación y cache.
- `arca-wsfev1-invoicing`: exposición de operaciones de facturación desde `ArcaClient` con flujo interno más directo.

## Impact

- Código afectado: API pública, autenticación WSAA, cliente SOAP WSFEv1, mapeadores y manejo de errores.
- APIs: incorporación de `ArcaClient` como punto de entrada principal y consolidación de rutas de llamada.
- Compatibilidad: se mantiene .NET 8, Windows/Linux, soporte WSAA + WSFEv1 y DTOs tipados; puede haber breaking changes en superficie pública heredada.
- Mantenimiento: menor cantidad de capas y archivos intermedios; trazabilidad de llamadas más corta para debugging.