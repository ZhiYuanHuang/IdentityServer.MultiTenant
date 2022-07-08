using mvc=Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer.MultiTenant.Repository;
using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.Service;
using Microsoft.Extensions.Configuration;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Framework.Enum;

namespace IdentityServer.MultiTenant.Quickstart
{
    [Route("{__tenant__=}/{controller=Home}/{action=Index}")]
    [Authorize(Policy = "sysManagePolicy")]
    public class ManagementController : mvc.Controller
    {
        ConfigurationDbContext _configurationDbContext;
        TenantRepository _tenantRepo;
        DbServerRepository _dbServerRepo;
        private readonly EncryptService _encryptService;
        ITenantDbOperation _tenantDbOperation;

        readonly bool _useMysql;

        public ManagementController(ConfigurationDbContext configurationDbContext,
            TenantRepository tenantRepo,
            DbServerRepository dbServerRepository,
            EncryptService encryptService,
            ITenantDbOperation tenantDbOperation,
            IConfiguration configuration) {
            _configurationDbContext = configurationDbContext;
            _tenantRepo = tenantRepo;
            _dbServerRepo = dbServerRepository;
            _encryptService = encryptService;
            _tenantDbOperation = tenantDbOperation;
            _useMysql = string.Compare(configuration["SampleDb:DbType"], "mysql", true) == 0;
        }
        public IActionResult Index() {
            return View();
        }

        public IActionResult Clients() {
            var clientList= _configurationDbContext.Clients.ToList();
            var domainList= _tenantRepo.GetTenantDomains();
            var allMainDomainList= domainList.Where(x => x.ParentDomainId == null).Select(x => x).ToList();
            ViewData["MainDomainList"] = allMainDomainList;
            return View(clientList);
        }

        public IActionResult Domains() {
            var domainList = _tenantRepo.GetTenantDomains();
            return View(domainList);
        }

        public async Task<IActionResult> Dbservers() {
            var dbServerList = _dbServerRepo.GetDbServers();
            
            Dictionary<DbServerModel, DbServerDto> dict = dbServerList.ToDictionary(k => k,
                v => new DbServerDto() {
                    ServerHost=v.ServerHost,
                    ServerPort=v.ServerPort,
                    EnableStatus=v.EnableStatus,
                    Id=v.Id,
                });

            if (dbServerList.Any()) {
                List<Task> checkTaskList = new List<Task>();
                foreach( var keyValue in dict) {
                    if (keyValue.Key.EnableStatus != (int)EnableStatusEnum.Enable) {
                        continue;
                    }

                    var task= Task.Factory.StartNew( (keyValueObj) => {
                        KeyValuePair<DbServerModel, DbServerDto> tmpKeyValue = (KeyValuePair<DbServerModel, DbServerDto>)keyValueObj;

                        var dbServer= tmpKeyValue.Key;
                        if (string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
                            dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
                        }

                        tmpKeyValue.Value.ConnectSuccess =  MysqlDbOperaService.CheckConnect(keyValue.Key).Result;
                    },keyValue);
                    checkTaskList.Add(task);
                }

                await Task.WhenAll(checkTaskList);
            }

            var dtoList = dict.Values.ToList();
            return View(dtoList);
        }

        public async Task<IActionResult> Tenants([FromQuery]string tenantDomain) {
            var tenantDomainList= _tenantRepo.GetTenantDomains();
            
            string selectedTenantDomain = string.Empty;
            if (tenantDomainList.FirstOrDefault(x => string.Compare(x.TenantDomain, tenantDomain,true) == 0) != null) {
                selectedTenantDomain = tenantDomain;
            }
            var tenantList = _tenantRepo.GetTenantInfoDtos(tenantDomain);
            var dbServerList = _dbServerRepo.GetDbServers();
            Dictionary<Int64, string> dbServerDict = new Dictionary<long, string>();
            if (tenantList.Any()) {
                List<Task> taskList = new List<Task>();
                foreach (var tenant in tenantList) {
                    
                    var task = Task.Factory.StartNew((tenantInfoObj)=> {
                        TenantInfoDto tmpTenant = tenantInfoObj as TenantInfoDto;
                        string connStr = tmpTenant.ConnectionString;
                        if (string.IsNullOrEmpty(tmpTenant.ConnectionString) && !string.IsNullOrEmpty(tmpTenant.EncryptedIdsConnectionString)) {
                            connStr = _encryptService.Decrypt_Aes(tmpTenant.EncryptedIdsConnectionString);
                        }

                        tmpTenant.UseMysql = _useMysql && DbConnStrExtension.IsUseMysql(connStr);

                        if (tmpTenant.EnableStatus == (int)EnableStatusEnum.Enable) {
                            tmpTenant.ConnectSuccess = _tenantDbOperation.CheckConnect(connStr).Result;
                        }
                    },tenant);
                    taskList.Add(task);
                }

                await Task.WhenAll(taskList.ToArray());

                foreach(var tenant in tenantList) {
                    if (tenant.UseMysql) {
                        if (tenant.DbServerId.HasValue) {
                            if (!dbServerDict.ContainsKey(tenant.DbServerId.Value)) {
                                var theDbServer = dbServerList.FirstOrDefault(x => x.Id == tenant.DbServerId.Value);
                                if (theDbServer != null) {
                                    dbServerDict[tenant.DbServerId.Value] = $"{theDbServer.ServerHost}:{theDbServer.ServerPort}";
                                }
                            }
                        }
                    }
                }
            }

            ViewData["DbServerDict"] = dbServerDict;

            var mainDomainList= tenantDomainList.Where(x => x.ParentDomainId == null).Select(x => x).ToList();
            List<TenantDomainModel> orderDomainList = new List<TenantDomainModel>();
            foreach(var mainDomain in mainDomainList) {
                orderDomainList.Add(mainDomain);
                orderDomainList.AddRange( tenantDomainList.Where(x => x.ParentDomainId == mainDomain.Id).Select(x => x));
            }

            var viewModel = new TenantManageViewModel() { 
                TenantDomainList= orderDomainList,
                SelectTenantDomain=selectedTenantDomain,
                TenantInfoList=tenantList
            };
            return View(viewModel);
        }

