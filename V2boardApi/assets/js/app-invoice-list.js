/**
 * App Invoice List (jquery)
 */

'use strict';
let fv, fv_edit;
$(function () {

    $("#usersSelect").wrap('<div class="position-relative"></div>').select2({
        placeholder: 'انتخاب نماینده',
        dropdownParent: $("#usersSelect").parent(),
        allowClear: false
    });

    GetUsersSelect('#usersSelect');

    var maxlengthInput = $('.bootstrap-maxlength-example'),
        formRepeater = $('.form-repeater');

    // Bootstrap Max Length
    // --------------------------------------------------------------------
    if (maxlengthInput.length) {
        maxlengthInput.each(function () {
            $(this).maxlength({
                warningClass: 'label label-success bg-success text-white',
                limitReachedClass: 'label label-danger',
                separator: ' از ',
                preText: 'شما ',
                postText: ' کارکتر مجاز تایپ کرده اید.',
                validate: true,
                threshold: +this.getAttribute('maxlength')
            });
        });
    }




    // Variable declaration for table
    var dt_invoice_table = $('.invoice-list-table');

    // Invoice datatable
    if (dt_invoice_table.length) {
        var dt_invoice = dt_invoice_table.DataTable({
            ajax: '/App/UserFactors/GetInvoices', // JSON file to add data
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'ID' },
                { data: 'Username' },
                { data: 'CreateTime' },
                { data: 'Price' },
                { data: 'Status' }
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

                        var $row_output = '<a href="app-invoice-preview.html">#' + $invoice_id + '</a>';
                        return $row_output;
                    }
                },
                {
                    // Client name and Service
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $name = full['Username'],
                            $service = full['Role'];

                        var $row_output =
                            '<div class="d-flex justify-content-start align-items-center">' +
                            '<div class="d-flex flex-column">' +
                            '<a href="pages-profile-user.html" class="text-body text-truncate"><span class="fw-medium">' +
                            $name +
                            '</span></a>' +
                            '<small class="text-truncate text-muted">' +
                            $service +
                            '</small>' +
                            '</div>' +
                            '</div>';
                        return $row_output;
                    }
                },
                {
                    // Due Date
                    targets: 3,
                    render: function (data, type, full, meta) {
                        return full['CreateTime'];
                    }
                },
                {
                    // Total Invoice Amount
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $total = full['Price'];
                        return '<span class="d-none">' + $total + '</span>' + $total + ' ءتء';
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
                            '<a href="/App/UserFactors/download?factor_id=' + full['ID'] + '" data-bs-toggle="popover" title="دانلود رسید" class="btn btn-sm btn-icon"/><i class="text-primary ti ti-download"></i>' +
                            '<a href="javascript:;" data-bs-toggle="popover" title="حذف رسید" class="btn btn-sm btn-icon delete-record" data-id="' + full['ID'] + '"/><i class="text-primary ti ti-trash"></i>' +
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
            },
            // Buttons with Dropdown
            buttons: [
                {
                    text: '<i class="ti ti-plus me-md-1"></i><span class="d-md-inline-block d-none">افزودن پرداخت</span>',
                    className: 'btn btn-primary waves-effect waves-light',
                    action: function (e, dt, button, config) {
                        $("#modalAddFactor").modal("show");
                    }
                }
            ]
        });
    }


    const factorForm = document.getElementById('factorForm');

    // Form validation for Add new record
    fv = FormValidation.formValidation(factorForm, {
        fields: {
            usersSelect: {
                validators: {
                    notEmpty: {
                        message: 'نماینده را انتخاب کنید'
                    }
                }
            },
            factorDate: {
                validators: {
                    notEmpty: {
                        message: 'تاریخ را وارد کنید'
                    }
                }
            },
            factorPrice: {
                validators: {
                    notEmpty: {
                        message: 'مبلغ را وارد کنید'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                eleValidClass: '',
                rowSelector: function (field, ele) {
                    return '.message-text';
                }
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        }
    });

    fv.on('core.form.valid', function (e) {

        blockUI('.section-block');

        if ($('#factorDebt').is(':checked')) {
            $('#factorDebt').val("True");
        } else {
            $('#factorDebt').val("False");
        }

        AjaxFormPostWithFiles('/App/UserFactors/CreateOrEdit', "factorForm", "factorFile").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('factorForm').reset();
                $("#modalAddFactor").modal("hide");
                dt_invoice.ajax.reload(null, false);

            }
            else {
                eval(res.data);
            }

        });
    });

    // Delete Record
    $('.invoice-list-table tbody').on('click', '.delete-record', function () {

        var factor_id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "مطمئنی میخای فاکتور پرداخت رو حذف کنی ؟",
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

                AjaxGet("/App/UserFactors/delete?factor_id=" + factor_id).then(res => {

                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status == "success") {

                        dt_invoice.ajax.reload(null, false);
                    }


                });

            }
        });


    });

    // Filter form control to default size
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});
