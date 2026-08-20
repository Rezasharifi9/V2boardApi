// لیست کاربران موبایل (dataTable)
$(function () {
    var dt_devices_table = $('.datatables-devices');
    var dt_devices;

    if (dt_devices_table.length) {
        dt_devices = dt_devices_table.DataTable({
            ajax: {
                url: '/App/MobileUsers/GetAll',
                type: 'POST',
                data: function (d) {
                    d.filterDevice = $('#filterDevice').val() || '';
                    d.filterAgent = $('#filterAgent').val() || '';
                    d.filterPush = $('#filterPush').val() || '';
                    d.filterActive = $('#filterActive').val() || '';
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
                { data: 'Device' },
                { data: 'Agent' },
                { data: 'AppVersion' },
                { data: 'AndroidVersion' },
                { data: 'LastSeenDate' },
                { data: 'PushReady' },
                { data: 'FactorCount' },
                { data: 'IsActive' },
                { data: '' }
            ],
            columnDefs: [
                {
                    // For Responsive
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
                    // دستگاه
                    targets: 1,
                    responsivePriority: 3,
                    render: function (data, type, full) {
                        var $rooted = full['Rooted']
                            ? " <span class='badge bg-label-danger' title='دستگاه روت شده'>Root</span>"
                            : "";
                        return "<a href='javascript:void(0);' class='device-details' data-id=" + full['Id'] + ">"
                             + "<i class='ti ti-device-mobile me-1'></i>" + full['Device'] + "</a>" + $rooted;
                    }
                },
                {
                    // نماینده
                    targets: 2,
                    render: function (data, type, full) {
                        return "<span dir='ltr'>" + full['Agent'] + "</span>";
                    }
                },
                {
                    // نسخه برنامه
                    targets: 3,
                    render: function (data, type, full) {
                        return "<span dir='ltr'>" + full['AppVersion'] + "</span>";
                    }
                },
                {
                    // نسخه اندروید
                    targets: 4,
                    render: function (data, type, full) {
                        return "<span dir='ltr'>" + full['AndroidVersion'] + "</span>";
                    }
                },
                {
                    // آخرین بازدید
                    targets: 5,
                    render: function (data, type, full) {
                        return "<span>" + full['LastSeenDate'] + "</span>";
                    }
                },
                {
                    // آمادگی نوتیفیکیشن
                    targets: 6,
                    render: function (data, type, full) {
                        return full['PushReady']
                            ? "<span class='badge bg-label-success'>آماده</span>"
                            : "<span class='badge bg-label-secondary'>ندارد</span>";
                    }
                },
                {
                    // تعداد فاکتور
                    targets: 7,
                    render: function (data, type, full) {
                        return "<span class='badge bg-label-primary'>" + full['FactorCount'] + "</span>";
                    }
                },
                {
                    // وضعیت
                    targets: 8,
                    render: function (data, type, full) {
                        return full['IsActive']
                            ? "<span class='badge bg-label-success'>فعال</span>"
                            : "<span class='badge bg-label-secondary'>غیرفعال</span>";
                    }
                },
                {
                    // عملیات
                    targets: -1,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full) {
                        var $toggleTitle = full['IsActive'] ? 'غیرفعال کردن' : 'فعال کردن';
                        var $toggleIcon = full['IsActive'] ? 'ti-toggle-right text-success' : 'ti-toggle-left text-secondary';
                        return (
                            '<a href="javascript:void(0);" title="جزئیات" data-id=' + full['Id'] + ' class="btn btn-sm btn-icon device-details"><i class="text-primary ti ti-eye"></i></a>' +
                            '<a href="javascript:void(0);" title="' + $toggleTitle + '" data-id=' + full['Id'] + ' class="btn btn-sm btn-icon device-toggle"><i class="ti ' + $toggleIcon + '"></i></a>'
                        );
                    }
                }
            ],
            order: [[5, 'desc']],
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            return 'جزئیات ' + row.data()['Device'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col) {
                            return col.title !== ''
                                ? '<tr data-dt-row="' + col.rowIndex + '" data-dt-column="' + col.columnIndex + '">' +
                                  '<td>' + col.title + ':</td> <td>' + col.data + '</td></tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            }
        });

        $('div.head-label').html('<h5 class="card-title mb-0">کاربران موبایل</h5>');
    }

    $('#btnApplyDeviceFilters').on('click', function () {
        if (dt_devices) { dt_devices.ajax.reload(); }
    });

    $('#btnClearDeviceFilters').on('click', function () {
        $('#filterDevice, #filterAgent').val('');
        $('#filterPush, #filterActive').val('');
        if (dt_devices) { dt_devices.ajax.reload(); }
    });

    // جزئیات دستگاه — پارشال HTML مستقیم داخل مودال لود می شود
    $('body').on('click', '.device-details', function () {
        var device_id = $(this).attr('data-id');

        $('.dtr-bs-modal').modal('hide');
        BodyBlockUI();

        $.get('/App/MobileUsers/Details?device_id=' + device_id, function (html) {
            BodyUnblockUI();
            $('#deviceDetailsBody').html(html);
            $('#deviceDetailsModal').modal('show');
        }).fail(function () {
            BodyUnblockUI();
            Swal.fire({
                title: 'ناموفق',
                text: 'دریافت جزئیات دستگاه با خطا مواجه شد',
                icon: 'error',
                customClass: { confirmButton: 'btn btn-primary waves-effect waves-light' },
                buttonsStyling: false
            });
        });
    });

    // فعال / غیرفعال کردن دستگاه
    $('body').on('click', '.device-toggle', function () {
        var device_id = $(this).attr('data-id');

        Swal.fire({
            title: 'هشدار',
            text: 'وضعیت این دستگاه تغییر کند؟',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'بله',
            cancelButtonText: 'بازگشت',
            customClass: {
                confirmButton: 'btn btn-primary me-3 waves-effect waves-light',
                cancelButton: 'btn btn-label-secondary waves-effect waves-light'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (result.value) {
                BodyBlockUI();

                AjaxPost('/App/MobileUsers/ToggleActive?device_id=' + device_id).then(res => {
                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status == 'success') {
                        dt_devices.ajax.reload(null, false);
                    }
                });
            }
        });
    });

    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
