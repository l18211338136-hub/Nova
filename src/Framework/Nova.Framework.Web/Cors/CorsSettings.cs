namespace Nova.Framework.Web.Cors;

public class CorsSettings
{
    public const string Position = "Cors";
    
    public string[]? AllowedOrigins { get; set; }
    public string[]? AllowedMethods { get; set; }
    public string[]? AllowedHeaders { get; set; }
    public bool AllowCredentials { get; set; }
}
