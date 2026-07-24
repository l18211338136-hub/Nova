using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.DependencyInjection;

namespace Nova.Framework.Web.Extensions;

public static class ODataExtensions
{
    public static IServiceCollection AddNovaOData(this IServiceCollection services)
    {
        services.AddControllers().AddOData(options =>
        {
            options.Select().Filter().OrderBy().Expand().Count().SetMaxTop(100);
        });

        return services;
    }
}
