using MundoAwoo.Domain.Enums;

namespace MundoAwoo.Domain.Entities;

// Define un ítem compartido del catálogo. No representa su propietario.
public class ItemDeAvatar
{
    // EF reconstruye datos existentes sin ejecutar las reglas de creación de un objeto nuevo.
    // Es privado: el resto de la aplicación sigue usando el constructor con validaciones.
    private ItemDeAvatar()
    {
        Nombre = null!;
        RecursoVisual = null!;
    }

    public Guid Id { get; private set; }
    public string Nombre { get; private set; }
    public Guid CategoriaId { get; private set; }
    public RanuraAvatar Ranura { get; private set; }
    public string RecursoVisual { get; private set; }

    public ItemDeAvatar(string nombre, Categoria categoria, RanuraAvatar ranura, string recursoVisual)
    {
        ArgumentNullException.ThrowIfNull(categoria);

        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 100)
            throw new ArgumentException("El nombre debe tener entre 1 y 100 caracteres.", nameof(nombre));

        if (!Enum.IsDefined(ranura))
            throw new ArgumentOutOfRangeException(nameof(ranura), "La ranura no existe.");

        if (!categoria.PermiteRanura(ranura))
            throw new ArgumentException("La ranura no corresponde al tipo de categoría.", nameof(ranura));

        if (string.IsNullOrWhiteSpace(recursoVisual) || recursoVisual.Trim().Length > 500)
            throw new ArgumentException("La referencia visual debe tener entre 1 y 500 caracteres.", nameof(recursoVisual));

        Id = Guid.NewGuid();
        Nombre = nombre.Trim();
        CategoriaId = categoria.Id;
        Ranura = ranura;
        // Guardamos una referencia al recurso. Domain no carga imágenes ni animaciones.
        RecursoVisual = recursoVisual.Trim();
    }
}
