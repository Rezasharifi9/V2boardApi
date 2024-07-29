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
        private LinkedList<MySqlConnection> connectionPool = new LinkedList<MySqlConnection>();
        public MySqlConnection MySqlConnection { get; private set; }

        public MySqlEntities2(string connectionString)
        {
            _connection = connectionString;
            MySqlConnection = new MySqlConnection(_connection);
        }

        public async Task OpenAsync()
        {
            await MySqlConnection.OpenAsync();
            connectionPool.AddLast(MySqlConnection);
        }

        public async Task<MySqlDataReader> GetDataAsync(string query)
        {
            using (var sqlCommand = new MySqlCommand(query, MySqlConnection))
            {
                var reader = await sqlCommand.ExecuteReaderAsync();
                return (MySqlDataReader)reader;
            }
        }

        public void Close()
        {
            if (MySqlConnection.State == System.Data.ConnectionState.Open)
            {
                MySqlConnection.Close();
            }
            MySqlConnection.ClearPool(MySqlConnection);
            MySqlConnection.Dispose();
        }

        public async Task CloseAysnc()
        {

            if (MySqlConnection.State == System.Data.ConnectionState.Open)
            {
                await MySqlConnection.CloseAsync();
                MySqlConnection.ClearPool(MySqlConnection);
            }

            connectionPool.Remove(MySqlConnection);

        }

        public void Dispose()
        {
            MySqlConnection.Dispose();
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
