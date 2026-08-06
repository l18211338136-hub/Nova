using Nova.Contracts.Constants;

namespace Nova.Modules.Identity.Domain;

public static class NovaIdentityConstants
{
    public static class Tenants
    {
        public const string RootTenantId = TenantConstants.RootTenantId;
        public const string RootTenantName = TenantConstants.RootTenantName;
        public const string RetailTenantId = "retail"; // 散户/C端用户的默认隔离租户
    }

    public static class Roles
    {
        public const string Root = "Root";
        public const string Admin = "Admin";
        public const string User = "User"; // 默认普通用户角色
    }

    public static class Seed
    {
        public const string RootUserName = "root";
        public const string RootEmail = "761516331@qq.com";
        public const string RootPassword = "qwe@123!";
    }
}