        public async Task<IActionResult> MigrateDb([FromQuery]string tenantDomain,[FromQuery]string Identifier) {
            if (string.IsNullOrEmpty(tenantDomain) || string.IsNullOrEmpty(Identifier)) {
                return PartialView("Error", new ErrorViewModel("not found tenant"));
            }

            bool isExist = _tenantRepo.ExistTenant(tenantDomain, Identifier, out TenantInfoDto existedTenantInfo);

            if (!isExist) {
                return PartialView("Error", new ErrorViewModel("not found tenant"));
            }

            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)) {
                return PartialView("Error", new ErrorViewModel("tenant db conn is empty"));
            }

            if (!existedTenantInfo.DbServerId.HasValue) {
                return PartialView("Error", new ErrorViewModel("not found origin db server"));
            }

            string originDbConn = existedTenantInfo.ConnectionString;
            if (string.IsNullOrEmpty(existedTenantInfo.ConnectionString) && !string.IsNullOrEmpty(existedTenantInfo.EncryptedIdsConnectionString)) {
                originDbConn= _encryptService.Decrypt_Aes(existedTenantInfo.EncryptedIdsConnectionString);
            }

            bool originDbConnSuccess = await _tenantDbOperation.CheckConnect(originDbConn);
            if (!originDbConnSuccess) {
                return PartialView("Error", new ErrorViewModel("tenant origin db server can not connect"));
            }
            int tmpIndex = originDbConn.IndexOf("database=", StringComparison.OrdinalIgnoreCase);

            if (tmpIndex != -1) {
                int tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
                ViewData["OriginDbName"] = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);
            }


            var dbServerList = _dbServerRepo.GetDbServers();
            var originDbServer= dbServerList.FirstOrDefault(x => x.Id == existedTenantInfo.DbServerId);
            if (originDbServer == null) {
                return PartialView("Error", new ErrorViewModel("not found origin db server info"));
            }
            if (string.IsNullOrEmpty(originDbServer.Userpwd) && !string.IsNullOrEmpty(originDbServer.EncryptUserpwd)) {
                //解密
                originDbServer.Userpwd = _encryptService.Decrypt_Aes(originDbServer.EncryptUserpwd);
            }
            bool originConnSuccess = await MysqlDbOperaService.CheckConnect(originDbServer);
            if (!originConnSuccess) {
                return PartialView("Error", new ErrorViewModel("origin db server can not connect"));
            }
            dbServerList=dbServerList.Where(x => x.EnableStatus == (int)EnableStatusEnum.Enable && x.Id!=existedTenantInfo.DbServerId.Value).Select(x => x).ToList();
            if(!dbServerList.Any()) {
                return PartialView("Error", new ErrorViewModel("not other db server to migrate"));
            }

            bool canConnSuccess = false;
            foreach(var dbServer in dbServerList) {
                if (string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
                    //解密
                    dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
                }
                if (await MysqlDbOperaService.CheckConnect(dbServer)) {
                    canConnSuccess = true;
                    break;
                }
            }
            if (!canConnSuccess) {
                return PartialView("Error", new ErrorViewModel("not other db server can connect"));
            }

            ViewData["DbServerList"] = dbServerList;
            ViewData["OriginDbServer"] = originDbServer;
            return PartialView(existedTenantInfo);
        }
    }
}
