using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataLayer.DomainModel;
using DataLayer.Repository;
using V2boardApi.Areas.App.Data.Changelog;

namespace V2boardApi.Tools
{
    public static class PanelChangelogService
    {
        private const byte AdminAudience = 1;
        private const byte AgentAudience = 2;

        public static string GetCurrentVersion()
        {
            try
            {
                using (var db = new Entities())
                {
                    var versionRepository = new Repository<tbPanelChangelogVersions>(db);
                    var current = versionRepository
                        .Where(v => v.tbPclv_IsActive && v.tbPclv_IsCurrent)
                        .OrderByDescending(v => v.tbPclv_SortOrder)
                        .FirstOrDefault();

                    if (current != null)
                        return current.tbPclv_Version;

                    var latest = versionRepository
                        .Where(v => v.tbPclv_IsActive)
                        .OrderByDescending(v => v.tbPclv_SortOrder)
                        .FirstOrDefault();

                    return latest != null ? latest.tbPclv_Version : "1.0.0";
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

                var currentVersion = versions.FirstOrDefault(v => v.tbPclv_IsCurrent)
                    ?? versions.FirstOrDefault();

                return new PanelChangelogPageViewModel
                {
                    CurrentVersion = currentVersion != null ? currentVersion.tbPclv_Version : GetCurrentVersion(),
                    RoleLabel = isAdmin ? "ادمین" : isAgent ? "نماینده" : "کاربر",
                    Versions = mappedVersions
                };
            }
        }
    }
}
