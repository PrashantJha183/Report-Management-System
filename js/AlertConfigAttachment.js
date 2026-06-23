function parseAuditDate(str) {
    if (!str) return new Date(NaN);
    var d = new Date(str);
    if (!isNaN(d.getTime())) return d;
    var m = str.match(/^(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2}):(\d{2})/);
    return m ? new Date(+m[1], +m[2] - 1, +m[3], +m[4], +m[5], +m[6]) : d;
}

var editingSrNo = null;
var tempIdCounter = 0;

function showToast(message, type) {
    var $toast = $('#toast');
    $toast.text(message).css('background', type === 'success' ? '#4CAF50' : '#f44336').fadeIn(200);
    setTimeout(function () { $toast.fadeOut(500); }, 3000);
}

function closeEditForm() {
    if (!editingSrNo) return;
    var $dataRow = $('tr[id="alertconfigattachment-' + editingSrNo + '"]');
    var $formRow = $('tr.edit-row[data-editfor="' + editingSrNo + '"]');

    if (editingSrNo.indexOf('new') === 0) {
        $dataRow.remove();
        $formRow.remove();
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
        } else {
            $field.val(val);
        }
    });
}

function collectChanges($dataRow, $formRow) {
    var alertConfigAttachmentId = $dataRow.data('alertconfigattachmentid');
    var isNew = alertConfigAttachmentId < 0;
    var changes = [];
    var changed = false;

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var oldVal = $dataRow.attr(attrName) || '';
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        if (newVal !== oldVal) {
            changes.push({ AlertConfigAttachmentId: alertConfigAttachmentId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
            changed = true;
        }
    });
    return { changes: changes, changed: changed, alertConfigAttachmentId: alertConfigAttachmentId, isNew: isNew };
}

function updateDataRow($dataRow, $formRow, result) {
    var alertConfigAttachmentId = $dataRow.data('alertconfigattachmentid');

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        $dataRow.attr(attrName, newVal);
    });

    $dataRow.find('td:nth-child(1)').text(alertConfigAttachmentId);
    $dataRow.find('td:nth-child(2)').text($dataRow.attr('data-alertconfigid') || '');
    $dataRow.find('td:nth-child(3)').text($dataRow.attr('data-attachmentfiletypeid') || '');
    $dataRow.find('td:nth-child(4)').text($dataRow.attr('data-emailattachmenturl') || '');
}

function validateForm($formRow) {
    $formRow.find('.ef-field').removeClass('has-error');

    var fields = [
        { selector: '[data-column="AlertConfigId"]', name: 'Alert Config Id' },
        { selector: '[data-column="AttachmentFileTypeId"]', name: 'Attachment File Type Id' },
        { selector: '[data-column="EmailAttachmentUrl"]', name: 'Email Attachment Url' }
    ];

    var valid = true;

    fields.forEach(function (field) {
        var $input = $formRow.find(field.selector);
        if ($input.length === 0) return;
        var val = $input.is('select') ? $input.val() : $input.val().trim();

        if (val === '' || val === null) {
            valid = false;
            var $ef = $input.closest('.ef-field');
            $ef.addClass('has-error');
        } else if (field.selector.indexOf('number') > 0 && isNaN(Number(val))) {
            valid = false;
            var $ef = $input.closest('.ef-field');
            $ef.addClass('has-error');
        }
    });

    if (!valid) {
        showToast('Please fill all required fields with valid values.', 'error');
    }

    return valid;
}

$(document).on('click', '.btn-edit', function () {
    var $dataRow = $(this).closest('tr');
    var srNo = $dataRow.attr('id').replace('alertconfigattachment-', '');

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
    var $dataRow = $('tr[id="alertconfigattachment-' + srNo + '"]');

    if ($dataRow.length === 0) {
        showToast('Data row not found.', 'error');
        return;
    }

    if (!validateForm($formRow)) return;

    var result = collectChanges($dataRow, $formRow);

    if (!result.changed) {
        showToast('No changes in this row.', 'error');
        return;
    }

    saveTarget = result;
    saveDataRow = $dataRow;
    saveFormRow = $formRow;
    $('#saveConfirmMessage').text(result.isNew ? 'Are you sure you want to add this new row?' : 'Are you sure you want to save changes to this row?');
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
                    var attrName = 'data-' + change.Column.toLowerCase();
                    $dataRow.attr(attrName, change.NewValue);
                });
                if (response.IdMappings) {
                    for (var tempId in response.IdMappings) {
                        if (response.IdMappings.hasOwnProperty(tempId)) {
                            var realId = response.IdMappings[tempId];
                            var $mappedRow = $('tr[data-alertconfigattachmentid="' + tempId + '"]');
                            $mappedRow.data('alertconfigattachmentid', realId);
                            $mappedRow.attr('data-alertconfigattachmentid', realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr('data-alertconfigattachmentid', realId);
                                $dataRow.data('alertconfigattachmentid', realId);
                                result.alertConfigAttachmentId = realId;
                            }
                        }
                    }
                }
                updateDataRow($dataRow, $formRow, result);
                $dataRow.removeClass('editing');
                $formRow.hide();
                editingSrNo = null;
                if (result.isNew) {
                    $dataRow.appendTo('table tbody');
                    $formRow.appendTo('table tbody');
                }
                $('#successModalMessage').text(result.isNew ? 'Row added successfully.' : 'Row saved successfully.');
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

