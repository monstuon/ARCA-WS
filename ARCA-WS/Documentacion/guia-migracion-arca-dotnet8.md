# Guia de Migracion a Biblioteca .NET 8 ARCA

## Objetivo

Migrar consumidores legacy (.NET Framework) a la biblioteca reutilizable .NET 8 para WSAA + WSFEv1.

## Estrategia de adopcion

1. Integrar la libreria por referencia de proyecto en el monorepo.
2. Configurar opciones `Arca` por ambiente (homologacion/produccion).
3. Implementar un flujo piloto con operacion de consulta y luego autorizacion.
4. Comparar resultados funcionales con la implementacion legacy en homologacion.
5. Habilitar gradualmente en produccion por consumidor.

## Ejemplo de configuracion

```json
{
  "Arca": {
    "Environment": "Homologation",
    "Endpoints": {
      "WsaaHomologation": "https://wsaahomo.afip.gov.ar/ws/services/LoginCms",
      "WsaaProduction": "https://wsaa.afip.gov.ar/ws/services/LoginCms",
      "WsfeHomologation": "https://wswhomo.afip.gov.ar/wsfev1/service.asmx",
      "WsfeProduction": "https://servicios1.afip.gov.ar/wsfev1/service.asmx"
    },
    "Wsaa": {
      "ServiceName": "wsfe",
      "TimestampToleranceSeconds": 120,
      "RenewalWindowSeconds": 120
    },
    "Resilience": {
      "Timeout": "00:01:00",
      "MaxRetries": 0
    },
    "Certificate": {
      "Source": "File",
      "FilePath": "certificado.pfx",
      "Password": "<secreto>"
    }
  }
}
```

## Manejo de errores recomendado

- `ArcaValidationException`: datos obligatorios o consistencia de importes.
- `ArcaAuthenticationException`: fallas en WSAA/certificados.
- `ArcaFunctionalException`: rechazos funcionales de ARCA (codigo/mensaje).
- `ArcaInfrastructureException`: conectividad, timeouts o errores no controlados.

## Rollback

1. Revertir referencia a la libreria y restaurar el flujo legacy por consumidor.
2. Mantener version pinneada de la libreria en cada consumidor.
3. Conservar monitoreo de incidencias durante 48 horas posteriores.
