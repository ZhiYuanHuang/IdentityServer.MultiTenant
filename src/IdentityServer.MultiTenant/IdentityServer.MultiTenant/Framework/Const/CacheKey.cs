using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Framework.Const
{
    public class CacheKey
    {
        public const string LockToGetPre = "LockToGet:{1}";

        public const string Tenant_Format = "{0}domain__{1}:identifier__{2}";
    }
}
