using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Quickstart
{
    public class TenantManageViewModel
    {
        public List<TenantDomainModel> TenantDomainList { get; set; }
        public string SelectTenantDomain { get; set; }
        public List<TenantInfoDto> TenantInfoList { get; set; }
    }
}
