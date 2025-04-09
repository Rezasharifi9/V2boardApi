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
                { data: 'action' },
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


    var dt_invoice_table = $('.datatable-invoice');

    // Invoice datatable
    // --------------------------------------------------------------------
    if (dt_invoice_table.length) {
        var dt_invoice = dt_invoice_table.DataTable({
            ajax: '/App/Admin/Factors?user_id=' + getUrlParameter("user_id"), // JSON file to add data
            columns: [
                // columns according to JSON
                { data: '' },
                { data: 'PayDate' },
                { data: 'Price' },
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
                    // PayDate
                    targets: 1,
                    render: function (data, type, full, meta) {
                        var $PayDate = full['PayDate'];
                        // Creates full output for row
                        var $row_output = '<span>' + $PayDate + '</span>';
                        return $row_output;
                    }
                },
                {
                    // Price
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $due_date = full['Price'];
                        return (
                            "<span>" + $due_date + "</span>"
                        );
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
    dt_invoice_table.on('draw.dt', function () {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl, {
                boundary: document.body
            });
        });
    });


    //لیست تعرفه ها
    function Plans(selectId) {
        var $select = $(selectId);

        $.ajax({
            url: "/App/Plan/Select2Plans",
            type: "get",
            dataType: "json",
            success: function (res) {
                // پاک کردن گزینه‌های قبلی
                $select.empty();

                // افزودن گزینه‌های جدید
                $.each(res.result, function (index, item) {
                    var newOption = new Option(item.Name, item.id, false, false);
                    $select.append(newOption);
                });
            },
            error: function (xhr, status, error) {
                console.error("An error occurred: " + status + " " + error);
            }
        });
    }

    
    //جهت انتخاب تعرفه
    function SelectPlans(selectId, Ids) {
        $(selectId).val(Ids).trigger('change');
    }


    // Use plan Table
    var dt_plan = $('.datatable-plan');


    if (dt_plan.length) {

        Plans("#userPlan");

        var $this = $("#userPlan").select2();
        $this.wrap('<div class="position-relative"></div>').select2({
            placeholder: 'انتخاب تعرفه',
            dropdownParent: $this.parent(),
            allowClear: false
        });

        dt_plan = dt_plan.DataTable({
            ajax: '/App/Admin/GetPlans?user_id=' + getUrlParameter("user_id"),
            columns: [
                { data: '' },
                { data: 'UserPlan_Name' },
                { data: 'UserPlan_Price' },
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
                    orderable: false,
                    searchable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // UserPlan_Name
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['UserPlan_Name'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // UserPlan_Price
                    targets: 2,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        var $name = full['UserPlan_Price'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    targets: -1,
                    title: 'عملیات',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<a data-bs-toggle="popover" title="حذف" class="btn btn-sm btn-icon item-edit DeleteUserPlan" data-id="' + full["UserPlan_ID"] + '">' +
                            '<i class="text-primary ti ti-trash"></i></a>'
                        );
                    }
                }

            ],
            "language": {
                "paginate": {
                    "first": "اولین",
                    "last": "آخرین",
                    "next": "بعدی",
                    "previous": "قبلی"
                },
                "info": "نمایش _START_ تا _END_ از _TOTAL_ ورودی",
                "lengthMenu": "نمایش _MENU_ ورودی",
                "search": "جستجو:",
                "zeroRecords": "موردی یافت نشد",
                "infoEmpty": "هیچ موردی موجود نیست",
                "infoFiltered": "(فیلتر شده از _MAX_ ورودی)",
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جست و جو',
                loadingRecords: "در حال بارگزاری ..."
            },
            lengthChange: false,
            displayLength: 7,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['PlanName'];
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
        $('div.head-label').html('<h5 class="card-title mb-0">تعرفه ها</h5>');


        ////////////////////////////Form Section///////////////////////////////


        const phoneMaskList = document.querySelectorAll('.phone-mask'),
            addNewPlanForm = document.getElementById('addNewPlanForm');

        // Phone Number
        if (phoneMaskList) {
            phoneMaskList.forEach(function (phoneMask) {
                new Cleave(phoneMask, {
                    phone: true,
                    phoneRegionCode: 'US'
                });
            });
        }
        const fv = FormValidation.formValidation(addNewPlanForm, {
            plugins: {
                trigger: new FormValidation.plugins.Trigger(),
                bootstrap5: new FormValidation.plugins.Bootstrap5({
                    eleValidClass: '',
                    rowSelector: function (field, ele) {
                        return '.mb-3';
                    }
                }),
                submitButton: new FormValidation.plugins.SubmitButton(),
                autoFocus: new FormValidation.plugins.AutoFocus()
            }
        });

        fv.on('core.form.valid', function (e) {
            
            BodyBlockUI();
            AjaxFormPost('/App/Admin/SetPlan', "#addNewPlanForm").then(res => {
                BodyUnblockUI();
                eval(res.data);

                if (res.status == "success") {
                    dt_plan.ajax.reload(null, false);
                    document.getElementById('addNewPlanForm').reset();
                }

            });
        });



        $('body').on('click', '.DeleteUserPlan', function () {

            var id = $(this).attr("data-id");

            BodyBlockUI();

            AjaxGet("/App/Admin/DeletePlan?id=" + id).then(res => {

                BodyUnblockUI();
                eval(res.data);
                if (res.status == "success") {

                    dt_plan.ajax.reload(null, false);
                }


            });

        });




    }



    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});




