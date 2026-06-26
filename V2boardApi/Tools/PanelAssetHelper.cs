using System.Web;

namespace V2boardApi.Tools
{
    public static class PanelAssetHelper
    {
        public static string Js(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return path;

            var version = PanelChangelogService.GetCurrentVersion();
            var separator = path.Contains("?") ? "&" : "?";
            return path + separator + "v=" + HttpUtility.UrlEncode(version);
        }
    }
}
