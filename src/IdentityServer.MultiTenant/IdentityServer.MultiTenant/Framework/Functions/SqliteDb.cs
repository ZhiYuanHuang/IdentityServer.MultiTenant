using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Data.Common;
using System.Reflection;
using IdentityServer.MultiTenant.Dto;

namespace IdentityServer.MultiTenant.Framework
{
    public class SqliteDb: IDbFunc
    {
        private const string SQLITE_CONNECTION = "SqliteConnection";
        private const string SQLITE_TRANSACTION = "SqliteTransaction";
        private const string SQLITE_TRANSACTION_COUNTER = "SqliteTransactionCounter";

        private const int ALARM_THRESHOLD_VALUE = 1000000; // 50毫秒
        private const int COMMAND_TIMEOUT = 300;          // 300秒

        //调用超时
        private const int INVOKE_TIMEOUT = 1000 * 30;

        private readonly string _connectionString = null;

        //Data Source=IdentityServer.db;
        public string ConnectionString { get { return _connectionString; } }

        public string DbName { get; set; }

        private ILogger<SqliteDb> _logger;
        private readonly Mutex _mutex;

        private SQLiteConnection _connection = null;

        public SqliteDb(ILoggerFactory loggerFactory, string connectionString) {
            _logger = loggerFactory.CreateLogger<SqliteDb>();
            _mutex = new Mutex();

            _connectionString = connectionString;

            int tmpIndex = connectionString.IndexOf("Data Source=");

            if (tmpIndex != -1) {
                int tmpEndIndex = connectionString.IndexOf(';',tmpIndex);
                DbName = connectionString.Substring(tmpIndex+12, tmpEndIndex-tmpIndex-1);
            }
        }

        public void BeginTransaction() {
            bool getMutex= _mutex.WaitOne(INVOKE_TIMEOUT);

            if (!getMutex) {
                throw new Exception($"Get Mutex Timeout");
            }

            CallContext.SetData(SQLITE_TRANSACTION_COUNTER, ConvertUtil.ToInt(CallContext.GetData(SQLITE_TRANSACTION_COUNTER), 0) + 1);

            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                connection = GetConnection();

                SQLiteTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                CallContext.SetData(SQLITE_CONNECTION, connection);
                CallContext.SetData(SQLITE_TRANSACTION, transaction);
            }
        }

        public void CommitTransaction() {
            CallContext.SetData(SQLITE_TRANSACTION_COUNTER, ConvertUtil.ToInt(CallContext.GetData(SQLITE_TRANSACTION_COUNTER), 0) - 1);

            int counter = ConvertUtil.ToInt(CallContext.GetData(SQLITE_TRANSACTION_COUNTER), 0);
            if (counter > 0) {
                return;
            }

            SQLiteTransaction transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            if (transaction != null) {
                transaction.Commit();
                transaction.Dispose();

                SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
                FreeConnection(connection);

                CallContext.SetData(SQLITE_TRANSACTION, null);
                CallContext.SetData(SQLITE_CONNECTION, null);

                _mutex.ReleaseMutex();
            }
        }

        public void RollbackTransaction() {
            CallContext.SetData(SQLITE_TRANSACTION_COUNTER, null);

            SQLiteTransaction transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            if (transaction != null) {
                transaction.Rollback();
                transaction.Dispose();

                SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
                FreeConnection(connection);

                CallContext.SetData(SQLITE_TRANSACTION, null);
                CallContext.SetData(SQLITE_CONNECTION, null);

                _mutex.ReleaseMutex();
            }
        }

