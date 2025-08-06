const loadChartJs = (function () {
    let promise = null;
    return function () {
        if (!promise) {
            promise = new Promise((resolve, reject) => {
                // 이미 로드되었는지 확인
                if (document.querySelector(`script[src*="chart.umd.min.js"]`)) {
                    return resolve();
                }
                const script = document.createElement('script');
                script.src = "https://cdn.jsdelivr.net/npm/chart.js@4.4.1/dist/chart.umd.min.js";
                script.onload = () => resolve();
                script.onerror = () => reject(new Error("Chart.js 스크립트 로드 실패"));
                document.head.appendChild(script);
            });
        }
        return promise;
    };
})();

const chartInstances = {};

function getChartColors() {
    const rootStyles = getComputedStyle(document.documentElement);
    return {
        textColor: rootStyles.getPropertyValue('--bs-body-color').trim(),
        borderColor: rootStyles.getPropertyValue('--bs-border-color').trim()
    };
}

async function createOrUpdateChart(canvasId, chartType, chartData, chartOptions = {}) {
    try {
        await loadChartJs(); // Chart.js가 로드될 때까지 기다립니다.

        const ctx = document.getElementById(canvasId)?.getContext('2d');
        if (!ctx) {
            return;
        }

        // 동일한 canvas에 이미 차트가 그려져 있다면 파괴(destroy)합니다.
        if (chartInstances[canvasId]) {
            chartInstances[canvasId].destroy();
        }

        // 새 차트를 생성하고 인스턴스를 저장합니다.
        chartInstances[canvasId] = new Chart(ctx, {
            type: chartType,
            data: chartData,
            options: chartOptions,
        });
    } catch (error) {
        console.error("Chart rendering failed:", error);
    }
}

// C#에서 호출할 라인 차트 전용 함수
export function renderVerticalBarChart(dotNetHelper, canvasId, chartData) {
    const colors = getChartColors();
    
    const options = {
        responsive: true,
        maintainAspectRatio: false,
        plugins: { legend: { display: false } },
        scales: {
            y: {
                ticks: {
                    callback: function (value) {
                        return new Intl.NumberFormat('ko-KR').format(value);
                    },
                    color: colors.textColor
                },
                grid: {
                    color: colors.borderColor
                },
                border: {
                    color: colors.borderColor
                }
            },
            x: {
                ticks: {
                    font: { size: 11 },
                    color: colors.textColor
                },
                grid: { display: false },
                border: {
                    color: colors.borderColor
                }
            }
        },
        onClick: (event, elements) => {
            if (elements.length > 0) {
                const clickedIndex = elements[0].index;
                dotNetHelper.invokeMethodAsync('HandleChartClick', clickedIndex);
            }
        }
    };
    createOrUpdateChart(canvasId, 'bar', chartData, options);
}

export function renderBarChart(canvasId, chartData) {
    const colors = getChartColors();
    
    const options = {
        indexAxis: 'y',
        responsive: true,
        maintainAspectRatio: false,
        plugins: {
            legend: { display: false },
        },
        scales: {
            x: {
                beginAtZero: true,
                ticks: { color: colors.textColor },
                grid: { color: colors.borderColor }
            },
            y: {
                ticks: { color: colors.textColor }
            }
        }
    };
    createOrUpdateChart(canvasId, 'bar', chartData, options);
}
const themeObserver = new MutationObserver((mutationsList) => {
    for (const mutation of mutationsList) {
        if (mutation.type === 'attributes' && mutation.attributeName === 'data-bs-theme') {
            const newColors = getChartColors();
            
            for (const canvasId in chartInstances) {
                const chart = chartInstances[canvasId];
                if (chart) {
                    Object.values(chart.options.scales).forEach(scale => {
                        if (scale.ticks) {
                            scale.ticks.color = newColors.textColor;
                        }
                        if (scale.grid) {
                            scale.grid.color = newColors.borderColor;
                        }
                    });
                    
                    chart.update();
                }
            }
        }
    }
});

themeObserver.observe(document.documentElement, { attributes: true });