using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.Repository;
using IdentityServer.MultiTenant.Service;
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
    public class DbServerController : ControllerBase
    {
        private readonly DbServerRepository _dbServerRepo;
        private readonly EncryptService _encryptService;
        public DbServerController(EncryptService encryptService, DbServerRepository dbServerRepository) {
            _dbServerRepo = dbServerRepository;
            _encryptService = encryptService;
        }

        [HttpPost]
        public AppResponseDto Add([FromBody]DbServerModel dbServerModel) {
            if(string.IsNullOrEmpty(dbServerModel.ServerHost) || dbServerModel.ServerPort==0
                || string.IsNullOrEmpty(dbServerModel.UserName)
                || string.IsNullOrEmpty(dbServerModel.Userpwd)) {
                return new AppResponseDto(false) { ErrorMsg="参数缺失"};
            }

            dbServerModel.EncryptUserpwd = _encryptService.Encrypt_Aes(dbServerModel.Userpwd);
            dbServerModel.Userpwd = null;

            bool result= _dbServerRepo.AddDbServer(dbServerModel);
            return new AppResponseDto(result);
        }

        [HttpGet]
        public AppResponseDto Delete([FromQuery]Int64 dbServerId) {
            if (dbServerId <= 0) {
                return new AppResponseDto(false);
            }

            if (_dbServerRepo.GetDbServerRef(dbServerId) > 0) {
                return new AppResponseDto(false) { ErrorMsg="请先迁移改库绑定的tenant"};
            }

            bool result= _dbServerRepo.DeleteDbServer(dbServerId);
            return new AppResponseDto(result);
        }

        [HttpGet]
        public AppResponseDto ChangeStatus([FromQuery] Int64 dbServerId) {
            if (dbServerId <= 0) {
                return new AppResponseDto(false);
            }

            var list = _dbServerRepo.GetDbServers(dbServerId);
            if (!list.Any() || list[0].Id!=dbServerId) {
                return new AppResponseDto(false) { ErrorMsg = "db server 不存在" };
            }

            var existed = list[0];
            int changingStatus = (int)EnableStatusEnum.Disable;
            if (existed.EnableStatus == (int)EnableStatusEnum.Enable) {
                changingStatus = (int)EnableStatusEnum.Disable;
            } else {
                changingStatus = (int)EnableStatusEnum.Enable;
            }

            _dbServerRepo.ChangeStatus(dbServerId, changingStatus);
            return new AppResponseDto();
        }
    }
}
