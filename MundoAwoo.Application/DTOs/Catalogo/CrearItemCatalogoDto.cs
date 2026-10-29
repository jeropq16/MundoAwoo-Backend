namespace MundoAwoo.Application.DTOs.Catalogo;

public class CrearItemCatalogoDto
{
    public string Nombre { get; set; } = string.Empty;
    public Guid CategoriaId { get; set; }
    public string Ranura { get; set; } = string.Empty;
    public string RecursoVisual { get; set; } = string.Empty;
}
