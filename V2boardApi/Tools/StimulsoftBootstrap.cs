using NLog;
using Stimulsoft.Base;
using Stimulsoft.Report;
using System;
using System.IO;
using System.Web.Hosting;

namespace V2boardApi.Tools
{
    public static class StimulsoftBootstrap
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
        private static readonly object Sync = new object();
        private static bool _initialized;

        public static void Initialize()
        {
            if (_initialized)
                return;

            lock (Sync)
            {
                if (_initialized)
                    return;

                var stimulsoftDir = MapPath("~/App_Data/Stimulsoft");
                Directory.CreateDirectory(stimulsoftDir);

                StiConfig.ApplicationDirectory = stimulsoftDir;
                StiOptions.Configuration.DefaultReportConfigPath =
                    Path.Combine(stimulsoftDir, "Stimulsoft.Report.config");
                StiOptions.Configuration.DefaultReportSettingsPath =
                    Path.Combine(stimulsoftDir, "Stimulsoft.Report.settings");

                var licensePath = MapPath("~/Key/license.key");
                if (File.Exists(licensePath))
                {
                    StiLicense.LoadFromFile(licensePath);
                    Logger.Info("Stimulsoft license loaded from {0}", licensePath);
                }
                else
                {
                    Logger.Warn("Stimulsoft license file not found at {0}", licensePath);
                }

                _initialized = true;
            }
        }

        public static void EnsureInitialized()
        {
            Initialize();
        }

        private static string MapPath(string virtualPath)
        {
            if (HostingEnvironment.IsHosted)
                return HostingEnvironment.MapPath(virtualPath);

            var relative = virtualPath.TrimStart('~', '/').Replace('/', Path.DirectorySeparatorChar);
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, relative);
        }
    }
}
