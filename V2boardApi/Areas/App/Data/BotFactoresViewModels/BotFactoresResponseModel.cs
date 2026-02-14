using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace V2boardApi.Areas.App.Data.BotFactoresViewModels
{
    public class BotFactoresResponseModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string User { get; set; }
        public string TaxId { get; set; }
        public string Date { get; set; }
        public int Status  { get; set; }
        public string Price { get; set; }
        public string PayMethod { get; set; }
    }
}