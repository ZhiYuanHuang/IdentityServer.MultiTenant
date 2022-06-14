using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;

namespace IdentityServer.MultiTenant.Service
{
    public interface ITenantDbOperation
    {
        bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
        void DeleteTenantDb(DbServerModel dbServer, string toDeleteDb);
    }
}