/**
* DataTables Basic
*/

'use strict';

let fv, fv1, offCanvasEl;

// datatable (jquery)
$(function () {

    $('[data-bs-toggle="popover"]').tooltip();

    var dt_group_table = $('.datatables-groups');
    var dt_group;
    // DataTable with buttons
    // --------------------------------------------------------------------


    if (dt_group_table.length) {
        dt_group = dt_group_table.DataTable({
            ajax: '/App/ServerGroups/GetUserGroups?user_id=' + getUrlParameter("user_id"),
            columns: [
                { data: '' },
                { data: 'GroupName' },
                { data: 'PriceForGig' },
                { data: 'PriceForMonth' },
                { data: 'PriceForUser' },
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
                    orderable: false,
                    searchable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // Group Name
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['GroupName'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Price For Gig
                    targets: 2,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        var $name = full['PriceForGig'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Price For Month
                    targets: 3,
                    responsivePriority: 3,
                    render: function (data, type, full, meta) {
                        var $name = full['PriceForMonth'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Price For User
                    targets: 4,
                    responsivePriority: 3,
                    render: function (data, type, full, meta) {
                        var $name = full['PriceForUser'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
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
                            '<button type="button" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit EditUserGroup" data-id="' + full["Id"] + '" data-gig="' + full["PriceForGig"] + '" data-month="' + full["PriceForMonth"] + '" data-user="' + full["PriceForUser"] + '" data-group="' + full["groupId"] + '"><i class="text-primary ti ti-pencil"></i></button>' +
                            '<a data-bs-toggle="popover" title="حذف" class="btn btn-sm btn-icon item-edit DeleteUserGroup" data-id="' + full["Id"] + '"><i class="text-primary ti ti-trash"></i></a>'
                        );
                    }
                }
            ],
            "language": {
                "paginate": {
                    "first": "اولین",
                    "last": "آخرین",
                    "next": "بعدی",
                    "previous": "قبلی"
                },
                "info": "نمایش _START_ تا _END_ از _TOTAL_ ورودی",
                "lengthMenu": "نمایش _MENU_ ورودی",
                "search": "جستجو:",
                "zeroRecords": "موردی یافت نشد",
                "infoEmpty": "هیچ موردی موجود نیست",
                "infoFiltered": "(فیلتر شده از _MAX_ ورودی)",
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جستجوی دسته بندی',
                loadingRecords: "در حال بارگزاری ..."
            },
            lengthChange: false,
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['PlanName'];
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
        $('div.head-label').html('<h5 class="card-title mb-0">تعرفه ها</h5>');
    }

    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);

    //// Edit User Group
    $('body').on('click', '.EditUserGroup', function () {

        var id = $(this).attr("data-id");
        var priceForGig = $(this).attr("data-gig");
        var priceForMonth = $(this).attr("data-month");
        var priceForUser = $(this).attr("data-user");
        var GroupId = $(this).attr("data-group");

        $("#planGroup").val(GroupId).trigger('change');
        $("#userPriceForGig").val(priceForGig);
        $("#userPriceForMonth").val(priceForMonth);
        $("#userPriceForUser").val(priceForUser);
        $("#editPriceForm").find("#id").val(id);

    });

    //// Delete User Group
    $('body').on('click', '.DeleteUserGroup', function () {

        var id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "آیا مطمئن هستی ؟ این دسته بندی از روی تعرفه های نماینده ها حذف می شود !!",
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

                blockUI(".section-block");

                AjaxGet("/App/ServerGroups/DeleteUserGroup?id=" + id + "&user_id=" + getUrlParameter("user_id")).then(res => {

                    UnblockUI(".section-block");
                    eval(res.data);
                    if (res.status == "success") {

                        dt_group.ajax.reload(null, false);
                    }


                });

            }
        });

    });


    const formAddNewRecord = document.getElementById('editPriceForm');

    // Form validation for Add new record
    fv = FormValidation.formValidation(formAddNewRecord, {
        fields: {
            planGroup: {
                validators: {
                    notEmpty: {
                        message: 'عنوان دسته بندی را وارد کنید'
                    }
                }
            },
            userPriceForGig: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر گیگ را وارد کنید'
                    }
                }
            },
            userPriceForMonth: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر ماه را وارد کنید'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.mb-3'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    fv.on('core.form.valid', function (e) {

        blockUI(".section-block");
        AjaxFormPost('/App/ServerGroups/SetGroupForUser', "#editPriceForm").then(res => {
            UnblockUI(".section-block");
            eval(res.data);
            if (res.status == "success") {

                dt_group.ajax.reload(null, false);
                document.getElementById('editPriceForm').reset();
                $("#editPriceForm").find("#id").val(0);
                $("#planGroup").val(0).trigger('change');
            }

        });
    });



    const formAddBotRecord = document.getElementById('botSettingForm');

    // Form validation for Add new record
    fv1 = FormValidation.formValidation(formAddBotRecord, {
        fields: {
            BotToken: {
                validators: {
                    notEmpty: {
                        message: 'توکن ربات را وارد کنید'
                    }
                }
            },
            BotId: {
                validators: {
                    notEmpty: {
                        message: 'آیدی ربات را وارد کنید'
                    }
                }
            },
            PricePerGig_Admin: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر گیگ ادمین را وارد کنید'
                    }
                }
            },
            PricePerMonth_Admin: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر ماه ادمین را وارد کنید'
                    }
                }
            },
            PricePerGig_Major: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر گیگ نماینده را وارد کنید'
                    }
                }
            },
            PricePerMonth_Major: {
                validators: {
                    notEmpty: {
                        message: 'قیمت هر گیگ نماینده را وارد کنید'
                    }
                }
            },
            TelegramUserId: {
                validators: {
                    notEmpty: {
                        message: 'آیدی عددی ادمین را وارد کنید'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.mb-3'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    fv1.on('core.form.valid', function (e) {


        if ($("input[name='Active']").is(':checked')) {
            $("input[name='Active']").val("True");
        }
        else {
            $("input[name='Active']").val("false");
        }

        if ($("input[name='RequiredJoinChannel']").is(':checked')) {
            $("input[name='RequiredJoinChannel']").val("True");
        }
        else {
            $("input[name='RequiredJoinChannel']").val("False");
        }

        if ($("input[name='IsActiveSendReceipt']").is(':checked')) {
            $("input[name='IsActiveSendReceipt']").val("True");
        }
        else {
            $("input[name='IsActiveSendReceipt']").val("false");
        }


        if ($("input[name='IsActiveCardToCard']").is(':checked')) {
            $("input[name='IsActiveCardToCard']").val("True");
        }
        else {
            $("input[name='IsActiveCardToCard']").val("false");
        }

        $("#botSettingForm").find("#user_id").val(getUrlParameter("user_id"));

        blockUI(".section-block");

        AjaxFormPost('/App/Admin/SaveBotSetting', "#botSettingForm").then(res => {
            UnblockUI(".section-block");
            eval(res.data);
            if (res.status == "success") {

                dt_group.ajax.reload(null, false);
            }

        });
    });


    var $this = $("#userPlan").select2();
    $this.wrap('<div class="position-relative"></div>').select2({
        placeholder: 'انتخاب تعرفه',
        dropdownParent: $this.parent(),
        allowClear: false
    });

    Plans("#userPlan");
});