        public int ExecuteNonQuery(string sql) {
            int ret = 0;

            SQLiteCommand cmd = new SQLiteCommand();
            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                ret = cmd.ExecuteNonQuery();
            }catch(Exception ex) {
                _logger.LogError(ex.ToString()+"|"+sql);
                throw new Exception(ex.Message,ex);
            }
            finally {
                cmd.Dispose();
                cmd = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public int ExecuteNonQuery(string sql,Dictionary<string,object> p) {
            int ret = 0;

            SQLiteCommand cmd = new SQLiteCommand();
            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    SQLiteParameter param = new SQLiteParameter();
                    param.ParameterName = "?" + kvp.Key;
                    param.Value = kvp.Value;
                    param.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(param);
                }

                ret = cmd.ExecuteNonQuery();
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql + "|" + Newtonsoft.Json.JsonConvert.SerializeObject(p));
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public object ExecuteScalar(string sql) {
            object ret = null;

            SQLiteCommand cmd = new SQLiteCommand();
            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                ret = cmd.ExecuteScalar();
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql);
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public object ExecuteScalar(string sql,Dictionary<string,object> p) {
            object ret = null;

            SQLiteCommand cmd = new SQLiteCommand();
            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    SQLiteParameter param = new SQLiteParameter();
                    param.ParameterName = "?" + kvp.Key;
                    param.Value = kvp.Value;
                    param.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(param);
                }

