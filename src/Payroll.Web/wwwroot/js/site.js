// Site-wide behaviour: confirmation dialogs, pay-type toggling, timesheet totals.
(function () {
    'use strict';

    // --- Confirmation dialog -------------------------------------------------
    // Any submit button with data-confirm="message" asks before submitting its form.
    var modalEl = document.getElementById('confirmModal');
    var pendingButton = null;

    document.addEventListener('click', function (e) {
        var button = e.target.closest('[data-confirm]');
        if (!button || !modalEl) return;
        if (button.dataset.confirmed === 'true') {
            button.dataset.confirmed = '';
            return;
        }

        e.preventDefault();
        pendingButton = button;
        document.getElementById('confirmModalBody').textContent = button.getAttribute('data-confirm');
        bootstrap.Modal.getOrCreateInstance(modalEl).show();
    });

    if (modalEl) {
        document.getElementById('confirmOk').addEventListener('click', function () {
            bootstrap.Modal.getOrCreateInstance(modalEl).hide();
            if (pendingButton) {
                pendingButton.dataset.confirmed = 'true';
                pendingButton.click();
                pendingButton = null;
            }
        });
    }

    // --- Employee form: show the salary or hourly rate field -----------------
    var payType = document.querySelector('[data-pay-type]');
    if (payType) {
        var togglePayFields = function () {
            var selected = payType.options[payType.selectedIndex].text;
            document.querySelectorAll('[data-pay-field]').forEach(function (el) {
                el.classList.toggle('d-none', el.getAttribute('data-pay-field') !== selected);
            });
        };
        payType.addEventListener('change', togglePayFields);
        togglePayFields();
    }

    // --- Timesheet form: live weekly total ------------------------------------
    var totalCell = document.getElementById('timesheet-total');
    if (totalCell) {
        var recalc = function () {
            var total = 0;
            document.querySelectorAll('.hours-input').forEach(function (input) {
                var v = parseFloat(input.value);
                if (!isNaN(v)) total += v;
            });
            totalCell.textContent = (Math.round(total * 100) / 100).toString();
        };
        document.querySelectorAll('.hours-input').forEach(function (input) {
            input.addEventListener('input', recalc);
        });
    }
})();
