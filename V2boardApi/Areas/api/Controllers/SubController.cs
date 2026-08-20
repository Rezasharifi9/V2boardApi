using DataLayer.DomainModel;
using MySqlConnector;
using Newtonsoft.Json;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using System.Web.SessionState;
using V2boardApi.Areas.api.Data.ApiModels;
using V2boardApi.Tools;

namespace V2boardApi.Areas.api.Controllers
{
    /// <summary>
    /// سرویس سبک اطلاعات اشتراک بر اساس توکن لینک ساب
    /// GET /api/v1/Sub/Info?token=xxxxx
    /// GET /api/v1/Sub/Usage?token=xxxxx
    /// POST /api/v1/Sub/ResetLink?token=xxxxx
    /// عمداً بدون EntityFramework و Repository نوشته شده تا هزینه هر فراخوانی حداقل باشد.
    /// </summary>
    [SessionState(SessionStateBehavior.Disabled)]
    public class SubController : Controller
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        private const string SelectQuery =
            "SELECT email,u,d,transfer_enable,expired_at FROM v2_user WHERE token=@token LIMIT 1";

        private const string SelectEmailQuery =
            "SELECT email FROM v2_user WHERE token=@token LIMIT 1";

        private const string SelectIdEmailQuery =
            "SELECT id,email FROM v2_user WHERE token=@token LIMIT 1";

        private const string SelectUsageQuery =
            "SELECT d,u,updated_at FROM v2_stat_user WHERE user_id=@userId AND updated_at>=@startUnix";

        private const string UpdateTokenQuery =
            "update v2_user set token=@newToken,uuid=@Guid where token=@oldToken";

        private const string CacheKeyPrefix = "SubInfo:";
        private const string AgentCacheKeyPrefix = "SubAgent:";
        private const string UsageCacheKeyPrefix = "SubUsage:";

        /// <summary>مدت کش پاسخ (ثانیه). صفر یعنی بدون کش.</summary>
        private const int CacheSeconds = 10;

        /// <summary>مدت کش پاسخ تشخیص نماینده (ثانیه). صفر یعنی بدون کش.</summary>
        private const int AgentCacheSeconds = 60;

        /// <summary>مدت کش پاسخ تاریخچه مصرف (ثانیه). صفر یعنی بدون کش.</summary>
        private const int UsageCacheSeconds = 60;

        private static readonly char[] NameSeparators = { '$', '@' };

        private const string BadTokenJson = "{\"success\":false,\"message\":\"پارامتر token ارسال نشده است\"}";
        private const string NotFoundJson = "{\"success\":false,\"message\":\"اشتراک یافت نشد\"}";
        private const string NoServerJson = "{\"success\":false,\"message\":\"سرور پیکربندی نشده است\"}";
        private const string ErrorJson = "{\"success\":false,\"message\":\"خطای داخلی\"}";
        private const string NoAgentJson = "{\"success\":false,\"message\":\"نماینده این اشتراک یافت نشد\"}";
        private const string DisabledAgentJson = "{\"success\":false,\"message\":\"نماینده این اشتراک غیرفعال است\"}";
        private const string NoAgentTokenJson = "{\"success\":false,\"message\":\"برای نماینده این اشتراک توکن ثبت نشده است\"}";

