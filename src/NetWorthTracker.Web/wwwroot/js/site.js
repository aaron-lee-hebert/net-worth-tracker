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
    });
})();
