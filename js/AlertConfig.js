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

function getIstTime() {
    var now = new Date();
    return new Date(now.getTime() + 330 * 60 * 1000).toISOString().replace('T', ' ').substring(0, 19);
}

function closeEditForm() {
    if (!editingSrNo) return;
    var $dataRow = $('tr[id="alertconfig-' + editingSrNo + '"]');
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
        } else {
            $field.val(val);
        }
    });
}

function collectChanges($dataRow, $formRow) {
    var alertConfigId = $dataRow.data('alertconfigid');
    var isNew = alertConfigId < 0;
    var changes = [];
    var changed = false;

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var oldVal = $dataRow.attr(attrName) || '';
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        if (newVal !== oldVal) {
            changes.push({ AlertConfigId: alertConfigId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
            changed = true;
        }
    });
    return { changes: changes, changed: changed, alertConfigId: alertConfigId, isNew: isNew };
}

function updateDataRow($dataRow, $formRow, result) {
    var alertConfigId = $dataRow.data('alertconfigid');

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        $dataRow.attr(attrName, newVal);
    });

    $dataRow.find('td:nth-child(1)').text(alertConfigId);
    $dataRow.find('td:nth-child(2)').text($dataRow.attr('data-emailsubject') || '');
    $dataRow.find('td:nth-child(3)').text($dataRow.attr('data-emailcontentheader') || '');
    $dataRow.find('td:nth-child(4)').text($dataRow.attr('data-emailcontenturl') || '');
    $dataRow.find('td:nth-child(5)').text($dataRow.attr('data-emailcontentfooter') || '');
    $dataRow.find('td:nth-child(6)').text($dataRow.attr('data-statusid') || '');
    $dataRow.find('td:nth-child(7)').text($dataRow.attr('data-ismultialert') || '0');
}

