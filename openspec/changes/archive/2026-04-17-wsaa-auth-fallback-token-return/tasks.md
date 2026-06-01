## 1. Contratos de entrada y salida WSFE

- [x] 1.1 Extender el request de autorizacion para aceptar `Token` y `Sign` opcionales por operacion.
- [x] 1.2 Extender el response de autorizacion para incluir `Token`, `Sign` y `ExpirationTime` cuando la API emita o renueve credenciales.
- [x] 1.3 Confirmar que el contrato mantiene compatibilidad para consumidores que no envian credenciales externas.

## 2. Politica de resolucion de credenciales

- [x] 2.1 Implementar decision de uso de credenciales en `Wsfev1InvoicingService`: priorizar `Token/Sign` provistos por ERP cuando sean validos para la operacion.
- [x] 2.2 Si faltan credenciales, o se detectan vencidas/invalidas, ejecutar fallback a WSAA y continuar la autorizacion.
- [x] 2.3 Garantizar que el fallback no persista credenciales en almacenamiento duradero dentro de la API.

## 3. Reuso y control de llamadas WSAA

- [x] 3.1 Mantener o introducir cache en memoria de corto plazo para reducir renovaciones concurrentes dentro de la misma instancia.
- [x] 3.2 Asegurar comportamiento seguro bajo concurrencia para evitar tormentas de login WSAA en solicitudes simultaneas.
- [x] 3.3 Instrumentar metricas/logs para distinguir: credencial externa reutilizada, fallback ejecutado, y renovacion WSAA.

## 4. Errores y resiliencia

- [x] 4.1 Definir errores tipados para casos de credenciales externas invalidas y fallo de fallback WSAA.
- [x] 4.2 Asegurar que los mensajes de error incluyan contexto operativo sin exponer datos sensibles.
- [x] 4.3 Verificar que el flujo mantiene comportamiento robusto cuando ERP no provee token.

## 5. Cobertura automatizada

- [x] 5.1 Agregar tests para request con `Token/Sign` validos que no invocan WSAA.
- [x] 5.2 Agregar tests para request sin credenciales que disparan fallback WSAA exitoso.
- [x] 5.3 Agregar tests para request con credenciales vencidas que disparan renovacion y devuelven nuevas credenciales en response.
- [x] 5.4 Agregar tests para fallback WSAA fallido y validacion de error tipado.
- [x] 5.5 Ejecutar suite afectada de `tests/ARCA-WS.Tests/Wsfe` y `tests/ARCA-WS.Tests/Wsaa` en verde.