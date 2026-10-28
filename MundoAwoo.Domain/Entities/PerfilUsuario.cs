namespace MundoAwoo.Domain.Entities;

public class PerfilUsuario
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private PerfilUsuario()
    {
    }

    public Guid Id { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string NombreVisible { get; private set; } = string.Empty;
    public string Biografia { get; private set; } = string.Empty;

    // internal limita la creación a Domain: la aplicación crea el perfil mediante Usuario.
    internal PerfilUsuario(Guid usuarioId, string nombreVisible)
    {
        if (usuarioId == Guid.Empty)
            throw new ArgumentException("El usuario es obligatorio.", nameof(usuarioId));

        Id = Guid.NewGuid();
        UsuarioId = usuarioId;
        Actualizar(nombreVisible, string.Empty);
    }

    internal void Actualizar(string nombreVisible, string biografia)
    {
        if (string.IsNullOrWhiteSpace(nombreVisible) || nombreVisible.Trim().Length > 50)
            throw new ArgumentException("El nombre visible debe tener entre 1 y 50 caracteres.", nameof(nombreVisible));

        ArgumentNullException.ThrowIfNull(biografia);

        if (biografia.Trim().Length > 500)
            throw new ArgumentException("La biografía permite hasta 500 caracteres.", nameof(biografia));

        // Validamos todo antes de asignar: un error no deja el perfil a medio actualizar.
        NombreVisible = nombreVisible.Trim();
        Biografia = biografia.Trim();
    }
}
