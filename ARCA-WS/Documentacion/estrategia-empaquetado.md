# Estrategia de Empaquetado Inicial

## Opcion adoptada

Referencia de proyecto en monorepo (`ProjectReference`) para acelerar iteracion y estabilizacion temprana.

## Lineamientos

1. Versionado semantico en `ARCA-WS.csproj`.
2. Publicacion de paquete NuGet interno diferida a fase posterior de consolidacion.
3. Pipeline CI con restauracion, build y tests obligatorios.

## Criterio para pasar a NuGet interno

- Estabilidad funcional en al menos un consumidor productivo.
- Cobertura de pruebas para escenarios criticos de WSAA/WSFEv1.
- Politica de soporte y backward compatibility definida.
