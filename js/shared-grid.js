/**
 * ============================================
 * shared-grid.js — Common grid JavaScript
 * All 8 grid views share these functions.
 * Entity-specific functions reside in the
 * entity's own .js file (e.g., AlertConfig.js).
 *
 * Required globals set by the view:
 *   window.saveChangesUrl, window.deleteUrl,
 *   window.auditViewUrl, window.deletedRecordsUrl,
 *   window.tableName, window.auditTableName,
 *   window.recordIdLabel, window.colspan,
 *   window.currentUserRole
 *
 * Entity JS files must define:
 *   populateEditForm($dataRow, $formRow)
 *   collectChanges($dataRow, $formRow)
 *   updateDataRow($dataRow, $formRow, result)
 *   validateForm($formRow)
 * ============================================
 */

/* ---------- Utilities ---------- */
function parseAuditDate(str) {
    if (!str) return new Date(NaN);
    var d = new Date(str);
    if (!isNaN(d.getTime())) return d;
    var m = str.match(/^(\d{4})-(\d{2})-(\d{2})\s+(\d{2}):(\d{2}):(\d{2})/);
    return m ? new Date(+m[1], +m[2] - 1, +m[3], +m[4], +m[5], +m[6]) : d;
}

function getIstTime() {
    var now = new Date();
    return new Date(now.getTime() + 330 * 60 * 1000).toISOString().replace('T', ' ').substring(0, 19);
}

/* ---------- Shared State ---------- */
var editingSrNo = null;
var tempIdCounter = 0;
var displayIdCounter = 0;
var saveTarget = null;
var saveDataRow = null;
var saveFormRow = null;
var deleteTarget = null;
var deleteBtn = null;

/* ---------- Toast ---------- */
function showToast(message, type) {
    var $toast = $('#toast');
    $toast.text(message).css('background', type === 'success' ? '#4CAF50' : '#f44336').fadeIn(200);
    setTimeout(function () { $toast.fadeOut(500); }, 3000);
}

/* ---------- Edit Form Management ---------- */
function closeEditForm() {
    if (!editingSrNo) return;
    var entityPrefix = window.entityRowPrefix || 'row';
    var $dataRow = $('tr[id="' + entityPrefix + '-' + editingSrNo + '"]');
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

/* ---------- Edit Button ---------- */
$(document).on('click', '.btn-edit', function () {
    var $dataRow = $(this).closest('tr');
    var entityPrefix = window.entityRowPrefix || 'row';
    var srNo = $dataRow.attr('id').replace(entityPrefix + '-', '');

    if (editingSrNo === srNo) return;
    closeEditForm();

    var $formRow = $('tr.edit-row[data-editfor="' + srNo + '"]');
    if ($formRow.length === 0) return;

    populateEditForm($dataRow, $formRow);
    $dataRow.addClass('editing');
    $formRow.show();
    editingSrNo = srNo;
});

/* ---------- Cancel Button ---------- */
$(document).on('click', '.btn-cancel-form', function () {
    closeEditForm();
});

/* ---------- Save Flow ---------- */
$(document).on('click', '.btn-save-form', function () {
    var $formRow = $(this).closest('tr.edit-row');
    var srNo = $formRow.data('editfor');
    var entityPrefix = window.entityRowPrefix || 'row';
    var $dataRow = $('tr[id="' + entityPrefix + '-' + srNo + '"]');

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
                            var idAttr = window.idAttrName || 'data-id';
                            var $mappedRow = $('tr[' + idAttr + '="' + tempId + '"]');
                            $mappedRow.data(window.idAttrData || 'id', realId);
                            $mappedRow.attr(idAttr, realId);
                            if ($mappedRow.is($dataRow)) {
                                $dataRow.attr(idAttr, realId);
                                $dataRow.data(window.idAttrData || 'id', realId);
                                onIdMappingApplied($dataRow, realId);
                            }
                        }
                    }
                }
                if (typeof updateDataRow === 'function') {
                    updateDataRow($dataRow, $formRow, result);
                }
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

/**
 * Hook called after an ID mapping is applied on save.
 * Override in entity JS to rebuild action-cell buttons with the real ID.
 */
function onIdMappingApplied($dataRow, realId) {
    // Default: no-op; entity JS overrides this
}

/* ---------- Delete Flow ---------- */
$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this record?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data(window.idAttrData || 'id');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.fadeOut(300, function () { $(this).remove(); });
                showToast('Record deleted successfully.', 'success');
            } else if (response.Status && response.Status.indexOf('DEPENDENT:') === 0) {
                $('#deleteBlockedModal .modal-body p').text('This record canno
