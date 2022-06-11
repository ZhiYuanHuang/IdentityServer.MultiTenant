using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;

namespace IdentityServer.MultiTenant.Framework.Utils
{
    public class DbUtil
    {
        public MySqlDb Master { get; set; }
        public MySqlDb Slave { get; set; }
    }

    public static class DbUtilExtension
    {
        public static IServiceCollection AddDbUtil(this IServiceCollection services,Action<DbUtilOption> dbUtilOptionAction) {
            services.AddSingleton<DbUtilOption>();
            return services.AddSingleton<DbUtil>((provider) => {
                var dbUtilOption=provider.GetService<DbUtilOption>();
                dbUtilOptionAction(dbUtilOption);

                var log= provider.GetRequiredService<ILogger<MySqlDb>>();

                MySqlDb mastetDb = new MySqlDb(log,dbUtilOption.MasterConnStr);
                log= provider.GetRequiredService<ILogger<MySqlDb>>();
                MySqlDb slaveDb = new MySqlDb(log,dbUtilOption.SlaveConnStr);
                return new DbUtil() { 
                    Master=mastetDb,
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
