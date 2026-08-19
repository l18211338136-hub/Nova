using Nova.Contracts.Security;
using Nova.Framework.Web.Serialization;
using Xunit;

namespace Nova.UnitTests;

public class DataMaskingTests
{
    [Theory]
    [InlineData("13812345678", "138****5678")]
    [InlineData("15900001111", "159****1111")]
    public void MaskPhoneNumber_ShouldMaskMiddleDigits(string phone, string expected)
    {
        var result = DataMasker.Mask(phone, DataMaskType.PhoneNumber);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("testuser@gmail.com", "t***r@gmail.com")]
    [InlineData("admin@nova.com", "a***n@nova.com")]
    public void MaskEmail_ShouldMaskLocalPart(string email, string expected)
    {
        var result = DataMasker.Mask(email, DataMaskType.Email);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("110101199003072345", "110101********2345")]
    public void MaskIdCard_ShouldMaskBirthAndSeqDigits(string idCard, string expected)
    {
        var result = DataMasker.Mask(idCard, DataMaskType.IdCard);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("张三", "张*")]
    [InlineData("李小龙", "李*龙")]
    public void MaskChineseName_ShouldMaskMiddleChars(string name, string expected)
    {
        var result = DataMasker.Mask(name, DataMaskType.ChineseName);
        Assert.Equal(expected, result);
    }
}
