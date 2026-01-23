let chartInstance = null;
let chartData = null;
let currentViewMode = 'networth';

// Asset categories (will be stacked above zero)
const assetCategories = ['Banking', 'Investment', 'Real Estate', 'Vehicles & Property', 'Business'];
// Liability categories (will be shown below zero)
const liabilityCategories = ['Secured Debt', 'Unsecured Debt', 'Other Liabilities'];

// Old Money color palette - sophisticated, muted tones
const chartColors = {
    netWorth: {
        border: 'rgb(26, 60, 52)',      // Deep forest green
        background: 'rgba(26, 60, 52, 0.2)'
    },
    categories: {
        // Assets - refined greens and blues
        'Banking': { border: 'rgb(45, 90, 78)', background: 'rgba(45, 90, 78, 0.5)' },
        'Investment': { border: 'rgb(74, 111, 165)', background: 'rgba(74, 111, 165, 0.5)' },
        'Real Estate': { border: 'rgb(139, 119, 101)', background: 'rgba(139, 119, 101, 0.5)' },
        'Vehicles & Property': { border: 'rgb(184, 134, 11)', background: 'rgba(184, 134, 11, 0.5)' },
        'Business': { border: 'rgb(85, 107, 85)', background: 'rgba(85, 107, 85, 0.5)' },
        // Liabilities - muted reds and burgundy
        'Secured Debt': { border: 'rgb(139, 58, 58)', background: 'rgba(139, 58, 58, 0.5)' },
        'Unsecured Debt': { border: 'rgb(165, 85, 74)', background: 'rgba(165, 85, 74, 0.5)' },
        'Other Liabilities': { border: 'rgb(128, 70, 70)', background: 'rgba(128, 70, 70, 0.5)' }
    },
    // For individual accounts - distinguished, classic colors
    assets: [
        { border: 'rgb(26, 60, 52)', background: 'rgba(26, 60, 52, 0.4)' },       // Forest green
        { border: 'rgb(74, 111, 165)', background: 'rgba(74, 111, 165, 0.4)' },   // Steel blue
        { border: 'rgb(184, 134, 11)', background: 'rgba(184, 134, 11, 0.4)' },   // Antique gold
        { border: 'rgb(85, 107, 85)', background: 'rgba(85, 107, 85, 0.4)' },     // Sage
        { border: 'rgb(139, 119, 101)', background: 'rgba(139, 119, 101, 0.4)' }, // Taupe
        { border: 'rgb(70, 90, 100)', background: 'rgba(70, 90, 100, 0.4)' },     // Slate
        { border: 'rgb(45, 90, 78)', background: 'rgba(45, 90, 78, 0.4)' },       // Teal
        { border: 'rgb(100, 80, 60)', background: 'rgba(100, 80, 60, 0.4)' },     // Bronze
        { border: 'rgb(80, 100, 80)', background: 'rgba(80, 100, 80, 0.4)' },     // Moss
        { border: 'rgb(90, 90, 110)', background: 'rgba(90, 90, 110, 0.4)' }      // Pewter
    ],
    liabilities: [
        { border: 'rgb(139, 58, 58)', background: 'rgba(139, 58, 58, 0.4)' },     // Burgundy
        { border: 'rgb(165, 85, 74)', background: 'rgba(165, 85, 74, 0.4)' },     // Terracotta
        { border: 'rgb(128, 70, 70)', background: 'rgba(128, 70, 70, 0.4)' },     // Maroon
        { border: 'rgb(150, 100, 80)', background: 'rgba(150, 100, 80, 0.4)' },   // Rust
        { border: 'rgb(120, 80, 80)', background: 'rgba(120, 80, 80, 0.4)' }      // Mahogany
    ]
};

async function fetchChartData(startDate, endDate) {
    const params = new URLSearchParams();
    if (startDate) params.append('startDate', startDate);
    if (endDate) params.append('endDate', endDate);

    const response = await fetch(`/api/chartdata/history?${params.toString()}`);
    if (!response.ok) {
        throw new Error('Failed to fetch chart data');
    }
    return await response.json();
}

function renderChart(data, viewMode) {
    const ctx = document.getElementById('historyChart').getContext('2d');

    if (chartInstance) {
        chartInstance.destroy();
    }

    const { datasets, isStacked } = getDatasets(data, viewMode);

    chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: datasets
        },
        options: {
            responsive: true,
            maintainAspectRatio: false,
            interaction: {
                mode: 'index',
                intersect: false,
            },
            plugins: {
                legend: {
                    position: 'bottom',
                },
                tooltip: {
                    callbacks: {
                        label: function(context) {
                            let label = context.dataset.label || '';
                            if (label) {
                                label += ': ';
                            }
                            if (context.parsed.y !== null) {
                                // Show absolute value for liabilities in tooltip
                                const value = context.parsed.y;
                                label += new Intl.NumberFormat('en-US', {
                                    style: 'currency',
                                    currency: 'USD'
                                }).format(Math.abs(value));
                                if (value < 0) {
                                    label += ' (liability)';
                                }
                            }
                            return label;
                        }
                    }
                }
            },
            scales: {
                x: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Date'
                    }
                },
                y: {
                    display: true,
                    stacked: isStacked,
                    title: {
                        display: true,
                        text: 'Balance'
                    },
                    ticks: {
                        callback: function(value) {
                            return new Intl.NumberFormat('en-US', {
                                style: 'currency',
                                currency: 'USD',
                                minimumFractionDigits: 0,
                                maximumFractionDigits: 0
                            }).format(value);
                        }
                    }
                }
            }
        }
    });
}

