using Microsoft.AspNetCore.DataProtection;

namespace Nova.UnitTests.Fakes;

/// <summary>
/// 测试用无操作 IDataProtector：透传字节，不做任何加密。
/// 仅满足 AddDefaultTokenProviders 对 UserManager 的依赖，
/// 测试本身不会真正生成/校验 DataProtector Token（如密码重置、2FA）。
/// </summary>
public sealed class NoopDataProtector : IDataProtector
{
    public IDataProtector CreateProtector(string purpose) => this;
    public byte[] Protect(byte[] plaintext) => plaintext;
    public byte[] Unprotect(byte[] protectedData) => protectedData;
}

/// <summary>
/// 测试用无操作 IDataProtectionProvider，始终返回 <see cref="NoopDataProtector"/>。
/// </summary>
public sealed class NoopDataProtectionProvider : IDataProtectionProvider
{
    public IDataProtector CreateProtector(string purpose) => new NoopDataProtector();
}
