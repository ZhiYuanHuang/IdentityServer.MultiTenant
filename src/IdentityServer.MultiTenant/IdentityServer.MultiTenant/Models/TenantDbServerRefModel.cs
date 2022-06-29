using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Models
{
    public class TenantDbServerRefModel
    {
        public Int64 Id { get; set; }
        public Int64 TenantId { get; set; }
        public Int64 DbServerId { get; set; }
        public Int64? OldDbServerId { get; set; }
    }
}
