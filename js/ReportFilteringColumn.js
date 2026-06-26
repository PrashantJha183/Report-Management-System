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
    var $dataRow = $('tr[id="reportfilteringcolumn-' + editingSrNo + '"]');
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

function validateForm($formRow) {
    $formRow.find('.ef-field').removeClass('has-error');

    var fields = [
        { selector: '[data-column="ReportColumnId"]', name: 'Report Col Id' },
        { selector: '[data-column="ReportId"]', name: 'Report Id' },
        { selector: '[data-column="DataType"]', name: 'Data Type' },
        { selector: '[data-column="Operator"]', name: 'Operator' }
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
        } else if (field.selector === '[data-column="ReportColumnId"]' && isNaN(Number(val))) {
            valid = false;
            var $ef = $input.closest('.ef-field');
            $ef.addClass('has-error');
        } else if (field.selector === '[data-column="ReportId"]' && isNaN(Number(val))) {
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

function collectChanges($dataRow, $formRow) {
    var reportFilterColumnId = $dataRow.data('reportfiltercolumnid');
    var isNew = reportFilterColumnId < 0;
    var changes = [];
    var changed = false;

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var oldVal = $dataRow.attr(attrName) || '';
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        if (newVal !== oldVal) {
            changes.push({ ReportFilterColumnId: reportFilterColumnId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
            changed = true;
        }
    });
    return { changes: changes, changed: changed, reportFilterColumnId: reportFilterColumnId, isNew: isNew };
}

function updateDataRow($dataRow, $formRow, result) {
    var reportFilterColumnId = $dataRow.data('reportfiltercolumnid');

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var newVal = $field.is('select') ? $field.val() : $field.val().trim();
        $dataRow.attr(attrName, newVal);
    });

    $dataRow.find('td:nth-child(1)').text(reportFilterColumnId);
    $dataRow.find('td:nth-child(2)').text($dataRow.attr('data-reportcolumnid') || '');
    $dataRow.find('td:nth-child(3)').text($dataRow.attr('data-reportid') || '');
    $dataRow.find('td:nth-child(4)').text($dataRow.attr('data-datatype') || '');
    $dataRow.find('td:nth-child(5)').text($dataRow.attr('data-operator') || '');
    $dataRow.find('td:nth-child(6)').text($dataRow.attr('data-value1') || '');
    $dataRow.find('td:nth-child(7)').text($dataRow.attr('data-value2') || '');
    $dataRow.find('td:nth-child(8)').text($dataRow.attr('data-condn') || '');
    var isDisableVal = $dataRow.attr('data-isdisable') || '';
    $dataRow.find('td:nth-child(9)').text(isDisableVal);
}

