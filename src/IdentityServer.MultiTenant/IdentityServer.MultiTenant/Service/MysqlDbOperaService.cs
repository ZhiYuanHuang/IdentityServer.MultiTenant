using IdentityServer.MultiTenant.Dto;
using IdentityServer.MultiTenant.Framework;
using IdentityServer.MultiTenant.Framework.Enum;
using IdentityServer.MultiTenant.Models;
using IdentityServer.MultiTenant.Repository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public class MysqlDbOperaService: ITenantDbOperation
    {
        DbServerRepository _dbServerRepository;
        ILogger<MysqlDbOperaService> _logger;

        private readonly string _sampleDb_serverHost;
        private readonly int _sampleDb_serverPort;
        private readonly string _sampleDb_username;
        private readonly string _sampleDb_userpassword;
        private readonly string _sampleDb_dbname;

        private Mutex _mutex=new Mutex();

        private string _mysqlBinDirPath;

        private const string _returnDbConnTempalte = "Database={0};Data Source={1};Port={2};User Id={3};Password={4};Charset=utf8mb4;";

        private readonly EncryptService _encryptService;

        public MysqlDbOperaService(IConfiguration config,  DbServerRepository dbServerRepository,ILogger<MysqlDbOperaService> logger,EncryptService encryptService, IWebHostEnvironment hostingEnvironment) {
            _encryptService = encryptService;
            _dbServerRepository = dbServerRepository;
            _logger = logger;

            _mysqlBinDirPath = config["MysqlBinDirPath"];
            if (string.IsNullOrEmpty(_mysqlBinDirPath)) {
                _mysqlBinDirPath = System.IO.Path.Combine(hostingEnvironment.ContentRootPath, "MysqlExe");
            }

            _sampleDb_serverHost = config["SampleDb:Host"];
            _sampleDb_serverPort = int.Parse(config["SampleDb:Port"]);
            _sampleDb_username = config["SampleDb:UserName"];
            _sampleDb_userpassword = config["SampleDb:UserPassword"];
            _sampleDb_dbname = config["SampleDb:DbName"];
        }

        public bool CreateTenantDb(ref TenantInfoDto tenantInfoDto,out DbServerModel dbServer ,out string creatingDbName) {
            dbServer = null;
            creatingDbName = string.Empty;
            if (string.IsNullOrEmpty(_mysqlBinDirPath)
                || string.IsNullOrEmpty(_sampleDb_serverHost)
                || _sampleDb_serverPort==0
                || string.IsNullOrEmpty(_sampleDb_username)
                || string.IsNullOrEmpty(_sampleDb_userpassword)
                || string.IsNullOrEmpty(_sampleDb_dbname)
                ) {
                _logger.LogError("DbOperaService CreateTenantDb error,config error");
                
                return false;
            }

            bool result = false;

            _mutex.WaitOne();
            try {
                var dbServerList= _dbServerRepository.GetDbServers();
                if (!dbServerList.Any()) {
                    _logger.LogError("DbOperaService CreateTenantDb error,dbserver is empty");
                    return false;
                }

                var enableDbServerList= dbServerList.Where(x=>x.EnableStatus==(int)EnableStatusEnum.Enable).OrderBy(x => x.CreatedDbCount).ThenBy(x => x.ServerHost).ToList();
                DbServerModel selectedDbServer = null;

                foreach (var tmpDbServer in enableDbServerList) {

                    var userpassword = tmpDbServer.Userpwd;
                    if (string.IsNullOrEmpty(userpassword) && !string.IsNullOrEmpty(tmpDbServer.EncryptUserpwd)) {
                        //解密
                        userpassword = _encryptService.Decrypt_Aes(tmpDbServer.EncryptUserpwd);
                        tmpDbServer.Userpwd = userpassword;
                    }

                    if (CheckConnect(tmpDbServer).Result) {
                        selectedDbServer = tmpDbServer;
                        break;
                    }
                }

                if (selectedDbServer == null) {
                    _logger.LogError("CreateTenantDb error,errmsg:have not connect success db server");
                    return false;
                }

                dbServer = selectedDbServer;

                creatingDbName = $"ids_{tenantInfoDto.TenantDomain}_{tenantInfoDto.Identifier}";

                List<string> creatTenantDbCmdList = new List<string>() {
                    string.Format("mysql -h{0} -P{1} -u{2} -p{3}",selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd),
                    string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",creatingDbName),
                    "exit"
                };

                string executeResult= RunCmd(creatTenantDbCmdList);
                if (!string.IsNullOrEmpty(executeResult) && executeResult.Contains("error",StringComparison.OrdinalIgnoreCase)) {
                    _logger.LogError("create tenant db");
                    return false;
                }

                List<string> copyCmdList = new List<string>() {
                    string.Format("mysqldump {0} -h{1} -P{2} -u{3} -p{4} --add-drop-table --column-statistics=0 | mysql {5} -h{6} -P{7} -u{8} -p{9}",
                    _sampleDb_dbname,_sampleDb_serverHost,_sampleDb_serverPort,_sampleDb_username,_sampleDb_userpassword,
                    creatingDbName,selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd
                    ),
                };

                executeResult = RunCmd( copyCmdList);
                if (!string.IsNullOrEmpty(executeResult) && executeResult.Contains("error", StringComparison.OrdinalIgnoreCase)) {
                    _logger.LogError("copy tenant db");
                    return false;
                }

                //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
                string createdDbConnStr = string.Format(_returnDbConnTempalte,creatingDbName,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd);
                tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(createdDbConnStr);

                Task.Run(()=> { _dbServerRepository.AddDbCountByDbserver(selectedDbServer.Id); }).ConfigureAwait(false);

                result = true;
            } catch(Exception ex) {
                result = false;
                _logger.LogError(ex,$"create tenant db {dbServer?.ServerHost} {creatingDbName} error");
            } 
            finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        public bool DeleteTenantDb(DbServerModel dbServer,string toDeleteDb) {

            bool result = false;
            _mutex.WaitOne();
            try {
                //string fileName = "mysql";
                //string args = string.Format("-h{0} -P{1} -u{2} -p{3}", dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
                List<string> deleteTenantDbCmdList = new List<string>() {
                    string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),
                    string.Format("drop database if exists `{0}`;",toDeleteDb),
                    string.Format("exit")
                };

                string executeResult = RunCmd(deleteTenantDbCmdList);
                if (!string.IsNullOrEmpty(executeResult) && executeResult.Contains("error", StringComparison.OrdinalIgnoreCase)) {
                    _logger.LogError($"delete tenant db,error:{executeResult}");
                    return false;
                }

                result = true;
                Task.Run(() => { _dbServerRepository.AddDbCountByDbserver(dbServer.Id, false); }).ConfigureAwait(false);
            } catch(Exception ex) {
                _logger.LogError(ex, $"delete tenant db {dbServer?.ServerHost} {toDeleteDb} error");
            } finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        public bool DeleteTenantDb(DbServerModel dbServer, TenantInfoDto tenantInfo,out string errMsg) {
            errMsg = string.Empty;
            if (dbServer == null) {
                errMsg = "tenant dbserver is null";
                return false;
            }

            if(string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
                dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
            }

            if (!CheckConnect(dbServer).Result) {
                errMsg = $"db {dbServer.ServerHost}-{dbServer.ServerPort} can not connect";
                return false;
            }

            string connStr = tenantInfo.ConnectionString;
            if (string.IsNullOrEmpty(connStr) && !string.IsNullOrEmpty(tenantInfo.EncryptedIdsConnectionString)) {
                connStr = _encryptService.Decrypt_Aes(tenantInfo.EncryptedIdsConnectionString);
            }
            if (string.IsNullOrEmpty(connStr)) {
                errMsg = "db connstr is empty";
                return false;
            }

            int tmpIndex= connStr.IndexOf("Database=");
            if (tmpIndex == -1) {
                errMsg = "can not find database";
                return false;
            }

            int tmpEndIndex = connStr.IndexOf(';',tmpIndex);
            if (tmpEndIndex == -1) {
                errMsg = "can not find database";
                return false;
            }

            string dbName = connStr.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);
            if (string.IsNullOrEmpty(dbName)) {
                errMsg = "can not find database";
                return false;
            }

            return DeleteTenantDb(dbServer,dbName);
        }

        public static async Task<bool> CheckConnect(DbServerModel dbServer) {

            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            string connStr = string.Format("Data Source={0};Port={1};User Id={2};Password={3};",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd);
            var connection=new MySqlConnection(connStr);

            bool result = false;
            try {
                await connection.OpenAsync();
                result = true;
            }catch(Exception ex) {
                result = false;
            } finally {
                connection?.Close();
                connection?.Dispose();
                connection = null;
            }

            return result;
        }

        public async Task<bool> CheckConnect(string connStr) {
            if (string.IsNullOrEmpty(connStr)) {
                return false;
            }

            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            var connection = new MySqlConnection(connStr);

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

        private string RunCmd(List<string> innerCmdStrList) {
            StringBuilder errBuilder = new StringBuilder();

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.WorkingDirectory = _mysqlBinDirPath;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;
            p.ErrorDataReceived += (obj, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    errBuilder.Append(args.Data);
                }
            };

            p.Start();
            p.BeginErrorReadLine();

            if (innerCmdStrList != null && innerCmdStrList.Any()) {

                foreach (var cmd in innerCmdStrList) {
                    p.StandardInput.WriteLine(cmd);

                    if (string.Compare(cmd, "exit",true) == 0) {
                        Thread.Sleep(30);
                    }else {
                        Thread.Sleep(100);
                    }
                }
            }

            p.StandardInput.WriteLine("exit");

            p.WaitForExit();
      
            return errBuilder.ToString();
        }
    }
}
