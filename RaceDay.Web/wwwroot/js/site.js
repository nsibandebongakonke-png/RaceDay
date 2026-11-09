// Confirm before submitting any form marked with data-confirm="message".
document.addEventListener('submit', function (event) {
    var form = event.target;
    var message = form.getAttribute && form.getAttribute('data-confirm');
    if (message && !window.confirm(message)) {
        event.preventDefault();
    }
});

// Stop double-submits: disable the submit button once a valid form is sent.
document.addEventListener('submit', function (event) {
    if (event.defaultPrevented) return;
    var form = event.target;
    var button = form.querySelector && form.querySelector('button[type="submit"]');
    if (button && !form.hasAttribute('data-allow-resubmit')) {
        setTimeout(function () { button.disabled = true; }, 0);
    }
});
