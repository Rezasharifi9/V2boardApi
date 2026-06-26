using System.Collections.Generic;

namespace V2boardApi.Areas.App.Data.Changelog
{
    public enum PanelChangelogAudience
    {
        Admin = 1,
        Agent = 2
    }

    public class PanelChangelogEntry
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public PanelChangelogAudience Audience { get; set; }
    }

    public class PanelChangelogVersion
    {
        public string Version { get; set; }
        public string ReleaseDate { get; set; }
        public List<PanelChangelogEntry> Items { get; set; }
    }

    public class PanelChangelogPageViewModel
    {
        public string CurrentVersion { get; set; }
        public string RoleLabel { get; set; }
        public List<PanelChangelogVersion> Versions { get; set; }
    }

    public class PanelChangelogPopupViewModel
    {
        public string Version { get; set; }
        public string ReleaseDate { get; set; }
        public List<PanelChangelogEntry> Items { get; set; }
    }
}
