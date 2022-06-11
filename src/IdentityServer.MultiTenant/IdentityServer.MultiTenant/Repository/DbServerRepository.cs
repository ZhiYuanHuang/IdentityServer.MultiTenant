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

        public List<DbServerModel> GetDbServers() {
            return _dbUtil.Master.QueryList<DbServerModel>("Select * From DbServer ");
        }

        public void AddDbCountByDbserver(int dbServerId,bool isPlus=true) {
            _dbUtil.Master.ExecuteNonQuery($"Update DbServer Set CreatedDbCount=CreatedDbCount{(isPlus?"+":"-")}1 Where Id=@id",new Dictionary<string, object>() { { "id",dbServerId} });
        }

        public bool AddDbServer(DbServerModel dbServerModel) {
            string sql = @"Insert Into DbServer 
                            (ServerHost,ServerPort,UserName,Userpwd,EncryptUserpwd,CreatedDbCount,EnableStatus)
                            Value
                            (@serverHost,@serverPort,@userName,@userpwd,@encryptUserpwd,@createdDbCount,@enableStatus)
                            On Duplicate Key 
                            Update UserName=@userName,Userpwd=@userpwd,EncryptUserpwd=@encryptUserpwd";

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

        public void ChangeStatus(int dbServerId,int enableStatus) {
            _dbUtil.Master.ExecuteNonQuery("Update DbServer Set EnableStatus=@enableStatus Where Id=@id",new Dictionary<string, object>() {
                { "id",dbServerId},
                { "enableStatus",enableStatus}
            });
        }
    }
}
