using Finbuckle.MultiTenant;
using IdentityServer.MultiTenant.Domain;
using IdentityServer.MultiTenant.Dto;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class ValuesController : ControllerBase
    {
        ContextTenant _contextTenant;
        public ValuesController(ContextTenant contextTenant) {
            _contextTenant = contextTenant;

        }
        [HttpGet]
        [Authorize(Policy = "sysManagePolicy")] //(Policy  = "sysManagePolicy")
        public AppResponseDto Get() {
           
            return new AppResponseDto() { ErrorCode=0,ErrorMsg= _contextTenant.TenantInfo.Name };
        }

        [HttpGet("test")]
        public AppResponseDto Test() {

            return new AppResponseDto() { ErrorCode = 0, ErrorMsg = _contextTenant.TenantInfo.Name };
        }
    }
}
