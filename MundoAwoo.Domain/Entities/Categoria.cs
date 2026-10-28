using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

public class Categoria
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private Categoria()
    {
        Nombre = null!;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public TipoCategoria Tipo { get; private set; }

    public Categoria(string nombre, TipoCategoria tipo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50)
            throw new ArgumentException("El nombre debe tener entre 1 y 50 caracteres.", nameof(nombre));

        // Un enum también puede recibir números inválidos mediante una conversión.
        if (!Enum.IsDefined(tipo))
            throw new ArgumentOutOfRangeException(nameof(tipo), "El tipo de categoría no existe.");

        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        Tipo = tipo;
    }

    public bool PermiteRanura(RanuraAvatar ranura)
    {
        // Regla inicial del catálogo; podremos ampliarla cuando existan más prendas.
        switch (Tipo)
        {
            case TipoCategoria.Ropa:
                return ranura == RanuraAvatar.Torso || ranura == RanuraAvatar.Piernas || ranura == RanuraAvatar.Pies;
            case TipoCategoria.Accesorio:
                return ranura == RanuraAvatar.Cabeza || ranura == RanuraAvatar.Ojos || ranura == RanuraAvatar.Cuello;
            case TipoCategoria.Baile:
            case TipoCategoria.Gesto:
                return ranura == RanuraAvatar.Animacion;
            default:
                return false;
        }
    }
}
