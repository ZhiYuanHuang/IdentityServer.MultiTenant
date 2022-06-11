using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Repository
{
    public class TenantRepository
    {
        private readonly DbUtil _dbUtil;
        private ILogger<TenantRepository> _logger;
        public TenantRepository(DbUtil dbUtil,ILogger<TenantRepository> logger) {
            _dbUtil = dbUtil;
            _logger = logger;
        }

        public bool ExistTenant(string domain,string tenantId,out TenantInfoDto tenantInfoDto) {
            bool exist = false;

            string sql = @"Select a.*,b.TenantDomain,b.EnableStatus As DomainEnableStatus From TenantInfo a 
                            Inner Join TenantDomain b
                                On b.Id=a.TenantDomainId
                            Where a.Identifier=@identifier And b.TenantDomain=@domain";
            Dictionary<string, object> param = new Dictionary<string, object>() {
                { "identifier",tenantId},
                { "domain",domain}
            };

            tenantInfoDto = null;
            try {
                tenantInfoDto = _dbUtil.Slave.Query<TenantInfoDto>(sql,param);
                exist = tenantInfoDto != null;
            }catch(Exception ex) {
                exist = false;
                _logger.LogError(ex,"query tenant error");
            }

            return exist;
        }

        public bool AddOrUpdateTenantDomain(TenantDomainModel tenantDomainModel,out string errMsg,bool? isAdd=null) {
            errMsg = string.Empty;
            bool result = false;

            string updateSql = "Update TenantDomain Set EnableStatus=@enableStatus,Description=@description,UpdateTime=Now() Where Id=@id";
            string insertSql = "Insert Into TenantDomain (TenantDomain,EnableStatus,Description,CreateTime) Value (@domain,1,@description,Now())";

            try {
                var existTenantDomain= _dbUtil.Slave.Query<TenantDomainModel>("Select * From TenantDomain Where TenantDomain=@domain",new Dictionary<string, object>() { { "domain", tenantDomainModel.TenantDomain} });
                if (existTenantDomain != null) {
                    if(isAdd.HasValue && isAdd.Value) {   // to add
                        result = false;
                        errMsg = $"tenantDomain {tenantDomainModel.TenantDomain} exists";
                    } else {
                        result= _dbUtil.Master.ExecuteNonQuery(updateSql,new Dictionary<string, object>() { 
                            { "enableStatus",tenantDomainModel.EnableStatus},
                            { "description",tenantDomainModel.Description},
                            { "id",existTenantDomain.Id}
                        })>0;
                    }
                } else {
                    if(isAdd.HasValue && !isAdd.Value) {  //real to update
                        result = false;
                        errMsg = $"tenantDomain {tenantDomainModel.TenantDomain} no exist";
                    } else {
                        result= _dbUtil.Master.ExecuteNonQuery(insertSql,new Dictionary<string, object>() {
                            { "domain",tenantDomainModel.TenantDomain},
                            { "description",tenantDomainModel.Description}
                        })>0;
                    }
                }

            }
            catch(Exception ex) {
                result = false;
                errMsg = "save tenantdomain err";
                _logger.LogError(ex,"addorupdate tenantmain error");
            }

            return result;
            
        }

        public bool AddOrUpdateTenant(TenantInfoDto tenantInfoDto,out string errMsg,bool? isAdd = null) {
            errMsg = string.Empty;
            bool result = false;

            try {
                var existTenantDomain = _dbUtil.Slave.Query<TenantDomainModel>("Select * From TenantDomain Where TenantDomain=@domain", new Dictionary<string, object>() { { "domain", tenantInfoDto.TenantDomain } });
                if (existTenantDomain == null) {
                    result = false;
                    errMsg = "tenant domain not exists";
                } else {
                    var existTenant = _dbUtil.Slave.Query<TenantInfoModel>("Select * From TenantInfo Where Identifier =@identifier And TenantDomainId=@domainId",new Dictionary<string, object>() {
                        { "identifier",tenantInfoDto.Identifier},
                        { "domainId",existTenantDomain.Id}
                    });

                    string updateSql = "Update TenantInfo Set GuidId=@guidId,EnableStatus=@enableStatus,Name=@name,ConnectionString=@connStr,EncryptedIdsConnectionString=@encryptConnStr,UpdateTime=Now(),Description=@desc Where Id=@id";
                    string insertSql = @"Insert Into TenantInfo (GuidId,Identifier,TenantDomainId,EnableStatus,Name,ConnectionString,EncryptedIdsConnectionString,Description,CreateTime)
                                            Value (@guidId,@identifier,@tenantDomainId,1,@name,@connStr,@encryptConnStr,@desc,Now())";

                    if (existTenant != null) {
                        if (isAdd.HasValue && isAdd.Value) {   //real to add
                            result = false;
                            errMsg = $"tenant {existTenant.Identifier} exists";
                        } else {
                            result = _dbUtil.Master.ExecuteNonQuery(updateSql, new Dictionary<string, object>() {
                                { "guidId",tenantInfoDto.GuidId},
                                { "enableStatus",tenantInfoDto.EnableStatus},
                                { "name",tenantInfoDto.Name},
                                { "connStr",tenantInfoDto.ConnectionString},
                                { "encryptConnStr",tenantInfoDto.EncryptedIdsConnectionString},
                                { "desc",tenantInfoDto.Description},
                               
                                { "id",existTenant.Id}
                            }) > 0;
                        }
                    } else {
                        if (isAdd.HasValue && !isAdd.Value) {  //real to update
                            result = false;
                            errMsg = $"tenant {tenantInfoDto.Identifier} no exist";
                        } else {
                            result = _dbUtil.Master.ExecuteNonQuery(insertSql, new Dictionary<string, object>() {
                                { "guidId",tenantInfoDto.GuidId},
                                { "identifier",tenantInfoDto.Identifier},
                                { "tenantDomainId",existTenantDomain.Id},
                                { "name",tenantInfoDto.Name},
                                { "connStr",tenantInfoDto.ConnectionString},
                                { "encryptConnStr",tenantInfoDto.EncryptedIdsConnectionString},
                                { "desc",tenantInfoDto.Description},
                            }) > 0;
                        }
                    }
                }
            } catch(Exception ex) {
                result = false;
                errMsg = "save tenant err";
                _logger.LogError(ex, "addorupdate tenantmain error");
            }

            return result;
        }

        public void RemoveTenant(int tenantId) {
            try {
                _dbUtil.Master.ExecuteNonQuery("Delete From TenantInfo Where Id=@id",new Dictionary<string, object>() { { "id",tenantId} });

            }catch(Exception ex) {
                _logger.LogError(ex,"remove tenant error");
            }
        }
    }
}
