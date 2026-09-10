// Invoice create/edit screen: dynamic item rows, automatic Qty*Rate and
// GST calculation, and a lightweight customer autocomplete box.
// Server-side, InvoiceService recomputes every total from the posted
// Items array — nothing calculated here is trusted as-is on save.
(function () {
    'use strict';

    const itemsBody = document.getElementById('itemsBody');
    if (!itemsBody) {
        return; // this script only runs on the invoice Create/Edit pages
    }

    const addItemBtn = document.getElementById('addItemBtn');
    const gstEnabledInput = document.getElementById('gstEnabledInput');
    const gstPercentRow = document.getElementById('gstPercentRow');
    const cgstPercentInput = document.getElementById('cgstPercentInput');
    const sgstPercentInput = document.getElementById('sgstPercentInput');
    const gstRowEls = document.querySelectorAll('.gst-row');

    function formatMoney(n) {
        const rounded = Math.round((n + Number.EPSILON) * 100) / 100;
        return '₹' + rounded.toLocaleString('en-IN', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
    }

    function reindexRows() {
        const rows = itemsBody.querySelectorAll('.item-row');
        rows.forEach((row, idx) => {
            row.querySelector('.row-sno').textContent = idx + 1;
            row.querySelectorAll('[name]').forEach((el) => {
                el.name = el.name.replace(/Items\[\d+\]/, 'Items[' + idx + ']');
            });
        });

        const removeButtons = itemsBody.querySelectorAll('.remove-row-btn');
        removeButtons.forEach((btn) => {
            btn.disabled = rows.length <= 1;
        });
    }

    function recalcRow(row) {
        const qty = parseFloat(row.querySelector('.qty-input').value) || 0;
        const rate = parseFloat(row.querySelector('.rate-input').value) || 0;
        const amount = qty * rate;
        row.querySelector('.amount-cell').textContent = formatMoney(amount);
        return amount;
    }

    function recalcTotals() {
        let subtotal = 0;
        itemsBody.querySelectorAll('.item-row').forEach((row) => {
            subtotal += recalcRow(row);
        });

        const gstOn = gstEnabledInput.checked;
        gstPercentRow.classList.toggle('d-none', !gstOn);
        gstRowEls.forEach((el) => el.classList.toggle('d-none', !gstOn));

        let cgst = 0;
        let sgst = 0;
        if (gstOn) {
            const cgstPct = parseFloat(cgstPercentInput.value) || 0;
            const sgstPct = parseFloat(sgstPercentInput.value) || 0;
            cgst = (subtotal * cgstPct) / 100;
            sgst = (subtotal * sgstPct) / 100;
        }
        const grandTotal = subtotal + cgst + sgst;

        document.getElementById('subtotalDisplay').textContent = formatMoney(subtotal);
        document.getElementById('cgstDisplay').textContent = formatMoney(cgst);
        document.getElementById('sgstDisplay').textContent = formatMoney(sgst);
        document.getElementById('grandTotalDisplay').textContent = formatMoney(grandTotal);
    }

    function wireRow(row) {
        row.querySelector('.qty-input').addEventListener('input', recalcTotals);
        row.querySelector('.rate-input').addEventListener('input', recalcTotals);

        const select = row.querySelector('.service-select');
        select.addEventListener('change', function () {
            const opt = select.options[select.selectedIndex];
            const particularInput = row.querySelector('.particular-input');
            if (opt.value) {
                particularInput.value = opt.textContent.trim();
                const rate = opt.getAttribute('data-rate');
                if (rate) {
                    row.querySelector('.rate-input').value = parseFloat(rate).toFixed(2);
                }
            }
            recalcTotals();
        });

        row.querySelector('.remove-row-btn').addEventListener('click', function () {
            if (itemsBody.querySelectorAll('.item-row').length <= 1) {
                return; // always keep at least one row
            }
            row.remove();
            reindexRows();
            recalcTotals();
        });
    }

    function addRow() {
        const template = itemsBody.querySelector('.item-row');
        const newRow = template.cloneNode(true);

        newRow.querySelectorAll('input[type="number"]').forEach((el) => {
            el.value = el.classList.contains('qty-input') ? '1' : '0';
        });
        newRow.querySelector('.particular-input').value = '';
        newRow.querySelector('select').selectedIndex = 0;
        newRow.querySelector('.amount-cell').textContent = '₹0.00';

        itemsBody.appendChild(newRow);
        wireRow(newRow);
        reindexRows();
        recalcTotals();
    }

    addItemBtn.addEventListener('click', addRow);
    gstEnabledInput.addEventListener('change', recalcTotals);
    cgstPercentInput.addEventListener('input', recalcTotals);
    sgstPercentInput.addEventListener('input', recalcTotals);

    itemsBody.querySelectorAll('.item-row').forEach(wireRow);
    reindexRows();
    recalcTotals();

    // ---- Customer autocomplete ----
    const customerSearchInput = document.getElementById('customerSearchInput');
    const customerIdInput = document.getElementById('customerIdInput');
    const customerResults = document.getElementById('customerResults');
    let searchTimeout = null;

    if (customerSearchInput) {
        customerSearchInput.addEventListener('input', function () {
            customerIdInput.value = '';
            clearTimeout(searchTimeout);
            const term = customerSearchInput.value.trim();

            if (term.length < 2) {
                customerResults.style.display = 'none';
                customerResults.innerHTML = '';
                return;
            }

            searchTimeout = setTimeout(() => {
                fetch('/Customers/SearchJson?term=' + encodeURIComponent(term))
                    .then((r) => r.json())
                    .then((data) => {
                        customerResults.innerHTML = '';
                        if (!data.length) {
                            customerResults.style.display = 'none';
                            return;
                        }
                        data.forEach((c) => {
                            const item = document.createElement('button');
                            item.type = 'button';
                            item.className = 'list-group-item list-group-item-action';
                            item.textContent = c.customerName + ' — ' + c.mobile;
                            item.addEventListener('click', function () {
                                customerSearchInput.value = c.customerName;
                                customerIdInput.value = c.customerId;
                                customerResults.style.display = 'none';
                                customerResults.innerHTML = '';
                            });
                            customerResults.appendChild(item);
                        });
                        customerResults.style.display = '';
                    })
                    .catch(() => {
                        customerResults.style.display = 'none';
                    });
            }, 250);
        });

        document.addEventListener('click', function (e) {
            if (e.target !== customerSearchInput) {
                customerResults.style.display = 'none';
            }
        });
    }
})();
