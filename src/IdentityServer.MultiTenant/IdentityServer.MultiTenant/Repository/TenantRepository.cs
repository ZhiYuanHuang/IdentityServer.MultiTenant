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

        public List<TenantInfoDto> GetTenantInfoDtos(string tenantDomain) {
            Dictionary<string, object> param = new Dictionary<string, object>();
            string sql = @"Select a.*,b.TenantDomain,b.EnableStatus As DomainEnableStatus From TenantInfo a 
                            Inner Join TenantDomain b
                                On b.Id=a.TenantDomainId";
            if (!string.IsNullOrEmpty(tenantDomain)) {
                sql += " Where b.TenantDomain=@domain ";
                param["domain"] = tenantDomain;
            }
            sql += " order by b.TenantDomain ";

            List<TenantInfoDto> list = null;
            try {
                list=_dbUtil.Slave.QueryList<TenantInfoDto>(sql,param);
            }
            catch(Exception ex) {
                _logger.LogError(ex,"query tenant list error");
            }

            return list;
        }

        public bool ExistTenant(string domain,string tenantId,out TenantInfoDto tenantInfoDto) {
            bool exist = false;

            string sql = @"Select a.*,b.TenantDomain,b.EnableStatus As DomainEnableStatus,c.DbServerId From TenantInfo a 
                            Inner Join TenantDomain b
                                On b.Id=a.TenantDomainId
                            Left Join TenantDbServerRef c
                                On c.TenantId=a.Id
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

            string updateSql = "Update TenantDomain Set EnableStatus=@enableStatus,Description=@description,UpdateTime=@dtNow Where Id=@id";
            string insertSql = "Insert Into TenantDomain (TenantDomain,EnableStatus,Description,CreateTime) Values (@domain,1,@description,@dtNow)";

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
                            { "id",existTenantDomain.Id},
                            { "dtNow",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                        })>0;
                    }
                } else {
                    if(isAdd.HasValue && !isAdd.Value) {  //real to update
                        result = false;
                        errMsg = $"tenantDomain {tenantDomainModel.TenantDomain} no exist";
                    } else {
                        result= _dbUtil.Master.ExecuteNonQuery(insertSql,new Dictionary<string, object>() {
                            { "domain",tenantDomainModel.TenantDomain},
                            { "description",tenantDomainModel.Description},
                            { "dtNow",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
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

        public bool DeleteTenantDomain(string tenantDomain) {
            string sql = "Delete From TenantDomain Where TenantDomain=@domain";
            bool result = false;
            try {
                result= _dbUtil.Master.ExecuteNonQuery(sql,new Dictionary<string, object>() {
                    { "domain",tenantDomain}
                })>0;
            }catch(Exception ex) {
                result = false;
                _logger.LogError(ex,"delete domain error");
            }
            return result;
        }

        public List<TenantDomainModel> GetTenantDomains(string tenantDomain=null) {
            string sql = "Select * From TenantDomain ";

            Dictionary<string, object> param = new Dictionary<string, object>();
            if (!string.IsNullOrEmpty(tenantDomain)) {
                sql += "Where TenantDomain=@domain";
                param["domain"] = tenantDomain;
            }

            return _dbUtil.Master.QueryList<TenantDomainModel>(sql,param);
        }

        public bool AddOrUpdateTenant(TenantInfoDto tenantInfoDto,out string errMsg,bool isAdd) {
            errMsg = string.Empty;
            bool result = false;

            try {
                var existTenantDomain = _dbUtil.Master.Query<TenantDomainModel>("Select * From TenantDomain Where TenantDomain=@domain", new Dictionary<string, object>() { { "domain", tenantInfoDto.TenantDomain } });
                if (existTenantDomain == null) {
                    result = false;
                    errMsg = "tenant domain not exists";
                } else {
                    var existTenant = _dbUtil.Master.Query<TenantInfoModel>("Select * From TenantInfo Where Identifier =@identifier And TenantDomainId=@domainId",new Dictionary<string, object>() {
                        { "identifier",tenantInfoDto.Identifier},
                        { "domainId",existTenantDomain.Id}
                    });

                    string updateSql = "Update TenantInfo Set GuidId=@guidId,EnableStatus=@enableStatus,Name=@name,ConnectionString=@connStr,EncryptedIdsConnectionString=@encryptConnStr,UpdateTime=@dtNow,Description=@desc Where Id=@id";
                    string insertSql = @"Insert Into TenantInfo (GuidId,Identifier,TenantDomainId,EnableStatus,Name,ConnectionString,EncryptedIdsConnectionString,Description,CreateTime)
                                            Values (@guidId,@identifier,@tenantDomainId,1,@name,@connStr,@encryptConnStr,@desc,@dtNow)";

                    if (existTenant != null) {
                        if (isAdd) {   //real to add
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
                               
                                { "id",existTenant.Id},
                                { "dtNow",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
                            }) > 0;
                        }
                    } else {
                        if (!isAdd) {  //real to update
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
                                { "dtNow",DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}
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

        public bool AttachDbServerToTenant(TenantInfoDto tenantInfoDto,DbServerModel dbServer, out string errMsg) {
            bool result = false;
            errMsg = string.Empty;

            if (dbServer == null) {   //sqlite
                result = AddOrUpdateTenant(tenantInfoDto,out errMsg,false);
            } else {   //mysql
                try {
                    _dbUtil.Master.BeginTransaction();

                    if(!AddOrUpdateTenant(tenantInfoDto, out errMsg, false)) {
                        result = false;
                        _dbUtil.Master.RollbackTransaction();
                        return result;
                    }

                    if (!setDbServerOfTenant(tenantInfoDto.Id, dbServer.Id)) {
                        result = false;
                        _dbUtil.Master.RollbackTransaction();
                        return result;
                    }

                    _dbUtil.Master.CommitTransaction();
                    result = true;
                } catch (Exception ex) {
                    result = false;
                    errMsg = ex.Message;
                    _dbUtil.Master.RollbackTransaction();
                }
            }

            return result;
        }

        private bool setDbServerOfTenant(Int64 tenantId,Int64 dbServerId) {
            string sql = @"Insert Into TenantDbServerRef (TenantId,DbServerId)
                            Values (@tenantId,@dbServerId)
                            On Duplicate Key 
                            Update OldDbServerId=DbServerId,DbServerId=@dbServerId";
            return _dbUtil.Master.ExecuteNonQuery(sql,new Dictionary<string, object>() {
                { "tenantId",tenantId},
                { "dbServerId",dbServerId}
            })>0;
        }

        public void RemoveTenant(Int64 tenantId) {
            var p = new Dictionary<string, object>() { { "id", tenantId } };
            try {

                _dbUtil.Master.ExecuteNonQuery("Delete From TenantInfo Where Id=@id",p);
                _dbUtil.Master.ExecuteNonQuery("Delete From TenantDbServerRef Where TenantId=@id",p);
            }catch(Exception ex) {
                _logger.LogError(ex,"remove tenant error");
            }
        }

        public void ChangeTenantStatus(Int64 tenantId, int enableStatus) {
            _dbUtil.Master.ExecuteNonQuery("Update TenantInfo Set EnableStatus=@enableStatus Where Id=@id", new Dictionary<string, object>() {
                { "id",tenantId},
                { "enableStatus",enableStatus}
            });
        }
    }
}
