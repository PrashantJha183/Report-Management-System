function togglePw(id, btn) {
    var input = document.getElementById(id);
    var svg = btn.querySelector('svg');
    if (input.type === 'password') {
        input.type = 'text';
        svg.innerHTML = '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/>';
    } else {
        input.type = 'password';
        svg.innerHTML = '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>';
    }
}

function updateStrength() {
    var val = document.getElementById('newPw').value;
    var container = document.getElementById('pwStrength');
    var bar = document.getElementById('pwStrengthBar');
    var text = document.getElementById('pwStrengthText');

    if (val.length === 0) {
        container.style.display = 'none';
        return;
    }
    container.style.display = 'block';

    var score = 0;
    if (val.length >= 6) score++;
    if (val.length >= 10) score++;
    if (/[a-z]/.test(val) && /[A-Z]/.test(val)) score++;
    if (/\d/.test(val)) score++;
    if (/[^a-zA-Z0-9]/.test(val)) score++;

    var pct = (score / 5) * 100;
    bar.style.width = pct + '%';

    var colors = ['#dc2626', '#f97316', '#eab308', '#22c55e', '#16a34a'];
    var labels = ['Weak', 'Fair', 'Good', 'Strong', 'Very Strong'];
    var idx = Math.min(score, 4);
    bar.style.background = colors[idx];
    text.textContent = labels[idx];
    text.style.color = colors[idx];

    checkMatch();
}

function checkMatch() {
    var newPw = document.getElementById('newPw').value;
    var confirmPw = document.getElementById('confirmPw').value;
    var status = document.getElementById('matchStatus');

    if (confirmPw.length === 0) {
        status.textContent = '';
        return;
    }

    if (newPw === confirmPw) {
        status.textContent = '✓ Passwords match';
        status.style.color = '#16a34a';
    } else {
        status.textContent = '✗ Passwords do not match';
        status.style.color = '#dc2626';
    }
}

document.addEventListener('DOMContentLoaded', function () {
    document.getElementById('changePwForm').addEventListener('submit', function () {
        var btn = document.getElementById('submitBtn');
        btn.disabled = true;
        btn.innerHTML = '<span class="spinner"></span> Updating...';
    });
});
