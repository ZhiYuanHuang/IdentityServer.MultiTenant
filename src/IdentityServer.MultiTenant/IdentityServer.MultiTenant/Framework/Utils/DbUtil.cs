using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace IdentityServer.MultiTenant.Framework.Utils
{
    public class DbUtil
    {
        public IDbFunc Master { get; set; }
        public IDbFunc Slave { get; set; }
    }

    public static class DbUtilExtension
    {
        public static IServiceCollection AddDbUtil(this IServiceCollection services,Action<DbUtilOption> dbUtilOptionAction) {
            services.AddSingleton<DbUtilOption>();

            return services.AddSingleton<DbUtil>((provider) => {
                var dbUtilOption=provider.GetService<DbUtilOption>();
                dbUtilOptionAction(dbUtilOption);

                var loggerFactory= provider.GetRequiredService<ILoggerFactory>();

                IDbFunc masterDb = null;
                IDbFunc slaveDb = null;
                if (DbConnStrExtension.IsUseMysql(dbUtilOption.MasterConnStr)) {
                    masterDb = new MySqlDb(loggerFactory,dbUtilOption.MasterConnStr);
                } else {
                    masterDb = new SqliteDb(loggerFactory,dbUtilOption.MasterConnStr);
                }

                if (DbConnStrExtension.IsUseMysql(dbUtilOption.SlaveConnStr)) {
                    slaveDb = new MySqlDb(loggerFactory, dbUtilOption.SlaveConnStr);
                } else {
                    slaveDb = new SqliteDb(loggerFactory, dbUtilOption.SlaveConnStr);
                }

                return new DbUtil() { 
                    Master= masterDb,
                    Slave=slaveDb
                };
            });
        }
    }

    public class DbUtilOption
    {
        public string MasterConnStr { get; set; }
        public string SlaveConnStr { get; set; }
    }
}
