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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IdentityServer.MultiTenant.Service
{
    public class MysqlDbOperaService: ITenantDbOperation
    {
        DbServerRepository _dbServerRepository;
        ILogger<MysqlDbOperaService> _logger;
        ILoggerFactory _loggerFactory;

        private readonly string _sampleDb_dumpScript;

        private Mutex _mutex=new Mutex();

        private string _mysqlBinDirPath;
        private string _backupDbDirPath;

        private const string _returnDbConnTempalte = "Database={0};Data Source={1};Port={2};User Id={3};Password={4};Charset=utf8mb4;";
        private const string _tenantDbNameTemplate= "ids_{0}_{1}";

        private readonly EncryptService _encryptService;

        //--no-create-db 
        private const string _backDbCmdTemplate = "mysqldump -h{0} -P{1} -u{2} -p{3} --column-statistics=0  {4} > {5}";

        public MysqlDbOperaService(IConfiguration config,  DbServerRepository dbServerRepository, ILoggerFactory loggerFactory,EncryptService encryptService, IWebHostEnvironment hostingEnvironment) {
            _encryptService = encryptService;
            _dbServerRepository = dbServerRepository;
            _loggerFactory = loggerFactory;
            _logger = loggerFactory?.CreateLogger<MysqlDbOperaService>();

            _mysqlBinDirPath = config["MysqlBinDirPath"];
            if (string.IsNullOrEmpty(_mysqlBinDirPath)) {
                throw new System.ArgumentException("config item MysqlBinDirPath cann't be empty when use mysql");
            }
            if (!System.IO.Directory.Exists(_mysqlBinDirPath)) {
                throw new System.ArgumentException($"cann't found dir {_mysqlBinDirPath}");
            }
            string[] mysqlExeFileList= System.IO.Directory.GetFiles(_mysqlBinDirPath,"mysql*");
            if (!mysqlExeFileList.Any()) {
                throw new System.ArgumentException($"cann't found dir {_mysqlBinDirPath} exe files");
            }
            bool foundMysqlExe = false;
            bool foundMysqlDumpExe = false;
            foreach(var mysqlBinFile in mysqlExeFileList) {
                string fileNameWithoutExtension = System.IO.Path.GetFileNameWithoutExtension(mysqlBinFile);
                if(string.Compare(fileNameWithoutExtension, "mysql",true)==0) {
                    foundMysqlExe = true;
                }
                if (string.Compare(fileNameWithoutExtension, "mysqldump", true) == 0) {
                    foundMysqlDumpExe = true;
                }

                if(foundMysqlExe && foundMysqlDumpExe) {
                    break;
                }
            }

            if(!foundMysqlExe || !foundMysqlDumpExe) {
                throw new System.ArgumentException($"{_mysqlBinDirPath} missing bin file mysql or mysqldump");
            }

            _backupDbDirPath = config["BackupDbDirPath"];
            if (string.IsNullOrEmpty(_backupDbDirPath)) {
                _backupDbDirPath = System.IO.Path.Combine(hostingEnvironment.ContentRootPath,"DbBackup");
            }
            if(!System.IO.Directory.Exists(_backupDbDirPath)) {
                System.IO.Directory.CreateDirectory(_backupDbDirPath);
            }

            _sampleDb_dumpScript = System.IO.Path.Combine(hostingEnvironment.ContentRootPath, "Db", "ids_sample_dump.sql");

            if(!System.IO.File.Exists(_sampleDb_dumpScript)) {
                throw new Exception($"cann't find the sample idenitytserver db dump script file :{_sampleDb_dumpScript}");
            }
            _sampleDb_dumpScript = _sampleDb_dumpScript.Replace('\\','/');
            
        }

        //public bool CreateTenantDb(ref TenantInfoDto tenantInfoDto,out DbServerModel dbServer ,out string creatingDbName) {
        //    dbServer = null;
        //    creatingDbName = string.Empty;
        //    if (string.IsNullOrEmpty(_mysqlBinDirPath)
        //        || string.IsNullOrEmpty(_sampleDb_serverHost)
        //        || _sampleDb_serverPort==0
        //        || string.IsNullOrEmpty(_sampleDb_username)
        //        || string.IsNullOrEmpty(_sampleDb_userpassword)
        //        || string.IsNullOrEmpty(_sampleDb_dbname)
        //        ) {
        //        _logger.LogError("DbOperaService CreateTenantDb error,config error");

        //        return false;
        //    }

        //    bool result = false;

        //    _mutex.WaitOne();
        //    try {
        //        var dbServerList= _dbServerRepository.GetDbServers();
        //        if (!dbServerList.Any()) {
        //            _logger.LogError("DbOperaService CreateTenantDb error,dbserver is empty");
        //            return false;
        //        }

        //        var enableDbServerList= dbServerList.Where(x=>x.EnableStatus==(int)EnableStatusEnum.Enable).OrderBy(x => x.CreatedDbCount).ThenBy(x => x.ServerHost).ToList();
        //        DbServerModel selectedDbServer = null;

        //        foreach (var tmpDbServer in enableDbServerList) {

        //            var userpassword = tmpDbServer.Userpwd;
        //            if (string.IsNullOrEmpty(userpassword) && !string.IsNullOrEmpty(tmpDbServer.EncryptUserpwd)) {
        //                //解密
        //                userpassword = _encryptService.Decrypt_Aes(tmpDbServer.EncryptUserpwd);
        //                tmpDbServer.Userpwd = userpassword;
        //            }

        //            if (CheckConnect(tmpDbServer).Result) {
        //                selectedDbServer = tmpDbServer;
        //                break;
        //            }
        //        }

        //        if (selectedDbServer == null) {
        //            _logger.LogError("CreateTenantDb error,errmsg:have not connect success db server");
        //            return false;
        //        }

        //        dbServer = selectedDbServer;

        //        creatingDbName = $"ids_{tenantInfoDto.TenantDomain}_{tenantInfoDto.Identifier}";

        //        List<string> creatTenantDbCmdList = new List<string>() {
        //            string.Format("mysql -h{0} -P{1} -u{2} -p{3}",selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd),
        //            string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",creatingDbName),
        //            "exit"
        //        };

        //        bool runNormal= RunCmd(creatTenantDbCmdList);
        //        if (!runNormal) {
        //            _logger.LogError("create tenant db");
        //            return false;
        //        }

        //        List<string> copyCmdList = new List<string>() {
        //            string.Format("mysqldump {0} -h{1} -P{2} -u{3} -p{4} --add-drop-table --column-statistics=0 | mysql {5} -h{6} -P{7} -u{8} -p{9}",
        //            _sampleDb_dbname,_sampleDb_serverHost,_sampleDb_serverPort,_sampleDb_username,_sampleDb_userpassword,
        //            creatingDbName,selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd
        //            ),
        //        };

        //        runNormal = RunCmd( copyCmdList);
        //        if (!runNormal) {
        //            _logger.LogError("copy tenant db");
        //            return false;
        //        }

        //        //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
        //        string createdDbConnStr = string.Format(_returnDbConnTempalte,creatingDbName,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd);
        //        tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(createdDbConnStr);

        //        result = true;
        //    } catch(Exception ex) {
        //        result = false;
        //        _logger.LogError(ex,$"create tenant db {dbServer?.ServerHost} {creatingDbName} error");
        //    } 
        //    finally {
        //        _mutex.ReleaseMutex();
        //    }

        //    return result;
        //}

        public bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName) {
            StringBuilder createLogBuilder = null;
            return CreateTenantDb(ref tenantInfoDto,out dbServer,out creatingDbName,ref createLogBuilder);
        }

        public bool CreateTenantDb(ref TenantInfoDto tenantInfoDto, out DbServerModel dbServer, out string creatingDbName, ref StringBuilder createLogBuilder) {
            dbServer = null;
            creatingDbName = string.Empty;
            if (string.IsNullOrEmpty(_mysqlBinDirPath)){
                _logger.LogError("DbOperaService CreateTenantDb error,config error");

                return false;
            }

            bool result = false;

            _mutex.WaitOne();
            try {
                var dbServerList = _dbServerRepository.GetDbServers();
                if (!dbServerList.Any()) {
                    _logger.LogError("DbOperaService CreateTenantDb error,dbserver is empty");
                    return false;
                }

                var enableDbServerList = dbServerList.Where(x => x.EnableStatus == (int)EnableStatusEnum.Enable).OrderBy(x => x.CreatedDbCount).ThenBy(x => x.ServerHost).ToList();
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

                List<CommandInfo> creatTenantDbCmdInfos = new List<CommandInfo>() {
                    new CommandInfo(string.Format("mysql -h{0} -P{1} -u{2} -p{3}",selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd),CommandInfo.CheckErrorLabel),
                    new CommandInfo(string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",creatingDbName),CommandInfo.CheckErrorLabel,true,"mysql>"),
                    new CommandInfo("exit",null,true,"mysql> "),
                    new CommandInfo(string.Format("mysql -h{0} -P{1} -u{2} -p{3} {4} < {5} & exit",selectedDbServer.ServerHost,selectedDbServer.ServerPort,selectedDbServer.UserName,selectedDbServer.Userpwd,
                        creatingDbName,_sampleDb_dumpScript),CommandInfo.CheckErrorLabel),
                };

                bool runNormal = RunCmd(creatTenantDbCmdInfos);
                if (!runNormal) {
                    _logger.LogError("create tenant db");
                    return false;
                }

                //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
                string createdDbConnStr = string.Format(_returnDbConnTempalte, creatingDbName, dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
                tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(createdDbConnStr);

                result = true;
            } catch (Exception ex) {
                result = false;
                _logger.LogError(ex, $"create tenant db {dbServer?.ServerHost} {creatingDbName} error");
            } finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        //public bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto,DbServerModel originDbServer,DbServerModel dbServer, ref StringBuilder migrateLogBuilder, out string errMsg) {

        //    string originDbConn = tenantInfoDto.ConnectionString;
        //    errMsg = string.Empty;
        //    if (string.IsNullOrEmpty(originDbServer.Userpwd) && !string.IsNullOrEmpty(originDbServer.EncryptUserpwd)) {
        //        originDbServer.Userpwd = _encryptService.Decrypt_Aes(originDbServer.EncryptUserpwd);
        //    }
        //    if (string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
        //        dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
        //    }
        //    bool connSuccess= CheckConnect(dbServer).Result;
        //    if (!connSuccess) {
        //        errMsg = "to migrate db server can not connect";
        //        migrateLogBuilder?.AppendLine();
        //        migrateLogBuilder.AppendLine(errMsg);
        //        return false;
        //    }

        //    //Database=sys.IdentityServer.db;Data Source=127.0.0.1;Port=3307;User Id=root;Password=123456;Charset=utf8
        //    int tmpIndex = originDbConn.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
        //    int tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
        //    string originDbName = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);

        //    tmpIndex = originDbConn.IndexOf("Data Source=",StringComparison.OrdinalIgnoreCase);
        //    tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
        //    string originHost = originDbConn.Substring(tmpIndex+12,tmpEndIndex-tmpIndex-12);

        //    tmpIndex = originDbConn.IndexOf("Port=", StringComparison.OrdinalIgnoreCase);
        //    tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
        //    string originPort = originDbConn.Substring(tmpIndex+5,tmpEndIndex-tmpIndex-5);

        //    tmpIndex = originDbConn.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase);
        //    tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
        //    string originUserName = originDbConn.Substring(tmpIndex + 8, tmpEndIndex - tmpIndex - 8);

        //    tmpIndex = originDbConn.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
        //    tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
        //    string originUserpwd = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);


        //    bool result = false;

        //    _mutex.WaitOne();
        //    try {
                
        //        string migratingDbName = $"{originDbName}.tmp";

        //        #region create migrate server tmp db

        //        List<string> creatTenantDbCmdList = new List<string>() {
        //            string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),
        //            string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",migratingDbName),
        //            "exit"
        //        };

        //        migrateLogBuilder?.AppendLine("创建迁移临时库");
        //        bool runNormal = RunCmd(creatTenantDbCmdList,migrateLogBuilder);
        //        if (!runNormal) {
        //            _logger.LogError("create tmp migrating db error");
        //            migrateLogBuilder?.AppendLine();
        //            migrateLogBuilder.AppendLine("create tmp migrating db error");
        //            return false;
        //        }

        //        #endregion

        //        #region copy origin server db to migrate server tmp db

        //        List<string> copyCmdList = new List<string>() {
        //            string.Format("mysqldump {0} -h{1} -P{2} -u{3} -p{4} --add-drop-table --column-statistics=0 | mysql {5} -h{6} -P{7} -u{8} -p{9}",
        //            originDbName,originHost,originPort,originUserName,originUserpwd,
        //            migratingDbName,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd
        //            ),
        //        };

        //        migrateLogBuilder?.AppendLine("拷贝原始数据库至临时库");
        //        runNormal = RunCmd(copyCmdList, migrateLogBuilder);
        //        if (!runNormal) {
        //            migrateLogBuilder?.AppendLine();
        //            migrateLogBuilder.AppendLine("migrating tmp tenant db error");
        //            migrateLogBuilder?.AppendLine("回撤中。。。");
        //            DeleteTenantDb(dbServer, migratingDbName,migrateLogBuilder);
        //            _logger.LogError("migrating tmp tenant db error");
        //            return false;
        //        }

        //        #endregion

        //        #region create migrate server real db

        //        creatTenantDbCmdList = new List<string>() {
        //            string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),
        //            string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",originDbName),
        //            "exit"
        //        };

        //        migrateLogBuilder?.AppendLine("创建最终实际数据库");
        //        runNormal = RunCmd(creatTenantDbCmdList, migrateLogBuilder);
        //        if (!runNormal) {
        //            migrateLogBuilder?.AppendLine();
        //            migrateLogBuilder.AppendLine("create real migrating db error");
        //            migrateLogBuilder?.AppendLine("回撤中。。。");
        //            DeleteTenantDb(dbServer, migratingDbName, migrateLogBuilder);
        //            _logger.LogError("create real migrating db error");
                  
        //            return false;
        //        }

        //        #endregion

        //        #region copy tmp db to real db

        //        copyCmdList = new List<string>() {
        //            string.Format("mysqldump {0} -h{4} -P{5} -u{1} -p{2} --add-drop-table --column-statistics=0 | mysql {3} -h{4} -P{5} -u{1} -p{2}",
        //            migratingDbName,dbServer.UserName,dbServer.Userpwd,
        //            originDbName,dbServer.ServerHost,dbServer.ServerPort
        //            ),
        //        };

        //        migrateLogBuilder?.AppendLine("拷贝临时库至最终实际数据库");
        //        runNormal = RunCmd(copyCmdList, migrateLogBuilder);
        //        if (!runNormal) {
        //            migrateLogBuilder?.AppendLine();
        //            migrateLogBuilder.AppendLine("copy tmp tenant db to real db error");
        //            migrateLogBuilder?.AppendLine("回撤中。。。");
        //            DeleteTenantDb(dbServer, migratingDbName, migrateLogBuilder);
        //            DeleteTenantDb(dbServer, originDbName,migrateLogBuilder);
        //            _logger.LogError("copy tmp tenant db to real db error");
                    
        //            return false;
        //        }

        //        #endregion

        //        //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
        //        string migratedDbConnStr = string.Format(_returnDbConnTempalte, originDbName, dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
        //        tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(migratedDbConnStr);
        //        tenantInfoDto.ConnectionString = null;

        //        #region delete tmp and origin db

        //        migrateLogBuilder?.AppendLine();
        //        migrateLogBuilder?.AppendLine("删除临时库");
        //        DeleteTenantDb(dbServer, migratingDbName, migrateLogBuilder);
        //        migrateLogBuilder?.AppendLine("删除原数据库");
        //        DeleteTenantDb(originDbServer, originDbName, migrateLogBuilder,true);

        //        #endregion

        //        result = true;
        //    } catch (Exception ex) {
        //        result = false;
        //        _logger.LogError(ex, $"migrate tenant db {dbServer?.ServerHost} {originDbName} error");
        //        migrateLogBuilder.AppendLine($"migrate tenant db {dbServer?.ServerHost} {originDbName} error");
        //    } finally {
        //        _mutex.ReleaseMutex();
        //    }

        //    return result;
        //}

        public void AttachDbConn(ref TenantInfoDto tenantInfoDto, DbServerModel dbServer) {
            string tenantDbName = string.Format(_tenantDbNameTemplate,tenantInfoDto.TenantDomain,tenantInfoDto.Identifier);

            string migratedDbConnStr = string.Format(_returnDbConnTempalte, tenantDbName, dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
            tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(migratedDbConnStr);
            tenantInfoDto.ConnectionString = null;
        }

        public bool MigrateTenantDb(ref TenantInfoDto tenantInfoDto, DbServerModel originDbServer, DbServerModel dbServer, ref StringBuilder migrateLogBuilder, out string errMsg) {

            string originDbConn = tenantInfoDto.ConnectionString;
            errMsg = string.Empty;
            if (string.IsNullOrEmpty(originDbServer.Userpwd) && !string.IsNullOrEmpty(originDbServer.EncryptUserpwd)) {
                originDbServer.Userpwd = _encryptService.Decrypt_Aes(originDbServer.EncryptUserpwd);
            }
            if (string.IsNullOrEmpty(dbServer.Userpwd) && !string.IsNullOrEmpty(dbServer.EncryptUserpwd)) {
                dbServer.Userpwd = _encryptService.Decrypt_Aes(dbServer.EncryptUserpwd);
            }
            bool connSuccess = CheckConnect(dbServer).Result;
            if (!connSuccess) {
                errMsg = "to migrate db server can not connect";
                migrateLogBuilder?.AppendLine();
                migrateLogBuilder.AppendLine(errMsg);
                return false;
            }

            //Database=sys.IdentityServer.db;Data Source=127.0.0.1;Port=3307;User Id=root;Password=123456;Charset=utf8
            int tmpIndex = originDbConn.IndexOf("database=", StringComparison.OrdinalIgnoreCase);
            int tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originDbName = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);

            tmpIndex = originDbConn.IndexOf("Data Source=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originHost = originDbConn.Substring(tmpIndex + 12, tmpEndIndex - tmpIndex - 12);

            tmpIndex = originDbConn.IndexOf("Port=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originPort = originDbConn.Substring(tmpIndex + 5, tmpEndIndex - tmpIndex - 5);

            tmpIndex = originDbConn.IndexOf("User Id=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originUserName = originDbConn.Substring(tmpIndex + 8, tmpEndIndex - tmpIndex - 8);

            tmpIndex = originDbConn.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
            tmpEndIndex = originDbConn.IndexOf(';', tmpIndex);
            string originUserpwd = originDbConn.Substring(tmpIndex + 9, tmpEndIndex - tmpIndex - 9);


            bool result = false;

            _mutex.WaitOne();
            try {
                #region 备份原始库至脚本

                migrateLogBuilder?.AppendLine("备份原始库至脚本");
                string backSqlFilePath = System.IO.Path.Combine(_backupDbDirPath, $"Mysql_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{originDbName}");
                
                List<CommandInfo> backupDbCmdList = new List<CommandInfo>() {
                        new CommandInfo(string.Format(_backDbCmdTemplate,originHost,originPort,originUserName,originUserpwd,originDbName,backSqlFilePath)+" & exit",CommandInfo.CheckErrorLabel), 
                    };

                bool runNormal = RunCmd(backupDbCmdList, migrateLogBuilder);
                if (!runNormal) {
                    _logger.LogError("backup db to script error");
                    migrateLogBuilder?.AppendLine();
                    migrateLogBuilder.AppendLine("backup db to script error");
                    return false;
                }

                #endregion

                string migratingDbName = $"{originDbName}.tmp";

                #region create migrate server tmp db

                List<CommandInfo> creatTenantDbCmdInfos = new List<CommandInfo>() {
                    new CommandInfo(string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),CommandInfo.CheckErrorLabel),
                    new CommandInfo(string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",migratingDbName),CommandInfo.CheckErrorLabel,true,"mysql>"),
                    new CommandInfo("exit",null,true,"mysql> "),
                    new CommandInfo(string.Format("mysql -h{0} -P{1} -u{2} -p{3} {4} < {5} & exit",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd,
                        migratingDbName,backSqlFilePath.Replace('\\','/')),CommandInfo.CheckErrorLabel),
                };

                migrateLogBuilder?.AppendLine("创建迁移临时库");
                runNormal = RunCmd(creatTenantDbCmdInfos, migrateLogBuilder);
                if (!runNormal) {
                    _logger.LogError("create tmp migrating db error");
                    migrateLogBuilder?.AppendLine();
                    migrateLogBuilder.AppendLine("create tmp migrating db error");
                    return false;
                }

                #endregion

                #region create migrate server real db

                creatTenantDbCmdInfos = new List<CommandInfo>() {
                    new CommandInfo(string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),CommandInfo.CheckErrorLabel),
                    new CommandInfo(string.Format("create database `{0}` default character set utf8mb4 collate utf8mb4_general_ci;",originDbName),CommandInfo.CheckErrorLabel,true,"mysql>"),
                    new CommandInfo("exit",null,true,"mysql> "),
                    new CommandInfo(string.Format("mysqldump {0} -h{4} -P{5} -u{1} -p{2} --add-drop-table --column-statistics=0 | mysql {3} -h{4} -P{5} -u{1} -p{2} & exit",
                        migratingDbName,dbServer.UserName,dbServer.Userpwd,
                        originDbName,dbServer.ServerHost,dbServer.ServerPort
                        ),CommandInfo.CheckErrorLabel),
                };

                migrateLogBuilder?.AppendLine("创建最终实际数据库");
                runNormal = RunCmd(creatTenantDbCmdInfos, migrateLogBuilder);
                if (!runNormal) {
                    migrateLogBuilder?.AppendLine();
                    migrateLogBuilder.AppendLine("create real migrating db error");
                    migrateLogBuilder?.AppendLine("回撤中。。。");
                    DeleteTenantDb(dbServer, migratingDbName, migrateLogBuilder);
                    _logger.LogError("create real migrating db error");

                    return false;
                }

                #endregion

                //Database ={ 0}; Data Source = { 1 }; Port ={ 2}; User Id = { 3 }; Password ={ 4}; Charset = utf8mb4;
                string migratedDbConnStr = string.Format(_returnDbConnTempalte, originDbName, dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
                tenantInfoDto.EncryptedIdsConnectionString = _encryptService.Encrypt_Aes(migratedDbConnStr);
                tenantInfoDto.ConnectionString = null;

                #region delete tmp and origin db

                migrateLogBuilder?.AppendLine();
                migrateLogBuilder?.AppendLine("删除临时库");
                DeleteTenantDb(dbServer, migratingDbName, migrateLogBuilder);
                migrateLogBuilder?.AppendLine("删除原数据库");
                DeleteTenantDb(originDbServer, originDbName, migrateLogBuilder);

                #endregion

                result = true;
            } catch (Exception ex) {
                result = false;
                _logger.LogError(ex, $"migrate tenant db {dbServer?.ServerHost} {originDbName} error");
                migrateLogBuilder.AppendLine($"migrate tenant db {dbServer?.ServerHost} {originDbName} error");
            } finally {
                _mutex.ReleaseMutex();
            }

            return result;
        }

        public bool DeleteTenantDb(DbServerModel dbServer, string toDeleteDb, bool backup = false) {

            return DeleteTenantDb(dbServer,toDeleteDb,null,backup);
        }

        private bool DeleteTenantDb(DbServerModel dbServer,string toDeleteDb, StringBuilder migrateLogBuilder = null, bool backup=false) {

            bool result = false;
            _mutex.WaitOne();
            try {

                bool runNormal = false;
                //string fileName = "mysql";
                //string args = string.Format("-h{0} -P{1} -u{2} -p{3}", dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
                if (backup) {
                    string backSqlFilePath = System.IO.Path.Combine(_backupDbDirPath,$"Mysql_{DateTime.Now.ToString("yyyyMMddHHmmss")}_{toDeleteDb}");
                    List<CommandInfo> backupDbCmdList = new List<CommandInfo>() {
                        new CommandInfo( string.Format(_backDbCmdTemplate,dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd,toDeleteDb,backSqlFilePath)+" & exit",CommandInfo.CheckErrorLabel),
                    };

                    migrateLogBuilder?.AppendLine("备份原数据库。。。");
                    runNormal = RunCmd(backupDbCmdList, migrateLogBuilder);
                    if (!runNormal) {
                        var tmpMsg = $"backup db {dbServer.ServerHost}:{dbServer.ServerPort} {toDeleteDb} error";
                        migrateLogBuilder?.AppendLine(tmpMsg);
                        _logger.LogError(tmpMsg);
                        return false ;
                    }

                    if (!System.IO.File.Exists(backSqlFilePath)) {
                        var tmpMsg = $"cann't not found backuped sql {backSqlFilePath}";
                        migrateLogBuilder?.AppendLine(tmpMsg);
                        _logger.LogError(tmpMsg);
                        return false;
                    }
                }

                List<string> deleteTenantDbCmdList = new List<string>() {
                    string.Format("mysql -h{0} -P{1} -u{2} -p{3}",dbServer.ServerHost,dbServer.ServerPort,dbServer.UserName,dbServer.Userpwd),
                    string.Format("drop database if exists `{0}`;",toDeleteDb),
                    string.Format("exit")
                };

                runNormal = RunCmd(deleteTenantDbCmdList,migrateLogBuilder);
                if (!runNormal) {
                    var tmpMsg = $"delete tenant db error";
                    migrateLogBuilder?.AppendLine(tmpMsg);
                    _logger.LogError(tmpMsg);
                    return false;
                }

                result = true;
                
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

            return DeleteTenantDb(dbServer,dbName,null,true);
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

        public static async Task<Tuple<bool,string>> CheckConnectAndVersion(DbServerModel dbServer) {

            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            string connStr = string.Format("Data Source={0};Port={1};User Id={2};Password={3};", dbServer.ServerHost, dbServer.ServerPort, dbServer.UserName, dbServer.Userpwd);
            var connection = new MySqlConnection(connStr);

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

        public async Task<bool> CheckConnect(string connStr) {
            if (string.IsNullOrEmpty(connStr)) {
                return false;
            }

            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            var connection = new MySqlConnection(connStr);

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

            return result;
        }

        public async Task<Tuple<bool,string>> CheckConnectAndVersion(string connStr) {
            if (string.IsNullOrEmpty(connStr)) {
                return new Tuple<bool, string>(false,string.Empty);
            }

            //"Data Source=127.0.0.1;Port=13306;User Id=devuser;Password=devpwd;Charset=utf8;",
            var connection = new MySqlConnection(connStr);
            string version= string.Empty;
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

        private bool RunCmd(List<string> innerCmdStrList,StringBuilder migrateLogBuilder=null) {

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

                    migrateLogBuilder?.AppendLine(args.Data);
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

                    migrateLogBuilder?.AppendLine(args.Data);
                    outputBuilder.Append(args.Data);

                    Volatile.Write(ref outputSignal, 0);
                }
            };

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            bool runNormal = true;
            if (innerCmdStrList != null && innerCmdStrList.Any()) {
                bool firstCmd = true;
                foreach (var cmd in innerCmdStrList) {
                    errBuilder.Clear();
                    outputBuilder.Clear();
                    if(firstCmd) {
                        firstCmd = false;
                    } else {
                        migrateLogBuilder?.AppendLine($"mysql> {cmd}");
                    }
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

            migrateLogBuilder?.AppendLine();
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

                Thread.Sleep(1000);
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

        private bool RunCmd(List<CommandInfo> innerCmdInfoList, StringBuilder migrateLogBuilder = null) {

            System.Diagnostics.Process p = new System.Diagnostics.Process();
            p.StartInfo.WorkingDirectory = _mysqlBinDirPath;
            p.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)? "cmd.exe": "/bin/bash";
            p.StartInfo.UseShellExecute = false;
            p.StartInfo.RedirectStandardInput = true;
            p.StartInfo.RedirectStandardOutput = true;
            p.StartInfo.RedirectStandardError = true;
            
            p.StartInfo.CreateNoWindow = true;

            StringBuilder errBuilder = new StringBuilder();
            int errorSignal = 0;
            p.ErrorDataReceived += (obj, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    while (Interlocked.CompareExchange(ref errorSignal, 1, 0) != 0) {
                        SpinWait.SpinUntil(() => errorSignal == 0, 100);
                    }

                    migrateLogBuilder?.AppendLine(args.Data);
                    errBuilder.Append(args.Data);

                    Volatile.Write(ref errorSignal, 0);
                }
            };

            StringBuilder outputBuilder = new StringBuilder();
            int outputSignal = 0;
            p.OutputDataReceived += (obj, args) => {
                if (!string.IsNullOrEmpty(args.Data)) {
                    while (Interlocked.CompareExchange(ref outputSignal, 1, 0) != 0) {
                        SpinWait.SpinUntil(() => outputSignal == 0, 100);
                    }

                    migrateLogBuilder?.AppendLine(args.Data);
                    outputBuilder.Append(args.Data);

                    Volatile.Write(ref outputSignal, 0);
                }
            };

            p.Start();
            p.BeginErrorReadLine();
            p.BeginOutputReadLine();

            bool runNormal = true;
            if (innerCmdInfoList != null && innerCmdInfoList.Any()) {
                foreach (var cmdInfo in innerCmdInfoList) {
                    resetStringBuilder(ref outputBuilder,ref outputSignal);
                    resetStringBuilder(ref errBuilder, ref errorSignal);

                    if (cmdInfo.LogPrint) {
                        if (!string.IsNullOrEmpty(cmdInfo.LogPrintPre)) {
                            migrateLogBuilder?.Append($"{cmdInfo.LogPrintPre} ");
                        }
                        migrateLogBuilder?.AppendLine($"{cmdInfo.CmdStr}");
                    }

                    p.StandardInput.WriteLine(cmdInfo.CmdStr);

                    if (string.Compare(cmdInfo.CmdStr, "exit", true) == 0) {
                        Thread.Sleep(30);
                    } else {
                        if (!cmdInfo.RunAndCheckResult(ref outputBuilder, ref outputSignal, ref errBuilder, ref errorSignal, out string errMsg)) {
                            runNormal = false;
                            _logger.LogError($"run cmd error,msg:{errMsg}");
                            break;
                        }
                    }
                }
            }

            if(!runNormal) {
                p.StandardInput.WriteLine("exit");
            }

            p.WaitForExit(1000 * 30);

            migrateLogBuilder?.AppendLine();
            return runNormal;
        }

        private void resetStringBuilder(ref StringBuilder stringBuilder, ref int signal) {

            while (Interlocked.CompareExchange(ref signal, 1, 0) != 0) {
                Thread.Sleep(1);
            }

            stringBuilder.Clear();
            Volatile.Write(ref signal, 0);
        }


    }

    internal class CommandInfo
    {
        public readonly bool LogPrint;
        public readonly string LogPrintPre;
        public readonly string CmdStr;
        public readonly RunAndCheckDelegate InnerRunAndCheckResult;

        public delegate bool RunAndCheckDelegate(ref StringBuilder outputBuilder, ref int outputSignal, ref StringBuilder errorBuilder, ref int errorSignal, out string errMsg);

        public CommandInfo(string cmdStr, RunAndCheckDelegate runAndCheckDelegate, bool logPrint=false,string logPrintPre="") {
            CmdStr = cmdStr;
            InnerRunAndCheckResult = runAndCheckDelegate;
            LogPrint = logPrint;
            LogPrintPre=logPrintPre;
        }

        public bool RunAndCheckResult(ref StringBuilder outputBuilder, ref int outputSignal, ref StringBuilder errorBuilder, ref int errorSignal,  out string errMsg) {
            if (InnerRunAndCheckResult == null) {
                errMsg = string.Empty;
                return true;
            }
            return InnerRunAndCheckResult(ref outputBuilder, ref outputSignal, ref errorBuilder, ref errorSignal, out errMsg);
        }

        public static bool CheckErrorLabel(ref StringBuilder outputBuilder, ref int outputSignal, ref StringBuilder errorBuilder, ref int errorSignal, out string errMsg) {
            int tmpOutputStrLength = 0;
            int tmpErrorStrLength = 0;
            bool runNormal = true;
            int outputNotChangeCount = 0;
            int errorNotChangeCount = 0;
            int maxNotChangeCount = 3;
            errMsg = string.Empty;

            while (true) {

                #region 监测标准输出

                bool getOutputSignal = false;
                try {
                    if (Interlocked.CompareExchange(ref outputSignal, 1, 0) == 0) {
                        getOutputSignal = true;
                        if (outputBuilder.Length < tmpOutputStrLength) {   //标准输出长度变小，无法监测结果
                            runNormal = false;
                            break;
                        } else if (outputBuilder.Length == tmpOutputStrLength) {   //监测到标准输出长度不变
                            outputNotChangeCount++;
                        } else if (outputBuilder.Length > tmpOutputStrLength) {    //监测到标准输出长度改变
                            outputNotChangeCount = 0;

                            string tmpNewOutputMsg = outputBuilder.ToString(tmpOutputStrLength,outputBuilder.Length-tmpOutputStrLength);
                            if (tmpNewOutputMsg.IndexOf("error", StringComparison.OrdinalIgnoreCase) != -1) {   //检测到error标签
                                runNormal = false;
                                errMsg = tmpNewOutputMsg;
                                break;
                            }

                            tmpOutputStrLength = outputBuilder.Length;
                        }
                    }
                } catch {
                    runNormal = false;
                    break;
                } finally {
                    if (getOutputSignal) {
                        Volatile.Write(ref outputSignal, 0);
                    }
                }

                #endregion

                #region 监测错误输出

                bool getErrorSignal = false;
                try {
                    if (Interlocked.CompareExchange(ref errorSignal, 1, 0) == 0) {
                        getErrorSignal = true;
                        if (errorBuilder.Length < tmpErrorStrLength) {   //标准输出长度变小，无法监测结果
                            runNormal = false;
                            break;
                        } else if (errorBuilder.Length == tmpErrorStrLength) {   //监测到标准输出长度不变
                            errorNotChangeCount++;
                        } else if (errorBuilder.Length > tmpErrorStrLength) {    //监测到标准输出长度改变
                            errorNotChangeCount = 0;

                            string tmpNewErrorMsg = errorBuilder.ToString(tmpErrorStrLength, errorBuilder.Length - tmpErrorStrLength);
                            if (tmpNewErrorMsg.IndexOf("error", StringComparison.OrdinalIgnoreCase) != -1) {   //检测到error标签
                                runNormal = false;
                                errMsg = tmpNewErrorMsg;
                                break;
                            }

                            tmpErrorStrLength = errorBuilder.Length;
                        }
                    }
                } catch {
                    runNormal = false;
                    break;
                } finally {
                    if (getErrorSignal) {
                        Volatile.Write(ref errorSignal, 0);
                    }
                }

                #endregion

                //标准输出和错误输出不再变化
                if (outputNotChangeCount >= maxNotChangeCount && errorNotChangeCount >= maxNotChangeCount) {
                    break;
                }

                Thread.Sleep(500);
            }

            return runNormal;
        }
    }
}
