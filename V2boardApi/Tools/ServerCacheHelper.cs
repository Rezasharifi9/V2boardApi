using DataLayer.DomainModel;
using System;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Caching;

namespace V2boardApi.Tools
{
    /// <summary>
    /// سرور پیش‌فرض پنل را در HttpRuntime.Cache نگه می‌دارد.
    /// اگر کش خالی باشد (ریستارت، recycle، scavenger) از دیتابیس دوباره لود می‌کند.
    /// </summary>
    public static class ServerCacheHelper
    {
        public const string CacheKey = "Server";
        private static readonly object Sync = new object();

        public static tbServers Get()
        {
            var server = HttpRuntime.Cache[CacheKey] as tbServers;
            if (server != null)
                return server;

            lock (Sync)
            {
                server = HttpRuntime.Cache[CacheKey] as tbServers;
                if (server != null)
                    return server;

                server = LoadFromDatabase();
                if (server != null)
                    Set(server);

                return server;
            }
        }

        public static void Set(tbServers server)
        {
            if (server == null)
                return;

            HttpRuntime.Cache.Insert(
                CacheKey,
                server,
                null,
                Cache.NoAbsoluteExpiration,
                Cache.NoSlidingExpiration,
                CacheItemPriority.NotRemovable,
                null);
        }

        public static void Refresh()
        {
            lock (Sync)
            {
                HttpRuntime.Cache.Remove(CacheKey);
                var server = LoadFromDatabase();
                if (server != null)
                    Set(server);
            }
        }

        private static tbServers LoadFromDatabase()
        {
            try
            {
                using (var db = new Entities())
                {
                    var withBot = db.tbUsers
                        .Include(u => u.tbServers)
                        .Include(u => u.tbBotSettings)
                        .FirstOrDefault(p => p.tbBotSettings.Any(s => s.Bot_Token != null));

                    if (withBot?.tbServers != null)
                        return withBot.tbServers;

                    var anyUser = db.tbUsers
                        .Include(u => u.tbServers)
                        .FirstOrDefault(u => u.FK_Server_ID != null);

                    if (anyUser?.tbServers != null)
                        return anyUser.tbServers;

                    return db.tbServers.FirstOrDefault();
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
