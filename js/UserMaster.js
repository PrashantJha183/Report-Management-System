var editingSrNo = null;
var tempIdCounter = 0;

function togglePw(btn) {
    var wrapper = btn.closest('.pw-toggle-wrapper');
    var input = wrapper.querySelector('input');
    var svg = btn.querySelector('svg');
    if (input.type === 'password') {
        input.type = 'text';
        svg.innerHTML = '<path d="M17.94 17.94A10.07 10.07 0 0 1 12 20c-7 0-11-8-11-8a18.45 18.45 0 0 1 5.06-5.94M9.9 4.24A9.12 9.12 0 0 1 12 4c7 0 11 8 11 8a18.5 18.5 0 0 1-2.16 3.19m-6.72-1.07a3 3 0 1 1-4.24-4.24"/><line x1="1" y1="1" x2="23" y2="23"/>';
    } else {
        input.type = 'password';
        svg.innerHTML = '<path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/>';
    }
}

function showToast(message, type) {
    var $toast = $('#toast');
    $toast.text(message).css('background', type === 'success' ? '#4CAF50' : '#f44336').fadeIn(200);
    setTimeout(function () { $toast.fadeOut(500); }, 3000);
}

function closeEditForm() {
    if (!editingSrNo) return;
    var $dataRow = $('tr[id="user-' + editingSrNo + '"]');
    var $formRow = $('tr.edit-row[data-editfor="' + editingSrNo + '"]');

    if (editingSrNo.indexOf('new') === 0) {
        $dataRow.remove();
        $formRow.remove();
        displayIdCounter--;
    } else {
        $dataRow.removeClass('editing');
        $formRow.hide();
    }
    editingSrNo = null;
}

function populateEditForm($dataRow, $formRow) {
    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var val = $dataRow.attr(attrName) || '';
        if ($field.is('select')) {
            $field.val(val);
        } else if (col === 'Password') {
            $field.val('');
        } else {
            $field.val(val);
        }
    });
}

function getSelectedText($select) {
    return $select.find('option:selected').text();
}

function collectChanges($dataRow, $formRow) {
    var userId = parseInt($dataRow.data('userid')) || 0;
    var isNew = userId < 0;
    var changes = [];
    var changed = false;

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var oldVal = $dataRow.attr(attrName) || '';
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();

        if (col === 'Password') {
            if (newVal === '') return;
            changes.push({ UserId: userId, Column: col, OldValue: '', NewValue: newVal, IsNew: isNew });
            changed = true;
            return;
        }

        if (newVal !== oldVal) {
            changes.push({ UserId: userId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
            changed = true;
        }
    });

    return { changes: changes, changed: changed, userId: userId, isNew: isNew };
}

function updateDataRow($dataRow, $formRow) {
    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        if (col === 'Password') return;
        var attrName = 'data-' + col.toLowerCase();
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        $dataRow.attr(attrName, newVal);
    });

    $dataRow.find('td:nth-child(1)').text($dataRow.data('userid'));
    $dataRow.find('td:nth-child(2)').text($dataRow.attr('data-username') || '');
    $dataRow.find('td:nth-child(3)').text($dataRow.attr('data-displayname') || '');
    $dataRow.find('td:nth-child(4)').text($dataRow.attr('data-role') || '');
    var isActive = $dataRow.attr('data-isactive') === 'true';
    $dataRow.find('td:nth-child(5)').html(isActive
        ? '<span class="label label-success">Active</span>'
        : '<span class="label label-danger">Inactive</span>');
}

function validateForm($formRow, isNew) {
    $formRow.find('.ef-field').removeClass('has-error');
    var valid = true;

    var fields = [
        { selector: '[data-column="Username"]', name: 'Username' },
        { selector: '[data-column="DisplayName"]', name: 'Display Name' },
        { selector: '[data-column="Role"]', name: 'Role' }
    ];

    if (isNew) {
        fields.push({ selector: '[data-column="Password"]', name: 'Password' });
    }

    fields.forEach(function (field) {
        var $input = $formRow.find(field.selector);
        if ($input.length === 0) return;
        var val = $input.is('select') ? $input.val() : $input.val().trim();
        if (val === '' || val === null) {
            valid = false;
            var $ef = $input.closest('.ef-field');
            $ef.addClass('has-error');
        }
    });

    if (!valid) {
        showToast('Please fill all required fields.', 'error');
    }

    return valid;
}

$(document).on('click', '.btn-edit', function () {
    var $dataRow = $(this).closest('tr');
    var srNo = $dataRow.attr('id').replace('user-', '');

    if (editingSrNo === srNo) return;
    closeEditForm();

    var $formRow = $('tr.edit-row[data-editfor="' + srNo + '"]');
    if ($formRow.length === 0) return;

    populateEditForm($dataRow, $formRow);
    $dataRow.addClass('editing');
    $formRow.show();
    editingSrNo = srNo;
});

