using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi.Models;

namespace Nova.Framework.Web.OpenApi;

public class JwtBearerDocumentTransformer : IOpenApiDocumentTransformer
{
    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken)
    {
        document.Components ??= new OpenApiComponents();
        
        // Define the Security Scheme
        var scheme = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Name = "Authorization",
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter 'Bearer' [space] and then your valid token in the text input below."
        };

        document.Components.SecuritySchemes.Add("Bearer", scheme);

        return Task.CompletedTask;
    }
}