function validateForm($formRow) {
    $formRow.find('.ef-field').removeClass('has-error');

    var fields = [
        { selector: '[data-column="AlertName"]', name: 'Alert Name' },
        { selector: '[data-column="ReportId"]', name: 'Report Id' },
        { selector: '[data-column="IsEmailNotify"]', name: 'Is Email Notify' },
        { selector: '[data-column="EmailSubject"]', name: 'Email Subject' },
        { selector: '[data-column="EmailContentHeader"]', name: 'Email Content Header' },
        { selector: '[data-column="EmailContentUrl"]', name: 'Email Content Url' },
        { selector: '[data-column="EmailContentFooter"]', name: 'Email Content Footer' },
        { selector: '[data-column="StatusId"]', name: 'Status Id' },
        { selector: '[data-column="IsMultiAlert"]', name: 'Is Multi Alert' }
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
    var srNo = $dataRow.attr('id').replace('alertconfig-', '');

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
    var $dataRow = $('tr[id="alertconfig-' + srNo + '"]');

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
                            var $mappedRow = $('tr[data-alertconfigid="' + tempId + '"]');
                            $mappedRow.data('alertconfigid', realId);
                            $mappedRow.attr('data-alertconfigid', realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr('data-alertconfigid', realId);
                                $dataRow.data('alertconfigid', realId);
                                result.alertConfigId = realId;
                                var deleteBtnHtml = '';
                                if (currentUserRole === 'SuperAdmin') {
                                    deleteBtnHtml = '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>';
                                }
                                $dataRow.find('.action-cell').html(
                                    '<button type="button" class="btn btn-xs btn-primary" onclick="window.location=\'' + scheduleUrl + '?FilterAlertConfigId=' + realId + '\'" style="margin-right:3px;background:#0891b2!important;border-color:#06b6d4!important;color:#fff!important;font-weight:800;border-radius:6px!important;padding:6px 8px;font-size:11px;">Schedule</button>' +
                                    '<button type="button" class="btn btn-xs btn-primary" onclick="window.location=\'' + attachmentUrl + '?FilterAlertConfigId=' + realId + '\'" style="margin-right:3px;background:#d97706!important;border-color:#d97706!important;color:#fff!important;font-weight:800;border-radius:6px!important;padding:6px 8px;font-size:11px;">Attachment</button>' +
                                    '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
                                    deleteBtnHtml +
                                    '<button type="button" class="btn btn-xs btn-info btn-view-alert-audit" data-table="' + window.auditTableName + '" data-id="' + realId + '" data-alertname="' + $('<div>').text($dataRow.data('alertname') || '').html() + '" style="margin-left:3px;">View</button>'
                                );
                            }
                        }
                    }
                }
                updateDataRow($dataRow, $formRow, result);
                if (!result.isNew) {
                    $dataRow.attr('data-updatedon', getIstTime());
                }
                if (result.isNew) {
                    $dataRow.attr('data-createdon', getIstTime());
                }
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
    $newRow[0].id = 'alertconfig-' + srNo;
    $newRow.attr('data-table', window.tableName);
    $newRow.attr('data-alertconfigid', tempId);
    $newRow.attr('data-alertname', '');
    $newRow.attr('data-reportid', '0');
    $newRow.attr('data-isemailnotify', '1');
    $newRow.attr('data-emailsubject', '');
    $newRow.attr('data-emailcontentheader', '');
    $newRow.attr('data-emailcontenturl', '');
    $newRow.attr('data-attachmentfiletypeid', '0');
    $newRow.attr('data-emailattachmenturl', '');
    $newRow.attr('data-statusid', '');
    $newRow.attr('data-alerttypeid', '1');
    $newRow.attr('data-ismultialert', '0');
    $newRow.attr('data-multialertquery', '');
    $newRow.attr('data-emailcontentfooter', '');
    $newRow.attr('data-createdby', '1');
    $newRow.attr('data-createdon', '');
    $newRow.attr('data-updatedby', '1');
    $newRow.attr('data-updatedon', '');
    $newRow.addClass('editing');
    $newRow[0].innerHTML =
        '<td class="grid-text">' + displayIdCounter + '</td>' +
        '<td class="wrap-cell"></td>' +
        '<td class="wrap-cell"></td>' +
        '<td class="wrap-cell"></td>' +
        '<td class="wrap-cell"></td>' +
        '<td></td>' +
        '<td>0</td>' +
        '<td class="action-cell">' +
        '<button type="button" class="btn btn-xs btn-primary" onclick="" style="margin-right:3px;background:#0891b2!important;border-color:#06b6d4!important;color:#fff!important;font-weight:800;border-radius:6px!important;padding:6px 8px;font-size:11px;">Schedule</button>' +
        '<button type="button" class="btn btn-xs btn-primary" onclick="" style="margin-right:3px;background:#d97706!important;border-color:#d97706!important;color:#fff!important;font-weight:800;border-radius:6px!important;padding:6px 8px;font-size:11px;">Attachment</button>' +
        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
        '<button type="button" class="btn btn-xs btn-info btn-view-alert-audit" data-table="' + window.auditTableName + '" data-id="' + tempId + '" data-alertname="" style="margin-left:3px;">View</button>' +
        '</td>';

    var $formRow = $(document.createElement('tr'));
    $formRow.attr('class', 'edit-row');
    $formRow.attr('data-editfor', srNo);
    $formRow[0].innerHTML =
        '<td colspan="' + window.colspan + '">' +
        '<div class="edit-form">' +
        '<div class="edit-form-row">' +
        '<div class="ef-field"><label>Report Id<span class=\'required\'>*</span></label><input type="number" class="form-control no-arrows" data-column="ReportId"></div>' +
        '<div class="ef-field"><label>Is Email Notify<span class=\'required\'>*</span></label><select class="form-control" data-column="IsEmailNotify"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field"><label>Attachment File Type Id</label><input type="number" class="form-control no-arrows" data-column="AttachmentFileTypeId"></div>' +
        '<div class="ef-field"><label>Alert Type Id</label><input type="number" class="form-control no-arrows" data-column="AlertTypeId"></div>' +
        '<div class="ef-field"><label>Status Id<span class=\'required\'>*</span></label><select class="form-control" data-column="StatusId"><option value="30">30</option><option value="200">200</option></select></div>' +
        '<div class="ef-field"><label>Is Multi Alert<span class=\'required\'>*</span></label><select class="form-control" data-column="IsMultiAlert"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field full"><label>Alert Name<span class=\'required\'>*</span></label><textarea class="form-control" data-column="AlertName" rows="2"></textarea></div>' +
        '<div class="ef-field full"><label>Email Subject<span class=\'required\'>*</span></label><textarea class="form-control" data-column="EmailSubject" rows="3"></textarea></div>' +
        '<div class="ef-field full"><label>Email Content Header<span class=\'required\'>*</span></label><textarea class="form-control" data-column="EmailContentHeader" rows="3"></textarea></div>' +
        '<div class="ef-field full"><label>Email Content Url<span class=\'required\'>*</span></label><textarea class="form-control" data-column="EmailContentUrl" rows="2"></textarea></div>' +
        '<div class="ef-field full"><label>Email Attachment Url</label><textarea class="form-control" data-column="EmailAttachmentUrl" rows="2"></textarea></div>' +
        '<div class="ef-field full"><label>Multi Alert Query</label><textarea class="form-control" data-column="MultiAlertQuery" rows="3"></textarea></div>' +
        '<div class="ef-field full"><label>Email Content Footer<span class=\'required\'>*</span></label><textarea class="form-control" data-column="EmailContentFooter" rows="3"></textarea></div>' +
        '<input type="hidden" data-column="CreatedBy">' +
        '<input type="hidden" data-column="UpdatedBy">' +
        '<input type="hidden" data-column="CreatedOn">' +
        '<input type="hidden" data-column="UpdatedOn">' +
        '</div>' +
        '<div class="edit-form-actions"><button type="button" class="btn btn-save-form">Save</button><button type="button" class="btn btn-cancel-form">Cancel</button></div>' +
        '</div>' +
        '</td>';

    $('table tbody').prepend($formRow);
    $('table tbody').prepend($newRow);
    $formRow.find('[data-column="CreatedOn"]').val(getIstTime());
    $formRow.find('[data-column="UpdatedOn"]').val('');
    displayIdCounter++;
    editingSrNo = srNo;
    $('#btnAddRow').prop('disabled', false);
});

var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this alert config?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('alertconfigid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.fadeOut(300, function () { $(this).remove(); });
                showToast('Alert config deleted successfully.', 'success');
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

$(document).on('click', '.btn-view-alert-audit', function () {
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var recordId = $(this).data('id');
    var tableName = $(this).data('table');
    var src = (window.auditViewUrl || '/AuditTrail/AuditLogs') + '?tableName=' + encodeURIComponent(tableName) + '&recordId=' + encodeURIComponent(recordId) + '&recordIdLabel=' + encodeURIComponent(window.recordIdLabel) + '&alertName=' + encodeURIComponent($(this).data('alertname') || '');
    $('#auditPanelTitle').text('Change History');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
});

$('#btnDeletedRecords').on('click', function () {
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var src = (window.deletedRecordsUrl || '/AuditTrail/DeletedRecords') + '?tableName=' + encodeURIComponent(window.tableName);
    $('#auditPanelTitle').text('Deleted Records');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
});

$('#btnCloseAudit').on('click', function () {
    $('#tableWrapper').css('flex', '1 1 100%');
    $('#auditPanel').hide();
    $('#auditIframe').attr('src', '');
});
