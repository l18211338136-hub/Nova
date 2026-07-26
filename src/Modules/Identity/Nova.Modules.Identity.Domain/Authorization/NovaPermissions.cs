namespace Nova.Modules.Identity.Domain.Authorization;

public static class NovaPermissions
{
    public const string ClaimType = "Permission";

    public static class Users
    {
        public const string Default = "identity:users";
        public const string Create = "identity:users:create";
        public const string Update = "identity:users:update";
        public const string Delete = "identity:users:delete";
        public const string AssignRole = "identity:users:assign_role";
    }

    public static class Roles
    {
        public const string Default = "identity:roles";
        public const string Create = "identity:roles:create";
        public const string Update = "identity:roles:update";
        public const string Delete = "identity:roles:delete";
        public const string AssignPermission = "identity:roles:assign_permission";
    }

    public static class Menus
    {
        public const string Default = "identity:menus";
        public const string Create = "identity:menus:create";
        public const string Update = "identity:menus:update";
        public const string Delete = "identity:menus:delete";
    }
}
