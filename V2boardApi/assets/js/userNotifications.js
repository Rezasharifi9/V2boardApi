/**
 * DataTables Advanced (jquery)
 */

'use strict';
let fv, fv_edit;
$(function () {


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


    $('[data-bs-toggle="tooltip"]').tooltip();

    $("#usersSelect").wrap('<div class="position-relative"></div>').select2({
        placeholder: 'انتخاب نماینده',
        dropdownParent: $("#usersSelect").parent(),
        allowClear: true
    });


    GetUsersSelect('#usersSelect');

    var dt_responsive_table = $('.dt-responsive')
    // Responsive Table
    // --------------------------------------------------------------------

    if (dt_responsive_table.length) {
        var dt_responsive = dt_responsive_table.DataTable({
            ajax: '/App/UserNotifications/List',
            error: function (state) {
                console.log(state);
            },
            initComplete: function (setting, json) {

                //تولتیپ کردن بعد از لود دیتا
                $('[data-bs-toggle="tooltip"]').tooltip();
            },
            drawCallback: function (settings) {
                //تولتیپ کردن بعد از تغییر صفحه یا سرچ
                $('[data-bs-toggle="tooltip"]').tooltip();

            },
            columns: [
                { data: '' },
                { data: 'tbNoti_Title' },
                { data: 'tbNoti_Text' },
                { data: 'tbNoti_RegisterDate' },
                { data: 'tbNoti_User' },
                { data: 'tbNoti_UserSeen' },
                { data: 'tbNoti_Status' },
                { data: 'tbNoti_EndDate' }
            ],
            columnDefs: [
                {
                    className: 'control',
                    orderable: false,
                    targets: 0,
                    searchable: false,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // Text
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $invoice_status = full['tbNoti_Text']
                        return (
                            "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                            $invoice_status +
                            "</span>'>" +
                            "<i class='menu-icon tf-icons ti ti-text-recognition'></i>" +
                            '</span>'
                        );
                    }
                },
                {
                    // tbNoti_User
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $tbNoti_User = full['tbNoti_User']
                        return (
                            "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                            $tbNoti_User +
                            "</span>'>" +
                            "<i class='menu-icon tf-icons ti ti-users-group'></i>" +
                            '</span>'
                        );
                    }
                },
                {
                    // tbNoti_User
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $tbNoti_User = full['tbNoti_UserSeen'];
                        if ($tbNoti_User) {
                            return (
                                "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                                $tbNoti_User +
                                "</span>'>" +
                                "<i class='menu-icon tf-icons ti ti-eye-check'></i>" +
                                '</span>'
                            );
                        }
                        else {
                            return (
                                "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                                "هنوز دیده نشده" +
                                "</span>'>" +
                                "<i class='menu-icon tf-icons ti ti-eye-check'></i>" +
                                '</span>'
                            );
                        }

                    }
                },
                {
                    // Status
                    targets: 6,
                    render: function (data, type, full, meta) {
                        var $status_number = full['tbNoti_Status'];
                        var $status = {
                            0: { title: 'آرشیو شده', class: 'bg-label-warning' },
                            1: { title: 'فعال', class: 'bg-label-success' },
                        };
                        if (typeof $status[$status_number] === 'undefined') {
                            return data;
                        }
                        return (
                            '<span class="badge ' + $status[$status_number].class + '">' + $status[$status_number].title + '</span>'
                        );
                    }
                },
                {
                    // Actions
                    targets: -1,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<a data-bs-toggle="tooltip" title="حذف" class="btn btn-sm btn-icon item-delete" data-id="' + full["tbNoti_ID"] + '"><i class="text-primary ti ti-trash"></i></a>' +
                            '<a data-bs-toggle="tooltip" title="ویرایش" class="btn btn-sm btn-icon item-edit EditPlan" data-id="' + full["tbNoti_ID"] + '" data-bs-toggle="offcanvas" data-bs-target="#Add-Or-EditPlan"><i class="text-primary ti ti-pencil"></i></a>'
                        );
                    }
                }
            ],
            // scrollX: true,
            destroy: true,
            dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['full_name'];
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

        //مربوط به نمایش مودال ویرایش اطلاعیه
        $('body').on('click', '.item-edit', function () {


            $(".dtr-bs-modal").modal("hide");

            var Noti_ID = $(this).attr("data-id");
            document.getElementById('addNewUserNotiForm').reset();
            $("#modalCenter").modal("show");

            BodyBlockUI();

            AjaxGet('/App/UserNotifications/Edit?Noti_ID=' + Noti_ID).then(res => {

                if (res.status == "success") {

                    $("#addNewUserNotiForm input[name='Noti_ID']").val(Noti_ID);
                    var data = res.data;
                    console.log(data);
                    for (var key in data) {
                        if (data.hasOwnProperty(key)) {
                            var input = $('#addNewUserNotiForm input[name=' + key + ']');

                            if (key == "endDate") {
                                flatpickrDate = document.querySelector('#addNewUserNotiForm #endDate');

                                flatpickrDate.flatpickr({
                                    disableMobile: "true",
                                    monthSelectorType: 'static',
                                    locale: 'fa',
                                    altFormat: 'Y/m/d',
                                    defaultDate: data[key]
                                });
                            }
                            else if (key == "usersSelect") {
                                console.log(data[key]);
                                SelectUser('#usersSelect', data[key]);
                            }
                            else {
                                input.val(data[key]);
                            }


                        }
                    }

                }
                else {
                    eval(res.data);
                }
                BodyUnblockUI();
            });



        });

        //حذف اطلاعیه
        $('body').on('click', '.item-delete', function () {

            var noti_id = $(this).attr("data-id");

            Swal.fire({
                title: 'هشدار',
                text: "مطمئنی میخای اطلاعیه رو حذف کنی ؟",
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

                    AjaxGet("/App/UserNotifications/delete?noti_id=" + noti_id).then(res => {

                        BodyUnblockUI();
                        eval(res.data);
                        if (res.status == "success") {

                            dt_responsive.ajax.reload(null, false);
                        }


                    });

                }
            });



        });
    }


    const addNewUserNotiForm = document.getElementById('addNewUserNotiForm');

    // Form validation for Add new record
    fv = FormValidation.formValidation(addNewUserNotiForm, {
        fields: {
            Noti_Title: {
                validators: {
                    notEmpty: {
                        message: 'عنوان اطلاعیه را وارد کنید'
                    },
                    stringLength: {
                        max: 30, // حداکثر تعداد کاراکترها
                        message: 'طول ورودی باید حداکثر 30 کاراکتر باشد'
                    }
                }
            },
            endDate: {
                validators: {
                    notEmpty: {
                        message: 'تاریخ انقضا اطلاعیه را وارد کنید'
                    }
                }
            },
            usersSelect: {
                validators: {
                    notEmpty: {
                        message: 'نمایندگان را انتخاب کنید'
                    }
                }
            },
            Noti_Text: {
                validators: {
                    notEmpty: {
                        message: 'متن اطلاعیه را وارد کنید'
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
        AjaxFormPost('/App/UserNotifications/CreateOrEdit', "#addNewUserNotiForm").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('addNewUserNotiForm').reset();
                $("#modalCenter").modal("hide");
                dt_responsive.ajax.reload(null, false);

            }
            else {
                eval(res.data);
            }

        });
    });

    // اگر یک نماینده انتخاب شود پیغام خطا برداشته می شود
    $('#usersSelect').on('change', function () {
        var selectedItems = $(this).val(); // دریافت آیتم‌های انتخاب شده

        if (selectedItems && selectedItems.length > 0) {
            // غیرفعال کردن اعتبارسنجی برای این فیلد
            fv.updateFieldStatus('usersSelect', 'NotValidated');
        } else {
            // فعال کردن اعتبارسنجی در صورت عدم انتخاب آیتم‌ها
            fv.updateFieldStatus('usersSelect', 'Invalid', 'notEmpty');
        }
    });




    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 200);
});
