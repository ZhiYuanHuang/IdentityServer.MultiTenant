using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Enum;
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
    [Authorize(Policy = "sysManagePolicy")]
    public class SysTenantController : ControllerBase
    {
        private readonly TenantRepository _tenantRepo;
        private readonly ILogger<SysTenantController> _logger;
        private readonly ITenantDbOperation _dbOperaService;
        private readonly EncryptService _encryptService;
        private readonly DbServerRepository _dbServerRepository;
        public SysTenantController(EncryptService encryptService, ITenantDbOperation dbOperaService, TenantRepository tenantRepo, ILogger<SysTenantController> logger,
            DbServerRepository dbServerRepository) {
            _dbOperaService = dbOperaService;
            _tenantRepo = tenantRepo;
            _logger = logger;
            _encryptService = encryptService;
            _dbServerRepository = dbServerRepository;
        }

        [HttpPost]
        public AppResponseDto CreateTenant([FromBody] TenantInfoDto tenantInfoDto) {

            if (string.IsNullOrEmpty(tenantInfoDto.TenantDomain) || string.IsNullOrEmpty(tenantInfoDto.Identifier)) {
                return new AppResponseDto(false) { ErrorMsg = "tenant can not be empty" };
            }


            bool isExist = _tenantRepo.ExistTenant(tenantInfoDto.TenantDomain, tenantInfoDto.Identifier, out TenantInfoDto existedTenantInfo);

            if (isExist) {
                return new AppResponseDto(false) { ErrorMsg = "tenant existed" };
            }

            tenantInfoDto.GuidId = Guid.NewGuid().ToString("N");
            //先插tenant基本信息
            if (!_tenantRepo.AddOrUpdateTenant(tenantInfoDto, out string errMsg, true)) {
                return new AppResponseDto(false) { ErrorMsg = errMsg };
            }

            _tenantRepo.ExistTenant(tenantInfoDto.TenantDomain, tenantInfoDto.Identifier, out tenantInfoDto);

            bool realResult = false;

            //创建数据库
            bool createDbResult = _dbOperaService.CreateTenantDb(ref tenantInfoDto, out DbServerModel dbServer, out string creatingDbName);
            if (createDbResult) {
                if (_tenantRepo.AttachDbServerToTenant(tenantInfoDto, dbServer, out errMsg)) {
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

        [HttpGet]
        public AppResponseDto Delete([FromQuery] string tenantDomain, [FromQuery] string Identifier) {
            if (string.IsNullOrEmpty(tenantDomain) || string.IsNullOrEmpty(Identifier)) {
                return new AppResponseDto(false) { ErrorMsg = "参数 为空" };
            }

            bool isExist = _tenantRepo.ExistTenant(tenantDomain, Identifier, out TenantInfoDto existedTenantInfo);

            if (!isExist) {
                return new AppResponseDto(false) { ErrorMsg = "tenant not exist" };
            }

            DbServerModel dbServer = null;
            if (existedTenantInfo.DbServerId.HasValue) {
                var dbServerList= _dbServerRepository.GetDbServers(existedTenantInfo.DbServerId);
                if(dbServerList.Any() && dbServerList[0].Id == existedTenantInfo.DbServerId.Value) {
                    dbServer = dbServerList[0];
                }
            }

            if(!_dbOperaService.DeleteTenantDb(dbServer,existedTenantInfo,out string errMsg)) {
                return new AppResponseDto(false) { ErrorMsg=errMsg};
            }

            _tenantRepo.RemoveTenant(existedTenantInfo.Id);
            return new AppResponseDto();
        }

        [HttpGet]
        public AppResponseDto ChangeStatus([FromQuery] string tenantDomain, [FromQuery] string Identifier) {
            if (string.IsNullOrEmpty(tenantDomain) || string.IsNullOrEmpty(Identifier)) {
                return new AppResponseDto(false) { ErrorMsg = "参数 为空" };
            }

            bool isExist = _tenantRepo.ExistTenant(tenantDomain, Identifier, out TenantInfoDto existedTenantInfo);

            if (!isExist) {
                return new AppResponseDto(false) { ErrorMsg = "tenant not exist" };
            }

            int changingStatus= (int)EnableStatusEnum.Disable;
            if (existedTenantInfo.EnableStatus == (int)EnableStatusEnum.Enable) {
                changingStatus = (int)EnableStatusEnum.Disable;
            } else {
                changingStatus = (int)EnableStatusEnum.Enable;
            }

            _tenantRepo.ChangeTenantStatus(existedTenantInfo.Id, changingStatus);
            return new AppResponseDto() ;
        }
    }


}
