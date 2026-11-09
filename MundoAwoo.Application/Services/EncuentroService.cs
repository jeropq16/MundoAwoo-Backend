using System.Security.Cryptography;
using MundoAwoo.Application.DTOs.Encuentros;

namespace MundoAwoo.Application.Services;

// Estado efímero de una sola instancia de la API. Nunca guarda tokens ni correos.
// El candado protege entradas, salidas y mensajes simultáneos.
public class EncuentroService
{
    private sealed class Sala(string codigo)
    {
        public string Codigo { get; } = codigo;
        public long Revision { get; set; }
        public Dictionary<string, ParceroDto> Miembros { get; } = new();
        public List<MensajeSalaDto> Mensajes { get; } = new();
        public Dictionary<string, DateTime> UltimoMensaje { get; } = new();
    }
    private readonly object _candado = new();
    private readonly Dictionary<string, Sala> _salas = new();
    private readonly Dictionary<string, string> _conexiones = new();

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
            if (sala.Miembros.Values.Any(p => p.UsuarioId == parcero.UsuarioId))
                throw new ArgumentException("Tu cuenta ya está en esta sala desde otra pestaña.");
            sala.Miembros.Add(conexion, parcero);
            _conexiones.Add(conexion, sala.Codigo);
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public SalaDto? Salir(string conexion)
    {
        lock (_candado)
        {
            if (!_conexiones.Remove(conexion, out var codigo)) return null;
            var sala = _salas[codigo];
            sala.Miembros.Remove(conexion);
            sala.UltimoMensaje.Remove(conexion);
            sala.Revision++;
            if (sala.Miembros.Count == 0) _salas.Remove(codigo);
            return Copiar(sala);
        }
    }

    public SalaDto Escribir(string conexion, string texto)
    {
        lock (_candado)
        {
            var sala = Obtener(conexion);
            texto = texto?.Trim() ?? "";
            if (texto.Length is < 1 or > 500)
                throw new ArgumentException("Escribe entre 1 y 500 caracteres.");
            var ahora = DateTime.UtcNow;
            if (sala.UltimoMensaje.TryGetValue(conexion, out var ultimo) && ahora - ultimo < TimeSpan.FromMilliseconds(750))
                throw new ArgumentException("Espera un momento antes de enviar otro mensaje.");
            var autor = sala.Miembros[conexion];
            sala.Mensajes.Add(new(Guid.NewGuid(), autor.UsuarioId, autor.NombreVisible, texto, ahora));
            if (sala.Mensajes.Count > 100) sala.Mensajes.RemoveAt(0);
            sala.UltimoMensaje[conexion] = ahora;
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public SalaDto Actuar(string conexion, string movimiento)
    {
        lock (_candado)
        {
            var sala = Obtener(conexion);
            if (movimiento is not ("Reposo" or "Bailar" or "Saludar"))
                throw new ArgumentException("Ese movimiento no está disponible.");
            sala.Miembros[conexion] = sala.Miembros[conexion] with { Movimiento = movimiento };
            sala.Revision++;
            return Copiar(sala);
        }
    }

    public SalaDto Consultar(string conexion) { lock (_candado) { return Copiar(Obtener(conexion)); } }

    private Sala Obtener(string conexion)
    {
        if (!_conexiones.TryGetValue(conexion, out var codigo))
            throw new ArgumentException("Entra a una sala para participar.");
        return _salas[codigo];
    }
    private static SalaDto Copiar(Sala sala) =>
        new(sala.Codigo, sala.Revision, sala.Miembros.Values.ToArray(), sala.Mensajes.ToArray());
}
