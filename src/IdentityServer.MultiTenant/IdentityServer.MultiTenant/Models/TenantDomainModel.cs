using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Models
{
    public class TenantDomainModel
    {
        public Int64 Id { get; set; }
        public string TenantDomain { get; set; }
        /// <summary>
        /// 启用状态
        /// 1：启用，0：禁用
        /// </summary>
        public int EnableStatus { get; set; }
        public string Description { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
    }
}
