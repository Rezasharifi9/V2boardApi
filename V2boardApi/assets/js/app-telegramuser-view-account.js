/**
 * App User View - Account (jquery)
 */

$(function () {
    'use strict';

    var currentMoveLinkId = null;
    var dt_orders = null;
    var dt_accounts = null;

    function getActionToken() {
        return $('#telegramUserActionForm input[name="__RequestVerificationToken"]').val();
    }

    function postTelegramUserAction(url, data) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: url,
                type: 'POST',
                data: $.extend({}, data, {
                    __RequestVerificationToken: getActionToken()
                }),
                success: resolve,
                error: reject
            });
        });
    }

    function renderAccountActions(full) {
        return '<div class="d-flex align-items-center gap-1">' +
            '<button type="button" class="btn btn-sm btn-label-primary btn-move-account" ' +
            'data-link-id="' + full.LinkID + '" data-sub-name="' + full.V2boardUsername + '">' +
            '<i class="ti ti-arrows-exchange me-1"></i>انتقال</button>' +
            '<button type="button" class="btn btn-sm btn-label-danger btn-delete-account" ' +
            'data-link-id="' + full.LinkID + '" data-sub-name="' + full.V2boardUsername + '">' +
            '<i class="ti ti-trash me-1"></i>حذف</button>' +
            '</div>';
    }

    function closeResponsiveModal(callback) {
        var $responsiveModal = $('.dtr-bs-modal.show');
        if ($responsiveModal.length) {
            $responsiveModal.one('hidden.bs.modal', function () {
                if (typeof callback === 'function') callback();
            });
            $responsiveModal.modal('hide');
        } else if (typeof callback === 'function') {
            callback();
        }
    }

    function openMoveAccountModal() {
        var $modal = $('#modalMoveAccount');
        if (!$modal.parent().is('body')) {
            $modal.appendTo('body');
        }
        $modal.modal('show');
    }

    function renderOrderActions(full) {
        var status = Number(full.Status);
        var orderId = full.OrderId || full.orderId;
        if (status !== 0 || !orderId) {
            return '<span class="text-muted">—</span>';
        }
        return '<button type="button" class="btn btn-sm btn-label-danger btn-cancel-reserved-order" ' +
            'data-order-id="' + orderId + '">' +
            '<i class="ti ti-x me-1"></i>لغو رزرو</button>';
    }

    // Variable declaration for table
    var dt_project_table = $('.datatable-project'),
        dt_sub_table = $('.datatable-sub');

    // Sub datatable
    // --------------------------------------------------------------------
    if (dt_sub_table.length) {
        var dt_invoice = dt_sub_table.DataTable({
            ajax: '/App/Admin/GetUserAccountLog?user_id=' + getUrlParameter("user_id"), // JSON file to add data
            columns: [
                // columns according to JSON
                { data: 'id' },
                { data: 'SubName' },
                { data: 'Event' },
                { data: 'CreateDate' },
                { data: 'SellPrice' },
                { data: 'Plan' },
                { data: 'action' }
            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // SubName
                    targets: 1,
                    render: function (data, type, full, meta) {
                        var $invoice_id = full['SubName'];
                        // Creates full output for row
                        var $row_output = '<span>' + $invoice_id + '</span>';
                        return $row_output;
                    }
                },
                {
                    // Event
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $invoice_status = full['Event'],
                            $due_date = full['CreateDate'],
                            $balance = full['SellPrice'];
                        return (
                            "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                            $invoice_status +
                            '<br> <span class="fw-medium">مبلغ:</span> ' +
                            $balance +
                            '<br> <span class="fw-medium">تاریخ:</span> ' +
                            $due_date +
                            "</span>'>" +
                            $invoice_status +
                            '</span>'
                        );
                    }
                },
                {
                    // CreateDate
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $CreateDate = full['CreateDate'];
                        // Creates full output for row
                        var $row_output = '<span>' + $CreateDate + '</span>';
                        return $row_output;
                    }
                },
                {
                    // SellPrice
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $total = full['SellPrice'];
                        return $total + ' ءتء';
                    }
                },
                {
                    // Plan
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $Plan = full['Plan'];
                        // Creates full output for row
                        var $row_output = '<span>' + $Plan + '</span>';
                        return $row_output;
                    }
                },
                {
                    // Actions
                    targets: -1,
                    title: 'عملیات',
                    orderable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<div class="d-flex align-items-center">' +
                            '<a href="javascript:;" class="text-body" data-bs-toggle="tooltip" title="ارسال ایمیل"><i class="ti ti-mail me-2 ti-sm"></i></a>' +
                            '<a href="app-invoice-preview.html" class="text-body" data-bs-toggle="tooltip" title="نمایش"><i class="ti ti-eye mx-2 ti-sm"></i></a>' +
                            '<div class="d-inline-block">' +
                            '<a href="javascript:;" class="btn btn-sm btn-icon dropdown-toggle hide-arrow text-body" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical"></i></a>' +
                            '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                            '<li><a href="javascript:;" class="dropdown-item">جزئیات</a></li>' +
                            '<li><a href="javascript:;" class="dropdown-item">بایگانی</a></li>' +
                            '<div class="dropdown-divider"></div>' +
                            '<li><a href="javascript:;" class="dropdown-item text-danger delete-record">حذف</a></li>' +
                            '</ul>' +
                            '</div>' +
                            '</div>'
                        );
                    }
                }
            ],
            language: {
                sLengthMenu: 'نمایش _MENU_',
                search: '',
                searchPlaceholder: 'جستجوی اشتراک'
            },
            displayLength: 6,
            lengthMenu: [6, 10, 25, 50, 75, 100],
            // For responsive popup
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['SubName'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col, i) {
                            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                                ? '<tr data-dt-row="' +
                                col.rowIndex +
                                '" data-dt-column="' +
                                col.columnIndex +
                                '">' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td> ' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            }
        });
    }
    // On each datatable draw, initialize tooltip
    dt_sub_table.on('draw.dt', function () {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl, {
                boundary: document.body
            });
        });
    });

    //Invoice Table






    var dt_orders_table = $('.datatable-orders');

    // Invoice datatable
    // --------------------------------------------------------------------
    if (dt_orders_table.length) {
        dt_orders = dt_orders_table.DataTable({
            ajax: {
                url: '/App/TelegramUsers/GetOrders',
                type: 'POST',
                data: function (d) {
                    d.user_id = getUrlParameter('user_id');
                },
                error: function () {
                    location.replace(location.href);
                }
            },
            processing: true,
            serverSide: true,
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'SubName' },
                { data: 'Plan' },
                { data: 'CreateDate' },
                { data: 'Price' },
                { data: 'Status' },
                { data: 'OrderId' }
            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // SubName
                    targets: 1,
                    render: function (data, type, full, meta) {
                        var $SubName = full['SubName'];
                        // Creates full output for row
                        var $row_output = '<span>' + $SubName + '</span>';
                        return $row_output;
                    }
                },
                {
                    // Plan
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $Plan = full['Plan'];
                        return (
                            "<span>" + $Plan + "</span>"
                        );
                    }
                },
                {
                    // Plan
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $CreateDate = full['CreateDate'];
                        return (
                            "<span>" + $CreateDate + "</span>"
                        );
                    }
                },
                {
                    // Price
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $Price = full['Price'];
                        var $row_output = "<span>" + $Price + ' ءتء' + "</span>";

                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $State = full['Status'];
                        var statusObj = {
                            0: { title: 'در انتظار فعال سازی', class: 'bg-label-warning' },
                            1: { title: 'انجام شده', class: 'bg-label-success' },
                            3: { title: 'در انتظار پرداخت', class: 'bg-label-primary' }
                        };
                        var info = statusObj[$State];
                        if (!info) {
                            return "<span class='badge bg-label-secondary'>نامشخص</span>";
                        }
                        return "<span class='badge " + info.class + "'>" + info.title + "</span>";
                    }
                },
                {
                    targets: 6,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full) {
                        return renderOrderActions(full);
                    }
                }
            ],
            displayLength: 6,
            lengthMenu: [6, 10, 25, 50, 75, 100],
            language: {
                sLengthMenu: 'نمایش _MENU_',
                search: '',
                searchPlaceholder: 'جستجوی اشتراک'
            },
            // For responsive popup
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['SubName'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col, i) {
                            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                                ? '<tr data-dt-row="' +
                                col.rowIndex +
                                '" data-dt-column="' +
                                col.columnIndex +
                                '">' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td> ' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            }
        });
    }
    // On each datatable draw, initialize tooltip
    dt_orders_table.on('draw.dt', function () {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl, {
                boundary: document.body
            });
        });
    });









    var dt_accounts_table = $('.datatable-accounts');

    // Invoice datatable
    // --------------------------------------------------------------------
    if (dt_accounts_table.length) {
        dt_accounts = dt_accounts_table.DataTable({
            ajax: {
                url: '/App/TelegramUsers/GetAccounts',
                type: 'POST',
                data: function (d) {
                    d.user_id = getUrlParameter('user_id');
                },
                error: function () {
                    location.replace(location.href);
                }
            },
            processing: true,
            serverSide: true,
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'V2boardUsername' },
                { data: 'UsedVolume' },
                { data: 'RemainingVolume' },
                { data: 'TotalVolume' },
                { data: 'ExpireDate' },
                { data: 'State' },
                { data: 'LinkID' }
            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // V2boardUsername
                    targets: 1,
                    render: function (data, type, full, meta) {
                        var $V2boardUsername = full['V2boardUsername'];
                        // Creates full output for row
                        var $row_output = '<span>' + $V2boardUsername + '</span>';


                        return $row_output;
                    }
                },
                {
                    // TotalVolume
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $TotalVolume = full['TotalVolume'];
                        return (
                            "<span>" + $TotalVolume + "</span>"
                        );
                    }
                },
                {
                    // UsedVolume
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $UsedVolume = full['UsedVolume'];
                        return (
                            "<span>" + $UsedVolume + "</span>"
                        );
                    }
                },
                {
                    // RemainingVolume
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $RemainingVolume = full['RemainingVolume'];
                        return (
                            "<span>" + $RemainingVolume + "</span>"
                        );
                    }
                },
                {
                    // RemainingVolume
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $ExpireDate = full['ExpireDate'];
                        return (
                            "<span>" + $ExpireDate + "</span>"
                        );
                    }
                },
                {
                    // RemainingVolume
                    targets: 6,
                    render: function (data, type, full, meta) {
                        var $State = full['State'];
                        if ($State == 1) {
                            return (
                                "<span class='badge bg-label-success'>" + "فعال" + "</span>"
                            );
                        } else
                            if ($State == 2) {
                                return (
                                    "<span class='badge bg-label-danger'>" + "اتمام تاریخ انقضا" + "</span>"
                                );
                            }
                            else if ($State == 3) {
                                return (
                                    "<span class='badge bg-label-danger'>" + "اتمام حجم" + "</span>"
                                );
                            }
                            else if ($State == 4) {
                                return (
                                    "<span class='badge bg-label-danger'>" + "مسدود" + "</span>"
                                );
                            }
                    }
                },
                {
                    targets: 7,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full) {
                        return renderAccountActions(full);
                    }
                }
            ],
            displayLength: 6,
            lengthMenu: [6, 10, 25, 50, 75, 100],
            language: {
                sLengthMenu: 'نمایش _MENU_',
                search: '',
                searchPlaceholder: 'جستجوی اشتراک'
            },
            // For responsive popup
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['V2boardUsername'];
                        }
                    }),
                    type: 'column',
                    renderer: function (api, rowIdx, columns) {
                        var data = $.map(columns, function (col, i) {
                            return col.title !== '' // ? Do not show row in modal popup if title is blank (for check box)
                                ? '<tr data-dt-row="' +
                                col.rowIndex +
                                '" data-dt-column="' +
                                col.columnIndex +
                                '">' +
                                '<td>' +
                                col.title +
                                ':' +
                                '</td> ' +
                                '<td>' +
                                col.data +
                                '</td>' +
                                '</tr>'
                                : '';
                        }).join('');

                        return data ? $('<table class="table"/><tbody />').append(data) : false;
                    }
                }
            }
        });
    }
    // On each datatable draw, initialize tooltip
    dt_accounts_table.on('draw.dt', function () {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl, {
                boundary: document.body
            });
        });
    });




    $('body').on('click', '.btn-move-account', function () {
        var linkId = $(this).data('link-id');
        var subName = $(this).data('sub-name');
        currentMoveLinkId = linkId;

        closeResponsiveModal(function () {
            $('#moveAccountSubName').text(subName);

            $.get('/App/TelegramUsers/GetMoveAccountTargets', {
                linkId: linkId,
                currentUserId: getUrlParameter('user_id')
            }).done(function (res) {
                var $select = $('#moveAccountTarget');
                if ($select.hasClass('select2-hidden-accessible')) {
                    $select.select2('destroy');
                }
                $select.empty();
                if (!res.data || res.data.length === 0) {
                    $select.append('<option value="">مشترک دیگری یافت نشد</option>');
                } else {
                    res.data.forEach(function (item) {
                        $select.append('<option value="' + item.id + '">' + item.label + '</option>');
                    });
                }
                $select.select2({
                    dropdownParent: $('#modalMoveAccount'),
                    width: '100%',
                    placeholder: 'انتخاب مشترک'
                });
                openMoveAccountModal();
            });
        });
    });

    $('#btnConfirmMoveAccount').on('click', function () {
        var targetId = $('#moveAccountTarget').val();
        if (!currentMoveLinkId || !targetId) return;

        BodyBlockUI();
        postTelegramUserAction('/App/TelegramUsers/MoveAccount', {
            linkId: currentMoveLinkId,
            targetTelUserId: targetId
        }).then(function (res) {
            BodyUnblockUI();
            eval(res.data);
            if (res.status === 'success') {
                $('#modalMoveAccount').modal('hide');
                if (dt_accounts) dt_accounts.ajax.reload(null, false);
            }
        }).catch(function () {
            BodyUnblockUI();
            showToast('خطا', 'خطا در انتقال اشتراک', 'text-danger');
        });
    });

    $('body').on('click', '.btn-delete-account', function () {
        var linkId = $(this).data('link-id');
        var subName = $(this).data('sub-name');

        closeResponsiveModal(function () {
            Swal.fire({
            text: 'آیا مطمئن هستید می‌خواهید اشتراک «' + subName + '» را حذف کنید؟',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'بله، حذف می‌کنم',
            cancelButtonText: 'انصراف',
            customClass: {
                confirmButton: 'btn btn-danger me-3 waves-effect waves-light',
                cancelButton: 'btn btn-label-secondary waves-effect waves-light'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!(result.isConfirmed || result.value)) return;

            BodyBlockUI();
            postTelegramUserAction('/App/TelegramUsers/DeleteTelegramAccount', {
                linkId: linkId
            }).then(function (res) {
                BodyUnblockUI();
                eval(res.data);
                if (res.status === 'success' && dt_accounts) {
                    dt_accounts.ajax.reload(null, false);
                }
            }).catch(function () {
                BodyUnblockUI();
                showToast('خطا', 'خطا در حذف اشتراک', 'text-danger');
            });
        });
        });
    });

    $('body').on('click', '.btn-cancel-reserved-order', function () {
        var orderId = $(this).attr('data-order-id') || $(this).data('order-id');
        if (!orderId) {
            showToast('خطا', 'شناسه سفارش یافت نشد', 'text-danger');
            return;
        }

        closeResponsiveModal(function () {
            Swal.fire({
                text: 'آیا مطمئن هستید می‌خواهید این بسته تمدیدی را لغو کنید؟',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonText: 'بله، لغو می‌کنم',
                cancelButtonText: 'انصراف',
                customClass: {
                    confirmButton: 'btn btn-danger me-3 waves-effect waves-light',
                    cancelButton: 'btn btn-label-secondary waves-effect waves-light'
                },
                buttonsStyling: false
            }).then(function (result) {
                if (!(result.isConfirmed || result.value)) return;

                BodyBlockUI();
                postTelegramUserAction('/App/TelegramUsers/CancelReservedOrder', {
                    orderId: orderId
                }).then(function (res) {
                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status === 'success' && dt_orders) {
                        dt_orders.ajax.reload(null, false);
                    }
                }).catch(function () {
                    BodyUnblockUI();
                    showToast('خطا', 'خطا در لغو بسته تمدیدی', 'text-danger');
                });
            });
        });
    });


    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
