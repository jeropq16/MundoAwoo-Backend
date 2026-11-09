using MundoAwoo.Application.DTOs.Avatares;

namespace MundoAwoo.Application.DTOs.Encuentros;

public record ParceroDto(Guid UsuarioId, string NombreVisible, AvatarDto Avatar, string Movimiento = "Reposo");
public record MensajeSalaDto(Guid Id, Guid UsuarioId, string NombreVisible, string Texto, DateTime EnviadoEnUtc);
public record SalaDto(string Codigo, long Revision, IReadOnlyList<ParceroDto> Participantes, IReadOnlyList<MensajeSalaDto> Mensajes);
