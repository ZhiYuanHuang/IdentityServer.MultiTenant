using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;

namespace IdentityServer.MultiTenant.Service
{
    public class SqliteDbOperaService: ITenantDbOperation
    {
        ILogger<SqliteDbOperaService> _logger;
        private Mutex _mutex = new Mutex();

        private const string _returnDbConnTempalte = "Data Source={0};";

        private readonly string _sampleDb_dbFilePath;
        private readonly string _tenantDb_dbFileDir;

        public SqliteDbOperaService(IConfiguration config,  ILogger<SqliteDbOperaService> logger, EncryptService encryptService) { 
            _logger = logger;
            _sampleDb_dbFilePath = config["SampleDb:DbFilePath"];
            _tenantDb_dbFileDir = config["SampleDb:TenantFileDir"];
        }

        public bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName) {
            dbServer = null;
            creatingDbName = string.Empty;

            if(string.IsNullOrEmpty(_sampleDb_dbFilePath) || string.IsNullOrEmpty(_tenantDb_dbFileDir)) {
                _logger.LogError("SqliteDbOperaService config error");
                return false;
            }

            if (!System.IO.File.Exists(_sampleDb_dbFilePath)) {
                _logger.LogError("SqliteDbOperaService config error,sample db file not exist");
                return false;
            }

            bool result = false;
            _mutex.WaitOne();

            try {
                if (System.IO.Directory.Exists(_tenantDb_dbFileDir)) {
                    System.IO.Directory.CreateDirectory(_tenantDb_dbFileDir);
                }

                creatingDbName = $"ids_{tenantInfoDto.TenantDomain}_{tenantInfoDto.Identifier}.db";

                string dbFilePath = System.IO.Path.Combine(_tenantDb_dbFileDir, creatingDbName);

                if (System.IO.File.Exists(dbFilePath)) {
                    _logger.LogError($"tenant dbfile {dbFilePath} existed!");
                    return false;
                }

                string tmpFilePath =$"{dbFilePath}.tmp";

                bool copyToTmp = false;
                try {
                    System.IO.File.Copy(_sampleDb_dbFilePath, tmpFilePath);
                    copyToTmp = true;
                } catch {
                }

                bool renameResult = false;
                try {
                    if (!copyToTmp) {
                        if (System.IO.File.Exists(tmpFilePath)) {
                            System.IO.File.Delete(tmpFilePath);
                        }
                        
                        return false;
                    }

                    System.IO.FileInfo tmpFi = new System.IO.FileInfo(tmpFilePath);
                    tmpFi.MoveTo(dbFilePath);
                    renameResult = true;
                } catch {

                }

                if (!renameResult) {
                    if (System.IO.File.Exists(dbFilePath)) {
                        System.IO.File.Delete(dbFilePath);
                    }

                    return false;
                }
                tenantInfoDto.ConnectionString = creatingDbName;

                result = true;
            } catch (Exception ex) {
                result = false;
                _logger.LogError(ex, $"create tenant db {_tenantDb_dbFileDir} {creatingDbName} error");
            } finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        public void DeleteTenantDb(DbServerModel dbServer, string toDeleteDb) {

            string dbFilePath = System.IO.Path.Combine(_tenantDb_dbFileDir, toDeleteDb);

            if (System.IO.File.Exists(dbFilePath)) {
                try {
                    System.IO.File.Delete(dbFilePath);
                } catch {
                }
            }
               
        }
    }
}
