namespace Nova.Framework.Authorization.Abac;

public class AbacPolicyConfig
{
    public string EntityName { get; set; } = string.Empty;
    public string Logic { get; set; } = "AND"; // AND | OR
    public List<AbacRuleConfig> Rules { get; set; } = new();
    public List<AbacFieldPermissionConfig> Fields { get; set; } = new();
}

public class AbacRuleConfig
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "="; // =, !=, Like, In, Between, >=, <=
    public string? Value { get; set; }
    public string? ValueMin { get; set; }
    public string? ValueMax { get; set; }
}

public class AbacFieldPermissionConfig
{
    public string Field { get; set; } = string.Empty;
    public bool Hide { get; set; }
    public bool Mask { get; set; }
}
