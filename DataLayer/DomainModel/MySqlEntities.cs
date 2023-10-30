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
    public static class MySqlEntities
    {
        private static string _connection;
        private static MySqlConnection SqlConnection;
        public static bool Connect(string ConnectionString)
        {
            try
            {
                _connection = ConnectionString;
                SqlConnection = new MySqlConnection(_connection);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public static List<Dictionary<string, object>> GetData(string query)
        {
            var ListData = new List<Dictionary<string, object>>();
            MySqlCommand sqlCommand = new MySqlCommand(query);
            sqlCommand.Connection = SqlConnection;
            SqlConnection.Open();
            MySqlDataReader reader = sqlCommand.ExecuteReader();

            
            while (reader.Read())
            {
                var CountCol = reader.FieldCount;
                if(reader.HasRows)
                {
                    var dic = new Dictionary<string, object>();
                    for(var i = 0; i < CountCol; i++)
                    {
                        var val = reader.GetValue(i);
                        var name = reader.GetName(i);
                        dic.Add(name, val);
                    }
                    ListData.Add(dic);

                }
               
            }


            return ListData;
        }

        public static void Close()
        {
            SqlConnection.Close();
        }

        public static Type GetListType<T>(this List<T> _)
        {
            return typeof(T);
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
