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
    [Route("api/[controller]")]
    [ApiController]
    public class TenantController : ControllerBase
    {
        private readonly TenantRepository _tenantRepo;
        private readonly ILogger<TenantController> _logger;
        private readonly DbOperaService _dbOperaService;
        private readonly EncryptService _encryptService;
        public TenantController(EncryptService  encryptService,DbOperaService dbOperaService, TenantRepository tenantRepo,ILogger<TenantController> logger) {
            _dbOperaService = dbOperaService;
            _tenantRepo = tenantRepo;
            _logger = logger;
            _encryptService = encryptService;
        }

        [HttpPost("AddOrUpdate")]
        [Authorize(Policy = "manageTenantPolicy")] //(Policy  = "sysManagePolicy")
        public AppResponseDto AddOrUpdate([FromBody]TenantInfoDto tenantInfoDto) {
            bool isAdd = tenantInfoDto.Id <= 0;

            bool isExist= _tenantRepo.ExistTenant(tenantInfoDto.TenantDomain,tenantInfoDto.Identifier,out TenantInfoDto existedTenantInfo);

            if (isAdd && isExist) {
                return new AppResponseDto(false) { ErrorMsg="tenant existed"};
            }

            if(!isAdd) {
                if (!isExist) {
                    return new AppResponseDto(false) { ErrorMsg = "tenant not existed" };
                } else if(tenantInfoDto.Id!=existedTenantInfo.Id){
                    return new AppResponseDto(false) { ErrorMsg = "tenant id not match" };
                }
            }

            if (!isAdd) {    //update fetch db conn
                tenantInfoDto.ConnectionString = existedTenantInfo.ConnectionString;
                tenantInfoDto.EncryptedIdsConnectionString = existedTenantInfo.EncryptedIdsConnectionString;
            }

            if(!_tenantRepo.AddOrUpdateTenant(tenantInfoDto,out string errMsg, isAdd)) {
                return new AppResponseDto(false) { ErrorMsg=errMsg};
            }

            bool createDbResult = false;
            bool realResult = false;
            if (isAdd) {   //创建ids db
                createDbResult =  _dbOperaService.CreateTenantDb(tenantInfoDto,out DbServerModel dbServer,out string creatingDbName,out string createdDbConnStr);
                if (createDbResult) {
                    tenantInfoDto.ConnectionString = createdDbConnStr;
                    //加密数据库连接
                    tenantInfoDto.EncryptedIdsConnectionString =_encryptService.Encrypt_Aes( createdDbConnStr);

                    if(_tenantRepo.AddOrUpdateTenant(tenantInfoDto,out errMsg,false)) {
                        realResult = true;
                    }
                }

                if (!realResult) {
                    if (dbServer != null && !string.IsNullOrEmpty(creatingDbName)) {
                        //开启线程删数据库
                        Task.Run(()=> {
                            _dbOperaService.DeleteTenantDb(dbServer,creatingDbName);
                        }).ConfigureAwait(false);
                    }

                    if(_tenantRepo.ExistTenant(tenantInfoDto.TenantDomain,tenantInfoDto.Identifier,out existedTenantInfo)) {
                        _tenantRepo.RemoveTenant(existedTenantInfo.Id);
                    }
                }
            }

            return new AppResponseDto(realResult);
        }
    }
}
