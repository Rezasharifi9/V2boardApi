using System;
using System.Drawing;
using System.Windows.Forms;
using System.Data;
using Stimulsoft.Controls;
using Stimulsoft.Base.Drawing;
using Stimulsoft.Report;
using Stimulsoft.Report.Dialogs;
using Stimulsoft.Report.Components;

namespace Reports
{
    public class Report : Stimulsoft.Report.StiReport
    {
        public Report()        {
            this.InitializeComponent();
        }

        #region StiReport Designer generated code - do not modify
        public Stimulsoft.Dashboard.Components.StiDashboard Dashboard1;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart1;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item6;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item7;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item8;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart3;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item10;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item11;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item12;
        public Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement ComboBox1;
        public Stimulsoft.Data.Engine.StiDataSortRule Item14;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator1;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item16;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator2;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item18;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator3;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item20;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart2;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item22;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item23;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item24;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator4;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator5;
        public Stimulsoft.Base.Drawing.StiAdvancedWatermark Dashboard1_DashboardWatermark;
        public vi_FullSalesDataSource vi_FullSales;
        public tbUsersDataSource tbUsers;
        
        private void InitializeComponent()
        {
            this.tbUsers = new tbUsersDataSource();
            this.vi_FullSales = new vi_FullSalesDataSource();
            this.NeedsCompiling = false;
            this.CacheAllData = true;
            this.CalculationMode = Stimulsoft.Report.StiCalculationMode.Interpretation;
            this.EngineVersion = Stimulsoft.Report.Engine.StiEngineVersion.EngineV2;
            this.Key = "776498b381484671982548d41f0ab968";
            this.ParametersOrientation = Stimulsoft.Report.StiOrientation.Vertical;
            this.ParameterWidth = 100;
            this.PreviewSettings = 58663423;
            this.ReferencedAssemblies = new string[] {
                    "System.Dll",
                    "System.Drawing.Dll",
                    "System.Windows.Forms.Dll",
                    "System.Data.Dll",
                    "System.Xml.Dll",
                    "Stimulsoft.Controls.Dll",
                    "Stimulsoft.Base.Dll",
                    "Stimulsoft.Report.Dll"};
            this.ReportAlias = "Report";
            // 
            // ReportChanged
            // 
            this.ReportChanged = new DateTime(2024, 7, 14, 21, 44, 0, 283);
            // 
            // ReportCreated
            // 
            this.ReportCreated = new DateTime(2024, 6, 25, 17, 52, 39, 0);
            this.ReportFile = "D:\\VPN\\VPN Projects\\V2boardApi\\V2boardApi\\Reports\\Report.mrt";
            this.ReportGuid = "bc958339854346ecbd490bbefe3b0b86";
            this.ReportName = "Report";
            this.ReportUnit = Stimulsoft.Report.StiReportUnitType.Inches;
            this.ReportVersion = "2023.1.1.0";
            this.RequestParameters = true;
            this.ScriptLanguage = Stimulsoft.Report.StiReportLanguageType.CSharp;
            // 
            // Dashboard1
            // 
            this.Dashboard1 = new Stimulsoft.Dashboard.Components.StiDashboard();
            this.Dashboard1.AltSize = new Stimulsoft.Base.Drawing.SizeD(480, 800);
            this.Dashboard1.BackColor = System.Drawing.Color.Transparent;
            this.Dashboard1.Guid = "50b13f574c874160980c3e350e58f909";
            this.Dashboard1.Name = "Dashboard1";
            // 
            // Chart1
            // 
            this.Chart1 = new Stimulsoft.Dashboard.Components.Chart.StiChartElement();
            this.Chart1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 360, 480, 220);
            this.Chart1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart1.BackColor = System.Drawing.Color.Transparent;
            this.Chart1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 0, 480, 280);
            this.Chart1.Guid = "5455e07074da4a878aa45fa13429be6f";
            this.Chart1.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlCea14KqoBzTn5xKqqQT345qoBHZnoGqohjUXppbmpeCVwgPBWoBsENTi3KTC1WAvFiebliAcwjTseRAAAAWklQ";
            this.Chart1.Name = "Chart1";
            this.Chart1.NegativeSeriesColors = new System.Drawing.Color[0];
            this.Chart1.ParetoSeriesColors = new System.Drawing.Color[0];
            this.Chart1.RoundValues = true;
            this.Chart1.SeriesColors = new System.Drawing.Color[0];
            this.Chart1.Style = Stimulsoft.Report.Dashboard.StiElementStyleIdent.Turquoise;
            this.Chart1.Area = new Stimulsoft.Dashboard.Components.Chart.StiChartArea(false, false, false, new Stimulsoft.Dashboard.Components.Chart.StiHorChartGridLines(System.Drawing.Color.Transparent, true), new Stimulsoft.Dashboard.Components.Chart.StiVertChartGridLines(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiHorChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiVertChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, Stimulsoft.Base.StiAutoBool.Auto, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiXTopChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("B Titr", 12F), "تومان", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, true), new Stimulsoft.Dashboard.Components.Chart.StiYRightChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartYRightAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Left), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, true), false);
            // 
            // Item6
            // 
            this.Item6 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("dfd503a9fc1b427493b6c7103a5faa76", "vi_FullSales.PerMonth", "ماه");
            this.Chart1.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Chart1.Labels = new Stimulsoft.Dashboard.Components.Chart.StiChartLabels(Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Dashboard.Components.Chart.StiChartLabelsStyle.CategoryValue, new System.Drawing.Font("A Iranian Sans", 8F), System.Drawing.Color.Transparent, "", "", false);
            this.Chart1.Legend = new Stimulsoft.Dashboard.Components.Chart.StiChartLegend(new Stimulsoft.Dashboard.Components.Chart.StiChartLegendTitle(new System.Drawing.Font("A Iranian Sans", 12F), "", System.Drawing.Color.Transparent), new Stimulsoft.Dashboard.Components.Chart.StiChartLegendLabels(new System.Drawing.Font("B Mehr", 10F), System.Drawing.Color.Transparent, null), Stimulsoft.Report.Chart.StiLegendHorAlignment.Left, Stimulsoft.Report.Chart.StiLegendVertAlignment.TopOutside, Stimulsoft.Dashboard.Components.Chart.StiLegendVisibility.Auto, Stimulsoft.Report.Chart.StiLegendDirection.TopToBottom, 0);
            this.Chart1.ManuallyEnteredChartMeter = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("f0332e3108124e16b2c4cfa974176494", "", "", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            this.Chart1.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Chart1.Marker = new Stimulsoft.Dashboard.Components.Chart.StiChartMarker(7F, 0F, Stimulsoft.Report.Chart.StiMarkerType.Circle, Stimulsoft.Report.Chart.StiExtendedStyleBool.FromStyle);
            this.Chart1.SelectedViewStateKey = null;
            this.Chart1.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Chart1.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش ماهیانه", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Chart1.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Chart1.ValueFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, false, false, " ");
            // 
            // Item7
            // 
            this.Item7 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("7ecc129f2702439aa9074c7c5a138f01", "Sum(vi_FullSales.SalePanel)", "فروش از پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Item8
            // 
            this.Item8 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("791c62a5a0574007b5aeaf5562bd2692", "Sum(vi_FullSales.SaleRobot)", "فروش از ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Chart3
            // 
            this.Chart3 = new Stimulsoft.Dashboard.Components.Chart.StiChartElement();
            this.Chart3.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 580, 480, 220);
            this.Chart3.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart3.BackColor = System.Drawing.Color.Transparent;
            this.Chart3.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 280, 1200, 320);
            this.Chart3.Guid = "a42c195ca25a452eb7a023061ebf700d";
            this.Chart3.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlCea14KqoBzTn5xKqqQT345qoBHZnoGqohjUXppbmpeCVwgPBWoBsENTi3KTC1WAvFiebliAcwjTseRAAAAWklQ";
            this.Chart3.Name = "Chart3";
            this.Chart3.NegativeSeriesColors = new System.Drawing.Color[0];
            this.Chart3.ParetoSeriesColors = new System.Drawing.Color[0];
            this.Chart3.RoundValues = true;
            this.Chart3.SeriesColors = new System.Drawing.Color[0];
            this.Chart3.Style = Stimulsoft.Report.Dashboard.StiElementStyleIdent.Blue;
            this.Chart3.Area = new Stimulsoft.Dashboard.Components.Chart.StiChartArea(false, false, false, new Stimulsoft.Dashboard.Components.Chart.StiHorChartGridLines(System.Drawing.Color.Transparent, true), new Stimulsoft.Dashboard.Components.Chart.StiVertChartGridLines(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiHorChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiVertChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, Stimulsoft.Base.StiAutoBool.Auto, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiXTopChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("B Titr", 12F), "تومان", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, true), new Stimulsoft.Dashboard.Components.Chart.StiYRightChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartYRightAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Left), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, true), false);
            // 
            // Item10
            // 
            this.Item10 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("924200aa490a44c1ab0e11aa2d6cfee6", "vi_FullSales.PerDay", "روز");
            this.Chart3.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Chart3.DashboardInteraction = new Stimulsoft.Dashboard.Interactions.StiChartDashboardInteraction(Stimulsoft.Report.Dashboard.StiInteractionOnHover.ShowToolTip, Stimulsoft.Report.Dashboard.StiInteractionOnClick.DrillDown, Stimulsoft.Report.Dashboard.StiInteractionOpenHyperlinkDestination.NewTab, "", "", "", Stimulsoft.Dashboard.Interactions.StiDashboardDrillDownParameter.ToList(new Stimulsoft.Dashboard.Interactions.StiDashboardDrillDownParameter[0]), true, true, null, null, null, true, true, true, Stimulsoft.Report.Dashboard.StiInteractionViewsState.OnHover);
            this.Chart3.Labels = new Stimulsoft.Dashboard.Components.Chart.StiChartLabels(Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Dashboard.Components.Chart.StiChartLabelsStyle.CategoryValue, new System.Drawing.Font("A Iranian Sans", 8F), System.Drawing.Color.Transparent, "", "", false);
            this.Chart3.Legend = new Stimulsoft.Dashboard.Components.Chart.StiChartLegend(new Stimulsoft.Dashboard.Components.Chart.StiChartLegendTitle(new System.Drawing.Font("A Iranian Sans", 12F), "", System.Drawing.Color.Transparent), new Stimulsoft.Dashboard.Components.Chart.StiChartLegendLabels(new System.Drawing.Font("B Mehr", 10F), System.Drawing.Color.Transparent, null), Stimulsoft.Report.Chart.StiLegendHorAlignment.Left, Stimulsoft.Report.Chart.StiLegendVertAlignment.TopOutside, Stimulsoft.Dashboard.Components.Chart.StiLegendVisibility.Auto, Stimulsoft.Report.Chart.StiLegendDirection.TopToBottom, 0);
            this.Chart3.ManuallyEnteredChartMeter = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("3820fb93acf04c51840f7c667564d198", "", "", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            this.Chart3.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Chart3.Marker = new Stimulsoft.Dashboard.Components.Chart.StiChartMarker(7F, 0F, Stimulsoft.Report.Chart.StiMarkerType.Circle, Stimulsoft.Report.Chart.StiExtendedStyleBool.FromStyle);
            this.Chart3.SelectedViewStateKey = null;
            this.Chart3.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Chart3.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش روزانه | برای فیلتر ماه روی یکی از ماه های فروش ماهیانه کلیک کنید", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Chart3.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Chart3.ValueFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, false, false, " ");
            // 
            // Item11
            // 
            this.Item11 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("642894711d7f4d78a70d26926640014d", "Sum(vi_FullSales.SalePanel)", "فروش از پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Item12
            // 
            this.Item12 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("5958fb098d8e486b801ed9739adc36bd", "Sum(vi_FullSales.SaleRobot)", "فروش از ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // ComboBox1
            // 
            this.ComboBox1 = new Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement();
            this.ComboBox1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 0, 180, 40);
            this.ComboBox1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.ComboBox1.BackColor = System.Drawing.Color.Transparent;
            this.ComboBox1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(480, 0, 440, 40);
            this.ComboBox1.ForeColor = System.Drawing.Color.Transparent;
            this.ComboBox1.Guid = "e6ec486af5c14974aaa074d3164d27f2";
            this.ComboBox1.Name = "ComboBox1";
            this.ComboBox1.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.ComboBox1.Font = new System.Drawing.Font("B Titr", 10F);
            this.ComboBox1.KeyMeter = new Stimulsoft.Dashboard.Components.ComboBox.StiKeyComboBoxMeter("11170287ede84fc1b25c5c8708af96fa", "vi_FullSales.PerYear", "PerYear");
            this.ComboBox1.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.ComboBox1.NameMeter = new Stimulsoft.Dashboard.Components.ComboBox.StiNameComboBoxMeter("5226073f80444ef8b3525412c48c2a52", "vi_FullSales.PerYear", "PerYear");
            this.ComboBox1.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            // 
            // Item14
            // 
            this.Item14 = new Stimulsoft.Data.Engine.StiDataSortRule("PerYear2", Stimulsoft.Data.Engine.StiDataSortDirection.Descending);
            this.Item14.Direction = Stimulsoft.Data.Engine.StiDataSortDirection.Descending;
            this.Item14.Key = "PerYear2";
            // 
            // Indicator1
            // 
            this.Indicator1 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(180, 0, 300, 120);
            this.Indicator1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator1.BackColor = System.Drawing.Color.Transparent;
            this.Indicator1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(760, 40, 160, 120);
            this.Indicator1.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator1.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator1.Guid = "555e3f043b384376830806cd8a6d2c5e";
            this.Indicator1.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator1.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator1.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator1.Name = "Indicator1";
            this.Indicator1.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            // 
            // Item16
            // 
            this.Item16 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item16.IsExpression = true;
            this.Item16.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item16.Path = "vi_FullSales.date";
            this.Item16.Value = "Today";
            this.Item16.Value2 = "06/27/2024";
            this.Indicator1.Font = new System.Drawing.Font("B Titr", 10F);
            this.Indicator1.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Indicator1.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Indicator1.TextFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            this.Indicator1.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش امروز", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Indicator1.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Indicator1.Value = new Stimulsoft.Dashboard.Components.Indicator.StiValueIndicatorMeter("7e1a65f160d044fa982bc7aff2458121", "Sum(vi_FullSales.SaleRobot) + SUM(vi_FullSales.SalePanel)", "SaleRobot");
            // 
            // Indicator2
            // 
            this.Indicator2 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator2.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 40, 180, 160);
            this.Indicator2.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator2.BackColor = System.Drawing.Color.Transparent;
            this.Indicator2.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(480, 40, 280, 120);
            this.Indicator2.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator2.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator2.Guid = "ec75cd37299b4120a1c8cf029b6cf90c";
            this.Indicator2.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator2.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator2.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator2.Name = "Indicator2";
            this.Indicator2.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            // 
            // Item18
            // 
            this.Item18 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item18.IsExpression = true;
            this.Item18.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item18.Path = "vi_FullSales.date";
            this.Item18.Value = "Today";
            this.Item18.Value2 = "06/27/2024";
            this.Indicator2.Font = new System.Drawing.Font("B Titr", 10F);
            this.Indicator2.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Indicator2.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Indicator2.TextFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            this.Indicator2.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش پنل", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Indicator2.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Indicator2.Value = new Stimulsoft.Dashboard.Components.Indicator.StiValueIndicatorMeter("945ae4de19e34036b893e0df05f5aba9", "SUM(vi_FullSales.SalePanel)", "SaleRobot");
            // 
            // Indicator3
            // 
            this.Indicator3 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator3.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 200, 180, 160);
            this.Indicator3.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator3.BackColor = System.Drawing.Color.Transparent;
            this.Indicator3.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(760, 160, 160, 120);
            this.Indicator3.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator3.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator3.Guid = "a27100947ef94699918da69b0474a432";
            this.Indicator3.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator3.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator3.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator3.Name = "Indicator3";
            this.Indicator3.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            // 
            // Item20
            // 
            this.Item20 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item20.IsExpression = true;
            this.Item20.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item20.Path = "vi_FullSales.date";
            this.Item20.Value = "Today";
            this.Item20.Value2 = "06/27/2024";
            this.Indicator3.Font = new System.Drawing.Font("B Titr", 10F);
            this.Indicator3.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Indicator3.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Indicator3.TextFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            this.Indicator3.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش ربات", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Indicator3.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Indicator3.Value = new Stimulsoft.Dashboard.Components.Indicator.StiValueIndicatorMeter("c170198d12cf41ab8a5d33af827016a3", "Sum(vi_FullSales.SaleRobot)", "SaleRobot");
            // 
            // Chart2
            // 
            this.Chart2 = new Stimulsoft.Dashboard.Components.Chart.StiChartElement();
            this.Chart2.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(180, 120, 300, 240);
            this.Chart2.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart2.BackColor = System.Drawing.Color.Transparent;
            this.Chart2.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(920, 0, 280, 280);
            this.Chart2.Guid = "8ccc16be11294046acd65a85d332d594";
            this.Chart2.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlCea14KqoBzTn5xKqqQT345qoBHZnoGqohjUXppbmpeCVwgPBWoBsENTi3KTC1WAvFiebliAcwjTseRAAAAWklQ";
            this.Chart2.Name = "Chart2";
            this.Chart2.NegativeSeriesColors = new System.Drawing.Color[0];
            this.Chart2.ParetoSeriesColors = new System.Drawing.Color[0];
            this.Chart2.RoundValues = true;
            this.Chart2.SeriesColors = new System.Drawing.Color[0];
            this.Chart2.Style = Stimulsoft.Report.Dashboard.StiElementStyleIdent.Green;
            this.Chart2.Area = new Stimulsoft.Dashboard.Components.Chart.StiChartArea(false, false, false, new Stimulsoft.Dashboard.Components.Chart.StiHorChartGridLines(System.Drawing.Color.Transparent, true), new Stimulsoft.Dashboard.Components.Chart.StiVertChartGridLines(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiHorChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiVertChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, Stimulsoft.Base.StiAutoBool.Auto, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiXTopChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, true), new Stimulsoft.Dashboard.Components.Chart.StiYRightChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartYRightAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Left), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, true), false);
            // 
            // Item22
            // 
            this.Item22 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("acec1118e40f41369177cd54b760348f", "vi_FullSales.PerYear", "PerYear");
            this.Chart2.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Chart2.Labels = new Stimulsoft.Dashboard.Components.Chart.StiChartLabels(Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Dashboard.Components.Chart.StiChartLabelsStyle.CategoryValue, new System.Drawing.Font("B Mehr", 9F), System.Drawing.Color.Transparent, "", "", false);
            this.Chart2.Legend = new Stimulsoft.Dashboard.Components.Chart.StiChartLegend(new Stimulsoft.Dashboard.Components.Chart.StiChartLegendTitle(new System.Drawing.Font("B Mehr", 12F), "", System.Drawing.Color.Transparent), new Stimulsoft.Dashboard.Components.Chart.StiChartLegendLabels(new System.Drawing.Font("B Mehr", 10F), System.Drawing.Color.Transparent, null), Stimulsoft.Report.Chart.StiLegendHorAlignment.Left, Stimulsoft.Report.Chart.StiLegendVertAlignment.TopOutside, Stimulsoft.Dashboard.Components.Chart.StiLegendVisibility.Auto, Stimulsoft.Report.Chart.StiLegendDirection.TopToBottom, 0);
            this.Chart2.ManuallyEnteredChartMeter = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("e01d891ac23e49048f2103f4953d6206", "", "", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            this.Chart2.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Chart2.Marker = new Stimulsoft.Dashboard.Components.Chart.StiChartMarker(7F, 0F, Stimulsoft.Report.Chart.StiMarkerType.Circle, Stimulsoft.Report.Chart.StiExtendedStyleBool.FromStyle);
            this.Chart2.SelectedViewStateKey = null;
            this.Chart2.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Chart2.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش سال", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Chart2.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Chart2.ValueFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, 0, ".", 0, ",", 3, true, true, " ", (Stimulsoft.Report.Components.StiTextFormatState.DecimalDigits | Stimulsoft.Report.Components.StiTextFormatState.Abbreviation));
            // 
            // Item23
            // 
            this.Item23 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("d157d8351c0f4c3787cd38473d64d8b8", "Sum(vi_FullSales.SaleRobot)", "ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.Pie3d, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap);
            this.Item23.ShowNulls = Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap;
            this.Item23.ShowZeros = Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap;
            // 
            // Item24
            // 
            this.Item24 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("e3647237fc4f485a983194aff10f16e6", "Sum(vi_FullSales.SalePanel)", "پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.Pie3d, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Indicator4
            // 
            this.Indicator4 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator4.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(180, 0, 300, 120);
            this.Indicator4.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator4.BackColor = System.Drawing.Color.Transparent;
            this.Indicator4.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(480, 160, 140, 120);
            this.Indicator4.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator4.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator4.Guid = "7ab6193a42a747c6accd7cf479831803";
            this.Indicator4.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator4.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator4.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator4.Name = "Indicator4";
            this.Indicator4.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Indicator4.Font = new System.Drawing.Font("B Titr", 10F);
            this.Indicator4.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Indicator4.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Indicator4.Target = new Stimulsoft.Dashboard.Components.Indicator.StiTargetIndicatorMeter("321185ba43ac4be99bd3e1f24663de3c", "Sum(tbUsers.Wallet)", "Wallet");
            this.Indicator4.TextFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            this.Indicator4.Title = new Stimulsoft.Dashboard.Components.StiTitle("سقف کیف پول", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Indicator4.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Indicator4.Value = new Stimulsoft.Dashboard.Components.Indicator.StiValueIndicatorMeter("b6e986e48f1445b9a5c9ac83268a9d3f", "Sum(tbUsers.Limit)", "Limit");
            // 
            // Indicator5
            // 
            this.Indicator5 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator5.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(180, 0, 300, 120);
            this.Indicator5.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator5.BackColor = System.Drawing.Color.Transparent;
            this.Indicator5.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(620, 160, 140, 120);
            this.Indicator5.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator5.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator5.Guid = "6566990900464196884595f75d698d36";
            this.Indicator5.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator5.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator5.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator5.Name = "Indicator5";
            this.Indicator5.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Indicator5.Font = new System.Drawing.Font("B Titr", 10F);
            this.Indicator5.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Indicator5.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Indicator5.TextFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            this.Indicator5.Title = new Stimulsoft.Dashboard.Components.StiTitle("سقف کیف پول", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Indicator5.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Dashboard1_DashboardWatermark = new Stimulsoft.Base.Drawing.StiAdvancedWatermark();
            this.Dashboard1_DashboardWatermark.TextColor = System.Drawing.Color.Gray;
            this.Dashboard1_DashboardWatermark.WeaveMajorColor = System.Drawing.Color.FromArgb(119, 119, 119, 119);
            this.Dashboard1_DashboardWatermark.WeaveMinorColor = System.Drawing.Color.FromArgb(85, 119, 119, 119);
            this.Dashboard1_DashboardWatermark.TextFont = new System.Drawing.Font("Arial", 36F);
            this.Dashboard1.DashboardWatermark = this.Dashboard1_DashboardWatermark;
            this.Dashboard1.Report = this;
            this.Chart1.Page = this.Dashboard1;
            this.Chart1.Parent = this.Dashboard1;
            this.Chart3.Page = this.Dashboard1;
            this.Chart3.Parent = this.Dashboard1;
            this.ComboBox1.Page = this.Dashboard1;
            this.ComboBox1.Parent = this.Dashboard1;
            this.Indicator1.Page = this.Dashboard1;
            this.Indicator1.Parent = this.Dashboard1;
            this.Indicator2.Page = this.Dashboard1;
            this.Indicator2.Parent = this.Dashboard1;
            this.Indicator3.Page = this.Dashboard1;
            this.Indicator3.Parent = this.Dashboard1;
            this.Chart2.Page = this.Dashboard1;
            this.Chart2.Parent = this.Dashboard1;
            this.Indicator4.Page = this.Dashboard1;
            this.Indicator4.Parent = this.Dashboard1;
            this.Indicator5.Page = this.Dashboard1;
            this.Indicator5.Parent = this.Dashboard1;
            // 
            // Add to Chart1.Arguments
            // 
            this.Chart1.Arguments.Clear();
            this.Chart1.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item6});
            // 
            // Add to Chart1.Values
            // 
            this.Chart1.Values.Clear();
            this.Chart1.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item7,
                        this.Item8});
            // 
            // Add to Chart3.Arguments
            // 
            this.Chart3.Arguments.Clear();
            this.Chart3.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item10});
            // 
            // Add to Chart3.Values
            // 
            this.Chart3.Values.Clear();
            this.Chart3.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item11,
                        this.Item12});
            // 
            // Add to ComboBox1.TransformSorts
            // 
            this.ComboBox1.TransformSorts.Clear();
            this.ComboBox1.TransformSorts.AddRange(new Stimulsoft.Data.Engine.StiDataSortRule[] {
                        this.Item14});
            // 
            // Add to Indicator1.DataFilters
            // 
            this.Indicator1.DataFilters.Clear();
            this.Indicator1.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item16});
            // 
            // Add to Indicator2.DataFilters
            // 
            this.Indicator2.DataFilters.Clear();
            this.Indicator2.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item18});
            // 
            // Add to Indicator3.DataFilters
            // 
            this.Indicator3.DataFilters.Clear();
            this.Indicator3.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item20});
            // 
            // Add to Chart2.Arguments
            // 
            this.Chart2.Arguments.Clear();
            this.Chart2.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item22});
            // 
            // Add to Chart2.Values
            // 
            this.Chart2.Values.Clear();
            this.Chart2.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item23,
                        this.Item24});
            // 
            // Add to Dashboard1.Components
            // 
            this.Dashboard1.Components.Clear();
            this.Dashboard1.Components.AddRange(new Stimulsoft.Report.Components.StiComponent[] {
                        this.Chart1,
                        this.Chart3,
                        this.ComboBox1,
                        this.Indicator1,
                        this.Indicator2,
                        this.Indicator3,
                        this.Chart2,
                        this.Indicator4,
                        this.Indicator5});
            // 
            // Add to Pages
            // 
            this.Pages.Clear();
            this.Pages.AddRange(new Stimulsoft.Report.Components.StiPage[] {
                        this.Dashboard1});
            this.vi_FullSales.Columns.AddRange(new Stimulsoft.Report.Dictionary.StiDataColumn[] {
                        new Stimulsoft.Report.Dictionary.StiDataColumn("SaleRobot", "SaleRobot", "SaleRobot", typeof(int), "93399b12e3ef46b98aed138a97e97932"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("SalePanel", "SalePanel", "SalePanel", typeof(int), "ae9065bb126c4b83a274350f31141a7a"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerYear", "PerYear", "PerYear", typeof(int), "6c1e1dad2e7d4c90879a18927b7b45cc"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("User_ID", "User_ID", "User_ID", typeof(int), "bd982ef5123c4df9a89d7821bb023d56"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerMonth", "PerMonth", "PerMonth", typeof(int), "6f0479a463034525bb7f2ec0c5045e6c"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerDay", "PerDay", "PerDay", typeof(int), "4351810f9d7e4e498934d123a6e4a7fe"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("date", "date", "date", typeof(DateTime), null)});
            this.vi_FullSales.Parameters.AddRange(new Stimulsoft.Report.Dictionary.StiDataParameter[] {
                        new Stimulsoft.Report.Dictionary.StiDataParameter("User_ID", 8, 0, null)});
            this.DataSources.Add(this.vi_FullSales);
            this.tbUsers.Parameters.AddRange(new Stimulsoft.Report.Dictionary.StiDataParameter[] {
                        new Stimulsoft.Report.Dictionary.StiDataParameter("tbUserID", 8, 0, null)});
            this.DataSources.Add(this.tbUsers);
            this.Dictionary.Databases.Add(new Stimulsoft.Report.Dictionary.StiSqlDatabase("MS SQL", "MS SQL", "#%#", false, "650d0b0ee28844fcb7e30997a9778e9e"));
            ((Stimulsoft.Report.Dictionary.StiSqlDatabase)(this.Dictionary.Databases["MS SQL"])).ConnectionStringEncrypted = "9NrZocYPYx1FEajATiX2F4NW3YlmUdn181u5rWhc63LRevXPTXZJ7UVH7aVjAKK75nDbK8lx2YFhFaAEZ1xhYsNU0WheWdOZ4QbV";
            this.vi_FullSales.Connecting += new System.EventHandler(this.Getvi_FullSales_SqlCommand);
            this.tbUsers.Connecting += new System.EventHandler(this.GettbUsers_SqlCommand);
        }
        
        public void Getvi_FullSales_SqlCommand(object sender, System.EventArgs e)
        {
            this.vi_FullSales.SqlCommand = "select * from vi_FullSales where User_ID = @User_ID order by  PerYear desc, PerMonth desc,PerDay desc";
        }
        
        public void GettbUsers_SqlCommand(object sender, System.EventArgs e)
        {
            this.tbUsers.SqlCommand = "select Limit, Wallet from tbUsers where User_ID = 3058";
        }
        
        // CheckerInfo: *None* *DataSources*
        #region DataSource vi_FullSales
        public class vi_FullSalesDataSource : Stimulsoft.Report.Dictionary.StiSqlSource
        {
            
            public vi_FullSalesDataSource() : 
                    base("MS SQL", "vi_FullSales", "vi_FullSales", "", true, false, 30, "6d3eb4486cc446d2b64235b20cd3f339")
            {
            }
            
            public virtual int SaleRobot
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["SaleRobot"], typeof(int), true)));
                }
            }
            
            public virtual int SalePanel
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["SalePanel"], typeof(int), true)));
                }
            }
            
            public virtual int PerYear
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["PerYear"], typeof(int), true)));
                }
            }
            
            public virtual int User_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["User_ID"], typeof(int), true)));
                }
            }
            
            public virtual int PerMonth
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["PerMonth"], typeof(int), true)));
                }
            }
            
            public virtual int PerDay
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["PerDay"], typeof(int), true)));
                }
            }
            
            public virtual DateTime date
            {
                get
                {
                    return ((DateTime)(StiReport.ChangeType(this["date"], typeof(DateTime), true)));
                }
            }
        }
        #endregion DataSource vi_FullSales
        
        #region DataSource tbUsers
        public class tbUsersDataSource : Stimulsoft.Report.Dictionary.StiSqlSource
        {
            
            public tbUsersDataSource() : 
                    base("MS SQL", "tbUsers", "tbUsers", "", true, false, 30, "f89f4593b63e49f8b8d20c05ed344c95")
            {
            }
        }
        #endregion DataSource tbUsers
        #endregion StiReport Designer generated code - do not modify
    }
}
