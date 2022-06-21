using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Controller
{
    [Route("{__tenant__=}/api/[controller]/[action]")]
    [ApiController]
    [Authorize(Policy = "sysManagePolicy")]
    public class DomainController : ControllerBase
    {
        private readonly TenantRepository _tenantRepo;
        public DomainController(TenantRepository tenantRepo) {
            _tenantRepo = tenantRepo;
        }

        [HttpPost]
        public AppResponseDto AddOrUpdate([FromBody] TenantDomainModel tenantDomain) {
            if (string.IsNullOrEmpty(tenantDomain.TenantDomain)) {
                return new AppResponseDto(false) { ErrorMsg="Domain 不可为空"};
            }

            if (!System.Text.RegularExpressions.Regex.IsMatch(tenantDomain.TenantDomain, "^(?=^.{3,255}$)[a-zA-Z0-9][-a-zA-Z0-9]{0,62}(\\.[a-zA-Z0-9][-a-zA-Z0-9]{0,62})+$")) {
                return new AppResponseDto(false) { ErrorMsg = "Domain 应为合法域名" };
            }

            bool result = _tenantRepo.AddOrUpdateTenantDomain(tenantDomain,out string errMsg);

            return new AppResponseDto(result) { ErrorMsg=result?string.Empty:errMsg};
        }

        [HttpGet]
        public AppResponseDto Delete([FromQuery]string tenantDomain) {
            bool result = _tenantRepo.DeleteTenantDomain(tenantDomain);
            return new AppResponseDto(result);
        }

        [HttpGet]
        public AppResponseDto ChangeStatus([FromQuery]string tenantDomain) {
            if (string.IsNullOrEmpty(tenantDomain)) {
                return new AppResponseDto(false) { ErrorMsg="domain 为空"};
            }

            var list= _tenantRepo.GetTenantDomains(tenantDomain);
            if(!list.Any() || string.CompareOrdinal(list[0].TenantDomain, tenantDomain) != 0) {
                return new AppResponseDto(false) { ErrorMsg="domain 不存在"};
            }

            var existedDomain = list[0];
            if (existedDomain.EnableStatus == (int)EnableStatusEnum.Enable) {
                existedDomain.EnableStatus = (int)EnableStatusEnum.Disable;
            } else {
                existedDomain.EnableStatus = (int)EnableStatusEnum.Enable;
            }

            bool result= _tenantRepo.AddOrUpdateTenantDomain(existedDomain,out string errMsg,false);
            return new AppResponseDto(result) { ErrorMsg=result?string.Empty:errMsg};
        }
    }
}
