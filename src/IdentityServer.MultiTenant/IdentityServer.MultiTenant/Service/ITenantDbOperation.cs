using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public interface ITenantDbOperation
    {
        bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
        bool DeleteTenantDb(DbServerModel dbServer, string toDeleteDb);
        Task<bool> CheckConnect(string connStr);

        bool DeleteTenantDb(DbServerModel dbServer, TenantInfoDto tenantInfo, out string errMsg);

        bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto, DbServerModel originDbServer, DbServerModel dbServer, out string errMsg);
    }
}
