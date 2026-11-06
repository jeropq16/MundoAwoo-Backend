using MundoAwoo.Domain.Entities;
using MundoAwoo.Domain.ValueObjects;

namespace MundoAwoo.Application.Interfaces;

public interface IAvatarService
{
    Avatar Crear(Guid usuarioId);
    void CambiarColorFondo(Avatar avatar, ColorHex color);
    void CambiarEstilo3D(Avatar avatar, string personaje, bool chaquetaGhetto, bool chaquetaQueen);
    void CambiarApariencia3D(Avatar avatar, string personaje, string pelo, string barba, string tatuajes, ColorHex colorCabello);
    void Equipar(Avatar avatar, ItemDeAvatar item, Usuario propietario);
    void Desequipar(Avatar avatar, Guid itemDeAvatarId);
}
