using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using System;
using System.Text;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public interface ITenantDbOperation
    {
        bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
        bool DeleteTenantDb(DbServerModel dbServer, string toDeleteDb,bool backup=false);
        Task<bool> CheckConnect(string connStr);
        Task<Tuple<bool, string>> CheckConnectAndVersion(string connStr);

        bool DeleteTenantDb(DbServerModel dbServer, TenantInfoDto tenantInfo, out string errMsg);

        bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto, DbServerModel originDbServer, DbServerModel dbServer,ref StringBuilder migrateLogBuilder, out string errMsg);
    }
}
