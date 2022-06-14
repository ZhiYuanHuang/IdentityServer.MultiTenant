using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace IdentityServer.MultiTenant.Framework.Utils
{
    public static class DbConnStrExtension
    {
        private static string _mysqlMigrationAssemble = string.Empty;
        private static string getMysqlMigrationAssembl() {
            if (string.IsNullOrEmpty(_mysqlMigrationAssemble)) {
                _mysqlMigrationAssemble = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            }
            return _mysqlMigrationAssemble;
        }
        public static DbContextOptionsBuilder UseDbConn(this DbContextOptionsBuilder optionsBuilder, string connStr) {
            if (connStr.Contains("Port=")) {
                optionsBuilder.UseMySql(connStr,MySqlServerVersion.AutoDetect(connStr),sql=>sql.MigrationsAssembly(getMysqlMigrationAssembl()));
            } else {
                optionsBuilder.UseSqlite(connStr);
            }

            return optionsBuilder;
        }

        public static bool IsUseMysql(string connStr) {
            return connStr.Contains("Port=");
        }
    }
}