$(document).on('click', '.btn-cancel-form', function () {
    closeEditForm();
});

var saveTarget = null;
var saveDataRow = null;
var saveFormRow = null;

$(document).on('click', '.btn-save-form', function () {
    var $formRow = $(this).closest('tr.edit-row');
    var srNo = $formRow.data('editfor');
    var $dataRow = $('tr[id="user-' + srNo + '"]');

    if ($dataRow.length === 0) {
        showToast('Data row not found.', 'error');
        return;
    }

    var userId = parseInt($dataRow.data('userid')) || 0;
    if (!validateForm($formRow, userId < 0)) return;

    var result = collectChanges($dataRow, $formRow);

    if (!result.changed) {
        showToast('No changes in this row.', 'error');
        return;
    }

    saveTarget = result;
    saveDataRow = $dataRow;
    saveFormRow = $formRow;
    $('#saveConfirmMessage').text(result.isNew ? 'Are you sure you want to add this new user?' : 'Are you sure you want to save changes to this user?');
    $('#saveConfirmModal').modal('show');
});

$('#saveConfirmOk').on('click', function () {
    $(this).prop('disabled', true);
    $('#saveConfirmModal').modal('hide');
    if (!saveTarget) return;

    var result = saveTarget;
    var $dataRow = saveDataRow;
    var $formRow = saveFormRow;
    var $btn = saveFormRow.find('.btn-save-form').prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: saveChangesUrl,
        data: JSON.stringify({ mode: 'SAVE', changes: result.changes }),
        contentType: 'application/json',
        success: function (response) {
            if (response.Status === 'SAVED') {
                $.each(result.changes, function (i, change) {
                    if (change.Column === 'Password') return;
                    var attrName = 'data-' + change.Column.toLowerCase();
                    $dataRow.attr(attrName, change.NewValue);
                });
                if (response.IdMappings) {
                    for (var tempId in response.IdMappings) {
                        if (response.IdMappings.hasOwnProperty(tempId)) {
                            var realId = response.IdMappings[tempId];
                            var $mappedRow = $('tr[data-userid="' + tempId + '"]');
                            $mappedRow.data('userid', realId);
                            $mappedRow.attr('data-userid', realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr('data-userid', realId);
                                $dataRow.data('userid', realId);
                                result.userId = realId;
                                var deleteBtnHtml = '';
                                var isActive = $dataRow.attr('data-isactive') === 'true';
                                var toggleClass = isActive ? 'btn-warning' : 'btn-success';
                                var toggleText = isActive ? 'Deactivate' : 'Activate';
                                if (currentUserRole === 'SuperAdmin') {
                                    deleteBtnHtml = '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>';
                                }
                                $dataRow.find('.action-cell').html(
                                    '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
                                    '<button type="button" class="btn btn-xs ' + toggleClass + ' btn-toggle-active" style="margin-left:3px;">' + toggleText + '</button>' +
                                    deleteBtnHtml
                                );
                            }
                        }
                    }
                }
                updateDataRow($dataRow, $formRow);
                $dataRow.removeClass('editing');
                $formRow.hide();
                editingSrNo = null;
                if (result.isNew) {
                    $dataRow.appendTo('table tbody');
                    $formRow.appendTo('table tbody');
                }
                $('#successModalMessage').text(result.isNew ? 'User added successfully.' : 'User saved successfully.');
                $('#successModal').modal('show');
            } else {
                showToast('Error: ' + response.Status, 'error');
            }
        },
        error: function () {
            showToast('Server error occurred.', 'error');
        },
        complete: function () {
            $('#saveConfirmOk').prop('disabled', false);
            $btn.prop('disabled', false);
            saveTarget = null;
            saveDataRow = null;
            saveFormRow = null;
        }
    });
});

var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to deactivate this user?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('userid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.attr('data-isactive', 'false');
                $row.find('td:nth-child(5)').html('<span class="label label-danger">Inactive</span>');
                $row.find('.btn-toggle-active').removeClass('btn-warning').addClass('btn-success').text('Activate');
                showToast('User deactivated successfully.', 'success');
            } else {
                showToast('Error: ' + response.Status, 'error');
                $btn.prop('disabled', false);
            }
        },
        error: function () {
            showToast('Server error occurred.', 'error');
            $btn.prop('disabled', false);
        }
    });

    deleteTarget = null;
    deleteBtn = null;
});

$(document).on('click', '.btn-toggle-active', function () {
    var $row = $(this).closest('tr');
    var id = $row.data('userid');
    var $btn = $(this).prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: toggleActiveUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'ACTIVATED') {
                $row.attr('data-isactive', 'true');
                $row.find('td:nth-child(5)').html('<span class="label label-success">Active</span>');
                $btn.removeClass('btn-success').addClass('btn-warning').text('Deactivate');
                showToast('User activated successfully.', 'success');
            } else if (response.Status === 'DEACTIVATED') {
                $row.attr('data-isactive', 'false');
                $row.find('td:nth-child(5)').html('<span class="label label-danger">Inactive</span>');
                $btn.removeClass('btn-warning').addClass('btn-success').text('Activate');
                showToast('User deactivated successfully.', 'success');
            } else {
                showToast('Error: ' + response.Status, 'error');
            }
        },
        error: function () {
            showToast('Server error occurred.', 'error');
        },
        complete: function () {
            $btn.prop('disabled', false);
        }
    });
});