$(document).on('click', '.btn-edit', function () {
    var $dataRow = $(this).closest('tr');
    var srNo = $dataRow.attr('id').replace('reportfilteringcolumn-', '');

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
    var $dataRow = $('tr[id="reportfilteringcolumn-' + srNo + '"]');

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
                            var $mappedRow = $('tr[data-reportfiltercolumnid="' + tempId + '"]');
                            $mappedRow.data('reportfiltercolumnid', realId);
                            $mappedRow.attr('data-reportfiltercolumnid', realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr('data-reportfiltercolumnid', realId);
                                $dataRow.data('reportfiltercolumnid', realId);
                                result.reportFilterColumnId = realId;
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
    $newRow[0].id = 'reportfilteringcolumn-' + srNo;
    $newRow.attr('data-table', window.tableName);
    $newRow.attr('data-reportfiltercolumnid', tempId);
    $newRow.attr('data-reportcolumnid', '');
    $newRow.attr('data-reportid', '');
    $newRow.attr('data-datatype', 'string');
    $newRow.attr('data-operator', '');
    $newRow.attr('data-value1', '');
    $newRow.attr('data-value2', '');
    $newRow.attr('data-condn', '0');
    $newRow.attr('data-isdisable', '0');
    $newRow.addClass('editing');
    $newRow[0].innerHTML =
        '<td class="grid-text">' + displayIdCounter + '</td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td></td>' +
        '<td class="action-cell">' +
        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
        (currentUserRole === 'SuperAdmin' ? '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>' : '') +
        '<button type="button" class="btn btn-xs btn-info btn-view-repfiltercol-audit" data-table="' + window.auditTableName + '" data-id="' + tempId + '" data-linkitemname="' + (window.filterLinkItemName || '') + '" style="margin-left:3px;">View</button>' +
        '</td>';

    var datatypesHtml = '';
    for (var i = 0; i < datatypeOptions.length; i++) {
        var sel = datatypeOptions[i].Value === 'string' ? ' selected' : '';
        datatypesHtml += '<option value="' + datatypeOptions[i].Value + '"' + sel + '>' + datatypeOptions[i].Text + '</option>';
    }

    var $formRow = $(document.createElement('tr'));
    $formRow.attr('class', 'edit-row');
    $formRow.attr('data-editfor', srNo);
    $formRow[0].innerHTML =
        '<td colspan="' + window.colspan + '">' +
        '<div class="edit-form">' +
        '<div class="edit-form-row">' +
        '<div class="ef-field"><label>Report Col Id<span class=\'required\'>*</span></label><input type="number" class="form-control" data-column="ReportColumnId"></div>' +
        '<div class="ef-field"><label>Report Id<span class=\'required\'>*</span></label><input type="number" class="form-control" data-column="ReportId" value="' + (window.filterReportId || 0) + '" readonly></div>' +
        '<div class="ef-field"><label>Data Type<span class=\'required\'>*</span></label><select class="form-control" data-column="DataType">' + datatypesHtml + '</select></div>' +
        '<div class="ef-field"><label>Operator<span class=\'required\'>*</span></label><input type="text" class="form-control" data-column="Operator"></div>' +
        '<div class="ef-field"><label>Value1</label><input type="text" class="form-control" data-column="Value1"></div>' +
        '<div class="ef-field"><label>Value2</label><input type="text" class="form-control" data-column="Value2"></div>' +
        '<div class="ef-field"><label>Condition</label><select class="form-control" data-column="Condn"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field"><label>Is Disable</label><select class="form-control" data-column="IsDisable"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
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

var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this filtering column?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('reportfiltercolumnid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.fadeOut(300, function () { $(this).remove(); });
                showToast('Filtering column deleted successfully.', 'success');
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

$(document).on('click', '.btn-view-repfiltercol-audit', function () {
    console.log('[ReportFilteringColumn] View audit clicked');
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var tableName = $(this).data('table');
    var recordId = $(this).data('id');
    var linkItemName = $(this).data('linkitemname') || '';
    var src = (window.auditViewUrl || '/AuditTrail/AuditLogs') + '?tableName=' + encodeURIComponent(tableName) + '&recordId=' + encodeURIComponent(recordId) + '&linkItemName=' + encodeURIComponent(linkItemName);
    console.log('[ReportFilteringColumn] Loading:', src);
    $('#auditPanelTitle').text('Change History');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
});

$('#btnCloseAudit').on('click', function () {
    console.log('[ReportFilteringColumn] Close audit panel');
    $('#tableWrapper').css('flex', '1 1 100%');
    $('#auditPanel').hide();
    $('#auditIframe').attr('src', '');
});

$('#btnDeletedRecords').on('click', function () {
    console.log('[ReportFilteringColumn] Deleted Records clicked');
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var src = (window.deletedRecordsUrl || '/AuditTrail/DeletedRecords') + '?tableName=' + encodeURIComponent(window.tableName);
    console.log('[ReportFilteringColumn] Loading deleted records:', src);
    $('#auditPanelTitle').text('Deleted Records');
    $('#auditIframe').attr('src', src);
    $wrapper.css('flex', '1 1 50%');
    $panel.show();
});
