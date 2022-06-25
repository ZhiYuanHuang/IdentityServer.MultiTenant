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

                bool runNormal= RunCmd(creatTenantDbCmdList);
                if (!runNormal) {
                    _logger.LogError("create tenant db");
                    return false;
                }

                List<string> copyCmdList = new List<string>() {
                    string.Format("mysqldump {0} -h{1} -P{2} -u{3} -p{4} --add-drop-table --column-statistics=0 | mysql {5} -h{6} -P{7} -u{8} -p{9}",
                    _sampleDb_dbname,_sampleDb_serverHost,_sampleDb_serverPort,_sampleDb_username,_sampleDb_userpassword,
                    creatingDbName,selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd
                    ),
                };

                runNormal = RunCmd( copyCmdList);
                if (!runNormal) {
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

        public bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto,DbServerModel originDbServer,DbServerModel dbServer,out string errMsg) {

            string originDbConn = tenantInfoDto.ConnectionString;
            errMsg = string.Empty;
            if (string.IsNullOrEmpty(originDbServer.Userpwd) && !string.IsNullOrEmpty(originDbServer.EncryptUserpwd)) {
                originDbServer.Userpwd = _encryptService.Decrypt_Aes(originDbServer.EncryptUserpwd);
            }
            if (string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
                dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
            }
            bool connSuccess= CheckConnect(dbServer).Result;
            if (!connSuccess) {
                errMsg = "to migrate db server can not connect";
                return false;
            }

            //Database=sys.IdentityServer.db;Data Source=127.0.0.1;Port=3307;User Id=root;Password=123456;Charset=utf8
            int tmpIndex = originDbConn.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
            int tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originDbName = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);

            tmpIndex = originDbConn.IndexOf("Data Source=",StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originHost = originDbConn.Substring(tmpIndex+12,tmpEndIndex-tmpIndex-12);

            tmpIndex = originDbConn.IndexOf("Port=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originPort = originDbConn.Substring(tmpIndex+5,tmpEndIndex-tmpIndex-5);

            tmpIndex = originDbConn.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originUserName = originDbConn.Substring(tmpIndex + 8, tmpEndIndex - tmpIndex - 8);

            tmpIndex = originDbConn.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originUserpwd = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);


            bool result = false;

            _mutex.WaitOne();
            try {
                
                string migratingDbName = $"{originDbName}.tmp";

                List<string> creatTenantDbCmdList = new List<string>() {
                    string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),
                    string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",migratingDbName),
                    "exit"
                };

                bool runNormal = RunCmd(creatTenantDbCmdList);
                if (!runNormal) {
                    _logger.LogError("create tmp migrating db");
                    return false;
                }

                List<string> copyCmdList = new List<string>() {
                    string.Format("mysqldump {0} -h{1} -P{2} -u{3} -p{4} --add-drop-table --column-statistics=0 | mysql {5} -h{6} -P{7} -u{8} -p{9}",
                    originDbName,originHost,originPort,originUserName,originUserpwd,
                    migratingDbName,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd
                    ),
                };

                runNormal = RunCmd(copyCmdList);
                if (!runNormal) {
                    _logger.LogError("migrating tmp tenant db");
                    return false;
                }

                copyCmdList = new List<string>() {
                    string.Format("mysqldump {0} -u{1} -p{2} --add-drop-table --column-statistics=0 | mysql {3} -h{4} -P{5} -u{6} -p{7}",
                    migratingDbName,originUserName,originUserpwd,
                    originDbName,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd
                    ),
                };

                runNormal = RunCmd(copyCmdList);
                if (!runNormal) {
                    _logger.LogError("copy tmp tenant db to real db");
                    return false;
                }

                //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
                string migratedDbConnStr = string.Format(_returnDbConnTempalte, originDbName, dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
                tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(migratedDbConnStr);

                Task.Run(() => {
                    DeleteTenantDb(dbServer,migratingDbName);
                    DeleteTenantDb(originDbServer,originDbName);
                }).ConfigureAwait(false);

                result = true;
            } catch (Exception ex) {
                result = false;
                _logger.LogError(ex, $"migrate tenant db {dbServer?.ServerHost} {originDbName} error");
            } finally {
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

                bool runNormal = RunCmd(deleteTenantDbCmdList);
                if (!runNormal) {
                    _logger.LogError($"delete tenant db error");
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

        private bool RunCmd(List<string> innerCmdStrList) {

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.WorkingDirectory = _mysqlBinDirPath;
            p.StartInfo.FileName = "cmd.exe";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.CreateNoWindow = true;

            StringBuilder errBuilder = new StringBuilder();
            int errorSignal = 0;
            p.ErrorDataReceived += (obj, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    while(Interlocked.CompareExchange(ref errorSignal, 1, 0) != 0) {
                        SpinWait.SpinUntil(()=>errorSignal==0,100);
                    }

                    errBuilder.Append(args.Data);

                    Volatile.Write(ref errorSignal,0);
                }
            };

            StringBuilder outputBuilder = new StringBuilder();
            int outputSignal = 0;
            p.OutputDataReceived += (obj, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    while (Interlocked.CompareExchange(ref outputSignal, 1, 0) != 0) {
                        SpinWait.SpinUntil(() => outputSignal == 0, 100);
                    }

                    errBuilder.Append(args.Data);

                    Volatile.Write(ref outputSignal, 0);
                }
            };

            p.Start();
            p.BeginErrorReadLine();

            bool runNormal = true;
            if (innerCmdStrList != null && innerCmdStrList.Any()) {

                foreach (var cmd in innerCmdStrList) {
                    errBuilder.Clear();
                    outputBuilder.Clear();
                    p.StandardInput.WriteLine(cmd);

                    if (string.Compare(cmd, "exit",true) == 0) {
                        Thread.Sleep(30);
                    }else {
                        Thread.Sleep(100);
                        if(!checkCmdRunEnd(ref errBuilder,ref errorSignal,ref outputBuilder,ref outputSignal,out string errMsg)) {
                            runNormal = false;
                            _logger.LogError($"run cmd error,msg:{errMsg}");
                            break;
                        }
                    }
                }
            }

            p.StandardInput.WriteLine("exit");

            p.WaitForExit(1000*1);
      
            return runNormal;
        }

        private bool checkCmdRunEnd(ref StringBuilder errorBuilder,ref int errorSignal,ref StringBuilder outputBuilder,ref int outputSignal,out string errMsg) {
            int tmpOutputStrLength = 0;
            bool runNormal = true;
            int equalCount = 0;
            int maxEqualCount = 3;
            while (true) {
                bool getSignal = false;
                try {
                    if (Interlocked.CompareExchange(ref outputSignal, 1, 0) == 0) {
                        getSignal = true;
                        if (outputBuilder.Length < tmpOutputStrLength) {
                            runNormal = false;
                            break;
                        }
                        else if (outputBuilder.Length == tmpOutputStrLength) {
                            equalCount++;
                            if (equalCount >= maxEqualCount) {
                                break;
                            }
                        }
                        else if (outputBuilder.Length > tmpOutputStrLength) {
                            equalCount = 0;
                            tmpOutputStrLength = outputBuilder.Length;
                        }
                    }
                }
                catch {
                    runNormal = false;
                    break;
                } finally {
                    if (getSignal) {
                        Volatile.Write(ref outputSignal,0);
                    }
                }

                Thread.Sleep(100*3);
            }

            errMsg = string.Empty;
            try {
                while (Interlocked.CompareExchange(ref errorSignal, 1, 0) != 0) {
                    Thread.Sleep(1);
                }
                errMsg = errorBuilder.ToString();
                errorBuilder.Clear();
                while (Interlocked.CompareExchange(ref outputSignal, 1, 0) != 0) {
                    Thread.Sleep(1);
                }
                outputBuilder.Clear();
            } catch {
            } finally {
                Volatile.Write(ref errorSignal,0);
                Volatile.Write(ref outputSignal,0);
            }

            if (errMsg.Contains("error", StringComparison.OrdinalIgnoreCase)) {
                runNormal = false;
            } else {
                errMsg = string.Empty;
            }

            return runNormal;
        }
    }
}
