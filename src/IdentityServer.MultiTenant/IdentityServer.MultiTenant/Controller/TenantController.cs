using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Const;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.Repository;
using IdentityServer.MultiTenant.Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Controller
{
    [Route("{__tenant__=}/api/[controller]/[action]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly TenantRepository _tenantRepo;
        private readonly ILogger<TenantController> _logger;
        private readonly ITenantDbOperation _dbOperaService;
        private readonly EncryptService _encryptService;
        private readonly DbServerRepository _dbServerRepository;
        public TenantController(EncryptService  encryptService, ITenantDbOperation dbOperaService, TenantRepository tenantRepo,ILogger<TenantController> logger,
            DbServerRepository dbServerRepository) {
            _dbOperaService = dbOperaService;
            _tenantRepo = tenantRepo;
            _logger = logger;
            _encryptService = encryptService;
            _dbServerRepository = dbServerRepository;
        }

        [HttpPost]
        [Authorize(Policy = "manageTenantPolicy")] //(Policy  = "sysManagePolicy")
        public AppResponseDto CreateTenant([FromBody]TenantInfoDto tenantInfoDto) {
            if (string.IsNullOrEmpty(tenantInfoDto.Identifier)) {
                return new AppResponseDto(false) { ErrorMsg="tenant identifier can not be empty"};
            }

            string tenantDomain = string.Empty;

            var clientDomainClaim= Request.HttpContext.User.Claims.FirstOrDefault(x=>x.Type=="client_"+ MulTenantConstants.ClientDomainClaim);
            if (clientDomainClaim != null) {
                tenantDomain = clientDomainClaim.Value;
            } else {
                string requestHost = Request.Host.Value;
                int tmpStartIndx = requestHost.IndexOf('.');
                int tmpEndIndex = requestHost.LastIndexOf(':');
                tenantDomain = requestHost.Substring(tmpStartIndx + 1, tmpEndIndex - tmpStartIndx - 1);
            }

            if (string.IsNullOrEmpty(tenantDomain)) {
                return new AppResponseDto(false) {ErrorMsg="tenant domain cann't be empty" };
            }
            tenantInfoDto.TenantDomain = tenantDomain;

            bool isExist= _tenantRepo.ExistTenant(tenantDomain, tenantInfoDto.Identifier,out TenantInfoDto existedTenantInfo);

            if (isExist) {
                return new AppResponseDto(false) { ErrorMsg="tenant existed"};
            }

            tenantInfoDto.GuidId = Guid.NewGuid().ToString("N");
            //先插tenant基本信息
            if(!_tenantRepo.AddOrUpdateTenant(tenantInfoDto,out string errMsg, true)) {
                return new AppResponseDto(false) { ErrorMsg=errMsg};
            }

            _tenantRepo.ExistTenant(tenantDomain, tenantInfoDto.Identifier, out tenantInfoDto);

            bool realResult = false;

            //创建数据库
            bool createDbResult = _dbOperaService.CreateTenantDb(ref tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
            if (createDbResult) {
                _dbServerRepository.AddDbCountByDbserver(dbServer);
                if (_tenantRepo.AttachDbServerToTenant(tenantInfoDto, dbServer,out errMsg)) {
                    realResult = true;
                }
            }

            if (!realResult) {
                //回退
                
                if (!string.IsNullOrEmpty(creatingDbName)) {
                    //开启线程删数据库
                    Task.Run(() => {
                        _dbOperaService.DeleteTenantDb(dbServer, creatingDbName);
                        _dbServerRepository.AddDbCountByDbserver(dbServer, false);
                    }).ConfigureAwait(false);
                }

                if (_tenantRepo.ExistTenant(tenantDomain, tenantInfoDto.Identifier, out existedTenantInfo)) {
                    _tenantRepo.RemoveTenant(existedTenantInfo.Id);
                }
            }

            return new AppResponseDto(realResult);
        }
    }
}
