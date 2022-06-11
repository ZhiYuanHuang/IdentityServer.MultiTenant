using IdentityServer.MultiTenant.Dto;
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
    [Route("api/[controller]")]
    [ApiController]
    public class DbServerController : ControllerBase
    {
        private readonly DbServerRepository _dbServerRepo;
        private readonly EncryptService _encryptService;
        public DbServerController(EncryptService encryptService, DbServerRepository dbServerRepository) {
            _dbServerRepo = dbServerRepository;
            _encryptService = encryptService;
        }

        [HttpPost("addorupdate")]
        [Authorize(Policy = "sysManagePolicy")]
        public AppResponseDto AddOrUpdate([FromBody]DbServerModel dbServerModel) {
            if(string.IsNullOrEmpty(dbServerModel.EncryptUserpwd)) {
                dbServerModel.EncryptUserpwd = _encryptService.Encrypt_Aes(dbServerModel.Userpwd);
                dbServerModel.Userpwd = null;
            }
            bool result= _dbServerRepo.AddDbServer(dbServerModel);
            return new AppResponseDto(result);
        }
    }
}
