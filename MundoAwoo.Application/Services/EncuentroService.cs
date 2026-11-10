using System.Security.Cryptography;
using System.Text;
using MundoAwoo.Application.DTOs.Encuentros;
using MundoAwoo.Application.Exceptions;

namespace MundoAwoo.Application.Services;

// Estado efímero de una sola instancia. Las claves de retorno nunca se publican al grupo.
public class EncuentroService(TimeProvider? reloj = null)
{
    public static readonly TimeSpan PlazoReconexion = TimeSpan.FromMinutes(2);
    private readonly TimeProvider _reloj = reloj ?? TimeProvider.System;
    private sealed class Miembro(ParceroDto parcero, string conexion)
    {
        public ParceroDto Parcero { get; set; } = parcero;
        public string? Conexion { get; set; } = conexion;
        public string Token { get; } = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        public DateTimeOffset? ReservaHasta { get; set; }
        public DateTimeOffset? UltimoMensaje { get; set; }
    }
    private sealed class Sala(string codigo)
    {
        public string Codigo { get; } = codigo;
        public long Revision { get; set; }
        public Dictionary<Guid, Miembro> Miembros { get; } = new();
        public List<MensajeSalaDto> Mensajes { get; } = new();
    }
    private readonly object _candado = new();
    private readonly Dictionary<string, Sala> _salas = new();
    private readonly Dictionary<string, (Sala Sala, Guid Usuario)> _conexiones = new();