//لیست تعرفه ها
function Plans(selectId) {
    var $select = $(selectId);

    $.ajax({
        url: "/App/Plan/Select2Plans",
        type: "get",
        dataType: "json",
        success: function (res) {
            // پاک کردن گزینه‌های قبلی
            $select.empty();

            // افزودن گزینه‌های جدید
            $.each(res.result, function (index, item) {
                var newOption = new Option(item.Name, item.id, false, false);
                $select.append(newOption);
            });
        },
        error: function (xhr, status, error) {
            console.error("An error occurred: " + status + " " + error);
        }
    });
}















let bankFv;

// datatable (jquery)
$(function () {

    $('[data-bs-toggle="popover"]').tooltip();

    var dt_banks_table = $('.datatables-banks');
    var dt_banks;
    // DataTable with buttons
    // --------------------------------------------------------------------


    if (dt_banks_table.length) {
        dt_banks = dt_banks_table.DataTable({
            ajax: '/App/Admin/GetUserBankNumbers?user_id=' + getUrlParameter("user_id"),
            columns: [
                { data: '' },
                { data: 'CardNumber' },
                { data: 'SmsNumberOfCard' },
                { data: 'NameOfCard' },
                { data: 'phoneNumber' },
                { data: 'Status' },
                { data: '' },
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
                    orderable: false,
                    searchable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // CardNumber
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['CardNumber'];
                        // Creates full output for row
                        var $row_output = "<span dir='ltr'>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // SmsNumberOfCard
                    targets: 2,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        var $name = full['SmsNumberOfCard'];
                        // Creates full output for row
                        var $row_output = "<span dir='ltr'>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // NameOfCard
                    targets: 3,
                    responsivePriority: 3,
                    render: function (data, type, full, meta) {
                        var $name = full['NameOfCard'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // phoneNumber
                    targets: 4,
                    responsivePriority: 3,
                    render: function (data, type, full, meta) {
                        var $name = full['phoneNumber'];
                        // Creates full output for row
                        var $row_output = "<span dir='ltr'>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $status_number = full['Status'];
                        var $status = {
                            0: { title: 'غیرفعال', class: 'bg-label-danger' },
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
                            '<button type="button" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit EditCard" data-id="' + full["Card_ID"] + '" data-phoneNumber="' + full["phoneNumber"] + '" data-CardNumber="' + full["CardNumber"] + '" data-SmsNumberOfCard="' + full["SmsNumberOfCard"] + '" data-NameOfCard="' + full["NameOfCard"] + '"><i class="text-primary ti ti-pencil"></i></button>' +
                            '<a data-bs-toggle="popover" title="فعال کردن" class="btn btn-sm btn-icon item-edit ActiveCard" data-id="' + full["Card_ID"] + '"><i class="text-primary ti ti-lock-access"></i></a>'+
                            '<a data-bs-toggle="popover" title="حذف" class="btn btn-sm btn-icon item-edit DeleteCard" data-id="' + full["Card_ID"] + '"><i class="text-primary ti ti-trash"></i></a>'
                        );
                    }
                }
            ],
            "language": {
                "paginate": {
                    "first": "اولین",
                    "last": "آخرین",
                    "next": "بعدی",
                    "previous": "قبلی"
                },
                "info": "نمایش _START_ تا _END_ از _TOTAL_ ورودی",
                "lengthMenu": "نمایش _MENU_ ورودی",
                "search": "جستجو:",
                "zeroRecords": "موردی یافت نشد",
                "infoEmpty": "هیچ موردی موجود نیست",
                "infoFiltered": "(فیلتر شده از _MAX_ ورودی)",
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جستجوی کارت',
                loadingRecords: "در حال بارگزاری ..."
            },
            lengthChange: false,
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['PlanName'];
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
        $('div.head-label').html('<h5 class="card-title mb-0">تعرفه ها</h5>');
    }

    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);

    //// EditCard
    $('body').on('click', '.EditCard', function () {

        var id = $(this).attr("data-id");
        var CardNumber = $(this).attr("data-CardNumber");
        var NameOfCard = $(this).attr("data-NameOfCard");
        var SmsNumberOfCard = $(this).attr("data-SmsNumberOfCard");
        var phoneNumber = $(this).attr("data-phoneNumber");

        $("#CardNumber").val(CardNumber);
        $("#NameOfCard").val(NameOfCard);
        $("#SmsNumberOfCard").val(SmsNumberOfCard);
        $("#phoneNumber").val(phoneNumber);

        $("#bankNumbersForm").find("#id").val(id);

    });

    //// Active DeActive Card
    $('body').on('click', '.ActiveCard', function () {

        var id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "آیا مطمئن هستی ؟ این شماره کارت در دسترس عموم قرار میگرد !!",
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

                blockUI(".section-block");

                AjaxGet("/App/Admin/DeActiveCard?Card_ID=" + id + "&user_id=" + getUrlParameter("user_id")).then(res => {

                    UnblockUI(".section-block");
                    eval(res.data);
                    if (res.status == "success") {

                        dt_banks.ajax.reload(null, false);
                    }


                });

            }
        });

    });


    //// Remove Card
    $('body').on('click', '.DeleteCard', function () {

        var id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "آیا مطمئن هستی ؟",
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

                blockUI(".section-block");

                AjaxGet("/App/Admin/DeleteCard?Card_ID=" + id + "&user_id=" + getUrlParameter("user_id")).then(res => {

                    UnblockUI(".section-block");
                    eval(res.data);
                    if (res.status == "success") {

                        dt_banks.ajax.reload(null, false);
                    }


                });

            }
        });

    });




    const bankNumbersForm = document.getElementById('bankNumbersForm');

    // Form validation for Add new record
    bankFv = FormValidation.formValidation(bankNumbersForm, {
        fields: {
            CardNumber: {
                validators: {
                    notEmpty: {
                        message: 'شماره کارت را وارد کنید'
                    },
                    stringLength: {
                        min: 16,   // حداقل طول
                        max: 16,   // حداکثر طول
                        message: 'شماره کارت باید 16 رقم باشد !!'
                    }
                }
            },
            NameOfCard: {
                validators: {
                    notEmpty: {
                        message: 'نام و نام خانوادگی دارنده کارت را وارد کنید'
                    }
                }
            },
            phoneNumber: {
                validators: {
                    regexp: {
                        regexp: /^\+989\d{9}$/,
                        message: 'شماره همراه را به فرمت 98+ وارد کنید'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.mb-3'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    bankFv.on('core.form.valid', function (e) {


        $("#bankNumbersForm").find("#user_id").val(getUrlParameter("user_id"));

        blockUI(".section-block");

        AjaxFormPost('/App/Admin/SaveBankCard', "#bankNumbersForm").then(res => {
            UnblockUI(".section-block");
            eval(res.data);
            if (res.status == "success") {

                dt_banks.ajax.reload(null, false);
            }

        });
    });


});



















// Learns


let LearnsFv;

// datatable (jquery)
$(function () {

    $('[data-bs-toggle="popover"]').tooltip();

    var dt_learns_table = $('.datatables-learns');
    var dt_learns;
    // DataTable with buttons
    // --------------------------------------------------------------------


    if (dt_learns_table.length) {
        dt_learns = dt_learns_table.DataTable({
            ajax: '/App/Admin/_GetLearnsAsync?user_id=' + getUrlParameter("user_id"),
            columns: [
                { data: '' },
                { data: 'Learn_Title' },
                { data: 'Learn_Link' },
                { data: '' },
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
                    orderable: false,
                    searchable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // Learn_Title
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['Learn_Title'];
                        // Creates full output for row
                        var $row_output = "<span dir='ltr'>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Learn_Link
                    targets: 2,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        var $name = full['Learn_Link'];
                        // Creates full output for row
                        var $row_output = "<span dir='ltr'>" + $name + "</span>";
                        return $row_output;
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
                            '<button type="button" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit EditLearn" data-id="' + full["Learn_ID"] + '" data-Learn_Title="' + full["Learn_Title"] + '" data-Learn_Link="' + full["Learn_Link"] + '"><i class="text-primary ti ti-pencil"></i></button>' +
                            '<a data-bs-toggle="popover" title="حذف" class="btn btn-sm btn-icon item-edit DeleteLearn" data-id="' + full["Learn_ID"] + '"><i class="text-primary ti ti-trash"></i></a>'
                        );
                    }
                }
            ],
            "language": {
                "paginate": {
                    "first": "اولین",
                    "last": "آخرین",
                    "next": "بعدی",
                    "previous": "قبلی"
                },
                "info": "نمایش _START_ تا _END_ از _TOTAL_ ورودی",
                "lengthMenu": "نمایش _MENU_ ورودی",
                "search": "جستجو:",
                "zeroRecords": "موردی یافت نشد",
                "infoEmpty": "هیچ موردی موجود نیست",
                "infoFiltered": "(فیلتر شده از _MAX_ ورودی)",
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جستجوی کارت',
                loadingRecords: "در حال بارگزاری ..."
            },
            lengthChange: false,
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100]
        });
        $('div.head-label').html('<h5 class="card-title mb-0">تعرفه ها</h5>');
    }

    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);

    //// EditCard
    $('body').on('click', '.EditLearn', function () {

        var id = $(this).attr("data-id");
        var Learn_Title = $(this).attr("data-Learn_Title");
        var Learn_Link = $(this).attr("data-Learn_Link");

        $("#Learn_Title").val(Learn_Title);
        $("#Learn_Link").val(Learn_Link);

        $("#LearnsForm").find("#id").val(id);

    });



    //// Remove Card
    $('body').on('click', '.DeleteLearn', function () {

        var id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "آیا مطمئن هستی ؟",
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

                blockUI(".section-block");

                AjaxGet("/App/Admin/DeleteLearn?Learn_ID=" + id + "&user_id=" + getUrlParameter("user_id")).then(res => {

                    UnblockUI(".section-block");
                    eval(res.data);
                    if (res.status == "success") {

                        dt_learns.ajax.reload(null, false);
                    }


                });

            }
        });

    });




    const LearnsForm = document.getElementById('LearnsForm');

    // Form validation for Add new record
    LearnsFv = FormValidation.formValidation(LearnsForm, {
        fields: {
            Learn_Title: {
                validators: {
                    notEmpty: {
                        message: 'عنوان آموزش را وارد کنید'
                    }
                }
            },
            Learn_Link: {
                validators: {
                    notEmpty: {
                        message: 'لینک آموزش را وارد کنید'
                    }
                }
            }
        },
        plugins: {
            trigger: new FormValidation.plugins.Trigger(),
            bootstrap5: new FormValidation.plugins.Bootstrap5({
                // Use this for enabling/changing valid/invalid class
                // eleInvalidClass: '',
                eleValidClass: '',
                rowSelector: '.mb-3'
            }),
            submitButton: new FormValidation.plugins.SubmitButton(),
            // defaultSubmit: new FormValidation.plugins.DefaultSubmit(),
            autoFocus: new FormValidation.plugins.AutoFocus()
        },
        init: instance => {
            instance.on('plugins.message.placed', function (e) {
                if (e.element.parentElement.classList.contains('input-group')) {
                    e.element.parentElement.insertAdjacentElement('afterend', e.messageElement);
                }
            });
        }
    });

    LearnsFv.on('core.form.valid', function (e) {


        $("#LearnsForm").find("#user_id").val(getUrlParameter("user_id"));

        blockUI(".section-block");

        AjaxFormPost('/App/Admin/SaveLearn', "#LearnsForm").then(res => {
            UnblockUI(".section-block");
            eval(res.data);
            if (res.status == "success") {

                dt_learns.ajax.reload(null, false);
            }

        });
    });


});