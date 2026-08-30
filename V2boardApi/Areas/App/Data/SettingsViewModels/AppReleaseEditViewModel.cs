using System.Collections.Generic;

namespace V2boardApi.Areas.App.Data.SettingsViewModels
{
    public class AppReleaseEditViewModel
    {
        public string DownloadUrl { get; set; }
        public string Version { get; set; }
        public int? VersionCode { get; set; }
        public bool ForceInstall { get; set; }
        public List<string> ChangelogItems { get; set; }
    }
}
