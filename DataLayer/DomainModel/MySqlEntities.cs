using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
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
            using(MySqlCommand sqlCommand = new MySqlCommand(query, SqlConnection))
            {
                MySqlDataReader reader = sqlCommand.ExecuteReader();
                return reader;
            }
        }

        public  void Close()
        {
            SqlConnection.Close();
        }
    }

    //public IHttpActionResult ConnectSql()
    //{
    //    List<UserMySqlModel> sql = new List<UserMySqlModel>();
    //    var conn = ConfigurationManager.ConnectionStrings["mysql"].ConnectionString;
    //    MySqlConnection mysql = new MySqlConnection(conn);
    //    string query = "SELECT * FROM v2_user ORDER BY password ASC";
    //    MySqlCommand sqlCommand = new MySqlCommand(query);
    //    sqlCommand.Connection = mysql;
    //    mysql.Open();
    //    MySqlDataReader reader = sqlCommand.ExecuteReader();
    //    while (reader.Read())
    //    {
    //        sql.Add(new UserMySqlModel()
    //        {
    //            id = reader.GetInt32("id"),
    //            email = reader.GetString("email"),
    //            password = reader.GetString("password")
    //        });
    //    }
    //    mysql.Close();



    //    return Ok();


    //}
}
