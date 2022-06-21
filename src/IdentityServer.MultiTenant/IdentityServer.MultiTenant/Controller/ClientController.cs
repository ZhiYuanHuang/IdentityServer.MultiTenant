using IdentityServer.MultiTenant.Dto;
using IdentityServer4.EntityFramework.DbContexts;
using IdentityServer4.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Controller
{
    [Route("{__tenant__=}/api/[controller]/[action]")]
    [ApiController]
    [Authorize(Policy = "sysManagePolicy")]
    public class ClientController : ControllerBase {
        ConfigurationDbContext _configurationDbContext;
        public ClientController(ConfigurationDbContext configurationDbContext) {
            _configurationDbContext = configurationDbContext;
        }

        [HttpPost]
        public async Task<AppResponseDto> AddOrUpdate([FromBody]IdentityServer4.EntityFramework.Entities.Client client) {
            if (string.IsNullOrEmpty(client.ClientId)) {
                return new AppResponseDto(false) {ErrorMsg="ClientId 不可为空" };
            }

            if(!System.Text.RegularExpressions.Regex.IsMatch(client.ClientId, "[a-zA-Z0-9]{5,15}")) {
                return new AppResponseDto(false) {ErrorMsg="ClientId 应为英文字母、数字组合，最小5位，最大15位" };
            }

            if (client.ClientSecrets.Any() && !System.Text.RegularExpressions.Regex.IsMatch(client.ClientSecrets[0].Value, "[a-zA-Z0-9]{6,20}")) {
                return new AppResponseDto(false) { ErrorMsg = "ClientSecret 应为英文字母、数字组合，最小5位，最大20位" };
            }

            IdentityServer4.EntityFramework.Entities.Client existedClient =await _configurationDbContext.Clients
                .Include(x => x.ClientSecrets)
                .Include(x => x.Claims)
                .Include(x => x.AllowedScopes)
                .FirstOrDefaultAsync(x=>x.ClientId==client.ClientId);

            bool result = false;
            if (existedClient == null) {
                if(client.ClientSecrets==null || !client.ClientSecrets.Any()) {
                    client.ClientSecrets = new List<IdentityServer4.EntityFramework.Entities.ClientSecret>() { 
                        new IdentityServer4.EntityFramework.Entities.ClientSecret(){ Value="123!@#"}
                    };
                }
                client.AccessTokenLifetime = 60 * 60 * 24 * 15;
                for(int i = 0; i < client.ClientSecrets.Count; i++) {
                    client.ClientSecrets[i].Value = client.ClientSecrets[i].Value.Sha256();
                }

                if (client.AllowedScopes == null) {
                    client.AllowedScopes = new List<IdentityServer4.EntityFramework.Entities.ClientScope>();
                }

                if(client.AllowedScopes.FirstOrDefault(x=>string.CompareOrdinal( x.Scope,"idsmul.addtenant")==0)==null) {
                    client.AllowedScopes.Add(new IdentityServer4.EntityFramework.Entities.ClientScope() { Scope= "idsmul.addtenant" });
                }

                _configurationDbContext.Clients.Add(client);
                result = _configurationDbContext.SaveChanges() > 0;
            } else {
                IDbContextTransaction dbContextTransaction = null;

                try {
                    existedClient.AllowOfflineAccess = true;
                    existedClient.ClientName = client.ClientName;
                    existedClient.Description = client.Description;
                    dbContextTransaction = _configurationDbContext.Database.BeginTransaction();
                    existedClient.AllowedScopes.Clear();
                    result = _configurationDbContext.SaveChanges() > 0;
                    if (result) {
                        existedClient.AllowedScopes = client.AllowedScopes;
                    }
                    _configurationDbContext.SaveChanges();
                    dbContextTransaction.Commit();
                    result = true;

                }catch(Exception ex) {
                    dbContextTransaction?.Rollback();
                    result = false;

                } finally {
                    dbContextTransaction?.Dispose();
                }
            }

            return new AppResponseDto(result);
        }

        [HttpGet]
        public AppResponseDto Delete([FromQuery] string clientId) {
            IdentityServer4.EntityFramework.Entities.Client existedClient = _configurationDbContext.Clients
                .Include(x => x.ClientSecrets)
                .Include(x => x.Claims)
                .Include(x => x.AllowedScopes)
                .FirstOrDefault(x => x.ClientId == clientId);
            bool result = false;
            if (existedClient != null) {
                _configurationDbContext.Clients.Remove(existedClient);
                result = _configurationDbContext.SaveChanges() > 0;
            }
            return new AppResponseDto(result);
        }

        [HttpPost]
        public async Task<AppResponseDto<string>> ResetSecret([FromForm]string clientId) {
            var existClient=await _configurationDbContext.Clients.Include(x => x.ClientSecrets).FirstOrDefaultAsync(x=>x.ClientId==clientId);
            bool result = false;
            string newSecret = string.Empty;
            if (existClient != null) {
                IDbContextTransaction dbContextTransaction = null;

                try {
                    dbContextTransaction = _configurationDbContext.Database.BeginTransaction();
                    existClient.ClientSecrets.Clear();
                    result = _configurationDbContext.SaveChanges() > 0;
                    if (!result) {
                        dbContextTransaction.Rollback();
                    } else {
                        newSecret = Random(7);
                        existClient.ClientSecrets = new List<IdentityServer4.EntityFramework.Entities.ClientSecret>() {
                            new IdentityServer4.EntityFramework.Entities.ClientSecret(){ Value=newSecret.Sha256()}
                        };
                        _configurationDbContext.SaveChanges();
                        dbContextTransaction.Commit();
                        result = true;
                    }
                }
                catch(Exception ex) {
                    dbContextTransaction?.Rollback();
                    result = false;
                } finally {
                    dbContextTransaction?.Dispose();
                }
            }

            return new AppResponseDto<string>() {
                ErrorCode = result ? 0 : -1,
                Result = result ? $"请牢记新密码：{newSecret}" : string.Empty
            };
        }

        public static string Random(int length) {
            string allowed = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            return new string(allowed
                .OrderBy(o => Guid.NewGuid())
                .Take(length)
                .ToArray());
        }
    }
}