function getDatasets(data, viewMode) {
    const defaultColor = { border: 'rgb(128, 128, 128)', background: 'rgba(128, 128, 128, 0.5)' };

    switch (viewMode) {
        case 'individual':
            // Individual accounts - stacked area chart
            // Separate assets and liabilities
            const assetAccounts = data.accounts.filter(a => a.isAsset);
            const liabilityAccounts = data.accounts.filter(a => !a.isAsset);

            const individualDatasets = [];

            // Add asset accounts (positive, stacked)
            assetAccounts.forEach((account, index) => {
                const color = chartColors.assets[index % chartColors.assets.length];
                individualDatasets.push({
                    label: account.name,
                    data: account.data,
                    borderColor: color.border,
                    backgroundColor: color.background,
                    borderWidth: 2,
                    tension: 0.3,
                    fill: true,
                    stack: 'assets'
                });
            });

            // Add liability accounts (negative, stacked)
            liabilityAccounts.forEach((account, index) => {
                const color = chartColors.liabilities[index % chartColors.liabilities.length];
                individualDatasets.push({
                    label: account.name,
                    data: account.data.map(v => -v), // Negate for display below zero
                    borderColor: color.border,
                    backgroundColor: color.background,
                    borderWidth: 2,
                    tension: 0.3,
                    fill: true,
                    stack: 'liabilities'
                });
            });

            return { datasets: individualDatasets, isStacked: true };

        case 'bytype':
            const byTypeDatasets = [];

            // Add asset categories (positive values, stacked)
            assetCategories.forEach(category => {
                const values = data.byType[category] || [];
                if (values.some(v => v !== 0)) {
                    const color = chartColors.categories[category] || defaultColor;
                    byTypeDatasets.push({
                        label: category,
                        data: values,
                        borderColor: color.border,
                        backgroundColor: color.background,
                        borderWidth: 2,
                        tension: 0.3,
                        fill: true,
                        stack: 'assets'
                    });
                }
            });

            // Add liability categories (negative values, stacked below zero)
            liabilityCategories.forEach(category => {
                const values = data.byType[category] || [];
                if (values.some(v => v !== 0)) {
                    const color = chartColors.categories[category] || defaultColor;
                    byTypeDatasets.push({
                        label: category,
                        data: values.map(v => -v), // Negate for display below zero
                        borderColor: color.border,
                        backgroundColor: color.background,
                        borderWidth: 2,
                        tension: 0.3,
                        fill: true,
                        stack: 'liabilities'
                    });
                }
            });

            return { datasets: byTypeDatasets, isStacked: true };

        case 'networth':
        default:
            return {
                datasets: [{
                    label: 'Net Worth',
                    data: data.netWorth,
                    borderColor: chartColors.netWorth.border,
                    backgroundColor: chartColors.netWorth.background,
                    borderWidth: 2,
                    tension: 0.3,
                    fill: true
                }],
                isStacked: false
            };
    }
}

function setDateRange(preset) {
    const endDate = new Date();
    let startDate = new Date();

    switch (preset) {
        case '1m':
            startDate.setMonth(endDate.getMonth() - 1);
            break;
        case '3m':
            startDate.setMonth(endDate.getMonth() - 3);
            break;
        case '6m':
            startDate.setMonth(endDate.getMonth() - 6);
            break;
        case '1y':
            startDate.setFullYear(endDate.getFullYear() - 1);
            break;
        case 'ytd':
            startDate = new Date(endDate.getFullYear(), 0, 1);
            break;
        case 'all':
            startDate = new Date(2000, 0, 1);
            break;
    }

    document.getElementById('startDate').value = formatDate(startDate);
    document.getElementById('endDate').value = formatDate(endDate);

    updatePresetButtons(preset);
    loadChartData();
}

function formatDate(date) {
    return date.toISOString().split('T')[0];
}

function updatePresetButtons(activePreset) {
    document.querySelectorAll('.date-preset-btn').forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.preset === activePreset) {
            btn.classList.add('active');
        }
    });
}

function setViewMode(mode) {
    currentViewMode = mode;

    document.querySelectorAll('.view-mode-btn').forEach(btn => {
        btn.classList.remove('active');
        if (btn.dataset.mode === mode) {
            btn.classList.add('active');
        }
    });

    if (chartData) {
        renderChart(chartData, currentViewMode);
    }
}

async function loadChartData() {
    const startDate = document.getElementById('startDate').value;
    const endDate = document.getElementById('endDate').value;

    const loadingEl = document.getElementById('chartLoading');
    const chartContainer = document.getElementById('chartContainer');
    const noDataEl = document.getElementById('noChartData');

    loadingEl.classList.remove('d-none');
    chartContainer.classList.add('d-none');
    noDataEl.classList.add('d-none');

    try {
        chartData = await fetchChartData(startDate, endDate);

        loadingEl.classList.add('d-none');

        if (chartData.labels.length === 0) {
            noDataEl.classList.remove('d-none');
        } else {
            chartContainer.classList.remove('d-none');
            renderChart(chartData, currentViewMode);
        }
    } catch (error) {
        console.error('Error loading chart data:', error);
        loadingEl.classList.add('d-none');
        noDataEl.classList.remove('d-none');
        noDataEl.querySelector('p').textContent = 'Error loading chart data. Please try again.';
    }
}

document.addEventListener('DOMContentLoaded', function() {
    document.querySelectorAll('.view-mode-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            setViewMode(this.dataset.mode);
        });
    });

    document.querySelectorAll('.date-preset-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            setDateRange(this.dataset.preset);
        });
    });

    document.getElementById('startDate').addEventListener('change', function() {
        updatePresetButtons(null);
        loadChartData();
    });

    document.getElementById('endDate').addEventListener('change', function() {
        updatePresetButtons(null);
        loadChartData();
    });

    setDateRange('all');
});
