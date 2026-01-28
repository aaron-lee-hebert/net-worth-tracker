let chartInstance = null;
let forecastData = null;
let currentForecastMonths = 60; // Default 5 years

async function fetchForecastData(forecastMonths) {
    const response = await fetch(`/Forecasts/GetForecastData?forecastMonths=${forecastMonths}`);
    if (!response.ok) {
        throw new Error('Failed to fetch forecast data');
    }
    return await response.json();
}

function formatCurrency(value, decimals = 0) {
    return new Intl.NumberFormat(window.userLocale || 'en-US', {
        style: 'currency',
        currency: window.userCurrency || 'USD',
        minimumFractionDigits: decimals,
        maximumFractionDigits: decimals
    }).format(value);
}

function formatPercent(value) {
    return (value >= 0 ? '+' : '') + value.toFixed(1) + '%';
}

function renderChart(data) {
    const ctx = document.getElementById('forecastChart').getContext('2d');

    if (chartInstance) {
        chartInstance.destroy();
    }

    const historicalQuarters = data.historicalMonths; // This field now represents quarters

    // Build datasets
    const historicalData = [];
    const forecastDataPoints = [];

    for (let i = 0; i < data.labels.length; i++) {
        if (i < historicalQuarters) {
            historicalData.push(data.historicalNetWorth[i] || null);
            forecastDataPoints.push(null);
        } else {
            // Connect forecast to last historical point
            if (i === historicalQuarters && historicalData.length > 0) {
                forecastDataPoints.push(historicalData[historicalData.length - 1]);
            } else {
                forecastDataPoints.push(data.forecastedNetWorth[i] || null);
            }
            historicalData.push(null);
        }
    }

    chartInstance = new Chart(ctx, {
        type: 'line',
        data: {
            labels: data.labels,
            datasets: [
                {
                    label: 'Historical Net Worth',
                    data: historicalData,
                    borderColor: 'rgb(59, 130, 246)',
                    backgroundColor: 'rgba(59, 130, 246, 0.1)',
                    borderWidth: 3,
                    tension: 0.3,
                    fill: true,
                    pointRadius: 2
                },
                {
                    label: 'Projected Net Worth',
                    data: forecastDataPoints,
                    borderColor: 'rgb(16, 185, 129)',
                    backgroundColor: 'rgba(16, 185, 129, 0.1)',
                    borderWidth: 3,
                    borderDash: [5, 5],
                    tension: 0.3,
                    fill: true,
                    pointRadius: 2
                }
            ]
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
                                label += formatCurrency(context.parsed.y);
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
                    },
                    ticks: {
                        maxTicksLimit: 12
                    }
                },
                y: {
                    display: true,
                    title: {
                        display: true,
                        text: 'Net Worth'
                    },
                    ticks: {
                        callback: function(value) {
                            return formatCurrency(value);
                        }
                    }
                }
            }
        }
    });
}

function renderSummary(data) {
    const summary = data.summary;

    document.getElementById('currentNetWorth').textContent = formatCurrency(summary.currentNetWorth);
    document.getElementById('projectedNetWorth').textContent = formatCurrency(summary.projectedNetWorth);
    document.getElementById('projectionDate').textContent = new Date(summary.projectionDate).toLocaleDateString(window.userLocale || 'en-US', { month: 'short', year: 'numeric' });
    document.getElementById('projectedChange').textContent = (summary.projectedChange >= 0 ? '+' : '') + formatCurrency(summary.projectedChange);
    document.getElementById('growthRate').textContent = formatPercent(summary.projectedChangePercent);

    // Update card border color based on projection
    const projectedCard = document.getElementById('projectedCard');
    const isPositive = summary.projectedNetWorth >= summary.currentNetWorth;
    projectedCard.className = 'card h-100';
    projectedCard.style.borderLeft = isPositive
        ? '4px solid var(--color-success)'
        : '4px solid var(--color-danger)';

    // Update projected value color
    const projectedEl = document.getElementById('projectedNetWorth');
    projectedEl.style.color = isPositive ? 'var(--color-success)' : 'var(--color-danger)';

    const changeEl = document.getElementById('projectedChange');
    changeEl.style.color = summary.projectedChange >= 0 ? 'var(--color-success)' : 'var(--color-danger)';

    const growthEl = document.getElementById('growthRate');
    growthEl.style.color = summary.projectedChangePercent >= 0 ? 'var(--color-success)' : 'var(--color-danger)';
}

