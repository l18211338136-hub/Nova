using System;

namespace Nova.Contracts.CQRS;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class ApiEndpointAttribute : Attribute
{
    public string Method { get; }
    public string Route { get; }
    public Type ResponseType { get; }
    public string Tag { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ApiEndpointAttribute(string method, string route, Type responseType, string tag = "")
    {
        Method = method;
        Route = route;
        ResponseType = responseType;
        Tag = tag;
    }
}
