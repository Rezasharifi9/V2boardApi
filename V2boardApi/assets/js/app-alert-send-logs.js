$(function () {
    var dtTable = $('.datatables-alert-logs');
    var dt;

    if (typeof flatpickr !== 'undefined') {
        var fpOpts = {
            disableMobile: 'true',
            altInput: true,
            altFormat: 'j F Y',
            dateFormat: 'Y/m/d',
            locale: 'fa'
        };
        flatpickr('#filterAlertFromDate', fpOpts);
        flatpickr('#filterAlertToDate', fpOpts);
    }

    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
        });
    }

    if (dtTable.length) {
        dt = dtTable.DataTable({
            ajax: {
                url: '/App/Settings/GetAlertSendLogs',
                type: 'POST',
                data: function (d) {
                    d.filterAlertRecipient = $('#filterAlertRecipient').val() || '';
                    d.filterAlertType = $('#filterAlertType').val() || '';
                    d.filterAlertStatus = $('#filterAlertStatus').val() || '';
                    d.filterAlertFromDate = $('#filterAlertFromDate').val() || '';
                    d.filterAlertToDate = $('#filterAlertToDate').val() || '';
                    return d;
                }
            },
            processing: true,
            serverSide: true,
            order: [[2, 'desc']],
            columns: [
                { data: '' },
                { data: 'Recipient' },
                { data: 'SentAt' },
                { data: 'AlertType' },
                { data: 'Message' },
                { data: 'IsSuccess' }
            ],
            columnDefs: [
                {
                    className: 'control',
                    orderable: false,
                    searchable: false,
                    targets: 0,
                    render: function () {
                        return '';
                    }
                },
                {
                    targets: 1,
                    render: function (data, type, full) {
                        var name = escapeHtml(full.Recipient || '—');
                        var chatId = full.ChatId ? '<small class="text-muted d-block" dir="ltr">' + escapeHtml(full.ChatId) + '</small>' : '';
                        return '<div>' + name + chatId + '</div>';
                    }
                },
                {
                    targets: 2,
                    render: function (data) {
                        return '<span dir="ltr">' + escapeHtml(data || '—') + '</span>';
                    }
                },
                {
                    targets: 3,
                    render: function (data) {
                        return escapeHtml(data || '—');
                    }
                },
                {
                    targets: 4,
                    orderable: false,
                    render: function (data, type, full) {
                        var preview = escapeHtml(data || '—');
                        return '<div class="d-flex align-items-start gap-1">' +
                            '<span class="text-wrap">' + preview + '</span>' +
                            '<button type="button" class="btn btn-sm btn-icon btn-label-secondary ms-1 btn-alert-log-detail" title="مشاهده متن کامل" data-id="' + full.Id + '">' +
                            '<i class="ti ti-eye ti-xs"></i></button></div>';
                    }
                },
                {
                    targets: 5,
                    render: function (data, type, full) {
                        if (full.IsSuccess) {
                            return '<span class="badge bg-label-success">موفق</span>';
                        }
                        var error = full.Error ? '<small class="text-danger d-block mt-1">' + escapeHtml(full.Error) + '</small>' : '';
                        return '<span class="badge bg-label-danger">ناموفق</span>' + error;
                    }
                }
            ]
        });
    }

    $('#btnApplyAlertLogFilters').on('click', function () {
        if (dt) dt.ajax.reload();
    });

    $('#btnClearAlertLogFilters').on('click', function () {
        $('#filterAlertRecipient, #filterAlertType').val('');
        $('#filterAlertStatus').val('');
        var fromPicker = document.querySelector('#filterAlertFromDate') && document.querySelector('#filterAlertFromDate')._flatpickr;
        var toPicker = document.querySelector('#filterAlertToDate') && document.querySelector('#filterAlertToDate')._flatpickr;
        if (fromPicker) fromPicker.clear(); else $('#filterAlertFromDate').val('');
        if (toPicker) toPicker.clear(); else $('#filterAlertToDate').val('');
        if (dt) dt.ajax.reload();
    });

    $('body').on('click', '.btn-alert-log-detail', function () {
        var id = $(this).data('id');
        var rowData = null;
        if (dt) {
            dt.rows().every(function () {
                var d = this.data();
                if (d && d.Id == id) {
                    rowData = d;
                    return false;
                }
            });
        }
        if (!rowData) {
            return;
        }

        var html =
            '<div class="text-start">' +
            '<p class="mb-1"><strong>گیرنده:</strong> ' + escapeHtml(rowData.Recipient) + '</p>' +
            '<p class="mb-1"><strong>تاریخ:</strong> <span dir="ltr">' + escapeHtml(rowData.SentAt) + '</span></p>' +
            '<p class="mb-1"><strong>نوع هشدار:</strong> ' + escapeHtml(rowData.AlertType) + '</p>' +
            '<p class="mb-2"><strong>وضعیت:</strong> ' + (rowData.IsSuccess ? '<span class="text-success">موفق</span>' : '<span class="text-danger">ناموفق</span>') + '</p>' +
            (rowData.Error ? '<p class="mb-2 text-danger"><strong>علت:</strong> ' + escapeHtml(rowData.Error) + '</p>' : '') +
            '<pre class="bg-lighter p-3 rounded text-wrap" style="white-space:pre-wrap;text-align:right;">' + escapeHtml(rowData.MessageFull || '') + '</pre>' +
            '</div>';

        Swal.fire({
            title: 'جزئیات ارسال هشدار',
            html: html,
            width: '42rem',
            confirmButtonText: 'بستن',
            customClass: { confirmButton: 'btn btn-primary' }
        });
    });
});