function renderAccountsTable(data) {
    const tbody = document.getElementById('accountsTableBody');
    tbody.innerHTML = '';

    const assetAccounts = data.accounts.filter(a => !a.isLiability);
    const liabilityAccounts = data.accounts.filter(a => a.isLiability);

    // Assets section
    if (assetAccounts.length > 0) {
        const assetHeader = document.createElement('tr');
        assetHeader.className = 'table-success';
        assetHeader.innerHTML = '<td colspan="7"><strong>Assets</strong></td>';
        tbody.appendChild(assetHeader);

        assetAccounts.forEach(account => {
            const change = account.projectedBalance - account.currentBalance;
            const changePercent = account.currentBalance !== 0
                ? (change / account.currentBalance) * 100
                : 0;

            const row = document.createElement('tr');
            row.innerHTML = `
                <td><a href="/Accounts/Details/${account.id}">${account.name}</a></td>
                <td><span class="badge bg-secondary">${account.type}</span></td>
                <td class="text-end">${formatCurrency(account.currentBalance, 2)}</td>
                <td class="text-end">${formatCurrency(account.projectedBalance, 2)}</td>
                <td class="text-end ${change >= 0 ? 'text-success' : 'text-danger'}">
                    ${change >= 0 ? '+' : ''}${formatCurrency(change, 2)}
                    <small class="d-block">(${formatPercent(changePercent)})</small>
                </td>
                <td class="text-center">
                    ${getTrendIcon(account.trendDirection, false)}
                </td>
                <td>-</td>
            `;
            tbody.appendChild(row);
        });
    }

    // Liabilities section
    if (liabilityAccounts.length > 0) {
        const liabilityHeader = document.createElement('tr');
        liabilityHeader.className = 'table-danger';
        liabilityHeader.innerHTML = '<td colspan="7"><strong>Liabilities</strong></td>';
        tbody.appendChild(liabilityHeader);

        liabilityAccounts.forEach(account => {
            const change = account.projectedBalance - account.currentBalance;
            const changePercent = account.currentBalance !== 0
                ? (change / account.currentBalance) * 100
                : 0;
            const isPayingDown = change < 0;

            const row = document.createElement('tr');
            row.innerHTML = `
                <td><a href="/Accounts/Details/${account.id}">${account.name}</a></td>
                <td><span class="badge bg-danger">${account.type}</span></td>
                <td class="text-end text-danger">${formatCurrency(account.currentBalance, 2)}</td>
                <td class="text-end ${account.projectedBalance === 0 ? 'text-success' : 'text-danger'}">
                    ${formatCurrency(account.projectedBalance, 2)}
                    ${account.projectedBalance === 0 ? '<i class="bi bi-check-circle-fill text-success ms-1"></i>' : ''}
                </td>
                <td class="text-end ${isPayingDown ? 'text-success' : 'text-danger'}">
                    ${change >= 0 ? '+' : ''}${formatCurrency(change, 2)}
                    <small class="d-block">(${formatPercent(changePercent)})</small>
                </td>
                <td class="text-center">
                    ${getTrendIcon(account.trendDirection, true, isPayingDown)}
                </td>
                <td>${getPayoffDateBadge(account)}</td>
            `;
            tbody.appendChild(row);
        });
    }

    // Update footer totals
    document.getElementById('footerCurrentNetWorth').textContent = formatCurrency(data.summary.currentNetWorth, 2);
    document.getElementById('footerProjectedNetWorth').textContent = formatCurrency(data.summary.projectedNetWorth, 2);

    const footerChange = document.getElementById('footerProjectedChange');
    footerChange.textContent = (data.summary.projectedChange >= 0 ? '+' : '') + formatCurrency(data.summary.projectedChange, 2);
    footerChange.className = data.summary.projectedChange >= 0 ? 'text-success' : 'text-danger';
}

function getTrendIcon(direction, isLiability, isPayingDown = false) {
    if (isLiability) {
        if (isPayingDown) {
            return '<i class="bi bi-arrow-down-circle-fill text-success fs-5" title="Paying Down"></i>';
        } else {
            return '<i class="bi bi-arrow-up-circle-fill text-danger fs-5" title="Increasing"></i>';
        }
    }

    switch (direction) {
        case 'up':
            return '<i class="bi bi-arrow-up-circle-fill text-success fs-5" title="Trending Up"></i>';
        case 'down':
            return '<i class="bi bi-arrow-down-circle-fill text-danger fs-5" title="Trending Down"></i>';
        default:
            return '<i class="bi bi-dash-circle-fill text-secondary fs-5" title="Stable"></i>';
    }
}

function getPayoffDateBadge(account) {
    if (account.payoffDate) {
        const date = new Date(account.payoffDate);
        return `<span class="badge bg-success"><i class="bi bi-calendar-check me-1"></i>${date.toLocaleDateString(window.userLocale || 'en-US', { month: 'short', year: 'numeric' })}</span>`;
    } else if (account.projectedBalance === 0) {
        return '<span class="badge bg-success">Paid Off!</span>';
    }
    return '<span class="text-muted">-</span>';
}

function setForecastPeriod(months) {
    currentForecastMonths = months;

    // Update button states
    document.querySelectorAll('.forecast-period-btn').forEach(btn => {
        btn.classList.remove('btn-primary');
        btn.classList.add('btn-outline-primary');
        if (parseInt(btn.dataset.months) === months) {
            btn.classList.remove('btn-outline-primary');
            btn.classList.add('btn-primary');
        }
    });

    loadForecastData();
}

