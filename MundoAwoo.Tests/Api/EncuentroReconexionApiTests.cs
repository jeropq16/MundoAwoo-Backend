using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using MundoAwoo.Infrastructure.Persistence;

namespace MundoAwoo.Tests.Api;

public class EncuentroReconexionApiTests
{
    [Fact]
    public async Task Hub_ReclamaLugarConCuentaYClaveSinDuplicarYRevocaConexionAnterior()
    {
        using var api = new PerfilApiFactory();
        await Preparar(api);
        using var original = await Conectar(api, api.PrimerUsuarioId);
        var creada = await original.Invocar("CrearSala");
        var codigo = creada.GetProperty("sala").GetProperty("codigo").GetString()!;
        var clave = creada.GetProperty("tokenReconexion").GetString()!;
        using var observador = await Conectar(api, api.SegundoUsuarioId);
        await observador.Invocar("UnirseSala", codigo);
        Assert.All(observador.Eventos, e => Assert.False(e.TryGetProperty("tokenReconexion", out _)));
        await observador.Error("ReanudarSala", codigo, clave);
        // La misma cuenta sin la clave tampoco puede desplazar a la conexión original.
        using var nueva = await Conectar(api, api.PrimerUsuarioId);
        Assert.Contains("REANUDACION_NO_DISPONIBLE", await nueva.Error("ReanudarSala", codigo, new string('0', 64)));
        var regreso = await nueva.Invocar("ReanudarSala", codigo, clave);
        Assert.Equal(2, regreso.GetProperty("sala").GetProperty("participantes").GetArrayLength());
        Assert.Equal(clave, regreso.GetProperty("tokenReconexion").GetString());
        Assert.Contains("Entra a una sala", await original.Error("Escribir", "Obsoleto"));
        await original.Invocar("SalirSala"); // Una salida tardía no quita al cliente recuperado.
        await nueva.Invocar("Escribir", "Regresé al parche");
        await observador.Invocar("Actuar", "Saludar"); // Vacía los eventos anteriores a su confirmación.
        var ultima = observador.Eventos.Last();
        Assert.Equal(2, ultima.GetProperty("participantes").GetArrayLength());
        Assert.Equal("Regresé al parche", ultima.GetProperty("mensajes")[0].GetProperty("texto").GetString());
        Assert.All(observador.Eventos, e => Assert.DoesNotContain(clave, e.GetRawText()));
    }

    [Fact]
    public async Task Hub_ConservaSalaDelUltimoParticipanteTrasCorteYPermiteVolver()
    {
        using var api = new PerfilApiFactory();
        await Preparar(api);
        string codigo, clave;
        using (var original = await Conectar(api, api.PrimerUsuarioId))
        {
            var creada = await original.Invocar("CrearSala");
            codigo = creada.GetProperty("sala").GetProperty("codigo").GetString()!;
            clave = creada.GetProperty("tokenReconexion").GetString()!;
            await original.Invocar("Escribir", "Antes del corte");
            await original.Cortar();
        }
        using var nueva = await Conectar(api, api.PrimerUsuarioId);
        var regreso = await nueva.Invocar("ReanudarSala", codigo, clave);
        var sala = regreso.GetProperty("sala");
        Assert.Equal(codigo, sala.GetProperty("codigo").GetString());
        Assert.Equal(1, sala.GetProperty("participantes").GetArrayLength());
        Assert.True(sala.GetProperty("participantes")[0].GetProperty("conectado").GetBoolean());
        Assert.Equal("Antes del corte", sala.GetProperty("mensajes")[0].GetProperty("texto").GetString());
        await nueva.Invocar("SalirSala");
        using var despues = await Conectar(api, api.PrimerUsuarioId);
        Assert.Contains("REANUDACION_NO_DISPONIBLE", await despues.Error("ReanudarSala", codigo, clave));
    }

    private static async Task Preparar(PerfilApiFactory api)
    {
        await api.PrepararDatosAsync();
        using var scope = api.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AwoorldDbContext>();
        db.Avatares.AddRange(TestData.CrearAvatar(api.PrimerUsuarioId), TestData.CrearAvatar(api.SegundoUsuarioId));
        await db.SaveChangesAsync();
    }

    private static async Task<ClienteHub> Conectar(PerfilApiFactory api, Guid usuario)
    {
        var cliente = api.Server.CreateWebSocketClient();
        cliente.ConfigureRequest = r => r.Headers.Authorization = "Bearer " + api.CrearToken(usuario.ToString());
        var socket = await cliente.ConnectAsync(new Uri("wss://localhost/api/encuentros"), CancellationToken.None);
        var hub = new ClienteHub(socket);
        await hub.Enviar(new { protocol = "json", version = 1 });
        await hub.Recibir();
        return hub;
    }

    // Cliente JSON mínimo del protocolo público de SignalR, sin dependencias adicionales.
    private sealed class ClienteHub(WebSocket socket) : IDisposable
    {
        private int _id;
        private readonly Queue<JsonElement> _pendientes = new();
        public List<JsonElement> Eventos { get; } = new();
        public async Task Enviar(object mensaje)
        {
            var datos = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mensaje) + '\u001e');
            await socket.SendAsync(datos.AsMemory(), WebSocketMessageType.Text, true, CancellationToken.None);
        }
        public async Task<JsonElement> Recibir()
        {
            using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            while (_pendientes.Count == 0)
            {
                using var datos = new MemoryStream();
                var buffer = new byte[8192];
                ValueWebSocketReceiveResult recibido;
                do
                {
                    recibido = await socket.ReceiveAsync(buffer.AsMemory(), plazo.Token);
                    Assert.NotEqual(WebSocketMessageType.Close, recibido.MessageType);
                    datos.Write(buffer, 0, recibido.Count);
                } while (!recibido.EndOfMessage);
                foreach (var trama in Encoding.UTF8.GetString(datos.ToArray()).Split('\u001e', StringSplitOptions.RemoveEmptyEntries))
                    _pendientes.Enqueue(JsonSerializer.Deserialize<JsonElement>(trama));
            }
            return _pendientes.Dequeue();
        }
        private async Task<JsonElement> Solicitar(string metodo, object[] argumentos)
        {
            var id = (++_id).ToString();
            await Enviar(new { type = 1, invocationId = id, target = metodo, arguments = argumentos });
            while (true)
            {
                var trama = await Recibir();
                var tipo = trama.GetProperty("type").GetInt32();
                if (tipo == 1) Eventos.Add(trama.GetProperty("arguments")[0].Clone());
                if (tipo == 3 && trama.GetProperty("invocationId").GetString() == id) return trama;
            }
        }
        public async Task<JsonElement> Invocar(string metodo, params object[] argumentos)
        {
            var respuesta = await Solicitar(metodo, argumentos);
            Assert.False(respuesta.TryGetProperty("error", out _), respuesta.ToString());
            return respuesta.TryGetProperty("result", out var resultado) ? resultado : default;
        }
        public async Task<string> Error(string metodo, params object[] argumentos) =>
            (await Solicitar(metodo, argumentos)).GetProperty("error").GetString()!;
        public async Task Cortar()
        {
            using var plazo = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Corte de prueba", plazo.Token);
        }
        public void Dispose() => socket.Dispose();
    }
}
