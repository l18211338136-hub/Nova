using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Nova.Contracts.DependencyInjection;
using Nova.Framework.Web.Logging;

namespace Nova.Modules.Audit.Domain.Services;

public class DefaultSanitizerEngine : ISanitizerEngine, ISingletonDependency
{
    private readonly ISensitiveRuleProvider _ruleProvider;

    public DefaultSanitizerEngine(ISensitiveRuleProvider? ruleProvider = null)
    {
        _ruleProvider = ruleProvider ?? new DefaultSensitiveRuleProvider();
    }

    public SanitizerResult Sanitize(string inputJsonOrText)
    {
        if (string.IsNullOrWhiteSpace(inputJsonOrText))
        {
            return new SanitizerResult(inputJsonOrText, Array.Empty<MaskedFieldInfo>());
        }

        var maskedFields = new List<MaskedFieldInfo>();
        var sensitiveKeys = _ruleProvider.GetSensitiveKeys();

        try
        {
            var node = JsonNode.Parse(inputJsonOrText);
            if (node is not null)
            {
                SanitizeJsonNode(node, sensitiveKeys, maskedFields);
                var sanitizedJson = node.ToJsonString(new JsonSerializerOptions { WriteIndented = false });
                return new SanitizerResult(sanitizedJson, maskedFields);
            }
        }
        catch (JsonException)
        {
            // 非有效 JSON，回退到正则处理
        }

        return SanitizePlainText(inputJsonOrText, sensitiveKeys);
    }

    private static void SanitizeJsonNode(JsonNode node, IReadOnlySet<string> sensitiveKeys, List<MaskedFieldInfo> maskedFields)
    {
        if (node is JsonObject obj)
        {
            foreach (var property in obj.ToList())
            {
                if (sensitiveKeys.Contains(property.Key))
                {
                    obj[property.Key] = "***SENSITIVE***";
                    maskedFields.Add(new MaskedFieldInfo(property.Key, "SensitiveKeyMask"));
                }
                else if (property.Value is not null)
                {
                    SanitizeJsonNode(property.Value, sensitiveKeys, maskedFields);
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                if (item is not null)
                {
                    SanitizeJsonNode(item, sensitiveKeys, maskedFields);
                }
            }
        }
    }

    private static SanitizerResult SanitizePlainText(string text, IReadOnlySet<string> sensitiveKeys)
    {
        var maskedFields = new List<MaskedFieldInfo>();
        if (sensitiveKeys.Count == 0) return new SanitizerResult(text, maskedFields);

        var keysPattern = string.Join("|", sensitiveKeys.Select(Regex.Escape));
        var pattern = $@"(?i)""?({keysPattern})""?\s*[:=]\s*""?([^""\s,}}]+)""?";

        var sanitized = Regex.Replace(text, pattern, match =>
        {
            var fieldName = match.Groups[1].Value;
            maskedFields.Add(new MaskedFieldInfo(fieldName, "RegexTextMask"));
            return $"\"{fieldName}\": \"***SENSITIVE***\"";
        });

        return new SanitizerResult(sanitized, maskedFields);
    }
}