async function loadForecastData() {
    const loadingEl = document.getElementById('forecastLoading');
    const contentEl = document.getElementById('forecastContent');
    const noDataEl = document.getElementById('noForecastData');

    loadingEl.classList.remove('d-none');
    contentEl.classList.add('d-none');
    noDataEl.classList.add('d-none');

    try {
        forecastData = await fetchForecastData(currentForecastMonths);

        loadingEl.classList.add('d-none');

        if (!forecastData.accounts || forecastData.accounts.length === 0) {
            noDataEl.classList.remove('d-none');
        } else {
            contentEl.classList.remove('d-none');
            renderSummary(forecastData);
            renderChart(forecastData);
            renderAccountsTable(forecastData);
        }
    } catch (error) {
        console.error('Error loading forecast data:', error);
        loadingEl.classList.add('d-none');
        noDataEl.classList.remove('d-none');
        noDataEl.querySelector('p').textContent = 'Error loading forecast data. Please try again.';
    }
}

async function loadAssumptions() {
    try {
        const response = await fetch('/Forecasts/GetAssumptions');
        if (!response.ok) throw new Error('Failed to load assumptions');
        const data = await response.json();

        document.getElementById('investmentRate').value = data.investmentGrowthRate;
        document.getElementById('realEstateRate').value = data.realEstateGrowthRate;
        document.getElementById('bankingRate').value = data.bankingGrowthRate;
        document.getElementById('businessRate').value = data.businessGrowthRate;
        document.getElementById('vehicleRate').value = data.vehicleDepreciationRate;

        // Show badge if custom overrides are active
        const badge = document.getElementById('customBadge');
        if (data.hasCustomOverrides) {
            badge.classList.remove('d-none');
        } else {
            badge.classList.add('d-none');
        }
    } catch (error) {
        console.error('Error loading assumptions:', error);
    }
}

async function saveAssumptions() {
    const btn = document.getElementById('saveAssumptionsBtn');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Saving...';

    try {
        const assumptions = {
            investmentGrowthRate: parseFloat(document.getElementById('investmentRate').value) || 7,
            realEstateGrowthRate: parseFloat(document.getElementById('realEstateRate').value) || 2,
            bankingGrowthRate: parseFloat(document.getElementById('bankingRate').value) || 0.5,
            businessGrowthRate: parseFloat(document.getElementById('businessRate').value) || 3,
            vehicleDepreciationRate: parseFloat(document.getElementById('vehicleRate').value) || 15
        };

        const response = await fetch('/Forecasts/SaveAssumptions', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            },
            body: JSON.stringify(assumptions)
        });

        if (!response.ok) throw new Error('Failed to save assumptions');

        // Reload the forecast with new assumptions
        await loadForecastData();
        await loadAssumptions();

        btn.innerHTML = '<i class="bi bi-check-lg"></i> Saved!';
        setTimeout(() => {
            btn.innerHTML = originalText;
            btn.disabled = false;
        }, 1500);
    } catch (error) {
        console.error('Error saving assumptions:', error);
        btn.innerHTML = originalText;
        btn.disabled = false;
        alert('Failed to save assumptions. Please try again.');
    }
}

async function resetAssumptions() {
    const btn = document.getElementById('resetAssumptionsBtn');
    const originalText = btn.innerHTML;
    btn.disabled = true;
    btn.innerHTML = '<span class="spinner-border spinner-border-sm me-1"></span> Resetting...';

    try {
        const response = await fetch('/Forecasts/ResetAssumptions', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'RequestVerificationToken': document.querySelector('input[name="__RequestVerificationToken"]')?.value || ''
            }
        });

        if (!response.ok) throw new Error('Failed to reset assumptions');

        // Reload assumptions and forecast
        await loadAssumptions();
        await loadForecastData();

        btn.innerHTML = '<i class="bi bi-check-lg"></i> Reset!';
        setTimeout(() => {
            btn.innerHTML = originalText;
            btn.disabled = false;
        }, 1500);
    } catch (error) {
        console.error('Error resetting assumptions:', error);
        btn.innerHTML = originalText;
        btn.disabled = false;
        alert('Failed to reset assumptions. Please try again.');
    }
}

document.addEventListener('DOMContentLoaded', function() {
    // Get initial forecast months from page
    const initialMonths = parseInt(document.body.dataset.forecastMonths) || 60;
    currentForecastMonths = initialMonths;

    // Set up period button handlers
    document.querySelectorAll('.forecast-period-btn').forEach(btn => {
        btn.addEventListener('click', function() {
            setForecastPeriod(parseInt(this.dataset.months));
        });
    });

    // Set up assumption button handlers
    const saveBtn = document.getElementById('saveAssumptionsBtn');
    if (saveBtn) {
        saveBtn.addEventListener('click', saveAssumptions);
    }

    const resetBtn = document.getElementById('resetAssumptionsBtn');
    if (resetBtn) {
        resetBtn.addEventListener('click', resetAssumptions);
    }

    // Initial load
    loadForecastData();
    loadAssumptions();
});
