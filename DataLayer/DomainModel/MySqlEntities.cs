using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace DataLayer.DomainModel
{
    public class MySqlEntities : IDisposable
    {
        private readonly string _connection;
        private LinkedList<MySqlConnection> connectionPool = new LinkedList<MySqlConnection>();
        public MySqlConnection MySqlConnection { get; private set; }
        private bool disposed = false; // برای پیگیری وضعیت Dispose

        public MySqlEntities(string connectionString)
        {
            _connection = connectionString;
            MySqlConnection = new MySqlConnection(_connection);
        }

        public async Task OpenAsync()
        {
            await MySqlConnection.OpenAsync().ConfigureAwait(false);
            connectionPool.AddLast(MySqlConnection);
        }

        public async Task<MySqlDataReader> GetDataAsync(string query, Dictionary<string, object> parameters)
        {
            using (var sqlCommand = new MySqlCommand(query, MySqlConnection))
            {
                // اضافه کردن پارامترها به پرس‌وجو
                foreach (var param in parameters)
                {
                    sqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }

                var reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
                return (MySqlDataReader)reader;
            }
        }
        public async Task<MySqlDataReader> GetDataAsync(string query)
        {
            using (var sqlCommand = new MySqlCommand(query, MySqlConnection))
            {
                var reader = await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
                return (MySqlDataReader)reader;
            }
        }

        public async Task CloseAsync()
        {
            if (MySqlConnection != null && MySqlConnection.State == System.Data.ConnectionState.Open)
            {
                await MySqlConnection.CloseAsync().ConfigureAwait(false);
            }

            if (connectionPool.Count > 0)
            {
                connectionPool.Remove(MySqlConnection);
                await MySqlConnection.ClearPoolAsync(MySqlConnection).ConfigureAwait(false);
            }

            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // آزادسازی منابع مدیریتی
                    if (MySqlConnection != null)
                    {
                        if (MySqlConnection.State == System.Data.ConnectionState.Open)
                        {
                            MySqlConnection.CloseAsync().ConfigureAwait(false);
                        }
                        MySqlConnection.Dispose();
                        MySqlConnection = null;
                    }

                    if (connectionPool != null)
                    {
                        foreach (var connection in connectionPool)
                        {
                            connection.Dispose();
                        }
                        connectionPool.Clear();
                        connectionPool = null;
                    }
                }

                // آزادسازی منابع غیرمدیریتی (در صورت وجود)
                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
