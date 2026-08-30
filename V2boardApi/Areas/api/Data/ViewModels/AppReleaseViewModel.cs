using System.Collections.Generic;
using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    public class AppReleaseViewModel
    {
        [JsonProperty("version")]
        public string Version { get; set; }

        [JsonProperty("versionCode")]
        public int? VersionCode { get; set; }

        [JsonProperty("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonProperty("changelog")]
        public List<string> Changelog { get; set; }

        [JsonProperty("forceInstall")]
        public bool ForceInstall { get; set; }

        public static List<string> ParseItems(string raw)
        {
            var items = new List<string>();
            if (string.IsNullOrWhiteSpace(raw))
                return items;

            raw = raw.Trim();
            if (raw.StartsWith("["))
            {
                try
                {
                    var parsed = JsonConvert.DeserializeObject<List<string>>(raw);
                    if (parsed != null)
                    {
                        foreach (var item in parsed)
                        {
                            if (!string.IsNullOrWhiteSpace(item))
                                items.Add(item.Trim());
                        }
                        return items;
                    }
                }
                catch (JsonException)
                {
                }
            }

            foreach (var line in raw.Replace("\r\n", "\n").Split('\n'))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    items.Add(line.Trim());
            }

            return items;
        }

        public static string SerializeItems(IList<string> items)
        {
            var cleaned = new List<string>();
            if (items != null)
            {
                foreach (var item in items)
                {
                    if (!string.IsNullOrWhiteSpace(item))
                        cleaned.Add(item.Trim());
                }
            }

            if (cleaned.Count == 0)
                return null;

            return JsonConvert.SerializeObject(cleaned);
        }
    }
}
