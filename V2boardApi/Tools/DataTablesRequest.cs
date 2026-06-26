using System;
using System.Linq;
using System.Web;

namespace V2boardApi.Tools
{
    public class DataTablesRequest
    {
        public string Draw { get; set; }
        public int Start { get; set; }
        public int Length { get; set; }
        public string SearchValue { get; set; }
        public int SortColumnIndex { get; set; }
        public string SortDirection { get; set; }

        public static DataTablesRequest Parse(HttpRequestBase request)
        {
            var lengthStr = request.Form.GetValues("length")?.FirstOrDefault();
            var startStr = request.Form.GetValues("start")?.FirstOrDefault();
            var sortColStr = request.Form.GetValues("order[0][column]")?.FirstOrDefault();

            int sortCol = 0;
            if (!string.IsNullOrEmpty(sortColStr))
                int.TryParse(sortColStr, out sortCol);

            return new DataTablesRequest
            {
                Draw = request.Form.GetValues("draw")?.FirstOrDefault(),
                Start = startStr != null ? Convert.ToInt32(startStr) : 0,
                Length = lengthStr != null ? Convert.ToInt32(lengthStr) : 10,
                SearchValue = request.Form.GetValues("search[value]")?.FirstOrDefault()?.Trim(),
                SortColumnIndex = sortCol,
                SortDirection = request.Form.GetValues("order[0][dir]")?.FirstOrDefault() ?? "desc"
            };
        }

        public bool IsAscending => string.Equals(SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
    }
}
