using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DomainModel
{
    public class MySqlEntities
    {
        private string _connection;
        private MySqlConnection SqlConnection;
        private MySqlDataReader SqlDataReader;

        public MySqlEntities(string ConnectionString)
        {
            try
            {
                _connection = ConnectionString;
                SqlConnection = new MySqlConnection(_connection);

            }
            catch (Exception)
            {

            }
        }

        public void Open()
        {
            SqlConnection.Open();

        }

        public MySqlDataReader GetData(string query)
        {
            using (MySqlCommand sqlCommand = new MySqlCommand(query, SqlConnection))
            {
                SqlDataReader = sqlCommand.ExecuteReader();
                return SqlDataReader;
            }
        }

        public void Close()
        {
            SqlConnection.Close();
            MySqlConnection.ClearPool(SqlConnection);
            SqlConnection.Dispose();
        }
    }

    public class MySqlEntities2 : IDisposable
    {
        private readonly string _connection;
        public MySqlConnection SqlConnection { get; private set; }

        public MySqlEntities2(string connectionString)
        {
            _connection = connectionString;
            SqlConnection = new MySqlConnection(_connection);
        }

        public async Task OpenAsync()
        {
            await SqlConnection.OpenAsync();
        }

        public async Task<MySqlDataReader> GetDataAsync(string query)
        {
            using (var sqlCommand = new MySqlCommand(query, SqlConnection))
            {
                var reader = await sqlCommand.ExecuteReaderAsync();
                return (MySqlDataReader)reader;
            }
        }

        public void Close()
        {
            SqlConnection.Close();
            MySqlConnection.ClearPool(SqlConnection);
            SqlConnection.Dispose();
        }

        public async Task CloseAysnc()
        {
            await SqlConnection.CloseAsync();
            MySqlConnection.ClearPool(SqlConnection);
            await SqlConnection.DisposeAsync();
        }

        public void Dispose()
        {
            SqlConnection?.Dispose();
        }
    }

    public sealed class MySqlDatabase
    {
        private static readonly Lazy<MySqlDatabase> lazy = new Lazy<MySqlDatabase>(() => new MySqlDatabase());
        public static MySqlDatabase Instance => lazy.Value;

        private string _connectionString;
        private MySqlConnection _connection;

        private MySqlDatabase()
        {
        }

        public void Initialize(string connectionString)
        {
            _connectionString = connectionString;
            _connection = new MySqlConnection(_connectionString);
        }

        public MySqlConnection Connection
        {
            get
            {
                if (_connection.State == System.Data.ConnectionState.Closed)
                {
                    _connection.Open();
                }
                return _connection;
            }
        }

        public void CloseConnection()
        {
            if (_connection.State != System.Data.ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public async Task<MySqlDataReader> ExecuteQueryAsync(string query)
        {
            using (var sqlCommand = new MySqlCommand(query, Connection))
            {
                return (MySqlDataReader)await sqlCommand.ExecuteReaderAsync();
            }
        }
    }


}