    public SalaDto Entrar(string conexion, ParceroDto parcero, string? codigo)
    {
        lock (_candado)
        {
            if (_conexiones.ContainsKey(conexion))
                throw new ArgumentException("Sal de tu sala actual antes de entrar a otra.");
            Sala sala;
            if (codigo is null)
            {
                if (_salas.Count >= 500) throw new ArgumentException("El barrio está lleno. Intenta más tarde.");
                string nuevo;
                do { nuevo = Convert.ToHexString(RandomNumberGenerator.GetBytes(6)); } while (_salas.ContainsKey(nuevo));
                sala = new Sala(nuevo);
                _salas.Add(nuevo, sala);
            }
            else
            {
                codigo = codigo.Trim().ToUpperInvariant();
                if (codigo.Length != 12 || !_salas.TryGetValue(codigo, out sala!))
                    throw new ArgumentException("Esta sala ya no existe. Revisa el código o crea otra.");
            }
            if (sala.Miembros.Count >= 6) throw new ArgumentException("La sala ya tiene seis parceros.");
            if (sala.Miembros.ContainsKey(parcero.UsuarioId))
                throw new ArgumentException("Tu cuenta ya está en esta sala. Reconecta desde la pestaña original.");
            sala.Miembros.Add(parcero.UsuarioId, new Miembro(parcero, conexion));
            _conexiones.Add(conexion, (sala, parcero.UsuarioId));
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public string ObtenerTokenReconexion(string conexion)
    {
        lock (_candado) { var (sala, usuario) = Obtener(conexion); return sala.Miembros[usuario].Token; }
    }

    public (SalaDto Sala, string? ConexionAnterior) Reanudar(string conexion, Guid usuario, string codigo, string token)
    {
        lock (_candado)
        {
            codigo = codigo?.Trim().ToUpperInvariant() ?? "";
            if (!_salas.TryGetValue(codigo, out var sala) ||
                !sala.Miembros.TryGetValue(usuario, out var miembro) ||
                token is null || token.Length != 64 ||
                !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(miembro.Token), Encoding.UTF8.GetBytes(token)) ||
                miembro.ReservaHasta <= _reloj.GetUtcNow())
                throw new ReconexionNoDisponibleException();
            if (_conexiones.TryGetValue(conexion, out var actual) && (actual.Sala != sala || actual.Usuario != usuario))
                throw new ArgumentException("Sal de tu sala actual antes de entrar a otra.");
            var anterior = miembro.Conexion;
            if (anterior == conexion) return (Copiar(sala), null);
            if (anterior is not null) _conexiones.Remove(anterior);
            miembro.Conexion = conexion;
            miembro.ReservaHasta = null;
            _conexiones[conexion] = (sala, usuario);
            sala.Revision++;
            return (Copiar(sala), anterior);
        }
    }

    public SalaDto? Desconectar(string conexion)
    {
        lock (_candado)
        {
            if (!_conexiones.Remove(conexion, out var entrada)) return null;
            var miembro = entrada.Sala.Miembros[entrada.Usuario];
            miembro.Conexion = null;
            miembro.ReservaHasta = _reloj.GetUtcNow() + PlazoReconexion;
            entrada.Sala.Revision++;
            return Copiar(entrada.Sala);
        }
    }

    public SalaDto? Salir(string conexion)
    {
        lock (_candado)
        {
            if (!_conexiones.Remove(conexion, out var entrada)) return null;
            entrada.Sala.Miembros.Remove(entrada.Usuario);
            entrada.Sala.Revision++;
            if (entrada.Sala.Miembros.Count == 0) _salas.Remove(entrada.Sala.Codigo);
            return Copiar(entrada.Sala);
        }
    }

    public IReadOnlyList<SalaDto> LimpiarReservas()
    {
        lock (_candado)
        {
            var cambios = new List<SalaDto>();
            var ahora = _reloj.GetUtcNow();
            foreach (var sala in _salas.Values.ToArray())
            {
                var vencidos = sala.Miembros.Where(p => p.Value.ReservaHasta <= ahora).Select(p => p.Key).ToArray();
                if (vencidos.Length == 0) continue;
                foreach (var usuario in vencidos) sala.Miembros.Remove(usuario);
                sala.Revision++;
                if (sala.Miembros.Count == 0) _salas.Remove(sala.Codigo);
                cambios.Add(Copiar(sala));
            }
            return cambios;
        }
    }

    public SalaDto Escribir(string conexion, string texto)
    {
        lock (_candado)
        {
            var (sala, usuario) = Obtener(conexion);
            texto = texto?.Trim() ?? "";
            if (texto.Length is < 1 or > 500) throw new ArgumentException("Escribe entre 1 y 500 caracteres.");
            var ahora = _reloj.GetUtcNow();
            var miembro = sala.Miembros[usuario];
            if (miembro.UltimoMensaje is { } ultimo && ahora - ultimo < TimeSpan.FromMilliseconds(750))
                throw new ArgumentException("Espera un momento antes de enviar otro mensaje.");
            var autor = miembro.Parcero;
            sala.Mensajes.Add(new(Guid.NewGuid(), autor.UsuarioId, autor.NombreVisible, texto, ahora.UtcDateTime));
            if (sala.Mensajes.Count > 100) sala.Mensajes.RemoveAt(0);
            miembro.UltimoMensaje = ahora;
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public SalaDto Actuar(string conexion, string movimiento)
    {
        lock (_candado)
        {
            var (sala, usuario) = Obtener(conexion);
            if (movimiento is not ("Reposo" or "Bailar" or "Saludar"))
                throw new ArgumentException("Ese movimiento no está disponible.");
            var miembro = sala.Miembros[usuario];
            miembro.Parcero = miembro.Parcero with { Movimiento = movimiento };
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public SalaDto Consultar(string conexion) { lock (_candado) { return Copiar(Obtener(conexion).Sala); } }
    private (Sala Sala, Guid Usuario) Obtener(string conexion) => _conexiones.TryGetValue(conexion, out var entrada)
        ? entrada : throw new ArgumentException("Entra a una sala para participar.");
    private static SalaDto Copiar(Sala sala) => new(sala.Codigo, sala.Revision,
        sala.Miembros.Values.Select(p => p.Parcero with { Conectado = p.Conexion is not null }).ToArray(), sala.Mensajes.ToArray());
}
