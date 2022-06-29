using IdentityServer.MultiTenant.Dto;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Reflection;

namespace IdentityServer.MultiTenant.Framework
{
    public class MySqlDb: IDbFunc
    {
        private const string MYSQL_CONNECTION = "MySqlConnection";
        private const string MYSQL_TRANSACTION = "MySqlTransaction";
        private const string MYSQL_TRANSACTION_COUNTER = "MySqlTransactionCounter";

        private const int ALARM_THRESHOLD_VALUE = 1000000; // 50毫秒
        private const int COMMAND_TIMEOUT = 300;          // 300秒    

        private ConcurrentQueue<MySqlConnection> _queue = new ConcurrentQueue<MySqlConnection>();

        private readonly string _connectionString = null;
        public string ConnectionString { get { return _connectionString; } }
        /// <summary>
        /// 数据库名称
        /// </summary>
        public string DbName { get; }

        private ILogger<MySqlDb> _logger;

        public MySqlDb(ILoggerFactory loggerFactory,string connectionString) {

            _logger = loggerFactory.CreateLogger<MySqlDb>();

            _connectionString = connectionString;
            {
                // get db_name from connection_string
                foreach (string s in _connectionString.Split(';')) {
                    if (s.Trim().ToLower().StartsWith("database")) {
                        DbName = s.Split('=')[1].Trim();
                        break;
                    }
                }
            }
        }

        public void BeginTransaction() {
            CallContext.SetData(MYSQL_TRANSACTION_COUNTER, ConvertUtil.ToInt(CallContext.GetData(MYSQL_TRANSACTION_COUNTER), 0) + 1);

            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
                MySqlTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                CallContext.SetData(MYSQL_CONNECTION, connection);
                CallContext.SetData(MYSQL_TRANSACTION, transaction);
            }
        }

        public void CommitTransaction() {
            CallContext.SetData(MYSQL_TRANSACTION_COUNTER, ConvertUtil.ToInt(CallContext.GetData(MYSQL_TRANSACTION_COUNTER), 0) - 1);

            int counter = ConvertUtil.ToInt(CallContext.GetData(MYSQL_TRANSACTION_COUNTER), 0);
            if (counter > 0) {
                return;
            }

            MySqlTransaction transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            if (transaction != null) {
                transaction.Commit();
                transaction.Dispose();

                MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
                FreeConnection(connection);

                CallContext.SetData(MYSQL_TRANSACTION, null);
                CallContext.SetData(MYSQL_CONNECTION, null);
            }
        }

        public void RollbackTransaction() {
            CallContext.SetData(MYSQL_TRANSACTION_COUNTER, null);

            MySqlTransaction transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            if (transaction != null) {
                transaction.Rollback();
                transaction.Dispose();

                MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
                FreeConnection(connection);

                CallContext.SetData(MYSQL_TRANSACTION, null);
                CallContext.SetData(MYSQL_CONNECTION, null);
            }
        }

        public int ExecuteNonQuery(string sql) {
            int ret = 0;
            MySqlCommand cmd = new MySqlCommand();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                ret = cmd.ExecuteNonQuery();
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + sql);
                throw new Exception(ex.Message, ex);
            } finally {
                cmd.Dispose();
                cmd = null;

                if (transaction == null) {
                    FreeConnection(connection);
                }
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public int ExecuteNonQuery(string sql, Dictionary<string, object> p) {
            int ret = 0;
            MySqlCommand cmd = new MySqlCommand();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    MySqlParameter param = new MySqlParameter();
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
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public object ExecuteScalar(string sql) {
            object ret = null;
            MySqlCommand cmd = new MySqlCommand();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
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
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public object ExecuteScalar(string sql, Dictionary<string, object> p) {
            object ret = null;
            MySqlCommand cmd = new MySqlCommand();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    MySqlParameter param = new MySqlParameter();
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
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                _logger.LogWarning($"执行SQL超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), SQL:{sql}, Mileseconds:{elapsedTicks / 10000}");
            }

            return ret;
        }

        public DataTable ExecuteDataTable(string sql) {
            DataTable ret = new DataTable();
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter da = new MySqlDataAdapter();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
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
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > 10000000) {
                _logger.LogWarning($"执行SQL超过预定伐值:1(秒), SQL:{sql}, Seconds:{elapsedTicks / 10000000}");
            }

            return ret;
        }

        public DataTable ExecuteDataTable(string sql, Dictionary<string, object> p) {
            DataTable ret = new DataTable();
            MySqlCommand cmd = new MySqlCommand();
            MySqlDataAdapter da = new MySqlDataAdapter();

            MySqlTransaction transaction = null;
            MySqlConnection connection = CallContext.GetData(MYSQL_CONNECTION) as MySqlConnection;
            if (connection == null) {
                connection = GetConnection();
            } else {
                transaction = CallContext.GetData(MYSQL_TRANSACTION) as MySqlTransaction;
            }

            long startTicks = DateTime.Now.Ticks;

            try {
                cmd.Connection = connection;
                cmd.Transaction = transaction;
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = sql;
                cmd.CommandTimeout = COMMAND_TIMEOUT;

                foreach (KeyValuePair<string, object> kvp in p) {
                    MySqlParameter param = new MySqlParameter();
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

        private MySqlConnection GetConnection() {
            long startTicks = DateTime.Now.Ticks;

            bool ok = _queue.TryDequeue(out MySqlConnection connection);
            if (!ok) {
                connection = new MySqlConnection(_connectionString);
            }

            try {
                connection.Open();
            } catch (Exception ex) {
                _logger.LogError(ex.ToString() + "|" + _connectionString);
                throw new Exception(ex.Message, ex);
            }

            long elapsedTicks = DateTime.Now.Ticks - startTicks;
            if (elapsedTicks > ALARM_THRESHOLD_VALUE) {
                int indexdatabase = _connectionString.IndexOf("database");
                string logConnStr = indexdatabase > -1 ? _connectionString.Substring(indexdatabase, 30) : "[no database]";  // 避免暴露DB
                _logger.LogWarning($"打开连接超过预定伐值:{ALARM_THRESHOLD_VALUE / 10000}(毫秒), Queue Count:{_queue.Count}, ConnectionString:{logConnStr}, Mileseconds:{elapsedTicks / 10000}");
            }

            return connection;
        }

        private void FreeConnection(MySqlConnection connection) {
            connection.Close();

            if (_queue.Count < 1000) {
                _queue.Enqueue(connection);
            } else {
                connection = null;
            }
        }
    }
}
