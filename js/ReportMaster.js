console.log('[Audit] ReportMaster.js LOADED');
var debounceTimer;
var filterDebounceTimer;
var editingSrNo = null;
var tempIdCounter = 0;

// Autocomplete via event delegation � works for existing and dynamically added inputs
$(document).on('input', '.autocomplete-link', function () {
    var inputEl = this;
    var ef = inputEl.closest('.ef-field');
    if (!ef) return;

    ef.style.position = 'relative';
    var $suggestions = $(ef).find('.autocomplete-suggestions');
    if (!$suggestions || $suggestions.length === 0) {
        var ul = document.createElement('ul');
        ul.className = 'autocomplete-suggestions';
        ul.style.display = 'none';
        ef.appendChild(ul);
        $suggestions = $(ul);
    }

    clearTimeout(debounceTimer);
    var term = inputEl.value;
    if (term.length < 2) { $suggestions.empty().hide(); return; }
    debounceTimer = setTimeout(function () {
        var filtered = linkItems.filter(function (item) {
            return item.Text.toLowerCase().indexOf(term.toLowerCase()) > -1;
        });
        $suggestions.empty();
        if (filtered.length === 0) { $suggestions.hide(); return; }
        $.each(filtered, function (i, item) {
            $('<li>').text(item.Text).data('id', item.Value).appendTo($suggestions);
        });
        $suggestions.show();
    }, 300);
});

$(document).on('click', '.autocomplete-suggestions li', function () {
    var $li = $(this);
    var $suggestions = $li.closest('.autocomplete-suggestions');
    var $input = $suggestions.closest('.ef-field').find('.autocomplete-link');
    $input.val($li.text());
    $input.data('selected-id', $li.data('id'));
    $suggestions.empty().hide();
});

$(document).on('click', function (e) {
    if (!$(e.target).closest('.autocomplete-link, .autocomplete-suggestions').length) {
        $('.autocomplete-suggestions').hide();
    }
});

// Filter autocomplete for Link Item Name
$(function () {
    var $filterInput = $('#FilterLinkItemText');
    var $filterHidden = $('#FilterLinkItemId');
    var $filterList = $('#filterAutocompleteList');
    var form = $filterInput.closest('form');

    var filterId = parseInt($filterHidden.val()) || 0;
    if (filterId > 0) {
        var matched = linkItems.filter(function (item) { return parseInt(item.Value) === filterId; });
        if (matched.length > 0) $filterInput.val(matched[0].Text);
    }

    $filterInput.on('input', function () {
        var term = this.value.trim();
        clearTimeout(filterDebounceTimer);
        if (term === '') {
            $filterList.empty().hide();
            $filterHidden.val('0');
            form.submit();
            return;
        }
        filterDebounceTimer = setTimeout(function () {
            var filtered = linkItems.filter(function (item) {
                return item.Text.toLowerCase().indexOf(term.toLowerCase()) > -1;
            });
            $filterList.empty();
            if (filtered.length === 0) { $filterList.hide(); return; }
            $.each(filtered, function (i, item) {
                $('<li>').text(item.Text).data('id', item.Value).appendTo($filterList);
            });
            $filterList.show();
        }, 300);
    });

    $filterList.on('click', 'li', function () {
        var $li = $(this);
        $filterInput.val($li.text());
        $filterHidden.val($li.data('id'));
        $filterList.empty().hide();
        form.submit();
    });

    $(document).on('click', function (e) {
        if (!$(e.target).closest('#FilterLinkItemText, #filterAutocompleteList').length) {
            $filterList.hide();
        }
    });
});

function showToast(message, type) {
    var $toast = $('#toast');
    $toast.text(message).css('background', type === 'success' ? '#4CAF50' : '#f44336').fadeIn(200);
    setTimeout(function () { $toast.fadeOut(500); }, 3000);
}

function closeEditForm() {
    if (!editingSrNo) return;
    var $dataRow = $('tr[id="reportmaster-' + editingSrNo + '"]');
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

        if (col === 'ReferenceLinkId') {
            var displayText = $dataRow.find('td:nth-child(2)').text().trim();
            $field.val(displayText).data('selected-id', val);
        } else if ($field.is('select')) {
            $field.val(val);
        } else if ($field.is('textarea')) {
            $field.val(val);
        } else {
            $field.val(val);
        }
    });
}

