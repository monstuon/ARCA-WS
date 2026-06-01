## 1. API pública lineal

- [x] 1.1 Crear `ArcaClient` como entrypoint público único con configuración explícita y dependencias internas mínimas
- [x] 1.2 Exponer en `ArcaClient` los métodos async requeridos: `AutorizarFacturaAsync`, `ConsultarComprobanteAsync`, `ObtenerUltimoComprobanteAsync`, `SolicitarCAEAAsync`, `InformarCAEAAsync`
- [x] 1.3 Redirigir o descontinuar rutas públicas heredadas para que el consumo estándar pase por `ArcaClient`

## 2. Autenticación WSAA centralizada

- [x] 2.1 Implementar `WsaaTokenManager` como único orquestador de login, cache en memoria, expiración y renovación
- [x] 2.2 Integrar validación de credenciales externas (`Token`/`Sign`) con fallback controlado a WSAA desde `WsaaTokenManager`
- [x] 2.3 Garantizar uso correcto de certificados y firma WSAA en Windows/Linux dentro del flujo centralizado

## 3. Flujo WSFEv1 explícito y tipado

- [x] 3.1 Consolidar invocaciones SOAP en un cliente explícito (`WsfeSoapClient`) consumido por `ArcaClient`
- [x] 3.2 Mantener/ajustar mapeos SOAP -> DTO tipados para autorización, consulta de comprobantes y operaciones CAEA
- [x] 3.3 Asegurar trazabilidad de punta a punta con un flujo corto y directo desde API pública a SOAP

## 4. Modelo de errores unificado

- [x] 4.1 Introducir `ArcaException` con `Code`, `Message`, `IsRetryable` e `InnerException`
- [x] 4.2 Reemplazar conversiones de error dispersas para mapear fallos operativos al nuevo contrato unificado
- [x] 4.3 Reducir jerarquía de excepciones especializadas dejando solo las que aporten valor semántico real

## 5. Limpieza estructural y compatibilidad

- [x] 5.1 Reorganizar carpetas/namespaces a una estructura simple (`Clients|Services`, `Auth`, `Soap`, `Models`, `Exceptions`, `Utils`)
- [x] 5.2 Eliminar interfaces/factories/providers/handlers sin múltiples implementaciones reales
- [x] 5.3 Mantener compatibilidad funcional con WSAA + WSFEv1 y .NET 8 en Windows/Linux durante la simplificación

## 6. Logging, pruebas y migración

- [x] 6.1 Conservar logging básico en puntos críticos (login WSAA, renovación, llamadas SOAP, errores)
- [x] 6.2 Actualizar o crear pruebas para cache/renovación de token, operaciones principales de `ArcaClient` y mapeo de errores
- [x] 6.3 Documentar guía breve de migración hacia `ArcaClient` y cambios breaking de superficie pública
