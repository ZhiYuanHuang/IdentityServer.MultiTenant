using IdentityServer.MultiTenant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Dto
{
    public class TenantInfoDto: TenantInfoModel
    {
        public string TenantDomain { get; set; }
        public int DomainEnableStatus { get; set; }
        public bool ConnectSuccess { get; set; }
        public Int64? DbServerId { get; set; }
        public bool UseMysql { get; set; }
    }
}