$('#btnAddRow').on('click', function () {
    if ($(this).prop('disabled')) return;
    $(this).prop('disabled', true);
    closeEditForm();

    tempIdCounter--;
    var tempId = tempIdCounter;
    var srNo = 'new' + (-tempId);

                    var $emptyRow = $('table tbody tr td[colspan="' + window.colspan + '"].text-muted').closest('tr');
    if ($emptyRow.length > 0) $emptyRow.remove();

    var $newRow = $(document.createElement('tr'));
    $newRow[0].id = 'alertconfigattachment-' + srNo;
    $newRow.attr('data-alertconfigattachmentid', tempId);
    $newRow.attr('data-table', window.tableName);
    $newRow.attr('data-alertconfigid', '');
    $newRow.attr('data-attachmentfiletypeid', '');
    $newRow.attr('data-emailattachmenturl', '');
    $newRow.addClass('editing');
    $newRow[0].innerHTML =
        '<td class="grid-text"></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td class="email-cell"></td>' +
        '<td class="action-cell">' +
        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
        '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>' +
        '<button type="button" class="btn btn-xs btn-info btn-view-alert-audit" data-table="' + window.auditTableName + '" data-id="' + tempId + '" style="margin-left:3px;">View</button>' +
        '</td>';

    var $formRow = $(document.createElement('tr'));
    $formRow.attr('class', 'edit-row');
    $formRow.attr('data-editfor', srNo);
    $formRow[0].innerHTML =
        '<td colspan="' + window.colspan + '">' +
        '<div class="edit-form">' +
        '<div class="edit-form-row">' +
        '<div class="ef-field"><label>Alert Config Id<span class=\'required\'>*</span></label><input type="number" class="form-control no-arrows" data-column="AlertConfigId" value="' + (window.filterAlertConfigId || 0) + '" readonly></div>' +
        '<div class="ef-field"><label>Attachment File Type Id<span class=\'required\'>*</span></label><input type="number" class="form-control no-arrows" data-column="AttachmentFileTypeId"></div>' +
        '<div class="ef-field full"><label>Email Attachment Url<span class=\'required\'>*</span></label><textarea class="form-control" data-column="EmailAttachmentUrl" rows="2"></textarea></div>' +
        '</div>' +
        '<div class="edit-form-actions"><button type="button" class="btn btn-save-form">Save</button><button type="button" class="btn btn-cancel-form">Cancel</button></div>' +
        '</div>' +
        '</td>';

    $('table tbody').prepend($formRow);
    $('table tbody').prepend($newRow);
    editingSrNo = srNo;
    $('#btnAddRow').prop('disabled', false);
});

var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this attachment?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('alertconfigattachmentid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.fadeOut(300, function () { $(this).remove(); });
                showToast('Attachment deleted successfully.', 'success');
            } else if (response.Status && response.Status.indexOf('DEPENDENT:') === 0) {
                $('#deleteBlockedModal').modal('show');
                $btn.prop('disabled', false);
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

$(document).on('click', '.btn-view-alert-audit', function () {
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var recordId = $(this).data('id');
    var tableName = $(this).data('table');
    var src = (window.auditViewUrl || '/AuditTrail/AuditLogs') + '?tableName=' + encodeURIComponent(tableName) + '&recordId=' + encodeURIComponent(recordId) +                         '&recordIdLabel=' + encodeURIComponent(window.recordIdLabel);
    $('#auditPanelTitle').text('Change History');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
    console.log('AlertConfigAttachment View clicked: table=' + tableName + ', id=' + recordId);
});

$('#btnDeletedRecords').on('click', function () {
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var src = (window.deletedRecordsUrl || '/AuditTrail/DeletedRecords') + '?tableName=' + encodeURIComponent(window.tableName);
    $('#auditPanelTitle').text('Deleted Records');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
    console.log('AlertConfigAttachment Deleted Records clicked');
});

$('#btnCloseAudit').on('click', function () {
    $('#tableWrapper').css('flex', '1 1 100%');
    $('#auditPanel').hide();
    $('#auditIframe').attr('src', '');
    console.log('AlertConfigAttachment audit panel closed');
});