function validateForm($formRow) {
    $formRow.find('.ef-field').removeClass('has-error');

    var fields = [
        { selector: '[data-column="ReferenceLinkId"]', name: 'Link Item Name' },
        { selector: '[data-column="Query"]', name: 'Query' },
        { selector: '[data-column="IsStoreProcedure"]', name: 'Is Store Procedure' }
    ];

    var valid = true;

    fields.forEach(function (field) {
        var $input = $formRow.find(field.selector);
        if ($input.length === 0) return;
        var val = $input.is('textarea') ? $input.val() : $input.val().trim();

        if (val === '' || val === null) {
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
    var reportId = $dataRow.data('reportid');
    var isNew = reportId < 0;
    var changes = [];
    var changed = false;

    $formRow.find('[data-column]').each(function () {
        var $field = $(this);
        var col = $field.data('column');
        var attrName = 'data-' + col.toLowerCase();
        var oldVal = $dataRow.attr(attrName) || '';
        var newVal;

        if (col === 'ReferenceLinkId') {
            newVal = $field.data('selected-id') || '';
            if (newVal !== oldVal) {
                changes.push({ ReportId: reportId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
                changed = true;
            }
            return;
        }

        if ($field.is('select')) {
            newVal = $field.val();
        } else if ($field.is('textarea')) {
            newVal = $field.val();
        } else {
            newVal = $field.val().trim();
        }

        if (newVal !== oldVal) {
            changes.push({ ReportId: reportId, Column: col, OldValue: oldVal, NewValue: newVal, IsNew: isNew });
            changed = true;
        }
    });

    return { changes: changes, changed: changed, reportId: reportId, isNew: isNew };
}

function updateDataRow($dataRow, $formRow, result) {
    var reportId = result.reportId;
    $dataRow.find('td:nth-child(1)').text(reportId);

    var $linkField = $formRow.find('[data-column="ReferenceLinkId"]');
    var linkDisplay = $linkField.length ? $linkField.val() : '';
    var linkId = $dataRow.attr('data-referencelinkid') || '';
    $dataRow.find('td:nth-child(2)').text(linkDisplay).attr('title', linkDisplay);

    var queryText = $dataRow.attr('data-query') || '';
    $dataRow.find('td:nth-child(3)').text(queryText).attr('title', queryText);
}

$(document).on('click', '.btn-edit', function () {
    var $dataRow = $(this).closest('tr');
    var srNo = $dataRow.attr('id').replace('reportmaster-', '');

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
    var $dataRow = $('tr[id="reportmaster-' + srNo + '"]');

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
                            var $mappedRow = $('tr[data-reportid="' + tempId + '"]');
                            $mappedRow.data('reportid', realId);
                            $mappedRow.attr('data-reportid', realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr('data-reportid', realId);
                                $dataRow.data('reportid', realId);
                                result.reportId = realId;
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

                if (result.isNew) {
                    var rcUrl = reportColumnUrl;
                    var rfcUrl = reportFilteringColumnUrl;
                    var realId = result.reportId;
                    var code = $dataRow.attr('data-code') || '';
                    var rptTypeId = $dataRow.attr('data-reporttypeid') || '';
                    var deleteBtnHtml = '';
                    if (currentUserRole === 'SuperAdmin') {
                        deleteBtnHtml = '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>';
                    }
                    $dataRow.find('.action-cell').html(
                        '<button type="button" class="btn btn-xs btn-info btn-details" onclick="window.location=\'' + rcUrl + '?FilterReportId=' + realId + '&FilterCode=' + encodeURIComponent(code) + '&FilterReportTypeId=' + encodeURIComponent(rptTypeId) + '\'" style="margin-right:3px;">Report Column</button>' +
                        '<button type="button" class="btn btn-xs btn-filter-details" onclick="window.location=\'' + rfcUrl + '?FilterReportId=' + realId + '\'" style="margin-right:3px;">Report Filtering Column </button>' +
                        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
                        deleteBtnHtml +
                        '<button type="button" class="btn btn-xs btn-info btn-view-audit" data-table="' + window.auditTableName + '" data-id="' + realId + '" style="margin-left:3px;">View</button>'
                    );
                }
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
    var defaultItem = linkItems.length > 0 ? linkItems[0] : { Value: '', Text: '' };

                    var $emptyRow = $('table tbody tr td[colspan="' + window.colspan + '"].text-muted').closest('tr');
    if ($emptyRow.length > 0) $emptyRow.remove();

    var srNo = 'new' + (-tempId);

    var $newRow = $(document.createElement('tr'));
    $newRow[0].id = 'reportmaster-' + srNo;
    $newRow.attr('data-reportid', tempId);
    $newRow.attr('data-table', window.tableName);
    $newRow.attr('data-referencelinkid', defaultItem.Value);
    $newRow.attr('data-code', '');
    $newRow.attr('data-query', '');
    $newRow.attr('data-whereclause', '');
    $newRow.attr('data-orderby', '');
    $newRow.attr('data-detailquery', '');
    $newRow.attr('data-detailprimarykey', '');
    $newRow.attr('data-ismasterdetail', '0');
    $newRow.attr('data-isstoreprocedure', '0');
    $newRow.attr('data-reporttypeid', '1');
    $newRow.attr('data-islocationfilter', '0');
    $newRow.addClass('editing');
    $newRow[0].innerHTML =
        '<td class="grid-text">' + displayIdCounter + '</td>' +
        '<td class="grid-text">' + defaultItem.Text + '</td>' +
        '<td class="grid-text"></td>' +
        '<td class="action-cell">' +
        '<button type="button" class="btn btn-xs btn-info btn-details" onclick="" style="margin-right:3px;">Report Column</button>' +
        '<button type="button" class="btn btn-xs btn-filter-details" onclick="" style="margin-right:3px;">Report Column Filter</button>' +
        '<button type="button" class="btn btn-xs btn-primary btn-edit">Edit</button>' +
        (currentUserRole === 'SuperAdmin' ? '<button type="button" class="btn btn-xs btn-danger btn-delete" style="margin-left:3px;">Delete</button>' : '') +
        '<button type="button" class="btn btn-xs btn-info btn-view-audit" data-table="' + window.auditTableName + '" data-id="' + tempId + '" style="margin-left:3px;">View</button>' +
        '</td>';

    var $formRow = $(document.createElement('tr'));
    $formRow.attr('class', 'edit-row');
    $formRow.attr('data-editfor', srNo);
    $formRow[0].innerHTML =
        '<td colspan="' + window.colspan + '">' +
        '<div class="edit-form">' +
        '<div class="edit-form-row">' +
        '<div class="ef-field"><label>Link Item Name<span class=\'required\'>*</span></label><input type="text" class="form-control autocomplete-link" data-column="ReferenceLinkId" autocomplete="off"></div>' +
        '<div class="ef-field"><label>Code</label><input type="text" class="form-control" data-column="Code"></div>' +
        '<div class="ef-field"><label>Report Type</label><select class="form-control" data-column="ReportTypeId"><option value="1">1</option><option value="0">0</option></select></div>' +
        '<div class="ef-field"><label>Location Filter</label><select class="form-control" data-column="IsLocationFilter"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field"><label>Master Detail</label><select class="form-control" data-column="IsMasterDetail"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field"><label>Is Store Procedure<span class=\'required\'>*</span></label><select class="form-control" data-column="IsStoreProcedure"><option value="1">1</option><option value="0" selected>0</option></select></div>' +
        '<div class="ef-field"><label>Order By</label><input type="text" class="form-control" data-column="OrderBy"></div>' +
        '<div class="ef-field"><label>Detail Primary Key</label><input type="text" class="form-control" data-column="DetailPrimaryKey"></div>' +
        '<div class="ef-field full"><div class="ef-row-center">' +
        '<div class="ef-field" style="flex:2;"><label>Query<span class=\'required\'>*</span></label><textarea class="form-control" data-column="Query" rows="2"></textarea></div>' +
        '<div class="ef-field" style="flex:1;"><label>Where Clause</label><textarea class="form-control" data-column="WhereClause" rows="2"></textarea></div>' +
        '<div class="ef-field" style="flex:1;"><label>Detail Query</label><textarea class="form-control" data-column="DetailQuery" rows="2"></textarea></div>' +
        '</div></div>' +
        '</div>' +
        '<div class="edit-form-actions"><button type="button" class="btn btn-save-form">Save</button><button type="button" class="btn btn-cancel-form">Cancel</button></div>' +
        '</div>' +
        '</td>';

    $('table tbody').prepend($formRow);
    $('table tbody').prepend($newRow);

    var $autoInput = $formRow.find('.autocomplete-link');
    $autoInput.val(defaultItem.Text).data('selected-id', defaultItem.Value);

    displayIdCounter++;
    editingSrNo = srNo;
    $('#btnAddRow').prop('disabled', false);
});

var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this report?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('reportid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                var srNo = $row.attr('id').replace('reportmaster-', '');
                var $formRow = $('tr.edit-row[data-editfor="' + srNo + '"]');
                $row.fadeOut(300, function () { $(this).remove(); });
                $formRow.remove();
                showToast('Report deleted successfully.', 'success');
            } else if (response.Status && response.Status.indexOf('DEPENDENT:') === 0) {
                var parts = response.Status.split(':');
                var msg = 'Cannot delete: Referenced by ' + parts[2] + ' row(s) in ' + parts[1] + '.';
                showToast(msg, 'error');
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
$(document).on('click', '.btn-view-audit', function () {
    var $wrapper = $('#tableWrapper');
    var $panel = $('#auditPanel');
    var recordId = $(this).data('id');
    var tableName = $(this).data('table');
    var linkItemName = $(this).data('linkitemname') || '';
    var src = (window.auditViewUrl || '/AuditTrail/AuditLogs') + '?tableName=' + encodeURIComponent(tableName) + '&recordId=' + encodeURIComponent(recordId) + '&linkItemName=' + encodeURIComponent(linkItemName);
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