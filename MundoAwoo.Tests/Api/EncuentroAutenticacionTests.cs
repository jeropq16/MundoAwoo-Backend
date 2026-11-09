using System.Net;
using System.Net.Http.Headers;

namespace MundoAwoo.Tests.Api;
public class EncuentroAutenticacionTests
{
    [Fact]
    public async Task Negociar_ExigeSesionValida()
    {
        using var api = new PerfilApiFactory();
        using var cliente = api.CrearCliente();
        using var anonimo = await cliente.PostAsync("/api/encuentros/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, anonimo.StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(Guid.NewGuid().ToString()));
        using var autorizado = await cliente.PostAsync("/api/encuentros/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.OK, autorizado.StatusCode);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", api.CrearToken(Guid.NewGuid().ToString(), "vencido"));
        using var vencido = await cliente.PostAsync("/api/encuentros/negotiate?negotiateVersion=1", null);
        Assert.Equal(HttpStatusCode.Unauthorized, vencido.StatusCode);
    }
}
