using System.Collections.Generic;
using Newtonsoft.Json;

namespace V2boardApi.Areas.api.Data.ViewModels
{
    public class SupportLinkItemViewModel
    {
        [JsonProperty("id")]
        public int Id { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("link")]
        public string Link { get; set; }

        [JsonProperty("phone")]
        public string Phone { get; set; }
    }

    public class SupportLinkListViewModel
    {
        [JsonProperty("items")]
        public List<SupportLinkItemViewModel> Items { get; set; }
    }
}
