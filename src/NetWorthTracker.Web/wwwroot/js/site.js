// Site-wide JavaScript utilities

(function () {
    'use strict';

    // =========================================================================
    // Toast Notifications
    // Usage: window.showToast('Message text', 'success|danger|warning|info')
    // =========================================================================
    function getOrCreateToastContainer() {
        var container = document.getElementById('toastContainer');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toastContainer';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '1090';
            document.body.appendChild(container);
        }
        return container;
    }

    var toastIcons = {
        success: 'bi-check-circle-fill',
        danger: 'bi-exclamation-triangle-fill',
        warning: 'bi-exclamation-circle-fill',
        info: 'bi-info-circle-fill'
    };

    window.showToast = function (message, type) {
        type = type || 'info';
        var container = getOrCreateToastContainer();
        var icon = toastIcons[type] || toastIcons.info;

        var toastEl = document.createElement('div');
        toastEl.className = 'toast align-items-center text-bg-' + type + ' border-0';
        toastEl.setAttribute('role', 'alert');
        toastEl.setAttribute('aria-live', 'assertive');
        toastEl.setAttribute('aria-atomic', 'true');
        toastEl.innerHTML =
            '<div class="d-flex">' +
                '<div class="toast-body">' +
                    '<i class="bi ' + icon + ' me-2"></i>' + message +
                '</div>' +
                '<button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>' +
            '</div>';

        container.appendChild(toastEl);

        var toast = new bootstrap.Toast(toastEl, { delay: 5000 });
        toast.show();

        toastEl.addEventListener('hidden.bs.toast', function () {
            toastEl.remove();
        });
    };

    // =========================================================================
    // Form Submit Loading State
    // Disables submit button and shows spinner on form submission to prevent
    // double-clicks and provide visual feedback.
    // =========================================================================
    document.addEventListener('DOMContentLoaded', function () {
        document.addEventListener('submit', function (e) {
            var form = e.target;
            if (!form || form.tagName !== 'FORM') return;

            // Skip forms that opt out via data attribute
            if (form.dataset.noLoadingState) return;

            var btn = form.querySelector('[type="submit"]');
            if (!btn || btn.disabled) return;

            // Store original content for restoration on validation failure
            btn.dataset.originalText = btn.innerHTML;
            btn.disabled = true;
            btn.innerHTML =
                '<span class="spinner-border spinner-border-sm me-1" role="status" aria-hidden="true"></span>' +
                'Saving\u2026';
        });

        // Re-enable buttons if the page is loaded from cache (back/forward navigation)
        window.addEventListener('pageshow', function (e) {
            if (e.persisted) {
                document.querySelectorAll('[type="submit"][disabled]').forEach(function (btn) {
                    if (btn.dataset.originalText) {
                        btn.innerHTML = btn.dataset.originalText;
                        btn.disabled = false;
                    }
                });
            }
        });

        // =====================================================================
        // Password Visibility Toggle
        // Any input-group with a [data-toggle-password] button will toggle
        // the sibling password input between password/text.
        // =====================================================================
        document.addEventListener('click', function (e) {
            var btn = e.target.closest('[data-toggle-password]');
            if (!btn) return;

            var group = btn.closest('.input-group');
            if (!group) return;

            var input = group.querySelector('input[type="password"], input[data-password-field]');
            if (!input) return;

            var isPassword = input.type === 'password';
            input.type = isPassword ? 'text' : 'password';

            if (isPassword) {
                input.setAttribute('data-password-field', 'true');
            }

            var icon = btn.querySelector('i');
            if (icon) {
                icon.className = isPassword ? 'bi bi-eye-slash' : 'bi bi-eye';
            }
        });

        // =====================================================================
        // Password Strength Meter
        // Inputs with data-password-strength will show a strength indicator
        // below their parent .mb-3 container.
        // =====================================================================
        document.querySelectorAll('[data-password-strength]').forEach(function (input) {
            var container = input.closest('.mb-3');
            if (!container) return;

            var meter = document.createElement('div');
            meter.className = 'password-strength mt-1';
            meter.innerHTML =
                '<div class="progress" style="height: 4px;">' +
                    '<div class="progress-bar" role="progressbar" style="width: 0%;"></div>' +
                '</div>' +
                '<small class="password-strength-text text-muted"></small>';
            container.appendChild(meter);

            input.addEventListener('input', function () {
                var val = input.value;
                var score = 0;
                if (val.length >= 8) score++;
                if (val.length >= 12) score++;
                if (/[a-z]/.test(val) && /[A-Z]/.test(val)) score++;
                if (/\d/.test(val)) score++;
                if (/[^a-zA-Z0-9]/.test(val)) score++;

                var bar = meter.querySelector('.progress-bar');
                var text = meter.querySelector('.password-strength-text');
                var levels = [
                    { width: '0%', cls: '', label: '' },
                    { width: '20%', cls: 'bg-danger', label: 'Weak' },
                    { width: '40%', cls: 'bg-danger', label: 'Weak' },
                    { width: '60%', cls: 'bg-warning', label: 'Fair' },
                    { width: '80%', cls: 'bg-info', label: 'Good' },
                    { width: '100%', cls: 'bg-success', label: 'Strong' }
                ];

                var level = val.length === 0 ? levels[0] : levels[Math.min(score, 5)];
                bar.style.width = level.width;
                bar.className = 'progress-bar ' + level.cls;
                text.textContent = level.label;
            });
        });
    });
})();
