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
        public ManagementController(ConfigurationDbContext configurationDbContext,
            TenantRepository tenantRepo,
            DbServerRepository dbServerRepository,
            EncryptService encryptService) {
            _configurationDbContext = configurationDbContext;
            _tenantRepo = tenantRepo;
            _dbServerRepo = dbServerRepository;
            _encryptService = encryptService;
        }
        public IActionResult Index() {
            return View();
        }

        public IActionResult Clients() {
            var clientList= _configurationDbContext.Clients.ToList();
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
                    var task= Task.Factory.StartNew( (keyValueObj) => {
                        KeyValuePair<DbServerModel, DbServerDto> tmpKeyValue = (KeyValuePair<DbServerModel, DbServerDto>)keyValueObj;

                        var dbServer= tmpKeyValue.Key;
                        if (string.IsNullOrEmpty(dbServer.Userpwd)) {
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
    }
}
