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
        public Stimulsoft.Report.Dictionary.StiDataRelation ParentName;
        public Stimulsoft.Dashboard.Components.StiDashboard Dashboard1;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart1;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item7;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item8;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item9;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart3;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item11;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item12;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item13;
        public Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement ComboBox1;
        public Stimulsoft.Data.Engine.StiDataSortRule Item15;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator1;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item17;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator2;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item19;
        public Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement Indicator3;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item21;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart2;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item23;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item24;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item25;
        public Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement ComboBox2;
        public Stimulsoft.Data.Engine.StiDataSortRule Item27;
        public Stimulsoft.Dashboard.Components.Chart.StiChartElement Chart4;
        public Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter Item29;
        public Stimulsoft.Data.Engine.StiDataFilterRule Item30;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item31;
        public Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter Item32;
        public Stimulsoft.Base.Drawing.StiAdvancedWatermark Dashboard1_DashboardWatermark;
        public vi_FullSalesDataSource vi_FullSales;
        public tbUsersDataSource tbUsers;
        
        private void InitializeComponent()
        {
            this.tbUsers = new tbUsersDataSource();
            this.vi_FullSales = new vi_FullSalesDataSource();
            this.ParentName = new Stimulsoft.Report.Dictionary.StiDataRelation("Relation", "Name", "Name", this.tbUsers, this.vi_FullSales, new string[] {
                        "User_ID"}, new string[] {
                        "User_ID"}, "e7a4ba72c55d41dab45f056a12d3a508");
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
            this.ReportChanged = new DateTime(2024, 6, 27, 12, 3, 19, 292);
            // 
            // ReportCreated
            // 
            this.ReportCreated = new DateTime(2024, 6, 25, 17, 52, 39, 0);
            this.ReportFile = "D:\\VPN\\VPN Projects\\V2boardApi\\V2boardApi\\Reports\\ReportAdmin.mrt";
            this.ReportGuid = "a12afd9086454f80a54e639b8c3fef80";
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
            this.Chart1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(260, 360, 220, 220);
            this.Chart1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart1.BackColor = System.Drawing.Color.Transparent;
            this.Chart1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 0, 600, 280);
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
            // Item7
            // 
            this.Item7 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("dfd503a9fc1b427493b6c7103a5faa76", "vi_FullSales.PerMonth", "ماه");
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
            // Item8
            // 
            this.Item8 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("7ecc129f2702439aa9074c7c5a138f01", "Sum(vi_FullSales.SalePanel)", "فروش از پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Item9
            // 
            this.Item9 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("791c62a5a0574007b5aeaf5562bd2692", "Sum(vi_FullSales.SaleRobot)", "فروش از ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Chart3
            // 
            this.Chart3 = new Stimulsoft.Dashboard.Components.Chart.StiChartElement();
            this.Chart3.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 360, 260, 220);
            this.Chart3.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart3.BackColor = System.Drawing.Color.Transparent;
            this.Chart3.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(600, 280, 600, 320);
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
            // Item11
            // 
            this.Item11 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("924200aa490a44c1ab0e11aa2d6cfee6", "vi_FullSales.PerDay", "روز");
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
            this.Chart3.ValueFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, true, false, " ");
            // 
            // Item12
            // 
            this.Item12 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("642894711d7f4d78a70d26926640014d", "Sum(vi_FullSales.SalePanel)", "فروش از پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.Spline, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Item13
            // 
            this.Item13 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("5958fb098d8e486b801ed9739adc36bd", "Sum(vi_FullSales.SaleRobot)", "فروش از ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.Spline, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // ComboBox1
            // 
            this.ComboBox1 = new Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement();
            this.ComboBox1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 0, 220, 40);
            this.ComboBox1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.ComboBox1.BackColor = System.Drawing.Color.Transparent;
            this.ComboBox1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(600, 0, 320, 40);
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
            // Item15
            // 
            this.Item15 = new Stimulsoft.Data.Engine.StiDataSortRule("PerYear2", Stimulsoft.Data.Engine.StiDataSortDirection.Descending);
            this.Item15.Direction = Stimulsoft.Data.Engine.StiDataSortDirection.Descending;
            this.Item15.Key = "PerYear2";
            // 
            // Indicator1
            // 
            this.Indicator1 = new Stimulsoft.Dashboard.Components.Indicator.StiIndicatorElement();
            this.Indicator1.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 40, 300, 120);
            this.Indicator1.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator1.BackColor = System.Drawing.Color.Transparent;
            this.Indicator1.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(600, 40, 320, 120);
            this.Indicator1.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator1.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator1.Guid = "555e3f043b384376830806cd8a6d2c5e";
            this.Indicator1.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator1.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator1.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator1.Name = "Indicator1";
            this.Indicator1.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            // 
            // Item17
            // 
            this.Item17 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item17.IsExpression = true;
            this.Item17.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item17.Path = "vi_FullSales.date";
            this.Item17.Value = "Today";
            this.Item17.Value2 = "06/27/2024";
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
            this.Indicator2.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(300, 40, 180, 160);
            this.Indicator2.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Indicator2.BackColor = System.Drawing.Color.Transparent;
            this.Indicator2.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(600, 160, 160, 120);
            this.Indicator2.ForeColor = System.Drawing.Color.Transparent;
            this.Indicator2.GlyphColor = System.Drawing.Color.Transparent;
            this.Indicator2.Guid = "ec75cd37299b4120a1c8cf029b6cf90c";
            this.Indicator2.IconRangeMode = Stimulsoft.Report.Dashboard.StiIndicatorIconRangeMode.Percentage;
            this.Indicator2.IconSet = Stimulsoft.Report.Helpers.StiFontIconSet.Rating;
            this.Indicator2.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlBeSGJRemoJnBucWpSZWqwE4sXycsUCALmUbWY5AAAAWklQ";
            this.Indicator2.Name = "Indicator2";
            this.Indicator2.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            // 
            // Item19
            // 
            this.Item19 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item19.IsExpression = true;
            this.Item19.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item19.Path = "vi_FullSales.date";
            this.Item19.Value = "Today";
            this.Item19.Value2 = "06/27/2024";
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
            this.Indicator3.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(300, 200, 180, 160);
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
            // Item21
            // 
            this.Item21 = new Stimulsoft.Data.Engine.StiDataFilterRule("b3cf7c70c3d442d990d6752a21bb48dd", "vi_FullSales.date", null, Stimulsoft.Data.Engine.StiDataFilterCondition.EqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "Today", "06/27/2024", true, true);
            this.Item21.IsExpression = true;
            this.Item21.Key = "b3cf7c70c3d442d990d6752a21bb48dd";
            this.Item21.Path = "vi_FullSales.date";
            this.Item21.Value = "Today";
            this.Item21.Value2 = "06/27/2024";
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
            this.Chart2.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 160, 300, 200);
            this.Chart2.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart2.BackColor = System.Drawing.Color.Transparent;
            this.Chart2.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(920, 40, 280, 240);
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
            // Item23
            // 
            this.Item23 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("acec1118e40f41369177cd54b760348f", "vi_FullSales.PerYear", "PerYear");
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
            // Item24
            // 
            this.Item24 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("d157d8351c0f4c3787cd38473d64d8b8", "Sum(vi_FullSales.SaleRobot)", "ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.Pie3d, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap);
            this.Item24.ShowNulls = Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap;
            this.Item24.ShowZeros = Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Gap;
            // 
            // Item25
            // 
            this.Item25 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("e3647237fc4f485a983194aff10f16e6", "Sum(vi_FullSales.SalePanel)", "پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.Pie3d, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // ComboBox2
            // 
            this.ComboBox2 = new Stimulsoft.Dashboard.Components.ComboBox.StiComboBoxElement();
            this.ComboBox2.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(220, 0, 260, 40);
            this.ComboBox2.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.ComboBox2.BackColor = System.Drawing.Color.Transparent;
            this.ComboBox2.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(920, 0, 280, 40);
            this.ComboBox2.ForeColor = System.Drawing.Color.Transparent;
            this.ComboBox2.Guid = "943348b4ac0243cd87b71e21cfeae49e";
            this.ComboBox2.Name = "ComboBox2";
            this.ComboBox2.SelectionMode = Stimulsoft.Report.Dashboard.StiItemSelectionMode.Multi;
            this.ComboBox2.ShowAllValue = true;
            this.ComboBox2.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.ComboBox2.Font = new System.Drawing.Font("B Titr", 10F);
            this.ComboBox2.KeyMeter = new Stimulsoft.Dashboard.Components.ComboBox.StiKeyComboBoxMeter("88199721b4334b0fa9ce930e35e4666c", "tbUsers.Username", "Username");
            this.ComboBox2.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.ComboBox2.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            // 
            // Item27
            // 
            this.Item27 = new Stimulsoft.Data.Engine.StiDataSortRule("PerYear2", Stimulsoft.Data.Engine.StiDataSortDirection.Descending);
            this.Item27.Direction = Stimulsoft.Data.Engine.StiDataSortDirection.Descending;
            this.Item27.Key = "PerYear2";
            // 
            // Chart4
            // 
            this.Chart4 = new Stimulsoft.Dashboard.Components.Chart.StiChartElement();
            this.Chart4.AltClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 580, 480, 220);
            this.Chart4.AltParentKey = "50b13f574c874160980c3e350e58f909";
            this.Chart4.BackColor = System.Drawing.Color.Transparent;
            this.Chart4.ClientRectangle = new Stimulsoft.Base.Drawing.RectangleD(0, 280, 600, 320);
            this.Chart4.Guid = "531a4b46ed754aa0a9e448cc276d0d1b";
            this.Chart4.ManuallyEnteredData = "H4sIAAAAAAAEAIvm5VJQiAYRCgpKYYk5palKOlCea14KqoBzTn5xKqqQT345qoBHZnoGqohjUXppbmpeCVwgPBWoBsENTi3KTC1WAvFiebliAcwjTseRAAAAWklQ";
            this.Chart4.Name = "Chart4";
            this.Chart4.NegativeSeriesColors = new System.Drawing.Color[0];
            this.Chart4.ParetoSeriesColors = new System.Drawing.Color[0];
            this.Chart4.RoundValues = true;
            this.Chart4.SeriesColors = new System.Drawing.Color[0];
            this.Chart4.Style = Stimulsoft.Report.Dashboard.StiElementStyleIdent.Turquoise;
            this.Chart4.Area = new Stimulsoft.Dashboard.Components.Chart.StiChartArea(false, false, false, new Stimulsoft.Dashboard.Components.Chart.StiHorChartGridLines(System.Drawing.Color.Transparent, true), new Stimulsoft.Dashboard.Components.Chart.StiVertChartGridLines(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiHorChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiVertChartInterlacing(System.Drawing.Color.Transparent, false), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("B Titr", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, Stimulsoft.Base.StiAutoBool.Auto, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiXTopChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiXChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.LeftToRight, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, Stimulsoft.Base.StiAutoBool.Auto), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartAxisLabels("", "", 0F, new System.Drawing.Font("A Iranian Sans", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Right), new Stimulsoft.Dashboard.Components.Chart.StiChartAxisRange(true, 0, 0), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("B Titr", 12F), "تومان", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), true, true), new Stimulsoft.Dashboard.Components.Chart.StiYRightChartAxis(new Stimulsoft.Dashboard.Components.Chart.StiChartYRightAxisLabels("", "", 0F, new System.Drawing.Font("Arial", 8F), Stimulsoft.Report.Chart.StiLabelsPlacement.AutoRotation, System.Drawing.Color.Transparent, Stimulsoft.Base.Drawing.StiHorAlignment.Left), new Stimulsoft.Dashboard.Components.Chart.StiYChartAxisTitle(new System.Drawing.Font("Arial", 12F, System.Drawing.FontStyle.Bold), "", System.Drawing.Color.Transparent, System.Drawing.StringAlignment.Center, Stimulsoft.Report.Chart.StiDirection.BottomToTop, Stimulsoft.Report.Chart.StiTitlePosition.Outside, true), false, true), false);
            // 
            // Item29
            // 
            this.Item29 = new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter("61c78d74ba744944b5037fc47be2201b", "tbUsers.Username", "نام کاربری");
            this.Chart4.Border = new Stimulsoft.Base.Drawing.StiSimpleBorder(Stimulsoft.Base.Drawing.StiBorderSides.None, System.Drawing.Color.Gray, 1, Stimulsoft.Base.Drawing.StiPenStyle.Solid);
            this.Chart4.DashboardInteraction = new Stimulsoft.Dashboard.Interactions.StiChartDashboardInteraction(Stimulsoft.Report.Dashboard.StiInteractionOnHover.ShowToolTip, Stimulsoft.Report.Dashboard.StiInteractionOnClick.DrillDown, Stimulsoft.Report.Dashboard.StiInteractionOpenHyperlinkDestination.NewTab, "", "", "", Stimulsoft.Dashboard.Interactions.StiDashboardDrillDownParameter.ToList(new Stimulsoft.Dashboard.Interactions.StiDashboardDrillDownParameter[0]), true, true, null, null, null, true, true, true, Stimulsoft.Report.Dashboard.StiInteractionViewsState.OnHover);
            // 
            // Item30
            // 
            this.Item30 = new Stimulsoft.Data.Engine.StiDataFilterRule("80d10f72a383474a8d20cd8d79f7e2db", "tbUsers.Role", null, Stimulsoft.Data.Engine.StiDataFilterCondition.NotEqualTo, Stimulsoft.Data.Engine.StiDataFilterOperation.AND, "1", "0", true, false);
            this.Item30.Condition = Stimulsoft.Data.Engine.StiDataFilterCondition.NotEqualTo;
            this.Item30.Key = "80d10f72a383474a8d20cd8d79f7e2db";
            this.Item30.Path = "tbUsers.Role";
            this.Item30.Value = "1";
            this.Item30.Value2 = "0";
            this.Chart4.Labels = new Stimulsoft.Dashboard.Components.Chart.StiChartLabels(Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.Center, Stimulsoft.Report.Dashboard.StiChartLabelsPosition.None, Stimulsoft.Dashboard.Components.Chart.StiChartLabelsStyle.CategoryValue, new System.Drawing.Font("A Iranian Sans", 8F), System.Drawing.Color.Transparent, "", "", false);
            this.Chart4.Legend = new Stimulsoft.Dashboard.Components.Chart.StiChartLegend(new Stimulsoft.Dashboard.Components.Chart.StiChartLegendTitle(new System.Drawing.Font("A Iranian Sans", 12F), "", System.Drawing.Color.Transparent), new Stimulsoft.Dashboard.Components.Chart.StiChartLegendLabels(new System.Drawing.Font("B Mehr", 10F), System.Drawing.Color.Transparent, null), Stimulsoft.Report.Chart.StiLegendHorAlignment.Left, Stimulsoft.Report.Chart.StiLegendVertAlignment.TopOutside, Stimulsoft.Dashboard.Components.Chart.StiLegendVisibility.Auto, Stimulsoft.Report.Chart.StiLegendDirection.TopToBottom, 0);
            this.Chart4.ManuallyEnteredChartMeter = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("b06ddcfc369448609be46a1879ed908e", "", "", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            this.Chart4.Margin = new Stimulsoft.Report.Dashboard.StiMargin(3, 3, 3, 3);
            this.Chart4.Marker = new Stimulsoft.Dashboard.Components.Chart.StiChartMarker(7F, 0F, Stimulsoft.Report.Chart.StiMarkerType.Circle, Stimulsoft.Report.Chart.StiExtendedStyleBool.FromStyle);
            this.Chart4.SelectedViewStateKey = null;
            this.Chart4.Shadow = new Stimulsoft.Base.Drawing.StiSimpleShadow(System.Drawing.Color.FromArgb(68, 34, 34, 34), new System.Drawing.Point(2, 2), 5, false);
            this.Chart4.Title = new Stimulsoft.Dashboard.Components.StiTitle("فروش کلی عمده فروشان", System.Drawing.Color.Transparent, System.Drawing.Color.Transparent, new System.Drawing.Font("B Titr", 11.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 178), Stimulsoft.Base.Drawing.StiHorAlignment.Center, true, Stimulsoft.Report.Dashboard.StiTextSizeMode.Fit);
            this.Chart4.TopN = new Stimulsoft.Data.Engine.StiDataTopN(Stimulsoft.Data.Engine.StiDataTopNMode.None, 5, true, "", "");
            this.Chart4.ValueFormat = new Stimulsoft.Report.Components.TextFormats.StiNumberFormatService(1, ".", 0, ",", 3, false, false, " ");
            // 
            // Item31
            // 
            this.Item31 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("88686a1e7d9443f597c22f1da2c9ac72", "Sum(vi_FullSales.SalePanel)", "فروش از پنل", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
            // 
            // Item32
            // 
            this.Item32 = new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter("0414d2d2094448bebea11df8f8e95dc3", "Sum(vi_FullSales.SaleRobot)", "فروش از ربات", Stimulsoft.Report.Dashboard.StiChartSeriesType.ClusteredColumn, Stimulsoft.Report.Chart.StiSeriesYAxis.LeftYAxis, Stimulsoft.Base.Drawing.StiPenStyle.Solid, 2F, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero, Stimulsoft.Report.Dashboard.StiEmptyCellsAs.Zero);
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
            this.ComboBox2.Page = this.Dashboard1;
            this.ComboBox2.Parent = this.Dashboard1;
            this.Chart4.Page = this.Dashboard1;
            this.Chart4.Parent = this.Dashboard1;
            // 
            // Add to Chart1.Arguments
            // 
            this.Chart1.Arguments.Clear();
            this.Chart1.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item7});
            // 
            // Add to Chart1.Values
            // 
            this.Chart1.Values.Clear();
            this.Chart1.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item8,
                        this.Item9});
            // 
            // Add to Chart3.Arguments
            // 
            this.Chart3.Arguments.Clear();
            this.Chart3.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item11});
            // 
            // Add to Chart3.Values
            // 
            this.Chart3.Values.Clear();
            this.Chart3.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item12,
                        this.Item13});
            // 
            // Add to ComboBox1.TransformSorts
            // 
            this.ComboBox1.TransformSorts.Clear();
            this.ComboBox1.TransformSorts.AddRange(new Stimulsoft.Data.Engine.StiDataSortRule[] {
                        this.Item15});
            // 
            // Add to Indicator1.DataFilters
            // 
            this.Indicator1.DataFilters.Clear();
            this.Indicator1.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item17});
            // 
            // Add to Indicator2.DataFilters
            // 
            this.Indicator2.DataFilters.Clear();
            this.Indicator2.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item19});
            // 
            // Add to Indicator3.DataFilters
            // 
            this.Indicator3.DataFilters.Clear();
            this.Indicator3.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item21});
            // 
            // Add to Chart2.Arguments
            // 
            this.Chart2.Arguments.Clear();
            this.Chart2.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item23});
            // 
            // Add to Chart2.Values
            // 
            this.Chart2.Values.Clear();
            this.Chart2.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item24,
                        this.Item25});
            // 
            // Add to ComboBox2.TransformSorts
            // 
            this.ComboBox2.TransformSorts.Clear();
            this.ComboBox2.TransformSorts.AddRange(new Stimulsoft.Data.Engine.StiDataSortRule[] {
                        this.Item27});
            // 
            // Add to Chart4.Arguments
            // 
            this.Chart4.Arguments.Clear();
            this.Chart4.Arguments.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiArgumentChartMeter[] {
                        this.Item29});
            // 
            // Add to Chart4.DataFilters
            // 
            this.Chart4.DataFilters.Clear();
            this.Chart4.DataFilters.AddRange(new Stimulsoft.Data.Engine.StiDataFilterRule[] {
                        this.Item30});
            // 
            // Add to Chart4.Values
            // 
            this.Chart4.Values.Clear();
            this.Chart4.Values.AddRange(new Stimulsoft.Dashboard.Components.Chart.StiValueChartMeter[] {
                        this.Item31,
                        this.Item32});
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
                        this.ComboBox2,
                        this.Chart4});
            // 
            // Add to Pages
            // 
            this.Pages.Clear();
            this.Pages.AddRange(new Stimulsoft.Report.Components.StiPage[] {
                        this.Dashboard1});
            this.Dictionary.Relations.Add(this.ParentName);
            this.vi_FullSales.Columns.AddRange(new Stimulsoft.Report.Dictionary.StiDataColumn[] {
                        new Stimulsoft.Report.Dictionary.StiDataColumn("SaleRobot", "SaleRobot", "SaleRobot", typeof(int), "93399b12e3ef46b98aed138a97e97932"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("SalePanel", "SalePanel", "SalePanel", typeof(int), "ae9065bb126c4b83a274350f31141a7a"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerYear", "PerYear", "PerYear", typeof(int), "6c1e1dad2e7d4c90879a18927b7b45cc"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerMonth", "PerMonth", "PerMonth", typeof(int), "6f0479a463034525bb7f2ec0c5045e6c"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("User_ID", "User_ID", "User_ID", typeof(int), "bd982ef5123c4df9a89d7821bb023d56"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PerDay", "PerDay", "PerDay", typeof(int), "4351810f9d7e4e498934d123a6e4a7fe"),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("date", "date", "date", typeof(DateTime), null)});
            this.vi_FullSales.Parameters.AddRange(new Stimulsoft.Report.Dictionary.StiDataParameter[] {
                        new Stimulsoft.Report.Dictionary.StiDataParameter("User_ID", 8, 0, null)});
            this.DataSources.Add(this.vi_FullSales);
            this.tbUsers.Columns.AddRange(new Stimulsoft.Report.Dictionary.StiDataColumn[] {
                        new Stimulsoft.Report.Dictionary.StiDataColumn("User_ID", "User_ID", "User_ID", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Username", "Username", "Username", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Password", "Password", "Password", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("FirstName", "FirstName", "FirstName", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("LastName", "LastName", "LastName", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("PhoneNumber", "PhoneNumber", "PhoneNumber", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Status", "Status", "Status", typeof(bool), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Email", "Email", "Email", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("FK_Server_ID", "FK_Server_ID", "FK_Server_ID", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Token", "Token", "Token", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Limit", "Limit", "Limit", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Wallet", "Wallet", "Wallet", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Role", "Role", "Role", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("TelegramID", "TelegramID", "TelegramID", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("BussinesTitle", "BussinesTitle", "BussinesTitle", typeof(string), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Card Number", "Card Number", "Card Number", typeof(long), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("IsRenew", "IsRenew", "IsRenew", typeof(bool), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("ExpireTimeToken", "ExpireTimeToken", "ExpireTimeToken", typeof(DateTime), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Admin_Telegram_ID", "Admin_Telegram_ID", "Admin_Telegram_ID", typeof(int), null),
                        new Stimulsoft.Report.Dictionary.StiDataColumn("Profile_Filename", "Profile_Filename", "Profile_Filename", typeof(string), null)});
            this.DataSources.Add(this.tbUsers);
            this.Dictionary.Databases.Add(new Stimulsoft.Report.Dictionary.StiSqlDatabase("MS SQL", "MS SQL", "#%#", false, "650d0b0ee28844fcb7e30997a9778e9e"));
            ((Stimulsoft.Report.Dictionary.StiSqlDatabase)(this.Dictionary.Databases["MS SQL"])).ConnectionStringEncrypted = "9NrZocYPYx1FEajATiX2F4NW3YlmUdn181u5rWhc63LRevXPTXZJ7UVH7aVjAKK75nDbK8lx2YFhFaAEZ1xhYsNU0WheWdOZ4QbV";
            this.vi_FullSales.Connecting += new System.EventHandler(this.Getvi_FullSales_SqlCommand);
            this.tbUsers.Connecting += new System.EventHandler(this.GettbUsers_SqlCommand);
        }
        
        public void Getvi_FullSales_SqlCommand(object sender, System.EventArgs e)
        {
            this.vi_FullSales.SqlCommand = "select * from vi_FullSales order by  PerYear desc, PerMonth desc,PerDay desc";
        }
        
        public void GettbUsers_SqlCommand(object sender, System.EventArgs e)
        {
            this.tbUsers.SqlCommand = "select * from tbUsers";
        }
        
        // CheckerInfo: *None* *Relations*
        #region Relation ParentName
        public class ParentNameRelation : Stimulsoft.Report.Dictionary.StiDataRow
        {
            
            public ParentNameRelation(Stimulsoft.Report.Dictionary.StiDataRow dataRow) : 
                    base(dataRow)
            {
            }
            
            public virtual int User_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["User_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Username
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Username"], typeof(string), true)));
                }
            }
            
            public virtual string Password
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Password"], typeof(string), true)));
                }
            }
            
            public virtual string FirstName
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["FirstName"], typeof(string), true)));
                }
            }
            
            public virtual string LastName
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["LastName"], typeof(string), true)));
                }
            }
            
            public virtual string PhoneNumber
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["PhoneNumber"], typeof(string), true)));
                }
            }
            
            public virtual bool Status
            {
                get
                {
                    return ((bool)(StiReport.ChangeType(this["Status"], typeof(bool), true)));
                }
            }
            
            public virtual string Email
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Email"], typeof(string), true)));
                }
            }
            
            public virtual int FK_Server_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["FK_Server_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Token
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Token"], typeof(string), true)));
                }
            }
            
            public virtual int Limit
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Limit"], typeof(int), true)));
                }
            }
            
            public virtual int Wallet
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Wallet"], typeof(int), true)));
                }
            }
            
            public virtual int Role
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Role"], typeof(int), true)));
                }
            }
            
            public virtual string TelegramID
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["TelegramID"], typeof(string), true)));
                }
            }
            
            public virtual string BussinesTitle
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["BussinesTitle"], typeof(string), true)));
                }
            }
            
            public virtual long Card_Number
            {
                get
                {
                    return ((long)(StiReport.ChangeType(this["Card Number"], typeof(long), true)));
                }
            }
            
            public virtual bool IsRenew
            {
                get
                {
                    return ((bool)(StiReport.ChangeType(this["IsRenew"], typeof(bool), true)));
                }
            }
            
            public virtual DateTime ExpireTimeToken
            {
                get
                {
                    return ((DateTime)(StiReport.ChangeType(this["ExpireTimeToken"], typeof(DateTime), true)));
                }
            }
            
            public virtual int Admin_Telegram_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Admin_Telegram_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Profile_Filename
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Profile_Filename"], typeof(string), true)));
                }
            }
        }
        #endregion Relation ParentName
        
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
            
            public virtual int PerMonth
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["PerMonth"], typeof(int), true)));
                }
            }
            
            public virtual int User_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["User_ID"], typeof(int), true)));
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
            
            public new virtual ParentNameRelation Name
            {
                get
                {
                    return new ParentNameRelation(this.GetParentData("Relation"));
                }
            }
        }
        #endregion DataSource vi_FullSales
        
        #region DataSource tbUsers
        public class tbUsersDataSource : Stimulsoft.Report.Dictionary.StiSqlSource
        {
            
            public tbUsersDataSource() : 
                    base("MS SQL", "tbUsers", "tbUsers", "", true, false, 30, "93e508b53aea4de1a38b3201d0672118")
            {
            }
            
            public virtual int User_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["User_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Username
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Username"], typeof(string), true)));
                }
            }
            
            public virtual string Password
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Password"], typeof(string), true)));
                }
            }
            
            public virtual string FirstName
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["FirstName"], typeof(string), true)));
                }
            }
            
            public virtual string LastName
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["LastName"], typeof(string), true)));
                }
            }
            
            public virtual string PhoneNumber
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["PhoneNumber"], typeof(string), true)));
                }
            }
            
            public virtual bool Status
            {
                get
                {
                    return ((bool)(StiReport.ChangeType(this["Status"], typeof(bool), true)));
                }
            }
            
            public virtual string Email
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Email"], typeof(string), true)));
                }
            }
            
            public virtual int FK_Server_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["FK_Server_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Token
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Token"], typeof(string), true)));
                }
            }
            
            public virtual int Limit
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Limit"], typeof(int), true)));
                }
            }
            
            public virtual int Wallet
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Wallet"], typeof(int), true)));
                }
            }
            
            public virtual int Role
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Role"], typeof(int), true)));
                }
            }
            
            public virtual string TelegramID
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["TelegramID"], typeof(string), true)));
                }
            }
            
            public virtual string BussinesTitle
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["BussinesTitle"], typeof(string), true)));
                }
            }
            
            public virtual long Card_Number
            {
                get
                {
                    return ((long)(StiReport.ChangeType(this["Card Number"], typeof(long), true)));
                }
            }
            
            public virtual bool IsRenew
            {
                get
                {
                    return ((bool)(StiReport.ChangeType(this["IsRenew"], typeof(bool), true)));
                }
            }
            
            public virtual DateTime ExpireTimeToken
            {
                get
                {
                    return ((DateTime)(StiReport.ChangeType(this["ExpireTimeToken"], typeof(DateTime), true)));
                }
            }
            
            public virtual int Admin_Telegram_ID
            {
                get
                {
                    return ((int)(StiReport.ChangeType(this["Admin_Telegram_ID"], typeof(int), true)));
                }
            }
            
            public virtual string Profile_Filename
            {
                get
                {
                    return ((string)(StiReport.ChangeType(this["Profile_Filename"], typeof(string), true)));
                }
            }
        }
        #endregion DataSource tbUsers
        #endregion StiReport Designer generated code - do not modify
    }
}
