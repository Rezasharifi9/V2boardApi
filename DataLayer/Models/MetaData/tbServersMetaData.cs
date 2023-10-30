using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DataLayer.DomainModel
{
    public class tbServersMetaData
    {
        public string ServerAddress { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string DataBaseName { get; set; }

    }

    [MetadataType(typeof(tbServersMetaData))]
    public partial class tbServers
    {
        public string ConnectionString { get { return "Server=" + ServerAddress + ";User ID=" + Username + ";Password=" + Password + ";Database=" + DataBaseName + ""; } }
    }
}
