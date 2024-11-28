/**
 * App Invoice List (jquery)
 */

'use strict';
let fv, fv_edit;
$(function () {


    // Variable declaration for table
    var dt_invoice_table = $('.invoice-list-table');

    // Invoice datatable
    if (dt_invoice_table.length) {
        var dt_invoice = dt_invoice_table.DataTable({
            ajax: '/App/UserFactors/GetInvoicesUserAgent', // JSON file to add data
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'ID' },
                { data: 'CreateTime' },
                { data: 'Price' },
                { data: 'Status' },
                { data: '' }
            ],
            initComplete: function (setting, json) {

                //تولتیپ کردن بعد از لود دیتا
                $('[data-bs-toggle="popover"]').tooltip();
            },
            drawCallback: function (settings) {
                //تولتیپ کردن بعد از تغییر صفحه یا سرچ
                $('[data-bs-toggle="popover"]').tooltip();

            },
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    responsivePriority: 2,
                    searchable: false,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // Invoice ID
                    targets: 1,
                    render: function (data, type, full, meta) {
                        var $invoice_id = full['ID'];

                        var $row_output = '<a href="javascript:;">' + $invoice_id + '</a>';
                        return $row_output;
                    }
                },
                {
                    // Due Date
                    targets: 2,
                    render: function (data, type, full, meta) {
                        return full['CreateTime'];
                    }
                },
                {
                    // Total Invoice Amount
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $total = full['Price'];
                        return '<span class="d-none">' + $total + '</span>' + $total + ' ءتء';
                    }
                },
                {
                    // Total Invoice Amount
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $Desc = full['Desc'];
                        return (
                            '<div class="d-flex align-items-center">' +
                            '<a data-bs-toggle="popover" title="' + $Desc +'" class="btn btn-sm btn-icon"/><i class="text-primary ti ti-text-recognition"></i>' +
                            '</div>'
                        );
                    }

                },
                {
                    // Client Balance/Status
                    targets: 5,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        var $balance = full['Status'];
                        console.log($balance);
                        if ($balance === 1) {
                            var $badge_class = 'bg-label-warning';
                            return '<span class="badge ' + $badge_class + '" > تائید توسط کاربر </span>';
                        }
                        else if ($balance === 2) {
                            var $badge_class = 'bg-label-success';
                            return '<span class="badge ' + $badge_class + '" > تائید پرداخت </span>';
                        }
                        else if ($balance === 3) {
                            var $badge_class = 'bg-label-info';
                            return '<span class="badge ' + $badge_class + '" > کسر از بدهی </span>';
                        }

                    }
                },
                {
                    // Actions
                    targets: 6,
                    title: 'عملیات',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<div class="d-flex align-items-center">' +
                            '<a href="/App/UserFactors/download2?factor_id=' + full['ID'] + '" data-bs-toggle="popover" title="دانلود رسید" class="btn btn-sm btn-icon"/><i class="text-primary ti ti-download"></i>' +
                            '</div>'
                        );

                        //return (
                        //    '<div class="d-flex align-items-center">' +
                        //    '<div class="dropdown">' +
                        //    '<a href="javascript:;" class="btn dropdown-toggle hide-arrow text-body p-0" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm"></i></a>' +
                        //    '<div class="dropdown-menu dropdown-menu-end">' +
                        //    '<a href="/App/UserFactors/download?factor_id=' + full['ID'] + '" class="dropdown-item">دانلود رسید</a>' +
                        //    '<a href="app-invoice-edit.html" class="dropdown-item">کسر از بدهی</a>' +
                        //    '<div class="dropdown-divider"></div>' +
                        //    '<a href="javascript:;" class="dropdown-item delete-record text-danger" data-id="' + full['ID'] + '">حذف</a>' +
                        //    '</div>' +
                        //    '</div>' +
                        //    '</div>'
                        //);
                    }
                }
            ],
            order: [[3, 'desc']],
            buttons: [
                
            ],
            dom:
                '<"row me-2"' +
                '<"col-md-2"<"me-3"l>>' +
                '<"col-md-10"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start d-flex align-items-center justify-content-end flex-md-row flex-column mb-3 mb-md-0"fB>>' +
                '>t' +
                '<"row mx-2"' +
                '<"col-sm-12 col-md-6"i>' +
                '<"col-sm-12 col-md-6"p>' +
                '>',
            language: {
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جستجو..'
            }
        });
    }

    // Filter form control to default size
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
