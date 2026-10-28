using MundoAwoo.Application.Exceptions;

namespace MundoAwoo.Api.Middlewares;

// Convierte excepciones en respuestas HTTP en un único lugar.
public class ExceptionMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ExceptionMiddleware> _logger;

    public ExceptionMiddleware(RequestDelegate siguiente, ILogger<ExceptionMiddleware> logger)
    {
        _siguiente = siguiente;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _siguiente(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            // Si el cliente se desconectó, no intentamos escribir un error en esa conexión.
            context.Abort();
        }
        catch (Exception exception) when (!context.Response.HasStarted)
        {
            int estado;
            string titulo;
            string detalle;

            if (exception is CredencialesInvalidasException)
            {
                estado = StatusCodes.Status401Unauthorized;
                titulo = "No se pudo iniciar sesión";
                detalle = exception.Message;
            }
            else if (exception is CorreoYaRegistradoException)
            {
                estado = StatusCodes.Status409Conflict;
                titulo = "Correo ya registrado";
                detalle = exception.Message;
            }
            else if (exception is SolicitudInvalidaException)
            {
                // Este tipo contiene únicamente mensajes preparados para el usuario.
                estado = StatusCodes.Status400BadRequest;
                titulo = "Datos inválidos";
                detalle = exception.Message;
            }
            else if (exception is UsuarioNoEncontradoException)
            {
                estado = StatusCodes.Status404NotFound;
                titulo = "Perfil no encontrado";
                detalle = "No existe un usuario asociado al identificador autenticado.";
            }
            else if (exception is ArgumentException)
            {
                // Las validaciones actuales del perfil lanzan ArgumentException.
                // No exponemos exception.Message: puede contener detalles internos.
                estado = StatusCodes.Status400BadRequest;
                titulo = "Datos inválidos";
                detalle = "Revisa los datos enviados y sus límites de longitud.";
            }
            else
            {
                estado = StatusCodes.Status500InternalServerError;
                titulo = "Error interno";
                detalle = "No se pudo completar la operación. Inténtalo de nuevo más tarde.";
                _logger.LogError(exception, "Error procesando la solicitud {TraceId}", context.TraceIdentifier);
            }

            context.Response.Clear();
            context.Response.Headers.CacheControl = "no-store";
            if (estado == StatusCodes.Status401Unauthorized)
                context.Response.Headers.WWWAuthenticate = "Bearer";
            await Results.Problem(statusCode: estado, title: titulo, detail: detalle,
                extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier })
                .ExecuteAsync(context);
        }
    }
}
