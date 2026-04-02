(function () {
    const chartEl = document.getElementById("managerStatusChart");
    const revenueChartEl = document.getElementById("managerRevenueChart");

    if (typeof ApexCharts === "undefined") {
        return;
    }

    let statusChart = null;
    let revenueChart = null;

    if (chartEl) {
        const paid = Number(chartEl.getAttribute("data-paid")) || 0;
        const failed = Number(chartEl.getAttribute("data-failed")) || 0;

        const statusOptions = {
            chart: {
                type: "donut",
                height: 320,
                toolbar: {
                    show: false
                }
            },
            series: [paid, failed],
            labels: ["Paid", "Failed"],
            colors: ["#2E8B57", "#C0392B"],
            legend: {
                position: "bottom"
            },
            dataLabels: {
                enabled: true
            },
            stroke: {
                width: 2
            },
            plotOptions: {
                pie: {
                    donut: {
                        size: "60%"
                    }
                }
            }
        };

        statusChart = new ApexCharts(chartEl, statusOptions);
        statusChart.render();
    }

    if (revenueChartEl) {
        let labels = [];
        let series = [];

        try {
            labels = JSON.parse(revenueChartEl.getAttribute("data-labels") || "[]");
            series = JSON.parse(revenueChartEl.getAttribute("data-series") || "[]");
        } catch {
            labels = [];
            series = [];
        }

        const revenueOptions = {
            chart: {
                type: "line",
                height: 340,
                toolbar: {
                    show: false
                }
            },
            series: [
                {
                    name: "Revenue (CAD)",
                    data: series
                }
            ],
            xaxis: {
                categories: labels
            },
            yaxis: {
                labels: {
                    formatter: function (value) {
                        return "$" + Number(value).toFixed(0);
                    }
                }
            },
            stroke: {
                curve: "smooth",
                width: 3
            },
            colors: ["#1F7A8C"],
            markers: {
                size: 4
            },
            grid: {
                borderColor: "#E2E8F0"
            },
            tooltip: {
                y: {
                    formatter: function (value) {
                        return "$" + Number(value).toFixed(2);
                    }
                }
            }
        };

        revenueChart = new ApexCharts(revenueChartEl, revenueOptions);
        revenueChart.render();
    }

    window.addEventListener("sidebar-toggled", function () {
        if (statusChart) {
            statusChart.resize();
        }

        if (revenueChart) {
            revenueChart.resize();
        }
    });
})();
