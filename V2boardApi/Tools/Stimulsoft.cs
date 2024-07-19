using Stimulsoft.Report;
using Stimulsoft.Report.Dictionary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Web;

namespace V2boardApi.Tools
{
    public static class StiTools
    {
        public static  StiReport Load(string Path)
        {
            // بارگذاری داشبورد
            StiReport report = StiReport.CreateNewDashboard();
            report.Load(Path);

            // خواندن ConnectionString از Web.config
            string connectionString = ConfigurationManager.ConnectionStrings["StiConnection"].ConnectionString;

            // تغییر ConnectionString
            foreach (var dataSource in report.Dictionary.Databases)
            {
                if (dataSource is StiSqlDatabase sqlDataSource)
                {
                    sqlDataSource.ConnectionString = connectionString;
                }
            }
            return report;
        }
    }
}