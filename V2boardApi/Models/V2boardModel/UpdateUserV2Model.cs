using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Models.V2boardModel
{
    public class UpdateUserV2Model
    {
        public int Id { get; set; }
        public string email { get; set; }
        public int is_admin { get; set; }
        public int is_staff { get; set; }
        public int u { get; set; }
        public int d { get; set; }
        public long expired_at { get; set; }
        public long t { get; set; }
    }
}