        [HttpGet]
        public async Task<ActionResult> Info(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
                return JsonResponse(BadTokenJson, 400);

            var cacheKey = CacheKeyPrefix + token;
            if (CacheSeconds > 0)
            {
                var cached = HttpRuntime.Cache[cacheKey] as string;
                if (cached != null)
                    return JsonResponse(cached, 200);
            }

            try
            {
                var server = ServerCacheHelper.Get();
                if (server == null)
                    return JsonResponse(NoServerJson, 503);

                SubscriptionTokenInfoModel model = null;
                using (var sql = new MySqlEntities(server.ConnectionString))
                {
                    await sql.OpenAsync();

                    var parameters = new Dictionary<string, object>(1) { { "@token", token } };
                    using (var reader = await sql.GetDataAsync(SelectQuery, parameters))
                    {
                        if (await reader.ReadAsync())
                            model = Map(reader);
                    }
                }

                if (model == null)
                    return JsonResponse(NotFoundJson, 404);

                var json = JsonConvert.SerializeObject(model);

                if (CacheSeconds > 0)
                {
                    HttpRuntime.Cache.Insert(
                        cacheKey,
                        json,
                        null,
                        DateTime.Now.AddSeconds(CacheSeconds),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Low,
                        null);
                }

                return JsonResponse(json, 200);
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message + "|" + ex.StackTrace, ex);
                return JsonResponse(ErrorJson, 500);
            }
        }

        /// <summary>
        /// تشخیص نماینده صاحب یک اشتراک از روی توکن لینک ساب
        /// GET /api/v1/Sub/Agent?token=xxxxx
        /// نام اشتراک از MySQL خوانده و با @ جدا می شود ؛ بخش دوم نام کاربری نماینده است.
        /// خروجی توکن نماینده را برمی گرداند تا کلاینت با آن GetAgentPlans و CreateAgentInvoice را صدا بزند.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Agent(string token)
        {
            token = ReadSubToken(token);
            if (token == null)
                return JsonResponse(BadTokenJson, 400);

            var cacheKey = AgentCacheKeyPrefix + token;
            if (AgentCacheSeconds > 0)
            {
                var cached = HttpRuntime.Cache[cacheKey] as string;
                if (cached != null)
                    return JsonResponse(cached, 200);
            }

            try
            {
                var server = ServerCacheHelper.Get();
                if (server == null)
                    return JsonResponse(NoServerJson, 503);

                string email = null;
                using (var sql = new MySqlEntities(server.ConnectionString))
                {
                    await sql.OpenAsync();

                    var parameters = new Dictionary<string, object>(1) { { "@token", token } };
                    using (var reader = await sql.GetDataAsync(SelectEmailQuery, parameters))
                    {
                        if (await reader.ReadAsync())
                            email = reader.IsDBNull(0) ? null : reader.GetString(0);
                    }
                }

                if (string.IsNullOrWhiteSpace(email))
                    return JsonResponse(NotFoundJson, 404);

                // ساختار نام اشتراک : name$random@AgentUsername
                var parts = email.Split('@');
                if (parts.Length < 2 || string.IsNullOrWhiteSpace(parts[1]))
                {
                    logger.Warn("نام اشتراک " + email + " بخش نماینده ندارد");
                    return JsonResponse(NoAgentJson, 404);
                }

                var agentUsername = parts[1].Trim();

                var cut = email.IndexOfAny(NameSeparators);
                var subscriptionName = cut >= 0 ? email.Substring(0, cut) : email;

                SubscriptionAgentModel model;
                using (var db = new Entities())
                {
                    var agent = await db.tbUsers
                        .AsNoTracking()
                        .Where(p => p.Username == agentUsername)
                        .Select(p => new
                        {
                            p.Username,
                            p.BussinesTitle,
                            p.Token,
                            p.Status,
                            BotId = p.tbBotSettings.Select(s => s.Bot_ID).FirstOrDefault(),
                            SupportId = p.tbBotSettings.Select(s => s.AdminUsername).FirstOrDefault(),
                            NotActiveSell = p.tbBotSettings.Select(s => (bool?)s.IsNotActiveSell).FirstOrDefault()
                        })
                        .FirstOrDefaultAsync();

                    if (agent == null)
                        return JsonResponse(NoAgentJson, 404);

                    // فقط غیرفعال سازی صریح مانع می شود ؛ Status تهی یعنی تعیین نشده
                    if (agent.Status == false)
                        return JsonResponse(DisabledAgentJson, 403);

                    if (string.IsNullOrWhiteSpace(agent.Token))
                        return JsonResponse(NoAgentTokenJson, 503);

                    model = new SubscriptionAgentModel
                    {
                        Success = true,
                        SubscriptionName = subscriptionName,
                        AgentUsername = agent.Username,
                        BusinessTitle = agent.BussinesTitle,
                        AgentToken = agent.Token,
                        BotUsername = TrimAt(agent.BotId),
                        SupportUsername = TrimAt(agent.SupportId),
                        SellEnabled = agent.NotActiveSell != true
                    };
                }

                var json = JsonConvert.SerializeObject(model);

                if (AgentCacheSeconds > 0)
                {
                    HttpRuntime.Cache.Insert(
                        cacheKey,
                        json,
                        null,
                        DateTime.Now.AddSeconds(AgentCacheSeconds),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Low,
                        null);
                }

                return JsonResponse(json, 200);
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message + "|" + ex.StackTrace, ex);
                return JsonResponse(ErrorJson, 500);
            }
        }

        /// <summary>
        /// تاریخچه مصرف ۳۰ روز گذشته یک اشتراک از روی توکن لینک ساب
        /// GET /api/v1/Sub/Usage?token=xxxxx
        /// داده از جدول v2_stat_user خوانده و به تفکیک روز جمع می‌شود.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> Usage(string token)
        {
            token = ReadSubToken(token);
            if (token == null)
                return JsonResponse(BadTokenJson, 400);

            var cacheKey = UsageCacheKeyPrefix + token;
            if (UsageCacheSeconds > 0)
            {
                var cached = HttpRuntime.Cache[cacheKey] as string;
                if (cached != null)
                    return JsonResponse(cached, 200);
            }

            try
            {
                var server = ServerCacheHelper.Get();
                if (server == null)
                    return JsonResponse(NoServerJson, 503);

                var today = DateTime.Now.Date;
                var start = today.AddDays(-30);
                var startUnix = (long)Utility.ConvertDatetimeToSecond(start);

                SubscriptionUsageHistoryModel model;
                using (var sql = new MySqlEntities(server.ConnectionString))
                {
                    await sql.OpenAsync();

                    long userId = 0;
                    string email = null;
                    var tokenParams = new Dictionary<string, object>(1) { { "@token", token } };
                    using (var reader = await sql.GetDataAsync(SelectIdEmailQuery, tokenParams))
                    {
                        if (await reader.ReadAsync())
                        {
                            userId = Convert.ToInt64(reader.GetValue(0));
                            email = reader.IsDBNull(1) ? null : reader.GetString(1);
                        }
                    }

                    if (userId == 0 || string.IsNullOrWhiteSpace(email))
                        return JsonResponse(NotFoundJson, 404);

                    var cut = email.IndexOfAny(NameSeparators);
                    var name = cut >= 0 ? email.Substring(0, cut) : email;

                    var dayGroups = new Dictionary<DateTime, Tuple<long, long>>();
                    var usageParams = new Dictionary<string, object>(2)
                    {
                        { "@userId", userId },
                        { "@startUnix", startUnix }
                    };
                    using (var reader = await sql.GetDataAsync(SelectUsageQuery, usageParams))
                    {
                        while (await reader.ReadAsync())
                        {
                            var download = ToInt64(reader, 0);
                            var upload = ToInt64(reader, 1);
                            var day = Utility.ConvertSecondToDatetime(ToInt64(reader, 2)).Date;

                            Tuple<long, long> current;
                            if (!dayGroups.TryGetValue(day, out current))
                                current = Tuple.Create(0L, 0L);

                            dayGroups[day] = Tuple.Create(current.Item1 + download, current.Item2 + upload);
                        }
                    }

                    model = MapUsage(name, start, today, dayGroups);
                }

                var json = JsonConvert.SerializeObject(model);

                if (UsageCacheSeconds > 0)
                {
                    HttpRuntime.Cache.Insert(
                        cacheKey,
                        json,
                        null,
                        DateTime.Now.AddSeconds(UsageCacheSeconds),
                        Cache.NoSlidingExpiration,
                        CacheItemPriority.Low,
                        null);
                }

                return JsonResponse(json, 200);
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message + "|" + ex.StackTrace, ex);
                return JsonResponse(ErrorJson, 500);
            }
        }

        /// <summary>
        /// تغییر لینک اشتراک — معادل گزینه «تغییر لینک» در ربات
        /// POST /api/v1/Sub/ResetLink?token=xxxxx
        /// توکن و UUID در v2_user عوض می‌شود؛ لینک قبلی قطع است.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> ResetLink(string token)
        {
            token = ReadSubToken(token);
            if (token == null)
                return JsonResponse(BadTokenJson, 400);

            try
            {
                var server = ServerCacheHelper.Get();
                if (server == null)
                    return JsonResponse(NoServerJson, 503);

                string email = null;
                var newToken = Guid.NewGuid().ToString().Split('-')[0]
                    + Guid.NewGuid().ToString().Split('-')[1]
                    + Guid.NewGuid().ToString().Split('-')[2];

                using (var sql = new MySqlEntities(server.ConnectionString))
                {
                    await sql.OpenAsync();

                    var findParams = new Dictionary<string, object>(1) { { "@token", token } };
                    using (var reader = await sql.GetDataAsync(SelectEmailQuery, findParams))
                    {
                        if (await reader.ReadAsync())
                            email = reader.IsDBNull(0) ? null : reader.GetString(0);
                    }

                    if (string.IsNullOrWhiteSpace(email))
                        return JsonResponse(NotFoundJson, 404);

                    var updateParams = new Dictionary<string, object>(3)
                    {
                        { "@newToken", newToken },
                        { "@Guid", Guid.NewGuid() },
                        { "@oldToken", token }
                    };
                    using (var reader = await sql.GetDataAsync(UpdateTokenQuery, updateParams))
                    {
                    }
                }

                using (var db = new Entities())
                {
                    var link = await db.tbLinks.FirstOrDefaultAsync(p => p.tbL_Token == token);
                    if (link == null)
                        link = await db.tbLinks.FirstOrDefaultAsync(p => p.tbL_Email == email);
                    if (link != null)
                        link.tbL_Token = newToken;

                    var logs = await db.tbLogs.Where(s => s.SubToken == token).ToListAsync();
                    foreach (var item in logs)
                        item.SubToken = newToken;

                    await db.SaveChangesAsync();
                }

                RemoveSubCaches(token);

                var model = new SubscriptionResetLinkModel
                {
                    Success = true,
                    Token = newToken
                };

                if (!string.IsNullOrWhiteSpace(server.SubAddress))
                    model.SubscriptionLink = "https://" + server.SubAddress + "/api/v1/client/subscribe?token=" + newToken;

                if (!string.IsNullOrWhiteSpace(server.BackupSubAddr))
                    model.BackupSubscriptionLink = "https://" + server.BackupSubAddr + "/api/v1/client/subscribe?token=" + newToken;

                return JsonResponse(JsonConvert.SerializeObject(model), 200);
            }
            catch (Exception ex)
            {
                logger.Warn(ex.Message + "|" + ex.StackTrace, ex);
                return JsonResponse(ErrorJson, 500);
            }
        }

        /// <summary>حذف @ ابتدای آیدی تلگرام تا کلاینت خودش تصمیم بگیرد چطور نمایش دهد</summary>
        private static string TrimAt(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            return value.Trim().TrimStart('@');
        }

        /// <summary>
        /// خواندن توکن اشتراک از کوئری استرینگ و در نبود آن از هدر Authorization (با یا بدون پیشوند Bearer)
        /// </summary>
        private string ReadSubToken(string token)
        {
            if (!string.IsNullOrWhiteSpace(token))
                return token.Trim();

            var header = Request.Headers["Authorization"];
            if (string.IsNullOrWhiteSpace(header))
                return null;

            if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                header = header.Substring("Bearer ".Length);

            header = header.Trim();
            return header.Length == 0 ? null : header;
        }

        private static void RemoveSubCaches(string token)
        {
            HttpRuntime.Cache.Remove(CacheKeyPrefix + token);
            HttpRuntime.Cache.Remove(AgentCacheKeyPrefix + token);
            HttpRuntime.Cache.Remove(UsageCacheKeyPrefix + token);
        }

        private static SubscriptionTokenInfoModel Map(MySqlDataReader reader)
        {
            // ترتیب ستون‌ها مطابق SelectQuery است تا هزینه GetOrdinal حذف شود
            var email = reader.GetString(0);
            var cut = email.IndexOfAny(NameSeparators);
            var name = cut >= 0 ? email.Substring(0, cut) : email;

            var usedBytes = ToDouble(reader, 1) + ToDouble(reader, 2);
            var totalBytes = ToDouble(reader, 3);

            var model = new SubscriptionTokenInfoModel
            {
                Success = true,
                Name = name,
                TotalVolumeGb = Math.Round(Utility.ConvertByteToGB(totalBytes), 2),
                UsedVolumeGb = Math.Round(Utility.ConvertByteToGB(usedBytes), 2),
                RemainingDays = -1,
                ExpireDate = null
            };

            if (!reader.IsDBNull(4))
            {
                var expireSeconds = Convert.ToInt64(reader.GetValue(4));
                if (expireSeconds > 0)
                {
                    var expire = Utility.ConvertSecondToDatetime(expireSeconds);
                    model.RemainingDays = Utility.CalculateLeftDayes(expire);
                    model.ExpireDate = Utility.ConvertDateTimeToShamsi5(expire);
                }
            }

            return model;
        }

        private static SubscriptionUsageHistoryModel MapUsage(
            string name,
            DateTime start,
            DateTime today,
            Dictionary<DateTime, Tuple<long, long>> dayGroups)
        {
            long totalDownloadBytes = 0;
            long totalUploadBytes = 0;
            foreach (var group in dayGroups.Values)
            {
                totalDownloadBytes += group.Item1;
                totalUploadBytes += group.Item2;
            }

            var totalDownloadGb = Math.Round(Utility.ConvertByteToGB(totalDownloadBytes), 2, MidpointRounding.AwayFromZero);
            var totalUploadGb = Math.Round(Utility.ConvertByteToGB(totalUploadBytes), 2, MidpointRounding.AwayFromZero);

            var items = new List<SubscriptionUsageDayModel>(dayGroups.Count);
            foreach (var g in dayGroups.OrderByDescending(x => x.Key))
            {
                var downloadGb = Math.Round(Utility.ConvertByteToGB(g.Value.Item1), 2, MidpointRounding.AwayFromZero);
                var uploadGb = Math.Round(Utility.ConvertByteToGB(g.Value.Item2), 2, MidpointRounding.AwayFromZero);
                items.Add(new SubscriptionUsageDayModel
                {
                    Date = Utility.ConvertDateTimeToShamsi5(g.Key),
                    DownloadGb = downloadGb,
                    UploadGb = uploadGb,
                    TotalGb = Math.Round(downloadGb + uploadGb, 2, MidpointRounding.AwayFromZero)
                });
            }

            return new SubscriptionUsageHistoryModel
            {
                Success = true,
                Name = name,
                FromDate = Utility.ConvertDateTimeToShamsi5(start),
                ToDate = Utility.ConvertDateTimeToShamsi5(today),
                TotalDownloadGb = totalDownloadGb,
                TotalUploadGb = totalUploadGb,
                TotalGb = Math.Round(totalDownloadGb + totalUploadGb, 2, MidpointRounding.AwayFromZero),
                Items = items
            };
        }

        private static double ToDouble(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0d : Convert.ToDouble(reader.GetValue(ordinal));
        }

        private static long ToInt64(MySqlDataReader reader, int ordinal)
        {
            return reader.IsDBNull(ordinal) ? 0L : Convert.ToInt64(reader.GetValue(ordinal));
        }

        private ActionResult JsonResponse(string json, int statusCode)
        {
            Response.StatusCode = statusCode;
            Response.TrySkipIisCustomErrors = true;
            Response.AppendHeader("Access-Control-Allow-Origin", "*");
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            return Content(json, "application/json", Encoding.UTF8);
        }
    }
}
