using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Framework.Utils;
using IdentityServer.MultiTenant.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Repository
{
    public class DbServerRepository
    {
        private readonly DbUtil _dbUtil;
        private ILogger<DbServerRepository> _logger;
        public DbServerRepository(DbUtil dbUtil, ILogger<DbServerRepository> logger) {
            _dbUtil = dbUtil;
            _logger = logger;
        }

        public List<DbServerModel> GetDbServers(Int64? dbServerId=null) {
            string sql = "Select * From DbServer ";

            Dictionary<string, object> param = new Dictionary<string, object>();
            if (dbServerId.HasValue && dbServerId.Value > 0) {
                sql += "Where Id=@id";
                param["id"] = dbServerId.Value;
            }
            return _dbUtil.Master.QueryList<DbServerModel>(sql,param);
        }

        public void AddDbCountByDbserver(DbServerModel dbServer,bool isPlus=true) {
            if (dbServer == null) {
                return;
            }
            Int64 dbServerId = dbServer.Id;

            _dbUtil.Master.ExecuteNonQuery($"Update DbServer Set CreatedDbCount=CreatedDbCount{(isPlus?"+":"-")}1 Where Id=@id",new Dictionary<string, object>() { { "id",dbServerId} });
        }

        public bool AddDbServer(DbServerModel dbServerModel) {
            string sql = @"Insert Into DbServer 
                            (ServerHost,ServerPort,UserName,Userpwd,EncryptUserpwd,CreatedDbCount,EnableStatus)
                            Values
                            (@serverHost,@serverPort,@userName,@userpwd,@encryptUserpwd,@createdDbCount,@enableStatus)";
                            //On Duplicate Key 
                            //Update UserName=@userName,Userpwd=@userpwd,EncryptUserpwd=@encryptUserpwd";

            int row= _dbUtil.Master.ExecuteNonQuery(sql,new Dictionary<string, object>() {
                { "serverHost",dbServerModel.ServerHost},
                { "serverPort",dbServerModel.ServerPort},
                { "userName",dbServerModel.UserName},
                { "userpwd",dbServerModel.Userpwd},
                { "encryptUserpwd",dbServerModel.EncryptUserpwd},
                { "createdDbCount",0},
                { "enableStatus",(int)EnableStatusEnum.Enable}
            });
            return row > 0;
        }

        public bool DeleteDbServer(Int64 dbServerId) {
            return _dbUtil.Master.ExecuteNonQuery("Delete From DbServer Where Id=@id",new Dictionary<string, object>() {
                { "id",dbServerId}
            })>0;
        }

        public int GetDbServerRef(Int64 dbServerId) {
            var refTenantList= _dbUtil.Master.QueryList<TenantDbServerRefModel>("Select * From TenantDbServerRef Where DbServerId=@dbServerId",
                new Dictionary<string, object>() {
                    { "dbServerId",dbServerId}
                });
            return refTenantList.Count;
        }

        public void ChangeStatus(Int64 dbServerId,int enableStatus) {
            _dbUtil.Master.ExecuteNonQuery("Update DbServer Set EnableStatus=@enableStatus Where Id=@id",new Dictionary<string, object>() {
                { "id",dbServerId},
                { "enableStatus",enableStatus}
            });
        }
    }
}
