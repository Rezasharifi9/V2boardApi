/**
 * Management KPI dashboard charts
 */
'use strict';

window.ManagementDashboard = (function () {
    function palette() {
        var labelColor, headingColor, borderColor;
        if (typeof isDarkStyle !== 'undefined' && isDarkStyle) {
            labelColor = config.colors_dark.textMuted;
            headingColor = config.colors_dark.headingColor;
            borderColor = config.colors_dark.borderColor;
        } else {
            labelColor = config.colors.textMuted;
            headingColor = config.colors.headingColor;
            borderColor = config.colors.borderColor;
        }
        return { labelColor: labelColor, headingColor: headingColor, borderColor: borderColor };
    }

    function renderTrend(labels, values) {
        var el = document.querySelector('#mgmtTrendChart');
        if (!el) return;
        var colors = palette();
        new ApexCharts(el, {
            series: [{ name: 'فروش', data: values || [] }],
            chart: {
                height: 300,
                type: 'area',
                parentHeightOffset: 0,
                toolbar: { show: false }
            },
            dataLabels: { enabled: false },
            stroke: { curve: 'smooth', width: 3 },
            colors: [config.colors.primary],
            fill: {
                type: 'gradient',
                gradient: { shadeIntensity: 0.8, opacityFrom: 0.45, opacityTo: 0.08 }
            },
            grid: { borderColor: colors.borderColor, strokeDashArray: 6 },
            xaxis: {
                categories: labels || [],
                labels: { style: { colors: colors.labelColor } },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: {
                labels: {
                    style: { colors: colors.labelColor },
                    formatter: function (val) {
                        return Math.round(val).toLocaleString('fa-IR');
                    }
                }
            },
            tooltip: {
                y: {
                    formatter: function (val) {
                        return Math.round(val).toLocaleString('fa-IR') + ' تومان';
                    }
                }
            }
        }).render();
    }

    function renderDonut(selector, labels, values, colorsList) {
        var el = document.querySelector(selector);
        if (!el) return;
        var colors = palette();
        new ApexCharts(el, {
            series: values || [],
            labels: labels || [],
            chart: { type: 'donut', height: 260, parentHeightOffset: 0 },
            colors: colorsList,
            stroke: { width: 0 },
            dataLabels: { enabled: false },
            legend: {
                position: 'bottom',
                labels: { colors: colors.headingColor }
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: '68%',
                        labels: {
                            show: true,
                            total: {
                                show: true,
                                label: 'جمع',
                                formatter: function (w) {
                                    var total = w.globals.seriesTotals.reduce(function (a, b) { return a + b; }, 0);
                                    return Math.round(total).toLocaleString('fa-IR');
                                }
                            }
                        }
                    }
                }
            }
        }).render();
    }

    function renderBar(labels, values) {
        var el = document.querySelector('#mgmtMixChart');
        if (!el) return;
        var colors = palette();
        new ApexCharts(el, {
            series: [{ name: 'مبلغ', data: values || [] }],
            chart: {
                type: 'bar',
                height: 260,
                parentHeightOffset: 0,
                toolbar: { show: false }
            },
            plotOptions: { bar: { borderRadius: 8, columnWidth: '45%', distributed: true } },
            colors: [config.colors.info, config.colors.success],
            dataLabels: { enabled: false },
            legend: { show: false },
            grid: { borderColor: colors.borderColor, strokeDashArray: 6 },
            xaxis: {
                categories: labels || [],
                labels: { style: { colors: colors.labelColor } },
                axisBorder: { show: false },
                axisTicks: { show: false }
            },
            yaxis: {
                labels: {
                    style: { colors: colors.labelColor },
                    formatter: function (val) {
                        return Math.round(val).toLocaleString('fa-IR');
                    }
                }
            },
            tooltip: {
                y: {
                    formatter: function (val) {
                        return Math.round(val).toLocaleString('fa-IR') + ' تومان';
                    }
                }
            }
        }).render();
    }

    function loadCharts() {
        $.ajax({
            url: '/App/ManagementDashboard/GetCharts',
            type: 'get',
            dataType: 'json',
            success: function (res) {
                if (!res || res.status !== 'success' || !res.data) return;
                var data = res.data;
                renderTrend(data.TrendLabels, data.TrendSales);
                renderDonut('#mgmtChannelChart', data.ChannelLabels, data.ChannelValues, [config.colors.info, config.colors.primary]);
                renderDonut('#mgmtCustomerChart', data.CustomerLabels, data.CustomerValues, [
                    config.colors.success,
                    config.colors.warning,
                    config.colors.danger,
                    config.colors.secondary
                ]);
                renderBar(data.MixLabels, data.MixValues);
            }
        });
    }

    return { loadCharts: loadCharts };
})();
