using Finbuckle.MultiTenant;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Domain
{
    public class ContextTenant
    {
        public ExtendTenantInfo TenantInfo { get; set; }
    }
}
