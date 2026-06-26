using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using DataLayer.DomainModel;
using DataLayer.Repository;
using V2boardApi.Areas.App.Data.Changelog;

namespace V2boardApi.Tools
{
    public static class PanelChangelogService
    {
        private const byte AdminAudience = 1;
        private const byte AgentAudience = 2;

        public static void InvalidateVersionCache()
        {
            HttpRuntime.Cache.Remove("PanelCurrentVersion");
        }

        public static string GetCurrentVersion()        {
            var cached = HttpRuntime.Cache["PanelCurrentVersion"] as string;
            if (!string.IsNullOrEmpty(cached))
                return cached;

            try
            {
                using (var db = new Entities())
                {
                    var versionRepository = new Repository<tbPanelChangelogVersions>(db);
                    var current = versionRepository
                        .Where(v => v.tbPclv_IsActive && v.tbPclv_IsCurrent)
                        .OrderByDescending(v => v.tbPclv_SortOrder)
                        .FirstOrDefault();

                    string version;
                    if (current != null)
                        version = current.tbPclv_Version;
                    else
                    {
                        var latest = versionRepository
                            .Where(v => v.tbPclv_IsActive)
                            .OrderByDescending(v => v.tbPclv_SortOrder)
                            .FirstOrDefault();
                        version = latest != null ? latest.tbPclv_Version : "1.0.0";
                    }

                    HttpRuntime.Cache.Insert("PanelCurrentVersion", version, null,
                        System.Web.Caching.Cache.NoAbsoluteExpiration,
                        System.TimeSpan.FromMinutes(30));

                    return version;
                }
            }
            catch
            {
                return "1.0.0";
            }
        }

        public static async Task<PanelChangelogPageViewModel> GetPageForRoleAsync(string role)
        {
            var isAdmin = role == "1";
            var isAgent = role == "2" || role == "3" || role == "4";

            using (var db = new Entities())
            {
                var versionRepository = new Repository<tbPanelChangelogVersions>(db);
                var itemRepository = new Repository<tbPanelChangelogItems>(db);

                var versions = (await versionRepository.WhereAsync(v => v.tbPclv_IsActive))
                    .OrderByDescending(v => v.tbPclv_SortOrder)
                    .ToList();

                var items = (await itemRepository.WhereAsync(i => i.tbPcli_IsActive)).ToList();

                var mappedVersions = new List<PanelChangelogVersion>();

                foreach (var version in versions)
                {
                    var versionItems = items
                        .Where(i => i.FK_Version_ID == version.tbPclv_ID)
                        .Where(i =>
                            (i.tbPcli_Audience == AdminAudience && isAdmin) ||
                            (i.tbPcli_Audience == AgentAudience && isAgent))
                        .OrderBy(i => i.tbPcli_SortOrder)
                        .Select(i => new PanelChangelogEntry
                        {
                            Title = i.tbPcli_Title,
                            Description = i.tbPcli_Description,
                            Audience = i.tbPcli_Audience == AdminAudience
                                ? PanelChangelogAudience.Admin
                                : PanelChangelogAudience.Agent
                        })
                        .ToList();

                    if (!versionItems.Any())
                        continue;

                    mappedVersions.Add(new PanelChangelogVersion
                    {
                        Version = version.tbPclv_Version,
                        ReleaseDate = version.tbPclv_ReleaseDate,
                        Items = versionItems
                    });
                }

                var currentVersion = versions
                    .Where(v => v.tbPclv_IsCurrent)
                    .OrderByDescending(v => v.tbPclv_SortOrder)
                    .FirstOrDefault()
                    ?? versions.FirstOrDefault();

                return new PanelChangelogPageViewModel
                {
                    CurrentVersion = currentVersion != null ? currentVersion.tbPclv_Version : GetCurrentVersion(),
                    RoleLabel = isAdmin ? "ادمین" : isAgent ? "نماینده" : "کاربر",
                    Versions = mappedVersions
                };
            }
        }

        public static async Task<PanelChangelogPopupViewModel> GetPendingPopupAsync(int userId, string role)
        {
            try
            {
                var page = await GetPageForRoleAsync(role);
                var currentVersionBlock = page.Versions
                    .FirstOrDefault(v => v.Version == page.CurrentVersion);

                if (currentVersionBlock == null || currentVersionBlock.Items == null || !currentVersionBlock.Items.Any())
                    return null;

                using (var db = new Entities())
                {
                    var seenRepository = new Repository<tbPanelChangelogSeen>(db);
                    var alreadySeen = await seenRepository.FirstOrDefaultAsync(s =>
                        s.FK_User_ID == userId && s.tbPcls_Version == page.CurrentVersion);

                    if (alreadySeen != null)
                        return null;
                }

                return new PanelChangelogPopupViewModel
                {
                    Version = currentVersionBlock.Version,
                    ReleaseDate = currentVersionBlock.ReleaseDate,
                    Items = currentVersionBlock.Items
                };
            }
            catch
            {
                return null;
            }
        }

        public static async Task MarkVersionSeenAsync(int userId, string version)
        {
            if (string.IsNullOrWhiteSpace(version))
                version = GetCurrentVersion();

            try
            {
                using (var db = new Entities())
                {
                    var seenRepository = new Repository<tbPanelChangelogSeen>(db);
                    var existing = await seenRepository.FirstOrDefaultAsync(s =>
                        s.FK_User_ID == userId && s.tbPcls_Version == version);

                    if (existing != null)
                        return;

                    seenRepository.Insert(new tbPanelChangelogSeen
                    {
                        FK_User_ID = userId,
                        tbPcls_Version = version,
                        tbPcls_SeenAt = DateTime.Now
                    });

                    await seenRepository.SaveChangesAsync();
                }
            }
            catch
            {
                // جدول هنوز migrate نشده — pop-up بدون ثبت seen دوباره نمایش داده می‌شود
            }
        }
    }
}