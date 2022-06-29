using IdentityServer.MultiTenant.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Dto
{
    public class DbServerDto:DbServerModel
    {
        public bool ConnectSuccess { get; set; }
    }
}
