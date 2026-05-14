using MySqlConnector; // نام فضای نام تغییری نکرده یا به این تغییر یافته
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace DataLayer.DomainModel
{
    public class MySqlEntities : IDisposable
    {
        private readonly string _connectionString;
        public MySqlConnection MySqlConnection { get; private set; }
        private bool _disposed = false;

        public MySqlEntities(string connectionString)
        {
            _connectionString = connectionString;
            // در کتابخانه مدرن، کانکشن همینجا ساخته می‌شود
            MySqlConnection = new MySqlConnection(_connectionString);
        }

        public async Task OpenAsync()
        {
            if (MySqlConnection.State != ConnectionState.Open)
            {
                await MySqlConnection.OpenAsync().ConfigureAwait(false);
            }
        }

        public async Task<MySqlDataReader> GetDataAsync(string query, Dictionary<string, object> parameters = null)
        {
            // اطمینان از باز بودن کانکشن قبل از اجرای دستور
            await OpenAsync();

            var sqlCommand = new MySqlCommand(query, MySqlConnection);

            if (parameters != null)
            {
                foreach (var param in parameters)
                {
                    sqlCommand.Parameters.AddWithValue(param.Key, param.Value);
                }
            }

            // CommandBehavior.CloseConnection باعث می‌شود وقتی Reader بسته شد، کانکشن هم بسته شود
            // اما چون شما از یک کلاس سراسری استفاده می‌کنید، فعلاً معمولی برمی‌گردانیم
            return await sqlCommand.ExecuteReaderAsync().ConfigureAwait(false);
        }

        // متد دوم را با استفاده از متد اول خلاصه می‌کنیم (DRY Principle)
        public async Task<MySqlDataReader> GetDataAsync(string query)
        {
            return await GetDataAsync(query, null);
        }

        public async Task CloseAsync()
        {
            if (MySqlConnection != null)
            {
                await MySqlConnection.CloseAsync().ConfigureAwait(false);
            }
            Dispose();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (MySqlConnection != null)
                    {
                        MySqlConnection.Dispose();
                        MySqlConnection = null;
                    }
                }
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}