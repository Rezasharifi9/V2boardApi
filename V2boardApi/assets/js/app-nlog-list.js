$(function () {
    var dt_nlog_table = $('.datatables-nlog');
    var dt_nlog;

    function escapeHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return { '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c];
        });
    }

    function levelBadge(level) {
        var map = {
            Fatal: 'bg-label-danger',
            Error: 'bg-label-danger',
            Warn: 'bg-label-warning',
            Info: 'bg-label-info',
            Debug: 'bg-label-secondary',
            Trace: 'bg-label-secondary'
        };
        var cls = map[level] || 'bg-label-secondary';
        return "<span class='badge " + cls + "'>" + escapeHtml(level || '-') + "</span>";
    }

    if (typeof flatpickr !== 'undefined') {
        var fpOpts = {
            disableMobile: "true",
            altInput: true,
            altFormat: 'j F Y',
            dateFormat: 'Y/m/d',
            locale: 'fa'
        };
        flatpickr('#filterFromDate', fpOpts);
        flatpickr('#filterToDate', fpOpts);
    }

    if (dt_nlog_table.length) {
        dt_nlog = dt_nlog_table.DataTable({
            ajax: {
                url: '/App/Settings/GetNLogs',
                type: 'POST',
                data: function (d) {
                    d.filterLevel = $('#filterLevel').val() || '';
                    d.filterHasException = $('#filterHasException').val() || '';
                    d.filterHttpMethod = $('#filterHttpMethod').val() || '';
                    d.filterFromDate = $('#filterFromDate').val() || '';
                    d.filterToDate = $('#filterToDate').val() || '';
                    d.filterLogger = $('#filterLogger').val() || '';
                    d.filterUserName = $('#filterUserName').val() || '';
                    d.filterIp = $('#filterIp').val() || '';
                    d.filterMessage = $('#filterMessage').val() || '';
                    d.filterController = $('#filterController').val() || '';
                    d.filterAction = $('#filterAction').val() || '';
                    return d;
                },
                error: function () {
                    location.replace(location.href);
                }
            },
            processing: true,
            serverSide: true,
            columns: [
                { data: '' },
                { data: 'Level' },
                { data: 'Logged' },
                { data: 'Logger' },
                { data: 'Message' },
                { data: 'UserName' },
                { data: 'IpAddress' },
                { data: 'Controller' },
                { data: '' }
            ],
            columnDefs: [
                {
                    className: 'control',
                    orderable: false,
                    searchable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function () {
                        return '';
                    }
                },
                {
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full) {
                        var badge = levelBadge(full['Level']);
                        if (full['HasException']) {
                            badge += " <i class='ti ti-alert-triangle text-danger' title='دارای Exception'></i>";
                        }
                        return badge;
                    }
                },
                {
                    targets: 2,
                    responsivePriority: 3,
                    render: function (data, type, full) {
                        return "<span>" + escapeHtml(full['Logged']) + "</span>";
                    }
                },
                {
                    targets: 3,
                    render: function (data, type, full) {
                        return "<span dir='ltr'>" + escapeHtml(full['Logger'] || '-') + "</span>";
                    }
                },
                {
                    targets: 4,
                    orderable: false,
                    responsivePriority: 4,
                    render: function (data, type, full) {
                        return "<span title='" + escapeHtml(full['Message']) + "'>" + escapeHtml(full['Message'] || '-') + "</span>";
                    }
                },
                {
                    targets: 5,
                    render: function (data, type, full) {
                        return "<span>" + escapeHtml(full['UserName'] || '-') + "</span>";
                    }
                },
                {
                    targets: 6,
                    render: function (data, type, full) {
                        return "<span dir='ltr'>" + escapeHtml(full['IpAddress'] || '-') + "</span>";
                    }
                },
                {
                    targets: 7,
                    orderable: false,
                    render: function (data, type, full) {
                        var method = full['HttpMethod'] ? "<span class='badge bg-label-primary me-1'>" + escapeHtml(full['HttpMethod']) + "</span>" : "";
                        var ctrl = escapeHtml(full['Controller'] || '-');
                        var action = escapeHtml(full['Action'] || '-');
                        return method + "<span dir='ltr'>" + ctrl + " / " + action + "</span>";
                    }
                },
                {
                    targets: -1,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full) {
                        return '<a href="javascript:void(0);" data-bs-toggle="popover" title="جزئیات لاگ" data-id="' + full['Id'] + '" class="btn btn-sm btn-icon item-nlog-detail"><i class="text-primary ti ti-eye"></i></a>';
                    }
                }
            ],
            order: [[2, 'desc']],
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            drawCallback: function () {
                $('[data-bs-toggle="popover"]').tooltip();
            },
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات لاگ ' + (data['Id'] || '');
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col) {
                            return col.title !== ''
                                ? '<tr data-dt-row="' + col.rowIndex + '" data-dt-column="' + col.columnIndex + '"><td>' + col.title + '</td><td>' + col.data + '</td></tr>'
                                : '';
                        }).join('');
                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            }
        });
    }

    $('#btnApplyNLogFilters').on('click', function () {
        if (dt_nlog) {
            dt_nlog.ajax.reload();
        }
    });

    $('#btnFilterAndroidAppLogs').on('click', function () {
        $('#filterLogger').val('AndroidApp');
        if (dt_nlog) {
            dt_nlog.ajax.reload();
        }
    });

    $('#btnClearNLogFilters').on('click', function () {
        $('#filterLevel, #filterHasException, #filterHttpMethod').val('');
        $('#filterLogger, #filterUserName, #filterIp, #filterMessage, #filterController, #filterAction').val('');
        var fromEl = document.querySelector('#filterFromDate');
        var toEl = document.querySelector('#filterToDate');
        var fromPicker = fromEl && fromEl._flatpickr;
        var toPicker = toEl && toEl._flatpickr;
        if (fromPicker) { fromPicker.clear(); } else { $('#filterFromDate').val(''); }
        if (toPicker) { toPicker.clear(); } else { $('#filterToDate').val(''); }
        if (dt_nlog) {
            dt_nlog.ajax.reload();
        }
    });

    $('body').on('click', '.item-nlog-detail', function () {
        var id = $(this).attr('data-id');
        $('.dtr-bs-modal').modal('hide');
        if (typeof BodyBlockUI === 'function') {
            BodyBlockUI();
        }

        $.get('/App/Settings/GetNLogDetail', { id: id }, function (html) {
            if (typeof BodyUnblockUI === 'function') {
                BodyUnblockUI();
            }
            $('#nlogDetailsBody').html(html);
            $('#nlogDetailsModal').modal('show');
        }).fail(function () {
            if (typeof BodyUnblockUI === 'function') {
                BodyUnblockUI();
            }
            Swal.fire({
                title: 'ناموفق',
                text: 'دریافت جزئیات لاگ با خطا مواجه شد',
                icon: 'error',
                customClass: { confirmButton: 'btn btn-primary waves-effect waves-light' },
                buttonsStyling: false
            });
        });
    });

    setTimeout(function () {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
