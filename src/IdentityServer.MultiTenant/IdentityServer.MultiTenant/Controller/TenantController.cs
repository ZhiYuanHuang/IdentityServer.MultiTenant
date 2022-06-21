using IdentityServer.MultiTenant.Dto;
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
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly TenantRepository _tenantRepo;
        private readonly ILogger<TenantController> _logger;
        private readonly ITenantDbOperation _dbOperaService;
        private readonly EncryptService _encryptService;
        public TenantController(EncryptService  encryptService, ITenantDbOperation dbOperaService, TenantRepository tenantRepo,ILogger<TenantController> logger) {
            _dbOperaService = dbOperaService;
            _tenantRepo = tenantRepo;
            _logger = logger;
            _encryptService = encryptService;
        }

        [HttpPost]
        [Authorize(Policy = "manageTenantPolicy")] //(Policy  = "sysManagePolicy")
        public AppResponseDto CreateTenant([FromBody]TenantInfoDto tenantInfoDto) {

            bool isExist= _tenantRepo.ExistTenant(tenantInfoDto.TenantDomain,tenantInfoDto.Identifier,out TenantInfoDto existedTenantInfo);

            if (isExist) {
                return new AppResponseDto(false) { ErrorMsg="tenant existed"};
            }

            //先插tenant基本信息
            if(!_tenantRepo.AddOrUpdateTenant(tenantInfoDto,out string errMsg, true)) {
                return new AppResponseDto(false) { ErrorMsg=errMsg};
            }

            bool realResult = false;

            //创建数据库
            bool createDbResult = _dbOperaService.CreateTenantDb(ref tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
            if (createDbResult) {
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
                    }).ConfigureAwait(false);
                }

                if (_tenantRepo.ExistTenant(tenantInfoDto.TenantDomain, tenantInfoDto.Identifier, out existedTenantInfo)) {
                    _tenantRepo.RemoveTenant(existedTenantInfo.Id);
                }
            }

            return new AppResponseDto(realResult);
        }
    }
}
