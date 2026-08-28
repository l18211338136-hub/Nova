namespace Nova.Framework.Authorization.Abac;

public static class AbacConstants
{
    public static class PropertyNames
    {
        public const string AbacPoliciesJson = "AbacPoliciesJson";
        public const string DataScope = "DataScope";
        public const string Id = "Id";
        public const string LeaderUserId = "LeaderUserId";
        public const string CreatedBy = "CreatedBy";
        public const string UserId = "UserId";
        public const string OrganizationId = "OrganizationId";
    }

    public static class HttpContextKeys
    {
        public const string AbacFieldConfigs = "AbacFieldConfigs";
        public const string AbacPoliciesJson = "AbacPoliciesJson";
        public const string AbacDataScope = "AbacDataScope";
        public const string AbacCurrentOrgId = "AbacCurrentOrgId";
    }

    public static class EntityNames
    {
        public const string Organization = "Organization";
    }

    public static class DynamicMacros
    {
        public const string CurrentUserId = "@CurrentUserId";
        public const string CurrentOrgId = "@CurrentOrgId";
        public const string CurrentOrgAndSubIds = "@CurrentOrgAndSubIds";
        public const string Today = "@Today";
        public const string Recent7Days = "@Recent7Days";
        public const string Recent30Days = "@Recent30Days";
        public const string ThisMonth = "@ThisMonth";
        public const string ThisYear = "@ThisYear";
    }
}
