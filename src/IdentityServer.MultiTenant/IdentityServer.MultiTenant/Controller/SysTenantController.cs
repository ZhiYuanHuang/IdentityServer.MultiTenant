using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Framework.Utils;
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
using System.Text;
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
                _dbServerRepository.AddDbCountByDbserver(dbServer);
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
                        _dbServerRepository.AddDbCountByDbserver(dbServer, false);
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

            _dbServerRepository.AddDbCountByDbserver(dbServer, false);

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

        [HttpPost]
        public async Task<AppResponseDto> MigrateTenant([FromBody] TenantInfoDto tenantInfoDto) {
            if (string.IsNullOrEmpty(tenantInfoDto.TenantDomain) || string.IsNullOrEmpty(tenantInfoDto.Identifier)) {
                return new AppResponseDto(false) { ErrorMsg = "tenant can not be empty" };
            }

            Int64? toMigrateDbServerId = tenantInfoDto.DbServerId;
            if (!toMigrateDbServerId.HasValue) {
                return new AppResponseDto(false) { ErrorMsg = "migrate db server can not empty" };
            }

            bool isExist = _tenantRepo.ExistTenant(tenantInfoDto.TenantDomain, tenantInfoDto.Identifier, out TenantInfoDto existedTenantInfo);

            if (!isExist) {
                return new AppResponseDto(false) { ErrorMsg = "tenant not existed" };
            }

            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)){
                return new AppResponseDto(false) { ErrorMsg="tenant db conn is empty"};
            }

            if(!existedTenantInfo.DbServerId.HasValue) {
                return new AppResponseDto(false) { ErrorMsg = "tenant db server is empty" };
            }

            string originDbConn = existedTenantInfo.ConnectionString;
            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && !string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)) {
                originDbConn = _encryptService.Decrypt_Aes(existedTenantInfo.EncryptedIdsConnectionString);
            }

            Tuple<bool,string> originDbConnResult = await _dbOperaService.CheckConnectAndVersion(originDbConn);
            if (!originDbConnResult.Item1) {
                return new AppResponseDto(false) { ErrorMsg = "tenant db can not connect" };
            }
            string originVersion = DbConnStrExtension.GetMysqlGeneralVersion(originDbConnResult.Item2);

            existedTenantInfo.ConnectionString = originDbConn;

            var dbServerList= _dbServerRepository.GetDbServers(existedTenantInfo.DbServerId.Value);
            if(!dbServerList.Any() || dbServerList[0].Id != existedTenantInfo.DbServerId.Value) {
                return new AppResponseDto(false) { ErrorMsg = "tenant origin db can not match" };
            }
            var originDbServer = dbServerList[0];

            dbServerList = _dbServerRepository.GetDbServers(toMigrateDbServerId.Value);
            if (!dbServerList.Any() || dbServerList[0].Id != toMigrateDbServerId.Value) {
                return new AppResponseDto(false) { ErrorMsg = "can not found to migrate db server" };
            }
            var toMigratingDbServer = dbServerList[0];

            if (string.IsNullOrEmpty(toMigratingDbServer.Userpwd) && !string.IsNullOrEmpty(toMigratingDbServer.EncryptUserpwd)) {
                toMigratingDbServer.Userpwd = _encryptService.Decrypt_Aes(toMigratingDbServer.EncryptUserpwd);
            }
            var migrateDbConnResult =await MysqlDbOperaService.CheckConnectAndVersion(toMigratingDbServer);
            if (!migrateDbConnResult.Item1) {
                return new AppResponseDto(false) { ErrorMsg = "to migrate db can not connect" };
            }
            string toMigrateVersion = DbConnStrExtension.GetMysqlGeneralVersion(migrateDbConnResult.Item2);
            if (string.Compare(originVersion, toMigrateVersion, true) != 0) {
                return new AppResponseDto(false) { ErrorMsg = "cann't migrate between not same version mysql" };
            }

            StringBuilder migrateBuilder = new StringBuilder();
            if (!_dbOperaService.MigrateTenantDb(ref existedTenantInfo,originDbServer,toMigratingDbServer,ref migrateBuilder, out string errMsg)) {
                return new AppResponseDto(false) { ErrorMsg=errMsg};
            }

            _dbServerRepository.AddDbCountByDbserver(originDbServer,false);
            _dbServerRepository.AddDbCountByDbserver(toMigratingDbServer);

            if(!_tenantRepo.AttachDbServerToTenant(existedTenantInfo,toMigratingDbServer,out errMsg)) {
                return new AppResponseDto(false) { ErrorMsg = errMsg };
            }

            return new AppResponseDto() {

            };
        }

        [HttpGet]
        public async Task MigrateTenant([FromQuery]string TenantDomain,[FromQuery]string Identifier,[FromQuery]Int64? DbServerId) {
            var response = HttpContext.Response;
            response.StatusCode = 200;
            response.ContentType = "text/html; charset=UTF-8";

            await response.WriteAsync(getScriptResp("Migrate Start!\n"));

            await response.Body.FlushAsync();

            if (string.IsNullOrEmpty(TenantDomain) || string.IsNullOrEmpty(Identifier)) {
                await response.WriteAsync(getScriptResp("tenant can not be empty!\n",false));

                await response.Body.FlushAsync();
                await response.CompleteAsync();
                return;
            }

            Int64? toMigrateDbServerId = DbServerId;
            if (!toMigrateDbServerId.HasValue) {
                await response.WriteAsync(getScriptResp("migrate db server can not empty!\n", false));

                await response.Body.FlushAsync();
                await response.CompleteAsync();
                return;
            }

            bool isExist = _tenantRepo.ExistTenant(TenantDomain, Identifier, out TenantInfoDto existedTenantInfo);

            if (!isExist) {
                await response.WriteAsync(getScriptResp("tenant not existed!\n", false));

                await response.Body.FlushAsync();
                await response.CompleteAsync();
                return;
            }

            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)) {
                await response.WriteAsync(getScriptResp("tenant db conn is empty!\n", false));

                await response.Body.FlushAsync();
                await response.CompleteAsync();
                return;
            }

            if (!existedTenantInfo.DbServerId.HasValue) {
                await response.WriteAsync(getScriptResp("tenant db server is empty!\n", false));

                await response.Body.FlushAsync();
                await response.CompleteAsync();
                return;
            }

            string originDbConn = existedTenantInfo.ConnectionString;
            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && !string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)) {
                originDbConn = _encryptService.Decrypt_Aes(existedTenantInfo.EncryptedIdsConnectionString);
            }

            Tuple<bool, string> originDbConnResult = await _dbOperaService.CheckConnectAndVersion(originDbConn);
            if (!originDbConnResult.Item1) {
                await response.WriteAsync(getScriptResp("tenant db can not connect!\n", false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }
            string originVersion = DbConnStrExtension.GetMysqlGeneralVersion(originDbConnResult.Item2);

            existedTenantInfo.ConnectionString = originDbConn;

            var dbServerList = _dbServerRepository.GetDbServers(existedTenantInfo.DbServerId.Value);
            if (!dbServerList.Any() || dbServerList[0].Id != existedTenantInfo.DbServerId.Value) {
                await response.WriteAsync(getScriptResp("tenant origin db can not match!\n", false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }
            var originDbServer = dbServerList[0];

            dbServerList = _dbServerRepository.GetDbServers(toMigrateDbServerId.Value);
            if (!dbServerList.Any() || dbServerList[0].Id != toMigrateDbServerId.Value) {
                await response.WriteAsync(getScriptResp("can not found to migrate db server!\n", false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }
            var toMigratingDbServer = dbServerList[0];

            if (string.IsNullOrEmpty(toMigratingDbServer.Userpwd) && !string.IsNullOrEmpty(toMigratingDbServer.EncryptUserpwd)) {
                toMigratingDbServer.Userpwd = _encryptService.Decrypt_Aes(toMigratingDbServer.EncryptUserpwd);
            }
            var migrateDbConnResult = await MysqlDbOperaService.CheckConnectAndVersion(toMigratingDbServer);
            if (!migrateDbConnResult.Item1) {
                await response.WriteAsync(getScriptResp("to migrate db can not connect!\n", false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }
            string toMigrateVersion = DbConnStrExtension.GetMysqlGeneralVersion(migrateDbConnResult.Item2);
            if (string.Compare(originVersion, toMigrateVersion, true) != 0) {
                await response.WriteAsync(getScriptResp("cann't migrate between not same version mysql!\n", false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }

            StringBuilder builder = new StringBuilder();

            bool migrateEnd = false;
            var sendMsgTask= Task.Run( ()=> {
                int tmpLength = builder.Length;
                while (!migrateEnd) {
                    string tmpMsg = string.Empty;
                    int tmpNewLength = builder.Length;
                    if (tmpNewLength > tmpLength) {
                        tmpMsg=builder.ToString(tmpLength, tmpNewLength - tmpLength);

                        response.WriteAsync(getScriptResp(tmpMsg)).Wait();

                        response.Body.Flush();

                        tmpLength = tmpNewLength;
                    }
                    Task.Delay(1000 * 3).Wait();
                }
            });

            if (!_dbOperaService.MigrateTenantDb(ref existedTenantInfo, originDbServer, toMigratingDbServer,ref builder, out string errMsg)) {
                migrateEnd = true;
                await sendMsgTask;

                await response.WriteAsync(getScriptResp(errMsg,false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }
            migrateEnd = true;
            await sendMsgTask;

            _dbServerRepository.AddDbCountByDbserver(originDbServer, false);
            _dbServerRepository.AddDbCountByDbserver(toMigratingDbServer);

            if (!_tenantRepo.AttachDbServerToTenant(existedTenantInfo, toMigratingDbServer, out errMsg)) {
                await response.WriteAsync(getScriptResp(errMsg, false));

                response.Body.Flush();
                await response.CompleteAsync();
                return;
            }

            //"<script> top.read('Migrate Finish!\\n');alert('迁移成功'); </script>"
            await response.WriteAsync(getScriptResp("Migrate Finish!\n",true));

            //await response.WriteAsync("<script> alert('迁移成功'); </script>");
            await response.Body.FlushAsync();
            await response.CompleteAsync();
            return;
        }

        private const string _scriptMsgTemplate = "<script> top.read('{0}') </script>";
        private const string _scriptResultTemplate = "<script> alert('{0}') </script>";
        private static string getScriptResp(string msg,bool? result = null) {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(string.Format(_scriptMsgTemplate, System.Web.HttpUtility.JavaScriptStringEncode(msg)));
            if (result.HasValue) {
                builder.AppendLine(string.Format(_scriptResultTemplate, result.Value?"迁移正常":"迁移异常"));
            }
            return builder.ToString();
        }
    }


}
