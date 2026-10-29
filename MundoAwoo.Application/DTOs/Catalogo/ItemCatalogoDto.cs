namespace MundoAwoo.Application.DTOs.Catalogo;

public class ItemCatalogoDto
{
    public Guid Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public string Ranura { get; set; } = string.Empty;
    public string RecursoVisual { get; set; } = string.Empty;
}
