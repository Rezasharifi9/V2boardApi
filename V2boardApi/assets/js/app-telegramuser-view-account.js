/**
 * App User View - Account (jquery)
 */

$(function () {
    'use strict';

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
        var dt_invoice = dt_orders_table.DataTable({
            ajax: '/App/TelegramUsers/Orders?user_id=' + getUrlParameter("user_id"), // JSON file to add data
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'SubName' },
                { data: 'Plan' },
                { data: 'CreateDate' },
                { data: 'Price' },
                { data: 'Status' }
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
                        if ($State == 1) {
                            return (
                                "<span class='badge bg-label-success'>" + "انجام شده" + "</span>"
                            );
                        }
                        if ($State == 0) {
                            return (
                                "<span class='badge bg-label-primary'>" + "در صف پرداخت" + "</span>"
                            );
                        } else {
                            return (
                                "<span class='badge bg-label-warning'>" + "در صف رزرو" + "</span>"
                            );
                        }  
                    }
                }
                //{
                //    // Actions
                //    targets: -1,
                //    title: 'عملیات',
                //    orderable: false,
                //    render: function (data, type, full, meta) {
                //        return (
                //            '<div class="d-flex align-items-center">' +
                //            '<a href="javascript:;" class="text-body" data-bs-toggle="tooltip" title="ارسال ایمیل"><i class="ti ti-mail me-2 ti-sm"></i></a>' +
                //            '<a href="app-invoice-preview.html" class="text-body" data-bs-toggle="tooltip" title="نمایش"><i class="ti ti-eye mx-2 ti-sm"></i></a>' +
                //            '<div class="d-inline-block">' +
                //            '<a href="javascript:;" class="btn btn-sm btn-icon dropdown-toggle hide-arrow text-body" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical"></i></a>' +
                //            '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                //            '<li><a href="javascript:;" class="dropdown-item">جزئیات</a></li>' +
                //            '<li><a href="javascript:;" class="dropdown-item">بایگانی</a></li>' +
                //            '<div class="dropdown-divider"></div>' +
                //            '<li><a href="javascript:;" class="dropdown-item text-danger delete-record">حذف</a></li>' +
                //            '</ul>' +
                //            '</div>' +
                //            '</div>'
                //        );
                //    }
                //}
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
                            return 'جزئیات ' + data['Price'];
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
        var dt_accounts = dt_accounts_table.DataTable({
            ajax: '/App/TelegramUsers/Accounts?user_id=' + getUrlParameter("user_id"), // JSON file to add data
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'V2boardUsername' },
                { data: 'UsedVolume' },
                { data: 'RemainingVolume' },
                { data: 'TotalVolume' },
                { data: 'ExpireDate' },
                { data: 'State' }
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
                }
                //{
                //    // Actions
                //    targets: -1,
                //    title: 'عملیات',
                //    orderable: false,
                //    render: function (data, type, full, meta) {
                //        return (
                //            '<div class="d-flex align-items-center">' +
                //            '<a href="javascript:;" class="text-body" data-bs-toggle="tooltip" title="ارسال ایمیل"><i class="ti ti-mail me-2 ti-sm"></i></a>' +
                //            '<a href="app-invoice-preview.html" class="text-body" data-bs-toggle="tooltip" title="نمایش"><i class="ti ti-eye mx-2 ti-sm"></i></a>' +
                //            '<div class="d-inline-block">' +
                //            '<a href="javascript:;" class="btn btn-sm btn-icon dropdown-toggle hide-arrow text-body" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical"></i></a>' +
                //            '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                //            '<li><a href="javascript:;" class="dropdown-item">جزئیات</a></li>' +
                //            '<li><a href="javascript:;" class="dropdown-item">بایگانی</a></li>' +
                //            '<div class="dropdown-divider"></div>' +
                //            '<li><a href="javascript:;" class="dropdown-item text-danger delete-record">حذف</a></li>' +
                //            '</ul>' +
                //            '</div>' +
                //            '</div>'
                //        );
                //    }
                //}
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
                            return 'جزئیات ' + data['Price'];
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




    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
