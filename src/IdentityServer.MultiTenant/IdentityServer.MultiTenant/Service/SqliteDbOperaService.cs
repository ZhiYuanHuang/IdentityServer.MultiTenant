using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Data.SQLite;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public class SqliteDbOperaService: ITenantDbOperation
    {
        ILogger<SqliteDbOperaService> _logger;
        private Mutex _mutex = new Mutex();

        private const string _returnDbConnTempalte = "Data Source={0};";

        private readonly string _sampleDb_dbFilePath;
        private readonly string _tenantDb_dbFileDir;

        private string _backupDbDirPath;

        public SqliteDbOperaService(IConfiguration config,  ILogger<SqliteDbOperaService> logger, EncryptService encryptService,IWebHostEnvironment hostingEnvironment) { 
            _logger = logger;
            string dbDirPath= config["SampleDb:DbDirPath"];
            if (string.IsNullOrEmpty(dbDirPath)) {
                dbDirPath =System.IO.Path.Combine( hostingEnvironment.ContentRootPath,"Db");
            }

            _sampleDb_dbFilePath = System.IO.Path.Combine(dbDirPath, "ids_sample.db");
            _logger.LogInformation($"_sampleDb_dbFilePath:{_sampleDb_dbFilePath}");

            _tenantDb_dbFileDir = config["SampleDb:TenantDbDirPath"];
            if (string.IsNullOrEmpty(_tenantDb_dbFileDir)) {
                _tenantDb_dbFileDir= System.IO.Path.Combine(hostingEnvironment.ContentRootPath, "Db","tenant");
            }

            _logger.LogInformation($"_tenantDb_dbFileDir:{_tenantDb_dbFileDir}");

            _backupDbDirPath = config["BackupDbDirPath"];
            if (string.IsNullOrEmpty(_backupDbDirPath)) {
                _backupDbDirPath = System.IO.Path.Combine(hostingEnvironment.ContentRootPath, "DbBackup");
            }
            if (!System.IO.Directory.Exists(_backupDbDirPath)) {
                System.IO.Directory.CreateDirectory(_backupDbDirPath);
            }
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
                if (!System.IO.Directory.Exists(_tenantDb_dbFileDir)) {
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
                tenantInfoDto.ConnectionString = $"Data Source={dbFilePath};";

                result = true;
            } catch (Exception ex) {
                result = false;
                _logger.LogError(ex, $"create tenant db {_tenantDb_dbFileDir} {creatingDbName} error");
            } finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        public bool DeleteTenantDb(DbServerModel dbServer, string toDeleteDb, bool backup = false) {
            bool result = false;
            string dbFilePath = System.IO.Path.Combine(_tenantDb_dbFileDir, toDeleteDb);

            if (System.IO.File.Exists(dbFilePath)) {
                try {
                    if (backup) {
                        string backSqlFilePath = System.IO.Path.Combine(_backupDbDirPath, $"Sqlite_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{toDeleteDb}");
                        System.IO.File.Copy(dbFilePath,backSqlFilePath);
                        if (!System.IO.File.Exists(backSqlFilePath)) {
                            _logger.LogError($"cann't not found backuped sql {backSqlFilePath}");
                            return false;
                        }
                    }

                    System.IO.File.Delete(dbFilePath);
                    result = true;
                } catch(Exception ex) {
                    _logger.LogError(ex,"delete tenant db error");
                }
            }

            return result;
        }

        public bool DeleteTenantDb(DbServerModel dbServer, TenantInfoDto tenantInfo,out string errMsg) {
            errMsg = string.Empty;
            bool result = false;

            int tmpIndex = tenantInfo.ConnectionString.IndexOf("Data Source=");
            if(tmpIndex==1) {
                errMsg = "can not find db file";
                return false;
            }
            int tmpEndIndex = tenantInfo.ConnectionString.IndexOf(';', tmpIndex);
            
            string dbFilePath = tenantInfo.ConnectionString.Substring(tmpIndex + 12, tmpEndIndex - tmpIndex - 12); ;

            try {
                if (System.IO.File.Exists(dbFilePath)) {
                    System.IO.File.Delete(dbFilePath);
                    result = true;
                } else {
                    errMsg = "tenent db file not exists";
                }
            }
            catch(Exception ex) {
                result = false;
                errMsg = ex.Message;
                _logger.LogError(ex, "DeleteTenantDb error");
            }

            return result;
        }

        public async Task<bool> CheckConnect(string connStr) {
            if (string.IsNullOrEmpty(connStr)) {
                return false;
            }
            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            SQLiteConnection connection = new SQLiteConnection(connStr);

            bool result = false;
            try {
                await connection.OpenAsync();
                result = true;
            } catch (Exception ex) {
                result = false;
            } finally {
                connection?.Close();
                connection?.Dispose();
                connection = null;
            }

            return result;
        }

        public bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto, DbServerModel originDbServer, DbServerModel dbServer, ref StringBuilder migrateLogBuilder, out string errMsg) {
            errMsg = "sqlite db not support migrate";
            return false;
        }

        public async Task<Tuple<bool, string>> CheckConnectAndVersion(string connStr) {
            if (string.IsNullOrEmpty(connStr)) {
                return new Tuple<bool, string>(false,string.Empty);
            }
            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            SQLiteConnection connection = new SQLiteConnection(connStr);

            string version = string.Empty;
            bool result = false;
            try {
                await connection.OpenAsync();
                version = connection.ServerVersion;
                result = true;
            } catch (Exception ex) {
                result = false;
            } finally {
                connection?.Close();
                connection?.Dispose();
                connection = null;
            }

            return new Tuple<bool, string>(result,version);
        }
    }
}
