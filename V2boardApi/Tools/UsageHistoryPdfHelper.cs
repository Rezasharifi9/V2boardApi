using System;
using System.Data;
using System.Drawing;
using System.IO;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Components;
using Stimulsoft.Report.Export;
using V2boardApi.Areas.App.Data.SubscriptionsViewModels;

namespace V2boardApi.Tools
{
    public static class UsageHistoryPdfHelper
    {
        public static byte[] Export(SubUsageHistoryResultViewModel model)
        {
            if (model == null)
                throw new ArgumentNullException(nameof(model));

            StimulsoftBootstrap.EnsureInitialized();

            var table = new DataTable("UsageHistory");
            table.Columns.Add("Date", typeof(string));
            table.Columns.Add("Download", typeof(string));
            table.Columns.Add("Upload", typeof(string));
            table.Columns.Add("Total", typeof(string));

            if (model.Items != null)
            {
                foreach (var item in model.Items)
                    table.Rows.Add(item.Date, item.Download, item.Upload, item.Total);
            }

            var report = new StiReport
            {
                CalculationMode = StiCalculationMode.Interpretation,
                ScriptLanguage = StiReportLanguageType.CSharp
            };
            report.Pages.Clear();
            var page = new StiPage();
            report.Pages.Add(page);

            var pageHeader = new StiPageHeaderBand { Height = 1.1 };
            page.Components.Add(pageHeader);

            var title = new StiText
            {
                ClientRectangle = new RectangleD(0, 0, page.Width, 0.55),
                Text = "تاریخچه مصرف" + (string.IsNullOrWhiteSpace(model.SubName) ? "" : " - " + model.SubName),
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 13f, FontStyle.Bold)
            };
            pageHeader.Components.Add(title);

            var summaryText = model.Summary == null
                ? string.Empty
                : string.Format("بازه: {0} تا {1} | مجموع مصرف: {2}",
                    model.Summary.FromDate,
                    model.Summary.ToDate,
                    model.Summary.Total);

            var summary = new StiText
            {
                ClientRectangle = new RectangleD(0, 0.55, page.Width, 0.45),
                Text = summaryText,
                HorAlignment = StiTextHorAlignment.Center,
                Font = new Font("Tahoma", 9f)
            };
            pageHeader.Components.Add(summary);

            var headerBand = new StiHeaderBand { Height = 0.45 };
            page.Components.Add(headerBand);

            var headers = new[] { "تاریخ", "دانلود", "آپلود", "مجموع" };
            var colWidth = page.Width / 4d;
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
                DataSourceName = "UsageHistory"
            };
            page.Components.Add(dataBand);

            var fields = new[] { "Date", "Download", "Upload", "Total" };
            for (var i = 0; i < fields.Length; i++)
            {
                var cell = new StiText
                {
                    ClientRectangle = new RectangleD(i * colWidth, 0, colWidth, 0.35),
                    Text = "{UsageHistory." + fields[i] + "}",
                    HorAlignment = StiTextHorAlignment.Center,
                    VertAlignment = StiVertAlignment.Center,
                    Font = new Font("Tahoma", 8.5f)
                };
                dataBand.Components.Add(cell);
            }

            report.RegData("UsageHistory", table);
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
