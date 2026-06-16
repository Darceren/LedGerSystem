(function () {
    if (typeof flatpickr === 'undefined') return;

    var dateOpts = {
        locale: 'default',
        dateFormat: 'Y-m-d',
        allowInput: true,
        disableMobile: true
    };

    var dateTimeOpts = {
        locale: 'default',
        enableTime: true,
        dateFormat: 'Y-m-d H:i',
        time_24hr: true,
        allowInput: true,
        disableMobile: true
    };

    document.querySelectorAll('input[type="date"], input.date-picker').forEach(function (el) {
        flatpickr(el, dateOpts);
    });

    document.querySelectorAll('input[type="datetime-local"], input.datetime-picker').forEach(function (el) {
        flatpickr(el, dateTimeOpts);
    });
})();
