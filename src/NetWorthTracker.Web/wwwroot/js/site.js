// Site-wide JavaScript utilities

(function () {
    'use strict';

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
