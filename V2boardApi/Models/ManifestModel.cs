using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Web;

namespace V2boardApi.Models
{
    public class ManifestModel
    {
        public string name { get; set; }
        public string short_name { get; set; }
        public string start_url { get; set; }
        public string background_color { get; set; }
        public string theme_color { get; set; }
        public string display { get; set; }
        public List<Icon> icons { get; set; }
    }

    public class Icon
    {
        public string src { get; set; }
        public string sizes { get; set; }
        public string type { get; set; }
    }
}