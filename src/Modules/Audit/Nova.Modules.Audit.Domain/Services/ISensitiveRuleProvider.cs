namespace Nova.Modules.Audit.Domain.Services;

public interface ISensitiveRuleProvider
{
    IReadOnlySet<string> GetSensitiveKeys();
}
