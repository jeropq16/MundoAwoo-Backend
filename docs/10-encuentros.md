# Encuentro con parceros

Hub autenticado: `/api/encuentros` (ASP.NET Core SignalR). El frontend usa Long Polling por el proxy de Next, con `Authorization: Bearer`; el JWT no se admite por query string. `CloseOnAuthenticationExpiration` cierra conexiones vencidas y las solicitudes posteriores vuelven a validar el token.

| Método del hub | Argumentos | Respuesta |
| --- | --- | --- |
| CrearSala | ninguno | SalaDto |
| UnirseSala | codigo: string | SalaDto |
| SalirSala | ninguno | void |
| Escribir | texto: string | void |
| Actuar | movimiento: Reposo, Saludar o Bailar | void |

Evento `SalaActualizada`: `{ codigo, revision, participantes, mensajes }`. Participantes incluyen `usuarioId`, `nombreVisible`, `avatar` (AvatarDto) y `movimiento`. Mensajes incluyen `id`, `usuarioId`, `nombreVisible`, `texto`, `enviadoEnUtc`. La revisión crece dentro de cada sala; el cliente ignora instantáneas atrasadas.

La identidad se obtiene exclusivamente del claim `sub`. El nombre y la apariencia se consultan con los servicios existentes al entrar. Un cliente no puede enviar el avatar, el autor del mensaje ni un código de otra sala para escribir. Los errores de validación se traducen a HubException con mensajes cortos; los detalles internos no se habilitan.

EncuentroService es singleton y protege el estado efímero con un candado. Límite: seis participantes, una sala por conexión, una conexión de cada usuario por sala, 500 salas activas, 100 mensajes por sala, 500 caracteres por mensaje y 750 ms entre mensajes del mismo participante. La desconexión retira al participante y la última salida destruye la sala. No hay cambios de esquema ni migraciones.

Alcance: una instancia de API, sin historial permanente, voz, invitaciones persistentes ni moderación. Antes de escalar a varias instancias se necesita estado compartido y coordinación de SignalR. El código de invitación permite acceso a cualquier cuenta autenticada que lo posea; compártelo solo con los invitados.

Pruebas: `dotnet test`. EncuentroServiceTests cubre aislamiento, identidad, validación, frecuencia, duplicados, eliminación y concurrencia al llenar una sala. EncuentroAutenticacionTests comprueba negociación anónima, autenticada y con token vencido. También se verificó el recorrido real con dos cuentas desde Next y MySQL.
