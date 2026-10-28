/**
 * Smart Purchase & Expense Tracker - Purchase Orders Dynamic Script
 * Handles dynamic row addition, deletion, DOM reindexing for ASP.NET Core model binding,
 * and real-time client-side financial bill preview calculation.
 */

function addItemRow(categoryOptionsHtml) {
    const tbody = document.getElementById('itemsTableBody');
    if (!tbody) return;

    const rowCount = tbody.querySelectorAll('tr.item-row').length;
    const tr = document.createElement('tr');
    tr.className = 'item-row';
    tr.setAttribute('data-index', rowCount);
    tr.innerHTML = `
        <td>
            <input type="hidden" name="Items[${rowCount}].Id" value="0" />
            <input type="text" name="Items[${rowCount}].ItemName" class="form-control form-control-sm item-name" placeholder="Item name..." required />
        </td>
        <td>
            <select name="Items[${rowCount}].CategoryId" class="form-select form-select-sm item-category" required>
                ${categoryOptionsHtml}
            </select>
        </td>
        <td>
            <input type="number" name="Items[${rowCount}].Quantity" value="1" class="form-control form-control-sm item-qty" min="1" max="10000" oninput="calculateLiveBill()" required />
        </td>
        <td>
            <input type="number" step="0.01" name="Items[${rowCount}].UnitPrice" value="0.00" class="form-control form-control-sm item-price" min="0" oninput="calculateLiveBill()" required />
        </td>
        <td>
            <span class="fw-semibold item-subtotal text-end d-block">₹0.00</span>
        </td>
        <td class="text-center">
            <button type="button" class="btn btn-sm btn-outline-danger" onclick="removeItemRow(this)" title="Remove item">
                <i class="bi bi-trash"></i>
            </button>
        </td>
    `;
    tbody.appendChild(tr);
    reindexRows();
    calculateLiveBill();
}

function removeItemRow(btn) {
    const tbody = document.getElementById('itemsTableBody');
    if (!tbody) return;

    const rows = tbody.querySelectorAll('tr.item-row');
    if (rows.length <= 1) {
        alert("An order must contain at least one item.");
        return;
    }
    btn.closest('tr').remove();
    reindexRows();
    calculateLiveBill();
}

/**
 * Reindexes all input and select element names to maintain 0-based contiguous indices:
 * Items[0], Items[1], Items[2]... for correct ASP.NET Core collection model binding.
 */
function reindexRows() {
    const rows = document.querySelectorAll('#itemsTableBody tr.item-row');
    rows.forEach((row, idx) => {
        row.setAttribute('data-index', idx);
        row.querySelectorAll('input, select').forEach(input => {
            const name = input.getAttribute('name');
            if (name) {
                const newName = name.replace(/Items\[\d+\]/, `Items[${idx}]`);
                input.setAttribute('name', newName);
            }
        });
    });
}

/**
 * Computes live bill amount in the browser matching server-side financial accounting formulas:
 * ItemsSubtotal = sum(Quantity * UnitPrice)
 * TotalFees = Delivery + Packaging + Platform
 * TotalTaxesAndOther = Taxes + OtherCharges
 * PreDiscount = ItemsSubtotal + TotalFees + TotalTaxesAndOther
 * FinalBill = max(0, PreDiscount - Discount)
 * NetSpending = Completed ? max(0, FinalBill - Refund) : 0
 */
function calculateLiveBill() {
    let subtotal = 0;
    const rows = document.querySelectorAll('#itemsTableBody tr.item-row');
    rows.forEach(row => {
        const qty = parseFloat(row.querySelector('.item-qty')?.value) || 0;
        const price = parseFloat(row.querySelector('.item-price')?.value) || 0;
        const lineTotal = Math.round(qty * price * 100) / 100;
        subtotal += lineTotal;
        const lineDisplay = row.querySelector('.item-subtotal');
        if (lineDisplay) lineDisplay.textContent = '₹' + lineTotal.toFixed(2);
    });

    const delivery = parseFloat(document.getElementById('DeliveryFee')?.value) || 0;
    const packaging = parseFloat(document.getElementById('PackagingFee')?.value) || 0;
    const platformFee = parseFloat(document.getElementById('PlatformFee')?.value) || 0;
    const taxes = parseFloat(document.getElementById('Taxes')?.value) || 0;
    const other = parseFloat(document.getElementById('OtherCharges')?.value) || 0;
    const discount = parseFloat(document.getElementById('Discount')?.value) || 0;
    const refund = parseFloat(document.getElementById('RefundAmount')?.value) || 0;
    const status = document.getElementById('orderStatusSelect')?.value || 'Completed';

    const totalFees = delivery + packaging + platformFee;
    const totalTaxesAndOther = taxes + other;
    const preDiscount = subtotal + totalFees + totalTaxesAndOther;
    const finalBill = Math.max(0, preDiscount - discount);

    let netSpending = 0;
    if (status === 'Completed') {
        netSpending = Math.max(0, finalBill - refund);
    }

    const setEl = (id, val) => {
        const el = document.getElementById(id);
        if (el) el.textContent = val;
    };

    setEl('previewSubtotal', '₹' + subtotal.toFixed(2));
    setEl('previewFees', '₹' + totalFees.toFixed(2));
    setEl('previewTaxes', '₹' + totalTaxesAndOther.toFixed(2));
    setEl('previewDiscount', '-₹' + discount.toFixed(2));
    setEl('previewFinalBill', '₹' + finalBill.toFixed(2));
    setEl('previewRefund', '-₹' + refund.toFixed(2));
    setEl('previewNetSpending', '₹' + netSpending.toFixed(2));

    const statusNotice = document.getElementById('statusNotice');
    if (statusNotice) {
        if (status !== 'Completed') {
            statusNotice.innerHTML = `<i class="bi bi-info-circle text-warning me-1"></i> Order is marked as <strong>${status}</strong>. Net spending will not contribute to realized expenses until completed.`;
        } else {
            statusNotice.innerHTML = `<i class="bi bi-shield-check text-success me-1"></i> All monetary amounts are verified and calculated on the server before saving.`;
        }
    }
}

