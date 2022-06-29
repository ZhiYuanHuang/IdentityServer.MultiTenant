using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IdentityServer4.EntityFramework.Entities;

namespace IdentityServer.MultiTenant.Dto
{
    public class IdsClientDto
    {
        public Client ClientInfo { get; set; }
        public string SecondDomain { get; set; }
        public string MainDomain { get; set; }
    }
}
