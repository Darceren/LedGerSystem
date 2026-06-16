(function () {
    const typeSelect = document.getElementById('transType');
    if (!typeSelect) return;

    const rules = JSON.parse(document.getElementById('typeRules')?.textContent || '{}');
    const form = document.getElementById('entryForm');

    const fields = {
        party: document.querySelector('.field-party'),
        partyLabel: document.getElementById('partyLabel'),
        partySelect: document.getElementById('partySelect'),
        supplier: document.querySelector('.field-supplier'),
        bank: document.querySelector('.field-bank'),
        payMethod: document.querySelector('.field-paymethod'),
        payout: document.querySelector('.field-payout'),
        payoutAccount: document.querySelector('.field-payout-account'),
        rate: document.querySelector('.field-rate'),
        rateLabel: document.getElementById('rateLabel'),
        quantity: document.querySelector('.field-quantity'),
        quantityLabel: document.getElementById('quantityLabel'),
        equivalent: document.querySelector('.field-equiv'),
        equivLabel: document.getElementById('equivLabel'),
        currency: document.getElementById('currency'),
        amountLabel: document.getElementById('amountLabel')
    };

    const partyLists = {
        Customer: document.getElementById('customersJson')?.textContent,
        Supplier: document.getElementById('suppliersJson')?.textContent,
        Shufen: document.getElementById('shufenJson')?.textContent
    };

    function toggle(el, show) {
        if (el) el.classList.toggle('d-none', !show);
    }

    function getRule() {
        const typeId = parseInt(typeSelect.value || '0', 10);
        return rules[typeId] || null;
    }

    function applyRule() {
        const rule = getRule();
        if (!rule) {
            ['party', 'supplier', 'bank', 'payMethod', 'payout', 'payoutAccount', 'rate', 'quantity', 'equivalent']
                .forEach(function (k) { toggle(fields[k], false); });
            return;
        }

        toggle(fields.party, rule.ShowParty);
        toggle(fields.supplier, rule.ShowSupplier);
        toggle(fields.bank, rule.ShowBank);
        toggle(fields.payMethod, rule.ShowPayMethod);
        toggle(fields.payout, rule.ShowPayout);
        toggle(fields.payoutAccount, rule.ShowPayout);
        toggle(fields.rate, rule.ShowRate);
        toggle(fields.quantity, rule.ShowQuantity);
        toggle(fields.equivalent, rule.ShowEquivalent);

        if (fields.partyLabel) fields.partyLabel.textContent = rule.PartyRole || 'Party';
        if (fields.currency) fields.currency.value = rule.Currency || 'BDT';
        if (fields.amountLabel) {
            fields.amountLabel.textContent = rule.Code === 'SELL_RMB' ? 'RMB Amount' :
                rule.Code === 'USDT_TO_RMB' ? 'USDT Amount (in Quantity)' : 'Amount';
        }
        if (fields.rateLabel) {
            fields.rateLabel.textContent = rule.Code === 'SELL_RMB' ? 'Rate (BDT/RMB)' :
                rule.Code === 'USDT_TO_RMB' ? 'Rate (BDT/RMB)' :
                rule.Code === 'BUY_USDT' ? 'Cost (BDT/USDT)' : 'Rate / Unit Price';
        }
        if (fields.equivLabel) {
            fields.equivLabel.textContent = rule.Code === 'SELL_RMB' ? 'Expected BDT' :
                rule.Code === 'USDT_TO_RMB' ? 'RMB Received' : 'Equivalent Amount';
        }

        if (fields.partySelect && rule.PartyRole) {
            const json = partyLists[rule.PartyRole];
            if (json) {
                const items = JSON.parse(json);
                const current = fields.partySelect.value;
                fields.partySelect.innerHTML = '<option value="">-- Select --</option>';
                items.forEach(function (item) {
                    const opt = document.createElement('option');
                    opt.value = item.value;
                    opt.textContent = item.text;
                    if (item.value === current) opt.selected = true;
                    fields.partySelect.appendChild(opt);
                });
            }
        }

        calcEquivalent();
    }

    function calcEquivalent() {
        const typeId = parseInt(typeSelect.value || '0', 10);
        const amount = parseFloat(document.getElementById('amount')?.value || '0');
        const rate = parseFloat(document.getElementById('unitPrice')?.value || '0');
        const equiv = document.getElementById('equivalentAmount');
        if (!equiv) return;

        if (typeId === 3 && amount > 0 && rate > 0) {
            equiv.value = (amount * rate).toFixed(4);
        }
    }

    function buildSummary() {
        const rule = getRule();
        const typeText = typeSelect.options[typeSelect.selectedIndex]?.text || '';
        const partyText = fields.partySelect?.selectedOptions[0]?.text || '-';
        const amount = document.getElementById('amount')?.value || '0';
        const currency = fields.currency?.value || 'BDT';
        const date = document.querySelector('[name="Entry.TransDate"]')?.value || '';
        return '<ul class="mb-0">' +
            '<li><strong>Type:</strong> ' + typeText + '</li>' +
            '<li><strong>Date:</strong> ' + date + '</li>' +
            (rule && rule.ShowParty ? '<li><strong>Party:</strong> ' + partyText + '</li>' : '') +
            '<li><strong>Amount:</strong> ' + amount + ' ' + currency + '</li>' +
            '</ul>';
    }

    const btnPreview = document.getElementById('btnPreview');
    const btnConfirm = document.getElementById('btnConfirmSave');
    const modalEl = document.getElementById('confirmModal');
    let modal;

    if (btnPreview && modalEl && form) {
        modal = new bootstrap.Modal(modalEl);
        btnPreview.addEventListener('click', function () {
            if (!getRule()) {
                alert('Please select a valid transaction type.');
                return;
            }
            document.getElementById('confirmSummary').innerHTML = buildSummary();
            modal.show();
        });
        btnConfirm?.addEventListener('click', function () {
            const hidden = document.createElement('input');
            hidden.type = 'hidden';
            hidden.name = 'Entry.SaveAndAddAnother';
            hidden.value = 'false';
            form.appendChild(hidden);
            form.submit();
        });
    }

    typeSelect.addEventListener('change', applyRule);
    document.getElementById('amount')?.addEventListener('input', calcEquivalent);
    document.getElementById('unitPrice')?.addEventListener('input', calcEquivalent);

    applyRule();
})();
