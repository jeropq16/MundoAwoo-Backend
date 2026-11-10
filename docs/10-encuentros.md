# Encuentro con parceros

Hub autenticado: `/api/encuentros` (ASP.NET Core SignalR). El frontend usa Long Polling por el proxy de Next, con `Authorization: Bearer`; el JWT no se admite por query string. `CloseOnAuthenticationExpiration` cierra conexiones vencidas y las solicitudes posteriores vuelven a validar el token.

| Método del hub | Argumentos | Respuesta |
| --- | --- | --- |
| CrearSala | ninguno | EntradaSalaDto |
| UnirseSala | codigo: string | EntradaSalaDto |
| ReanudarSala | codigo: string, tokenReconexion: string | EntradaSalaDto |
| SalirSala | ninguno | void |
| Escribir | texto: string | void |
| Actuar | movimiento: Reposo, Saludar o Bailar | void |

Evento `SalaActualizada`: `{ codigo, revision, participantes, mensajes }`. Participantes incluyen `usuarioId`, `nombreVisible`, `avatar` (AvatarDto), `movimiento` y `conectado`. Mensajes incluyen `id`, `usuarioId`, `nombreVisible`, `texto`, `enviadoEnUtc`. La revisión crece dentro de cada sala; el cliente ignora instantáneas atrasadas.

La identidad se obtiene exclusivamente del claim `sub`. El nombre y la apariencia se consultan con los servicios existentes al entrar. Un cliente no puede enviar el avatar, el autor del mensaje ni un código de otra sala para escribir. Los errores de validación se traducen a HubException con mensajes cortos; los detalles internos no se habilitan.

EncuentroService es singleton y protege el estado efímero con un candado. Límite: seis participantes, una sala por conexión, una conexión de cada usuario por sala, 500 salas activas, 100 mensajes por sala, 500 caracteres por mensaje y 750 ms entre mensajes del mismo participante. La salida explícita retira al participante inmediatamente. Una desconexión reserva su lugar durante dos minutos desde que la API detecta el corte, incluso si era el último participante. La reserva ocupa cupo y muestra `conectado: false`. Un servicio de fondo limpia reservas vencidas cada cinco segundos y publica los cambios; una clave vencida se rechaza desde el instante del vencimiento. La sala desaparece al perder su último miembro o reserva. No hay cambios de esquema ni migraciones.

`EntradaSalaDto` contiene `{ sala, tokenReconexion }`. La clave aleatoria de 256 bits se entrega únicamente al solicitante y permanece estable hasta que abandona la sala o vence su reserva. Nunca se incluye en `SalaActualizada`. Reanudar exige la misma cuenta, el código y esa clave; puede reemplazar una conexión antigua que todavía no haya detectado el corte. La conexión anterior pierde permisos para escribir o actuar y sus callbacks tardíos no eliminan al participante recuperado. Se conservan avatar, movimiento, mensajes y límite de frecuencia. Una reserva inexistente, vencida o inválida devuelve `HubException("REANUDACION_NO_DISPONIBLE")`.

El frontend guarda el retorno en `sessionStorage` por cuenta y pestaña. Al recargar o volver a la ruta intenta recuperarlo; al cortar la red reintenta automáticamente durante unos 110 segundos y ofrece reintento manual. Conserva la instantánea y el borrador en memoria durante el corte, y mantiene los controles bloqueados hasta recibir confirmación. El borrador no se persiste al recargar. La clave no se comparte en el código de invitación ni se envía en la URL. La renovación coordinada del JWT ocurre antes de conectar si está próximo a vencer.

Este contrato requiere desplegar frontend y API juntos: CrearSala/UnirseSala ahora devuelven el envoltorio EntradaSalaDto.

Alcance: una instancia de API, sin historial permanente, voz, invitaciones persistentes ni moderación. Antes de escalar a varias instancias se necesita estado compartido y coordinación de SignalR. El código de invitación permite acceso a cualquier cuenta autenticada que lo posea; compártelo solo con los invitados.

Pruebas: `dotnet test`. EncuentroServiceTests cubre el comportamiento base; EncuentroReconexionTests cubre reservas, expiración con reloj controlado, capacidad, recuperación simultánea, identidad, claves, frecuencia y callbacks tardíos. EncuentroReconexionApiTests recorre el hub real con JWT y transporte WebSocket del servidor de pruebas, incluyendo reemplazo de conexión viva, retorno tras corte del último participante y ausencia de claves en eventos. EncuentroAutenticacionTests comprueba negociación anónima, autenticada y con token vencido. El frontend mantiene Long Polling en producción y tiene pruebas del estado del cliente mediante `npm run test:encuentros`.
