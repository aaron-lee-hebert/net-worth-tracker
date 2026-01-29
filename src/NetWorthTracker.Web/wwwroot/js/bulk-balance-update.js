// Bulk Balance Update Modal JavaScript
(function () {
    'use strict';

    let accountsData = [];
    let changedAccounts = new Set();

    // Initialize when DOM is ready
    document.addEventListener('DOMContentLoaded', function () {
        const modal = document.getElementById('bulkUpdateModal');
        if (!modal) return;

        // Set default date to today
        const dateInput = document.getElementById('bulkUpdateDate');
        if (dateInput) {
            dateInput.value = new Date().toISOString().split('T')[0];
        }

        // Load accounts when modal opens
        modal.addEventListener('show.bs.modal', loadAccounts);

        // Handle save button
        const saveBtn = document.getElementById('bulkUpdateSaveBtn');
        if (saveBtn) {
            saveBtn.addEventListener('click', saveChanges);
        }
    });

    async function loadAccounts() {
        const accountsList = document.getElementById('bulkUpdateAccountsList');
        const loadingEl = document.getElementById('bulkUpdateLoading');
        const errorEl = document.getElementById('bulkUpdateError');
        const saveBtn = document.getElementById('bulkUpdateSaveBtn');

        // Show loading state
        loadingEl.classList.remove('d-none');
        accountsList.classList.add('d-none');
        errorEl.classList.add('d-none');
        saveBtn.disabled = true;

        try {
            const response = await fetch('/Dashboard/GetAccountsForBulkUpdate');
            if (!response.ok) {
                throw new Error('Failed to load accounts');
            }

            accountsData = await response.json();
            changedAccounts.clear();
            renderAccounts();

            loadingEl.classList.add('d-none');
            accountsList.classList.remove('d-none');
            updateSummary();
        } catch (error) {
            console.error('Error loading accounts:', error);
            loadingEl.classList.add('d-none');
            errorEl.classList.remove('d-none');
            errorEl.textContent = 'Failed to load accounts. Please try again.';
        }
    }

    function renderAccounts() {
        const accountsList = document.getElementById('bulkUpdateAccountsList');
        accountsList.innerHTML = '';

        if (accountsData.length === 0) {
            accountsList.innerHTML = '<p class="text-muted text-center py-4">No accounts found.</p>';
            return;
        }

        // Group accounts by category
        const grouped = {};
        accountsData.forEach(account => {
            if (!grouped[account.category]) {
                grouped[account.category] = {
                    accounts: [],
                    order: account.categoryOrder,
                    isLiability: account.isLiability
                };
            }
            grouped[account.category].accounts.push(account);
        });

        // Sort categories by order
        const sortedCategories = Object.entries(grouped)
            .sort((a, b) => a[1].order - b[1].order);

        sortedCategories.forEach(([category, data]) => {
            // Category header
            const headerDiv = document.createElement('div');
            headerDiv.className = 'category-header bg-light px-3 py-2 fw-semibold text-uppercase small';
            headerDiv.style.letterSpacing = '0.05em';
            headerDiv.textContent = category;
            accountsList.appendChild(headerDiv);

            // Account rows
            data.accounts.forEach(account => {
                const row = createAccountRow(account, data.isLiability);
                accountsList.appendChild(row);
            });
        });
    }

    function createAccountRow(account, isLiability) {
        const row = document.createElement('div');
        row.className = 'account-row d-flex align-items-center px-3 py-2 border-bottom';
        row.dataset.accountId = account.id;

        const nameCol = document.createElement('div');
        nameCol.className = 'flex-grow-1';
        nameCol.innerHTML = `
            <div class="fw-medium">${escapeHtml(account.name)}</div>
            ${account.institution ? `<small class="text-muted">${escapeHtml(account.institution)}</small>` : ''}
        `;

        const currentCol = document.createElement('div');
        currentCol.className = 'text-end me-3';
        currentCol.style.minWidth = '100px';
        currentCol.innerHTML = `<span class="${isLiability ? 'text-danger' : ''}">${formatCurrency(account.currentBalance)}</span>`;

        const inputCol = document.createElement('div');
        inputCol.style.width = '140px';

        const input = document.createElement('input');
        input.type = 'number';
        input.step = '0.01';
        input.className = 'form-control form-control-sm text-end balance-input';
        input.value = account.currentBalance.toFixed(2);
        input.dataset.accountId = account.id;
        input.dataset.originalValue = account.currentBalance.toFixed(2);

        input.addEventListener('input', function () {
            handleBalanceChange(this, account.id);
        });

        input.addEventListener('blur', function () {
            // Format to 2 decimal places on blur
            if (this.value && !isNaN(parseFloat(this.value))) {
                this.value = parseFloat(this.value).toFixed(2);
            }
        });

        inputCol.appendChild(input);

        row.appendChild(nameCol);
        row.appendChild(currentCol);
        row.appendChild(inputCol);

        return row;
    }

    function handleBalanceChange(input, accountId) {
        const originalValue = parseFloat(input.dataset.originalValue);
        const newValue = parseFloat(input.value);

        if (isNaN(newValue)) {
            input.classList.remove('border-success', 'border-warning');
            changedAccounts.delete(accountId);
        } else if (Math.abs(newValue - originalValue) < 0.005) {
            // Same value (within rounding tolerance)
            input.classList.remove('border-success', 'border-warning');
            changedAccounts.delete(accountId);
        } else {
            // Changed value
            input.classList.add('border-success');
            input.classList.remove('border-warning');
            changedAccounts.add(accountId);
        }

        updateSummary();
    }

    function updateSummary() {
        const summaryEl = document.getElementById('bulkUpdateSummary');
        const saveBtn = document.getElementById('bulkUpdateSaveBtn');
        const count = changedAccounts.size;

        if (count === 0) {
            summaryEl.textContent = 'No changes';
            saveBtn.disabled = true;
        } else if (count === 1) {
            summaryEl.textContent = '1 account will be updated';
            saveBtn.disabled = false;
        } else {
            summaryEl.textContent = `${count} accounts will be updated`;
            saveBtn.disabled = false;
        }
    }

    async function saveChanges() {
        const saveBtn = document.getElementById('bulkUpdateSaveBtn');
        const originalText = saveBtn.innerHTML;

        if (changedAccounts.size === 0) return;

        // Show loading state
        saveBtn.disabled = true;
        saveBtn.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status"></span>Saving...';

        try {
            const dateInput = document.getElementById('bulkUpdateDate');
            const notesInput = document.getElementById('bulkUpdateNotes');

            // Collect changed accounts
            const accounts = [];
            changedAccounts.forEach(accountId => {
                const input = document.querySelector(`.balance-input[data-account-id="${accountId}"]`);
                if (input) {
                    accounts.push({
                        accountId: accountId,
                        newBalance: parseFloat(input.value)
                    });
                }
            });

            // Get CSRF token
            const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value;

            const response = await fetch('/Dashboard/BulkUpdateBalances', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': token
                },
                body: JSON.stringify({
                    recordedAt: dateInput.value,
                    notes: notesInput.value || null,
                    accounts: accounts
                })
            });

            if (!response.ok) {
                throw new Error('Failed to save changes');
            }

            const result = await response.json();

            if (result.success) {
                // Close modal and refresh dashboard via AJAX
                const modal = bootstrap.Modal.getInstance(document.getElementById('bulkUpdateModal'));
                modal.hide();
                await refreshDashboard();
            } else {
                throw new Error(result.message || 'Failed to save changes');
            }
        } catch (error) {
            console.error('Error saving changes:', error);
            alert('Failed to save changes. Please try again.');
            saveBtn.disabled = false;
            saveBtn.innerHTML = originalText;
        }
    }

    function formatCurrency(value) {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(value);
    }

    function escapeHtml(text) {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    }

    async function refreshDashboard() {
        try {
            // Fetch updated dashboard data
            const response = await fetch('/Dashboard/GetDashboardSummary');
            if (!response.ok) {
                throw new Error('Failed to fetch dashboard data');
            }

            const data = await response.json();

            // Update summary cards
            updateSummaryCards(data);

            // Update recent accounts table
            updateRecentAccountsTable(data.recentAccounts);

            // Refresh the chart (if loadChartData is available from dashboard-chart.js)
            if (typeof loadChartData === 'function') {
                loadChartData();
            }
        } catch (error) {
            console.error('Error refreshing dashboard:', error);
            // Fallback to page reload if AJAX refresh fails
            window.location.reload();
        }
    }

    function updateSummaryCards(data) {
        // Update Net Worth card using specific ID
        const netWorthEl = document.getElementById('netWorthValue');
        if (netWorthEl) {
            netWorthEl.textContent = formatCurrency(data.totalNetWorth);
        }

        // Update Total Assets card using specific ID
        const totalAssetsEl = document.getElementById('totalAssetsValue');
        if (totalAssetsEl) {
            totalAssetsEl.textContent = formatCurrency(data.totalAssets);
        }

        // Update Total Liabilities card using specific ID
        const totalLiabilitiesEl = document.getElementById('totalLiabilitiesValue');
        if (totalLiabilitiesEl) {
            totalLiabilitiesEl.textContent = formatCurrency(data.totalLiabilities);
        }

        // Update category cards
        const categoryCards = document.querySelectorAll('.col-lg-3.col-md-4.col-sm-6 .card');
        categoryCards.forEach(card => {
            const categoryName = card.querySelector('.card-subtitle')?.textContent?.trim();
            if (categoryName && data.totalsByCategory[categoryName] !== undefined) {
                const valueEl = card.querySelector('.card-title');
                if (valueEl) {
                    valueEl.textContent = formatCurrency(data.totalsByCategory[categoryName]);
                }
            }
        });

        // Update asset allocation percentages
        if (data.totalAssets > 0) {
            const progressBars = document.querySelectorAll('.col-lg-4 .card .mb-3');
            progressBars.forEach(item => {
                const categoryLabel = item.querySelector('span:first-child')?.textContent?.trim();
                if (categoryLabel && data.totalsByCategory[categoryLabel] !== undefined) {
                    const categoryValue = data.totalsByCategory[categoryLabel];
                    const percentage = (categoryValue / data.totalAssets) * 100;

                    const percentageEl = item.querySelector('span:last-child');
                    if (percentageEl) {
                        percentageEl.textContent = `${percentage.toFixed(1)}%`;
                    }

                    const progressBar = item.querySelector('.progress-bar');
                    if (progressBar) {
                        progressBar.style.width = `${percentage.toFixed(1)}%`;
                    }
                }
            });
        }
    }

    function updateRecentAccountsTable(accounts) {
        const tbody = document.getElementById('recentAccountsTableBody');
        if (!tbody || !accounts || accounts.length === 0) return;

        tbody.innerHTML = '';

        accounts.forEach(account => {
            const row = document.createElement('tr');
            row.innerHTML = `
                <td>${escapeHtml(account.name)}</td>
                <td>
                    <span class="badge ${getBadgeClass(account.accountTypeCategory)}">
                        ${escapeHtml(account.accountType)}
                    </span>
                </td>
                <td>${account.institution ? escapeHtml(account.institution) : '-'}</td>
                <td class="text-end ${account.isLiability ? 'text-danger' : ''}">
                    ${formatCurrency(account.currentBalance)}
                </td>
                <td class="text-end">
                    <a href="/Accounts/Details/${account.id}" class="btn btn-sm btn-outline-primary">
                        <i class="bi bi-eye"></i>
                    </a>
                </td>
            `;
            tbody.appendChild(row);
        });
    }

    function getBadgeClass(categoryKey) {
        const badgeClasses = {
            'Banking': 'bg-primary',
            'Investment': 'bg-info',
            'RealEstate': 'bg-warning text-dark',
            'VehiclesAndProperty': 'bg-secondary',
            'Business': 'bg-success',
            'SecuredDebt': 'bg-danger',
            'UnsecuredDebt': 'bg-danger',
            'OtherLiabilities': 'bg-danger'
        };
        return badgeClasses[categoryKey] || 'bg-secondary';
    }
})();
