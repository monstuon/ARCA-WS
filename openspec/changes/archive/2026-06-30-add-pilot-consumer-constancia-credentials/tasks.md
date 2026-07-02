## 1. Variable de estado Constancia

- [x] 1.1 Agregar variable ErpCredentialSnapshot? constanciaCredentials = null; debajo de erpCredentials en Program.cs de PilotConsumer

## 2. Helper ApplyConstanciaCredentials

- [x] 2.1 Implementar helper local (string? token, string? sign) ApplyConstanciaCredentials() en Program.cs, analogo a ApplyErpCredentials, que retorna (constanciaCredentials.Token, constanciaCredentials.Sign) si el snapshot es vigente o (null, null) si es null o esta vencido
- [x] 2.2 Verificar que el helper pone constanciaCredentials = null cuando la expiracion <= DateTimeOffset.UtcNow.AddSeconds(30) y loguea advertencia analoga a la de ApplyErpCredentials

## 3. Helper CaptureConstanciaCredentialsFromResult

- [x] 3.1 Implementar helper local void CaptureConstanciaCredentialsFromResult(string scenario, PersonaTaxData result) que actualiza constanciaCredentials cuando result.CredentialsIssuedByApi es true y Token/Sign no son nulos ni vacios
- [x] 3.2 Agregar LogInformation con el scenario y la expiracion al capturar credenciales, analogo al log de CaptureErpCredentialsFromResult

## 4. Integracion en Escenario 16

- [x] 4.1 Reemplazar token: null, sign: null en la llamada a constanciaService.GetPersonaAsync del Escenario 16 por las variables (token, sign) obtenidas de ApplyConstanciaCredentials()
- [x] 4.2 Llamar a CaptureConstanciaCredentialsFromResult despues de la llamada exitosa a GetPersonaAsync en el Escenario 16

## 5. Verificacion

- [x] 5.1 Compilar el proyecto PilotConsumer sin errores
- [ ] 5.2 Ejecutar PilotConsumer y verificar que el Escenario 16 muestra 'Fuente credenciales: wsaa-fallback' en la primera ejecucion y que captura el token