                ret = cmd.ExecuteScalar();
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql + "|" + Newtonsoft.Json.JsonConvert.SerializeObject(p));
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public DataTable ExecuteDataTable(string sql) {
            DataTable ret = new DataTable();

            SQLiteCommand cmd = new SQLiteCommand();
            SQLiteDataAdapter da = new SQLiteDataAdapter();
           
            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                da.SelectCommand = cmd;
                da.Fill(ret);
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql);
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;
                da.Dispose();
                da = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > 10000000) {
                _logger.LogWarning($"执行SQL超过预定伐值:1(秒), SQL:{sql}, Seconds:{elapsedTicks / 10000000}");
            }

            return ret;
        }

        public DataTable ExecuteDataTable(string sql,Dictionary<string,object> p) {
            DataTable ret = new DataTable();

            SQLiteCommand cmd = new SQLiteCommand();
            SQLiteDataAdapter da = new SQLiteDataAdapter();

            Mutex tmpMutex = null;

            SQLiteTransaction transaction = null;
            SQLiteConnection connection = CallContext.GetData(SQLITE_CONNECTION) as SQLiteConnection;
            if (connection == null) {
                tmpMutex = _mutex;
                tmpMutex.WaitOne();

                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(SQLITE_TRANSACTION) as SQLiteTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    SQLiteParameter param = new SQLiteParameter();
                    param.ParameterName = "?" + kvp.Key;
                    param.Value = kvp.Value;
                    param.Direction = ParameterDirection.Input;
                    cmd.Parameters.Add(param);
                }

                da.SelectCommand = cmd;
                da.Fill(ret);
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql + "|" + Newtonsoft.Json.JsonConvert.SerializeObject(p));
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;
                da.Dispose();
                da = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }

                tmpMutex?.ReleaseMutex();
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > 10000000) {
                _logger.LogWarning($"执行SQL超过预定伐值:1(秒), SQL:{sql}, Seconds:{elapsedTicks / 10000000}");
            }

            return ret;
        }

        public DataTable ExecuteDataTable(string sql, int pageIndex, int pageSize) {
            int startIndex = pageSize * (pageIndex - 1);

            string sqlCommand = sql + " LIMIT " + startIndex.ToString() + "," + pageSize.ToString();

            return ExecuteDataTable(sqlCommand);
        }

        public DataTable ExecuteDataTable(string sql, int pageIndex, int pageSize, Dictionary<string, object> p) {
            int startIndex = pageSize * (pageIndex - 1);

            string sqlCommand = sql + " LIMIT " + startIndex.ToString() + "," + pageSize.ToString();

            return ExecuteDataTable(sqlCommand, p);
        }

        public T Query<T>(string sql) where T : new() {
            DataTable dt = ExecuteDataTable(sql);
            return QueryInternal<T>(dt);
        }

        public T Query<T>(string sql, Dictionary<string, object> p) where T : new() {
            DataTable dt = ExecuteDataTable(sql, p);
            return QueryInternal<T>(dt);
        }

        public T Query<T>(string sql, int pageIndex, int pageSize) where T : new() {
            DataTable dt = ExecuteDataTable(sql, pageIndex, pageSize);
            return QueryInternal<T>(dt);
        }

        public T Query<T>(string sql, int pageIndex, int pageSize, Dictionary<string, object> p) where T : new() {
            DataTable dt = ExecuteDataTable(sql, pageIndex, pageSize, p);
            return QueryInternal<T>(dt);
        }

        public List<T> QueryList<T>(string sql) where T : new() {
            DataTable dt = ExecuteDataTable(sql);
            return QueryListInternal<T>(dt);
        }

        public List<T> QueryList<T>(string sql, Dictionary<string, object> p) where T : new() {
            DataTable dt = ExecuteDataTable(sql, p);
            return QueryListInternal<T>(dt);
        }

        public List<T> QueryList<T>(string sql, int pageIndex, int pageSize) where T : new() {
            DataTable dt = ExecuteDataTable(sql, pageIndex, pageSize);
            return QueryListInternal<T>(dt);
        }

        public List<T> QueryList<T>(string sql, int pageIndex, int pageSize, Dictionary<string, object> p) where T : new() {
            DataTable dt = ExecuteDataTable(sql, pageIndex, pageSize, p);
            return QueryListInternal<T>(dt);
        }

        public PagingData<T> QueryPaging<T>(string sql, int pageIndex, int pageSize) where T : new() {
            // recordCount
            string sqlCount = $"SELECT COUNT(*) FROM ({sql}) T";
            int recordCount = ConvertUtil.ToInt(ExecuteScalar(sqlCount), 0);

            // dataRows
            List<T> list = null;
            if (recordCount > 0) {
                list = QueryList<T>(sql, pageIndex, pageSize);
            }
            return new PagingData<T>(pageIndex, pageSize, recordCount, list);
        }

        public PagingData<T> QueryPaging<T>(string sql, int pageIndex, int pageSize, Dictionary<string, object> p) where T : new() {
            // recordCount
            string sqlCount = $"SELECT COUNT(*) FROM ({sql}) T";
            int recordCount = ConvertUtil.ToInt(ExecuteScalar(sqlCount, p), 0);

            // dataRows
            List<T> list = null;
            if (recordCount > 0) {
                list = QueryList<T>(sql, pageIndex, pageSize, p);
            }
            return new PagingData<T>(pageIndex, pageSize, recordCount, list);
        }

        private T QueryInternal<T>(DataTable dt) where T : new() {
            T model = default(T);

            var typeBool = typeof(bool);
            foreach (DataRow row in dt.Rows) {
                model = new T();
                Type type = model.GetType();

                foreach (DataColumn col in dt.Columns) {
                    PropertyInfo property = type.GetProperty(col.ColumnName);
                    if (property != null && row[col] != DBNull.Value) {
                        if (property.PropertyType == typeBool) {
                            string v = row[col].ToString();
                            property.SetValue(model, v == "1" || v.ToLower() == "true");
                        } else {
                            property.SetValue(model, row[col]);
                        }
                    }
                }

                return model;
            }

            return model;
        }

        private List<T> QueryListInternal<T>(DataTable dt) where T : new() {
            List<T> list = new List<T>();

            var typeBool = typeof(bool);
            foreach (DataRow row in dt.Rows) {
                T model = new T();
                Type type = model.GetType();

                foreach (DataColumn col in dt.Columns) {
                    PropertyInfo property = type.GetProperty(col.ColumnName);
                    if (property != null && row[col] != DBNull.Value) {
                        if (property.PropertyType == typeBool) {
                            string v = row[col].ToString();
                            property.SetValue(model, v == "1" || v.ToLower() == "true");
                        } else {
                            property.SetValue(model, row[col]);
                        }
                    }
                }

                list.Add(model);
            }

            return list;
        }

        private SQLiteConnection GetConnection() {
            long startTicks = DateTime.Now.Ticks;

            SQLiteConnection connection = null;
            if (_connection == null) {
                connection = new SQLiteConnection(_connectionString);
                _connection = connection;
            } else {
                connection = _connection;
            }

            try {
                connection.Open();
            }
            catch(Exception ex) {
                _logger.LogError(ex.ToString()+"|"+ _connectionString);
                throw new Exception(ex.Message,ex);
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"打开连接超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), ConnectionString:{_connectionString}, Mileseconds:{elapsedTicks / 10000}");
            }

            return connection;
        }

        private void FreeConnection(SQLiteConnection connection) {
            connection.Close();
        }
    }
}
