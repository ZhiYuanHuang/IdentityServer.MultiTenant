using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Models
{
    public class DbServerModel
    {
        public int Id { get; set; }
        public string ServerHost { get; set; }
        public int ServerPort { get; set; }
        public string UserName { get; set; }
        public string Userpwd { get; set; }
        public string EncryptUserpwd { get; set; }
        public int CreatedDbCount { get; set; }
        public int EnableStatus { get; set; }
    }
}
