using System;
using System.Data;
using System.Drawing;
using System.IO;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;
using Stimulsoft.Report.Export;
using V2boardApi.Areas.App.Data.UsersViewModels;

namespace V2boardApi.Tools
{
    public static class AgentHistoryPdfHelper
    {
        private const string DataSourceName = "AgentHistory";

        public static byte[] Export(AgentHistoryPdfResultViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            StimulsoftBootstrap.EnsureInitialized();

            var table = new DataTable(DataSourceName);
            table.Columns.Add("SubName", typeof(string));
            table.Columns.Add("EventName", typeof(string));
            table.Columns.Add("CreateDate", typeof(string));
            table.Columns.Add("SellPrice", typeof(string));
            table.Columns.Add("Plan", typeof(string));

            if (model.Items != null)
            {
                foreach (var item in model.Items)
                    table.Rows.Add(item.SubName, item.Event, item.CreateDate, item.SellPrice, item.Plan);
            }

            var report = new StiReport
            {
                CalculationMode = StiCalculationMode.Interpretation,
                ScriptLanguage = StiReportLanguageType.CSharp
            };
            report.Pages.Clear();
            var page = new StiPage();
            report.Pages.Add(page);

            var pageHeader = new StiPageHeaderBand { Height = 1.85 };
            page.Components.Add(pageHeader);

            var title = new StiText
            {
                ClientRectangle = new RectangleD(0, 0, page.Width, 0.5),
                Text = "گزارش تاریخچه اشتراک‌های نماینده",
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 14f, FontStyle.Bold)
            };
            pageHeader.Components.Add(title);

            var agentLine = new StiText
            {
                ClientRectangle = new RectangleD(0, 0.5, page.Width, 0.35),
                Text = string.Format("نماینده: {0} ({1})", model.AgentName ?? "-", model.AgentUsername ?? "-"),
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 10f, FontStyle.Bold)
            };
            pageHeader.Components.Add(agentLine);

            var rangeLine = new StiText
            {
                ClientRectangle = new RectangleD(0, 0.85, page.Width, 0.35),
                Text = string.Format("بازه: {0} تا {1}", model.FromDate ?? "—", model.ToDate ?? "—"),
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 9f)
            };
            pageHeader.Components.Add(rangeLine);

            var summary = model.Summary;
            var summaryText = summary == null
                ? string.Empty
                : string.Format(
                    "ساخته‌شده: {0} | تمدید شده: {1} | مبلغ فروش: {2} تومان | فاکتور پرداخت‌شده: {3} تومان",
                    summary.CreatedCount,
                    summary.RenewedCount,
                    summary.TotalSalesAmountFormatted ?? "0",
                    summary.PaidInvoicesAmountFormatted ?? "0");

            var summaryBand = new StiText
            {
                ClientRectangle = new RectangleD(0, 1.2, page.Width, 0.55),
                Text = summaryText,
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 9f),
                Brush = new StiSolidBrush(Color.FromArgb(245, 245, 245))
            };
            pageHeader.Components.Add(summaryBand);

            var headerBand = new StiHeaderBand { Height = 0.45 };
            page.Components.Add(headerBand);

            var headers = new[] { "نام اشتراک", "رویداد", "تاریخ", "مبلغ فروش", "تعرفه" };
            var fields = new[] { "SubName", "EventName", "CreateDate", "SellPrice", "Plan" };
            var colWidth = page.Width / headers.Length;
            for (var i = 0; i < headers.Length; i++)
            {
                var headerCell = new StiText
                {
                    ClientRectangle = new RectangleD(i * colWidth, 0, colWidth, 0.45),
                    Text = headers[i],
                    HorAlignment = StiTextHorAlignment.Center,
                    VertAlignment = StiVertAlignment.Center,
                    Font = new Font("Tahoma", 9f, FontStyle.Bold),
                    Brush = new StiSolidBrush(Color.Gainsboro)
                };
                headerBand.Components.Add(headerCell);
            }

            var dataBand = new StiDataBand
            {
                Height = 0.35,
                DataSourceName = DataSourceName
            };
            page.Components.Add(dataBand);

            for (var i = 0; i < fields.Length; i++)
            {
                var cell = new StiText
                {
                    ClientRectangle = new RectangleD(i * colWidth, 0, colWidth, 0.35),
                    Text = "{" + DataSourceName + "." + fields[i] + "}",
                    HorAlignment = StiTextHorAlignment.Center,
                    VertAlignment = StiVertAlignment.Center,
                    Font = new Font("Tahoma", 8.5f)
                };
                dataBand.Components.Add(cell);
            }

            var pageFooter = new StiPageFooterBand { Height = 0.35 };
            page.Components.Add(pageFooter);

            var footerText = new StiText
            {
                ClientRectangle = new RectangleD(0, 0, page.Width, 0.35),
                Text = "تاریخ تهیه گزارش: " + (model.GeneratedAt ?? ""),
                HorAlignment = StiTextHorAlignment.Left,
                Font = new Font("Tahoma", 8f),
                TextBrush = new StiSolidBrush(Color.Gray)
            };
            pageFooter.Components.Add(footerText);

            report.RegData(DataSourceName, table);
            report.Dictionary.Synchronize();
            report.Render(false);

            using (var stream = new MemoryStream())
            {
                report.ExportDocument(StiExportFormat.Pdf, stream);
                return stream.ToArray();
            }
        }
    }
}
