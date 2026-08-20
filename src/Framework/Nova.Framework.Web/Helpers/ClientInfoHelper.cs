using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;

namespace Nova.Framework.Web.Helpers;

public static class ClientInfoHelper
{
    public static string GetClientIp(HttpContext? context)
    {
        if (context == null) return "127.0.0.1 (本地)";

        // 1. X-Forwarded-For
        if (context.Request.Headers.TryGetValue("X-Forwarded-For", out var forwardedFor) && !string.IsNullOrWhiteSpace(forwardedFor))
        {
            var rawIp = forwardedFor.ToString().Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(rawIp)) return FormatIp(rawIp);
        }

        // 2. X-Real-IP
        if (context.Request.Headers.TryGetValue("X-Real-IP", out var realIp) && !string.IsNullOrWhiteSpace(realIp))
        {
            var rawIp = realIp.ToString().Trim();
            if (!string.IsNullOrWhiteSpace(rawIp)) return FormatIp(rawIp);
        }

        // 3. RemoteIpAddress
        var remoteIp = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(remoteIp) ? "127.0.0.1 (本地)" : FormatIp(remoteIp);
    }

    public static string FormatIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return "-";
        
        if (ip == "::1" || ip == "127.0.0.1" || ip == "::ffff:127.0.0.1" || ip.Equals("localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "127.0.0.1 (本地)";
        }
        if (ip.StartsWith("192.168.") || ip.StartsWith("10.") || (ip.StartsWith("172.") && IsPrivate172(ip)))
        {
            return $"{ip} (局域网)";
        }
        return ip;
    }

    private static bool IsPrivate172(string ip)
    {
        var parts = ip.Split('.');
        if (parts.Length >= 2 && int.TryParse(parts[1], out var secondByte))
        {
            return secondByte >= 16 && secondByte <= 31;
        }
        return false;
    }

    public static string ParseUserAgent(HttpContext? context)
    {
        if (context == null) return "Unknown Device";
        var ua = context.Request.Headers["User-Agent"].ToString();
        return ParseUserAgentString(ua);
    }

    public static string ParseUserAgentString(string? ua)
    {
        if (string.IsNullOrWhiteSpace(ua)) return "Unknown Device";

        string os = "Unknown OS";
        if (ua.Contains("Windows NT 10.0")) os = "Windows 10/11";
        else if (ua.Contains("Windows NT 6.3")) os = "Windows 8.1";
        else if (ua.Contains("Windows NT 6.1")) os = "Windows 7";
        else if (ua.Contains("Mac OS X"))
        {
            os = (ua.Contains("iPhone") || ua.Contains("iPad")) ? "iOS" : "macOS";
        }
        else if (ua.Contains("Android")) os = "Android";
        else if (ua.Contains("Linux")) os = "Linux";

        string browser = "Unknown Browser";
        if (ua.Contains("Edg/")) browser = "Edge " + GetVersion(ua, @"Edg/([\d\.]+)");
        else if (ua.Contains("Chrome/")) browser = "Chrome " + GetVersion(ua, @"Chrome/([\d\.]+)");
        else if (ua.Contains("Firefox/")) browser = "Firefox " + GetVersion(ua, @"Firefox/([\d\.]+)");
        else if (ua.Contains("Safari/") && !ua.Contains("Chrome")) browser = "Safari " + GetVersion(ua, @"Version/([\d\.]+)");
        else if (ua.Contains("PostmanRuntime")) browser = "Postman";

        return $"{browser} / {os}";
    }

    private static string GetVersion(string ua, string pattern)
    {
        var match = Regex.Match(ua, pattern);
        if (match.Success)
        {
            var fullVer = match.Groups[1].Value;
            var parts = fullVer.Split('.');
            return parts.Length > 0 ? parts[0] : fullVer;
        }
        return "";
    }
}
