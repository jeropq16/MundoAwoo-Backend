using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MundoAwoo.Api.Security;

// En Swagger solo el perfil pide Bearer; registro y login siguen públicos.
public class JwtAuthorizeOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        Type? controlador = context.MethodInfo.DeclaringType;
        bool anonimo = context.MethodInfo.IsDefined(typeof(AllowAnonymousAttribute), true)
            || controlador?.IsDefined(typeof(AllowAnonymousAttribute), true) == true;
        bool protegido = context.MethodInfo.IsDefined(typeof(AuthorizeAttribute), true)
            || controlador?.IsDefined(typeof(AuthorizeAttribute), true) == true;

        if (anonimo || !protegido)
            return;

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = Array.Empty<string>()
        });
    }
}
