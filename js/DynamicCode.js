var debounceTimer;
var editingSrNo = null;
var tempIdCounter = 0;
var namespaceDebounceTimer;

function showToast(message, type) {
    var $toast = $('#toast');
    $toast.text(message).css('background', type === 'success' ? '#4CAF50' : '#f44336').fadeIn(20);
    setTimeout(function () { $toast.fadeOut(500); }, 3000);
}

// ── Show IDE panel, hide grid ──
function showIDE() {
    $('#gridSection').hide();
    $('#ideSection').show();
    $('#ideStatus').text('');
    loadClassesForNamespaces($('#txtNamespaces').val());
    setTimeout(function () {
        if (window.editor) {
            window.editor.layout();
            window.editor.focus();
        }
    }, 250);
}

// ── Hide IDE panel, show grid ──
function hideIDE() {
    $('#ideSection').hide();
    $('#gridSection').show();
    $('#ideStatus').text('');
}

// ── Clear IDE fields ──
function clearIDE() {
    $('#ideControllerName').val('');
    $('#ideActionName').val('');
    $('#ideDescription').val('');
    $('#txtNamespaces').val('');
    $('#hdnCode').val('');
    if (window.editor) {
        window.editor.setValue('');
    }
}

// ── Populate IDE fields from data ──
function populateIDE(data) {
    $('#ideControllerName').val(data.controllerName || '');
    $('#ideActionName').val(data.actionName || '');
    $('#ideDescription').val(data.description || '');
    $('#txtNamespaces').val(data.namespaces || '');
    $('#hdnCode').val(data.code || '');
    if (window.editor) {
        window.editor.setValue(data.code || '');
    }
    loadClassesForNamespaces(data.namespaces || '');
}

// ── Debounced namespace change handler ──
function loadClassesForNamespaces(ns) {
    if (!ns || ns.trim() === '') return;
    $.ajax({
        type: 'POST',
        url: loadClassesUrl,
        data: { Namespaces: ns },
        success: function (res) {
            if (typeof updateClasses === 'function') updateClasses(res);
        }
    });
}

// ══════════════════════════════════════════════
//  ADD ROW → open IDE with empty fields
// ══════════════════════════════════════════════
$('#btnAddRow').on('click', function () {
    $('#ideSection').data('codeid', 0);
    clearIDE();
    showIDE();
});

// ══════════════════════════════════════════════
//  EDIT → fetch code data → open IDE populated
// ══════════════════════════════════════════════
$(document).on('click', '.btn-edit', function () {
    var $row = $(this).closest('tr');
    var id = $row.data('dynamiccodeid');
    if (!id || id <= 0) return;

    $('#ideSection').data('codeid', id);
    var $btn = $(this).prop('disabled', true);
    $.ajax({
        type: 'GET',
        url: getCodeUrl,
        data: { id: id },
        success: function (data) {
            populateIDE(data);
            showIDE();
        },
        error: function () {
            showToast('Failed to load code data.', 'error');
        },
        complete: function () {
            $btn.prop('disabled', false);
        }
    });
});

// ══════════════════════════════════════════════
//  SAVE → compile + save via AJAX
// ══════════════════════════════════════════════
$('#ideSave').on('click', function () {
    var $btn = $(this).prop('disabled', true);
    $('#ideStatus').text('Compiling...').css('color', '#f59e0b');

    if (window.editor) {
        $('#hdnCode').val(window.editor.getValue());
    }

    var codeID = 0;
    if ($('#ideSection').data('codeid')) {
        codeID = $('#ideSection').data('codeid');
    }

    $.ajax({
        type: 'POST',
        url: saveCodeUrl,
        data: JSON.stringify({
            CodeID: codeID,
            ControllerName: $('#ideControllerName').val() || '',
            ActionName: $('#ideActionName').val() || '',
            Code: $('#hdnCode').val() || '',
            Namespaces: $('#txtNamespaces').val() || '',
            Description: $('#ideDescription').val() || ''
        }),
        contentType: 'application/json',
        success: function (res) {
            if (res.success) {
                $('#ideStatus').text('Saved successfully!').css('color', '#16a34a');
                setTimeout(function () {
                    window.location.reload();
                }, 800);
            } else {
                var errMsg = res.errors ? res.errors.join('<br/>') : 'Unknown error';
                $('#ideStatus').html('Error: ' + errMsg).css('color', '#dc2626');
                $btn.prop('disabled', false);
            }
        },
        error: function () {
            $('#ideStatus').text('Server error.').css('color', '#dc2626');
            $btn.prop('disabled', false);
        }
    });
});

// ══════════════════════════════════════════════
//  PREVIEW → toggle 50/50 split + compile/execute
// ══════════════════════════════════════════════
// ══════════════════════════════════════════════
//  CANCEL → hide IDE, show grid
// ══════════════════════════════════════════════
$('#ideBackToGrid, #ideCancel').on('click', function () {
    hideIDE();
    return false;
});

// ══════════════════════════════════════════════
//  DELETE
// ══════════════════════════════════════════════
var deleteTarget = null;
var deleteBtn = null;

$(document).on('click', '.btn-delete', function () {
    deleteTarget = $(this).closest('tr');
    deleteBtn = $(this);
    $('#deleteConfirmMessage').text('Are you sure you want to delete this dynamic code?');
    $('#deleteConfirmModal').modal('show');
});

$('#deleteConfirmOk').on('click', function () {
    $('#deleteConfirmModal').modal('hide');
    if (!deleteTarget) return;

    var $row = deleteTarget;
    var id = $row.data('dynamiccodeid');
    var $btn = deleteBtn.prop('disabled', true);

    $.ajax({
        type: 'POST',
        url: deleteUrl,
        data: { id: id },
        success: function (response) {
            if (response.Status === 'DELETED') {
                $row.fadeOut(300, function () { $(this).remove(); });
                showToast('Dynamic code deleted successfully.', 'success');
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

// ══════════════════════════════════════════════
//  NAMESPACE INPUT → debounced load classes
// ══════════════════════════════════════════════
$(document).on('input', '#txtNamespaces', function () {
    var ns = $(this).val();
    clearTimeout(namespaceDebounceTimer);
    namespaceDebounceTimer = setTimeout(function () {
        loadClassesForNamespaces(ns);
    }, 400);
});
