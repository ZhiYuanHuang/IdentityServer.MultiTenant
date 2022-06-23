using Finbuckle.MultiTenant;
using IdentityServer.MultiTenant.Domain;
using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Const;
using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Repository;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.TenantStore
{
    public class IdsMulTenantStoreV1<TTenantInfo> : IMultiTenantStore<TTenantInfo> where TTenantInfo : class, ITenantInfo, new()
    {
        private readonly IDistributedCache cache;
        private readonly string keyPrefix;
        private readonly TimeSpan? slidingExpiration;

        private readonly TenantRepository _tenantRepo;
        private readonly string _sysIdsConnStr;
        private readonly ContextSystem _contextSystem;

        public IdsMulTenantStoreV1(TenantRepository tenantRepo,ContextSystem contextSystem,string sysIdsConnStr,IDistributedCache cache,string keyPrefix,TimeSpan? slidingExpiration) {
            _tenantRepo = tenantRepo;
            _sysIdsConnStr = sysIdsConnStr;
            _contextSystem = contextSystem;
            this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
            this.keyPrefix = keyPrefix ?? throw new ArgumentNullException(nameof(keyPrefix));
            this.slidingExpiration = slidingExpiration;
        }

        public Task<IEnumerable<TTenantInfo>> GetAllAsync() {
            throw new System.NotImplementedException();
        }

        public Task<bool> TryAddAsync(TTenantInfo tenantInfo) {
            throw new System.NotImplementedException();
        }

        public Task<TTenantInfo> TryGetAsync(string id) {
            throw new System.NotImplementedException();
        }

        public async Task<TTenantInfo> TryGetByIdentifierAsync(string identifier) {
            if(string.Compare(identifier,MulTenantConstants.SysTenant,true)==0) {
                var sysTenant= new ExtendTenantInfo() { Id=identifier,Identifier=identifier,Name="MulIdsSysAdmin",ConnectionString=_sysIdsConnStr};
                return sysTenant as TTenantInfo;
            }

            if (MulTenantConstants.ManageTenantList.FirstOrDefault(x => string.Compare(x, identifier,true) == 0) != null) {
                var managerTenant = new ExtendTenantInfo() { Id = identifier, Identifier = identifier, Name = "MulIdsManager", ConnectionString = _sysIdsConnStr };
                return managerTenant as TTenantInfo;
            }

            string tenantCacheKey = string.Format(CacheKey.Tenant_Format, keyPrefix, _contextSystem.SystemDomain, identifier);
            
            var bytes = await cache.GetStringAsync(tenantCacheKey);
            //if (bytes == null)
            //    return null;

            var tenantNotExist = await cache.GetStringAsync(tenantCacheKey + "__noexist");
            if(!string.IsNullOrEmpty(tenantNotExist)) {
                return null;
            }

            return await LockToGetFromDb(tenantCacheKey, ()=> {
                if (!_tenantRepo.ExistTenant(_contextSystem.SystemDomain, identifier, out TenantInfoDto tenantInfoDto)) {  //db not contain
                    var options = new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddMinutes(2) };
                    cache.SetString(tenantCacheKey + "__noexist", "1", options);
                    return null;
                }

                ExtendTenantInfo cachedTenantInfo = new ExtendTenantInfo() { 
                    Id=tenantInfoDto.GuidId,
                    Identifier=tenantInfoDto.Identifier,
                    Name=tenantInfoDto.Name,
                    ConnectionString=tenantInfoDto.ConnectionString,
                    EncryptedIdsConnectionString=tenantInfoDto.EncryptedIdsConnectionString
                };

                cache.SetString(tenantCacheKey,Newtonsoft.Json.JsonConvert.SerializeObject(cachedTenantInfo));

                return cachedTenantInfo as TTenantInfo;
            },(cacheValue)=> {
                return Newtonsoft.Json.JsonConvert.DeserializeObject<TTenantInfo>(cacheValue);
            });
        }

        private async Task<T> LockToGetFromDb<T>(string key,Func<T> getFromDbAction,Func<string,T> formatCacheValue) {
            string lockKey = string.Format(CacheKey.LockToGetPre, keyPrefix, key);

            string isLocked=await cache.GetStringAsync(lockKey);

            SpinWait sp = new SpinWait();
            while (!string.IsNullOrEmpty(await cache.GetStringAsync(lockKey))) {
                sp.SpinOnce();
            }

            await cache.SetStringAsync(lockKey,"1", new DistributedCacheEntryOptions { AbsoluteExpiration = DateTimeOffset.UtcNow.AddSeconds(3) });

            var checkAgain = await cache.GetStringAsync(key);
            if (!string.IsNullOrEmpty(checkAgain)) {
                return formatCacheValue(checkAgain);
            }

            T result = default(T);
            try {
                result= getFromDbAction();
            } finally {
                try {
                    cache.RemoveAsync(lockKey);
                } catch { 
                }
            }

            return result;
        }

        public Task<bool> TryRemoveAsync(string identifier) {
            throw new System.NotImplementedException();
        }

        public Task<bool> TryUpdateAsync(TTenantInfo tenantInfo) {
            throw new System.NotImplementedException();
        }
    }
}
