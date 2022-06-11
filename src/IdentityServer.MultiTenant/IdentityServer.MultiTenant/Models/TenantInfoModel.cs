using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Models
{
    public class TenantInfoModel
    {
        public int Id { get; set; }
        public string GuidId { get; set; }
        public string Identifier { get; set; }
        public int TenantDomainId { get; set; }
        /// <summary>
        /// 启用状态
        /// 1：启用，0：禁用
        /// </summary>
        public int EnableStatus { get; set; }
        public string Name { get; set; }
        public string ConnectionString { get; set; }
        public string EncryptedIdsConnectionString { get; set; }
        public DateTime? CreateTime { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string Description { get; set; }
    }
}
