using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Nova.Contracts.Security;

namespace Nova.Framework.Web.Serialization;

/// <summary>
/// 敏感数据切面自动脱敏 JSON 转换器工厂
/// </summary>
public class DataMaskingJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        return typeToConvert == typeof(string);
    }

    public override JsonConverter? CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        return new DataMaskingStringConverter();
    }
}

public class DataMaskingStringConverter : JsonConverter<string>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return reader.GetString();
    }

    public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value);
    }
}

/// <summary>
/// 脱敏掩码生成工具类
/// </summary>
public static class DataMasker
{
    public static string Mask(string? input, DataMaskType maskType)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        return maskType switch
        {
            DataMaskType.PhoneNumber => MaskPhoneNumber(input),
            DataMaskType.Email => MaskEmail(input),
            DataMaskType.IdCard => MaskIdCard(input),
            DataMaskType.BankCard => MaskBankCard(input),
            DataMaskType.ChineseName => MaskChineseName(input),
            _ => input
        };
    }

    private static string MaskPhoneNumber(string phone)
    {
        if (phone.Length < 7) return phone;
        return Regex.Replace(phone, @"(\d{3})\d{4}(\d{4})", "$1****$2");
    }

    private static string MaskEmail(string email)
    {
        var atIndex = email.IndexOf('@');
        if (atIndex <= 1) return email;
        var name = email[..atIndex];
        var domain = email[atIndex..];

        if (name.Length <= 2)
            return $"{name[0]}*{domain}";

        return $"{name[0]}***{name[^1]}{domain}";
    }

    private static string MaskIdCard(string idCard)
    {
        if (idCard.Length < 10) return idCard;
        return Regex.Replace(idCard, @"(\d{6})\d+(\d{4})", "$1********$2");
    }

    private static string MaskBankCard(string cardNo)
    {
        if (cardNo.Length < 8) return cardNo;
        return Regex.Replace(cardNo, @"(\d{4})\d+(\d{4})", "$1****$2");
    }

    private static string MaskChineseName(string name)
    {
        if (name.Length <= 1) return name;
        if (name.Length == 2) return $"{name[0]}*";
        return $"{name[0]}*{name[^1]}";
    }
}
