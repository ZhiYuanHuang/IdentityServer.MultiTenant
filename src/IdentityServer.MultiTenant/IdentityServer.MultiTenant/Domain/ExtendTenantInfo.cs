using Finbuckle.MultiTenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Domain
{
    public class ExtendTenantInfo: ITenantInfo
    {
        public string Id { get; set; }
        public string Identifier { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string EncryptedIdsConnectionString { get; set; }
        public string TenantDomain { get; set; }
    }
}
