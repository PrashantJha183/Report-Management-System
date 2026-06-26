function togglePassword() {
    var pw = document.getElementById('loginPassword');
    var icon = document.getElementById('eyeIcon');
    if (pw.type === 'password') {
        pw.type = 'text';
        icon.innerHTML = '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/>';
    } else {
        pw.type = 'password';
        icon.innerHTML = '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>';
    }
}

function toggleDbDropdown(e) {
    e.stopPropagation();
    var panel = document.getElementById('dbPanel');
    var toggle = document.getElementById('dbToggle');
    var arrow = document.getElementById('dbArrow');
    var isOpen = panel.classList.contains('open');

    closeAllDbDropdowns();

    if (!isOpen) {
        panel.classList.add('open');
        arrow.classList.add('open');
        clearFieldError(toggle);
    }
}

function closeAllDbDropdowns() {
    document.querySelectorAll('.db-panel.open').forEach(function(p) {
        p.classList.remove('open');
    });
    document.querySelectorAll('.db-toggle .arrow.open').forEach(function(a) {
        a.classList.remove('open');
    });
}

function selectDb(el) {
    var value = el.getAttribute('data-value');
    var label = el.querySelector('span').textContent;

    document.getElementById('connectionName').value = value;
    document.getElementById('dbLabel').textContent = label;
    document.getElementById('dbLabel').style.color = '#0f172a';

    var toggle = document.getElementById('dbToggle');
    toggle.classList.add('selected');
    clearFieldError(toggle);

    document.querySelectorAll('.db-option').forEach(function(o) {
        o.classList.remove('selected');
    });
    el.classList.add('selected');

    closeAllDbDropdowns();
    document.getElementById('dbArrow').classList.remove('open');
}

function validateForm() {
    var isValid = true;

    var username = document.querySelector('input[name="username"]');
    if (!username.value.trim()) {
        showFieldError(username, 'Username is required');
        isValid = false;
    } else {
        clearFieldError(username);
    }

    var password = document.getElementById('loginPassword');
    if (!password.value.trim()) {
        showFieldError(password, 'Password is required');
        isValid = false;
    } else {
        clearFieldError(password);
    }

    var dbValue = document.getElementById('connectionName').value;
    if (!dbValue) {
        showFieldError(document.getElementById('dbToggle'), 'Please select a database');
        isValid = false;
    } else {
        clearFieldError(document.getElementById('dbToggle'));
    }

    return isValid;
}

function showFieldError(element, message) {
    element.classList.add('error');
    var msgEl = element.closest('.mb-3, .mb-4').querySelector('.error-message');
    if (msgEl) {
        msgEl.innerHTML = message;
        msgEl.classList.add('visible');
    }
    setTimeout(function() {
        element.classList.remove('error');
    }, 600);
}

function clearFieldError(element) {
    if (!element) return;
    element.classList.remove('error');
    var wrapper = element.closest('.mb-3, .mb-4');
    if (wrapper) {
        var msgEl = wrapper.querySelector('.error-message');
        if (msgEl) {
            msgEl.classList.remove('visible');
        }
    }
}

document.addEventListener('click', function(e) {
    var dbSelect = document.querySelector('.db-select');
    if (dbSelect && !dbSelect.contains(e.target)) {
        closeAllDbDropdowns();
        document.getElementById('dbArrow').classList.remove('open');
    }
});
