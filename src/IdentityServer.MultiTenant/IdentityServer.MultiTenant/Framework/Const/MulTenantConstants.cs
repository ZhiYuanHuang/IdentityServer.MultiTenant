namespace IdentityServer.MultiTenant.Framework.Const
{
    internal static class MulTenantConstants
    {
        public static int TenantIdMaxLength = 64;
        public static string TenantToken = "__tenant__";

        public static string SysTenant = "sys";

        public const string SysAdminUserName = "SysAdmin";
        public const string SysAdminUserPwd = "Pass123$";

        public const string SysAdminRole = "sysadmin";
    }
}