$('#btnAddRow').on('click', function () {
    if ($(this).prop('disabled')) return;
    $(this).prop('disabled', true);
    closeEditForm();

    tempIdCounter--;
    var tempId = tempIdCounter;
    var srNo = 'new' + (-tempId);

    var $emptyRow = $('table tbody tr td[colspan="7"].text-muted').closest('tr');
    if ($emptyRow.length > 0) $emptyRow.remove();

    var $newRow = $(document.createElement('tr'));
    $newRow[0].id = 'user-' + srNo;
    $newRow.attr('data-userid', tempId);
    $newRow.attr('data-username', '');
    $newRow.attr('data-displayname', '');
    $newRow.attr('data-role', '');
    $newRow.attr('data-isactive', 'true');
    $newRow.attr('data-createdon', '');

    $newRow.addClass('editing');
    $newRow[0].innerHTML =
        '<td class="grid-text">' + displayIdCounter + '</td>' +
        '<td class="wrap-cell"></td>' +
        '<td class="wrap-cell"></td>' +
        '<td class="wrap-cell"></td>' +
        '<td><span class="label label-success">Active</span></td>' +
        '<td class="grid-text"></td>' +
        '<td class="action-cell">' +
        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
        '<button type="button" class="btn btn-xs btn-warning btn-toggle-active" style="margin-left:3px;">Deactivate</button>' +
        (currentUserRole === 'SuperAdmin' ? '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>' : '') +
        '</td>';

    var roleOptions = '';
    $('select[data-column="Role"]:first option').each(function () {
        roleOptions += '<option value="' + $(this).val() + '">' + $(this).text() + '</option>';
    });

    var $formRow = $(document.createElement('tr'));
    $formRow.attr('class', 'edit-row');
    $formRow.attr('data-editfor', srNo);
    $formRow[0].innerHTML =
        '<td colspan="7">' +
        '<div class="edit-form">' +
        '<div class="edit-form-row edit-form-row-user">' +
        '<div class="ef-field"><label>Username<span class=\'required\'>*</span></label><input type="text" class="form-control" data-column="Username"></div>' +
        '<div class="ef-field"><label>Display Name<span class=\'required\'>*</span></label><input type="text" class="form-control" data-column="DisplayName"></div>' +
        '<div class="ef-field"><label>Password<span class=\'required\'>*</span></label><div class="pw-toggle-wrapper"><input type="password" class="form-control" data-column="Password" autocomplete="off"><button type="button" class="toggle-pw-btn" onclick="togglePw(this)" tabindex="-1" aria-label="Toggle password visibility"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M1 12s4-8 11-8 11 8 11 8-4 8-11 8-11-8-11-8z"/><circle cx="12" cy="12" r="3"/></svg></button></div></div>' +
        '<div class="ef-field"><label>Role<span class=\'required\'>*</span></label><select class="form-control" data-column="Role">' + roleOptions + '</select></div>' +
        '<div class="ef-field"><label>Active</label><select class="form-control" data-column="IsActive"><option value="true">Active</option><option value="false">Inactive</option></select></div>' +
        '</div>' +
        '<div class="edit-form-actions"><button type="button" class="btn btn-save-form">Save</button><button type="button" class="btn btn-cancel-form">Cancel</button></div>' +
        '</div>' +
        '</td>';

    $('table tbody').prepend($formRow);
    $('table tbody').prepend($newRow);
    displayIdCounter++;
    editingSrNo = srNo;
    $('#btnAddRow').prop('disabled', false);
});

$(function () {
    var $searchInput = $('#searchText');
    var searchTimer;
    $searchInput.on('input', function () {
        clearTimeout(searchTimer);
        searchTimer = setTimeout(function () {
            var form = $searchInput.closest('form');
            if (!form.length) {
                form = $('.toolbar-form');
            }
            if (!form.length) {
                form = $('form:first');
            }
            var searchVal = $searchInput.val();
            var $hiddenSearch = form.find('input[name="SearchText"]');
            if ($hiddenSearch.length) {
                $hiddenSearch.val(searchVal);
            } else {
                form.append('<input type="hidden" name="SearchText" value="' + searchVal + '" />');
            }
            var $pageNum = form.find('input[name="PageNumber"]');
            if (!$pageNum.length) {
                form.append('<input type="hidden" name="PageNumber" value="1" />');
            } else {
                $pageNum.val('1');
            }
            form.submit();
        }, 500);
    });
});
