/**
 * Page User List
 */

'use strict';

// Datatable (jquery)
$(function () {

    let borderColor, bodyBg, headingColor;

    if (isDarkStyle) {
        borderColor = config.colors_dark.borderColor;
        bodyBg = config.colors_dark.bodyBg;
        headingColor = config.colors_dark.headingColor;
    } else {
        borderColor = config.colors.borderColor;
        bodyBg = config.colors.bodyBg;
        headingColor = config.colors.headingColor;
    }

    // Variable declaration for table
    var dt_user_table = $('.datatables-users'),
        select2 = $('.select2'),
        statusObj = {
            1: { title: 'عادی', class: 'bg-label-success' },
            2: { title: 'نزدیک به اتمام سقف مصرف', class: 'bg-label-warning' },
            3: { title: 'اتمام سقف مصرف', class: 'bg-label-danger' },
            4: { title: 'غیرفعال', class: 'bg-label-danger' }
        };
    if (select2.length) {
        var $this = select2;
        $this.wrap('<div class="position-relative"></div>').select2({
            placeholder: 'انتخاب تعرفه',
            dropdownParent: $this.parent(),
            allowClear: true
        });

        Plans("#userPlan");
    }

    // Users datatable
    if (dt_user_table.length) {
        var dt_user = dt_user_table.DataTable({
            ajax: '/App/Admin/_PartialGetAllUsers', // JSON file to add data
            columns: [
                // columns according to JSON
                { data: 'id' },
                { data: 'profile' },
                { data: 'username' },
                { data: 'sellCount' },
                { data: 'sumSellCount' },
                { data: 'status' },
                { data: 'limit' },
                { data: 'used' }

            ],
            columnDefs: [
                {
                    // For Responsive
                    className: 'control',
                    searchable: false,
                    orderable: false,
                    responsivePriority: 2,
                    targets: 0,
                    render: function (data, type, full, meta) {
                        return '';
                    }
                },
                {
                    // User full name and email
                    targets: 1,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var userId = full["id"];
                        var $name = full['username'],
                            $image = full['profile'];
                        if ($image) {
                            // For Avatar image
                            var $output =
                                '<img src="' + assetsPath + 'img/avatars/' + $image + '" alt="profile" class="rounded-circle">';
                        } else {
                            // For Avatar badge
                            var stateNum = Math.floor(Math.random() * 6);
                            var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
                            var $state = states[stateNum],
                                $name = full['username'];
                            let nameParts = $name.split(" ");
                            let $initials = nameParts[0].charAt(0);
                            $output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';
                        }
                        // Creates full output for row
                        var $row_output =
                            '<div class="d-flex justify-content-start align-items-center user-name">' +
                            '<div class="avatar-wrapper">' +
                            '<div class="avatar me-3">' +
                            $output +
                            '</div>' +
                            '</div>' +
                            '<div class="d-flex flex-column">' +
                            '<a href="/App/Admin/Details?user_id=' + userId +
                            '" class="text-body text-truncate"><span class="fw-medium">' +
                            $name +
                            '</span></a>' +
                            '</div>' +
                            '</div>';
                        return $row_output;
                    }
                },
                {
                    // User Use
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $used = full['used'];

                        return (
                            '<span>' +
                            $used +
                            '</span>'
                        );
                    }
                },
                {
                    // User Limit
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $limit = full['limit'];

                        return (
                            '<span>' + $limit + '</span>'
                        );
                    }
                },
                {
                    // User Status
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $status = full['status'];
                        return (
                            '<span class="badge ' +
                            statusObj[$status].class +
                            '" text-capitalized>' +
                            statusObj[$status].title +
                            '</span>'
                        );
                    }
                },
                {
                    // Sell Count
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $role = full['sellCount'];
                        return "<span class='text-truncate d-flex align-items-center'>" + $role + '</span>';
                    }
                },
                {
                    // Sum Sell
                    targets: 6,
                    render: function (data, type, full, meta) {
                        var $SumSell = full['sumSellCount'];

                        return '<span class="fw-medium">' + $SumSell + '</span>';
                    }
                },
                {
                    // Actions
                    targets: -1,
                    title: 'عملیات',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        var userId = full["id"];
                        var $status = full['status'];
                        var $id = full["id"];
                        var $statusBot = full["RobotStatus"];
                        var $StatusTitle = "";
                        var $StatusBotTitle = "";
                        if ($status == 4) {
                            $StatusTitle = "فعال کردن";
                        }
                        else {
                            $StatusTitle = "غیرفعال کردن";
                        }

                        if ($statusBot == 1) {
                            $StatusBotTitle = "خاموش کردن ربات"
                        }
                        else {
                            $StatusBotTitle = "روشن کردن ربات"
                        }


                        return (
                            '<div class="d-flex align-items-center">' +
                            '<a href="javascript:;" class="text-body EditUser" data-id=' + $id + '><i class="ti ti-edit ti-sm me-2"></i></a>' +
                            '<a href="javascript:;" class="text-body dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm mx-1"></i></a>' +
                            '<div class="dropdown-menu dropdown-menu-end m-0">' +
                            '<a href="/App/Admin/Details?user_id=' + userId +
                            '" class="dropdown-item">نمایش</a>' +
                            '<a href="javascript:;" class="dropdown-item BanUser" data-id=' + $id + '>' + $StatusTitle + '</a>' +
                            '<a href="javascript:;" class="dropdown-item StartBot" data-id=' + $id + '>' + "تغییر وضعیت ربات" + '</a>' +
                            '</div>' +
                            '</div>'
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
                searchPlaceholder: 'جستجوی کاربران',
                loadingRecords:"در حال بارگزاری ..."
            },
            displayLength: 7,
            lengthMenu: [7, 10, 25, 50, 75, 100],
            dom:
                '<"row me-2"' +
                '<"col-md-2"<"me-3"l>>' +
                '<"col-md-10"<"dt-action-buttons text-xl-end text-lg-start text-md-end text-start d-flex align-items-center justify-content-end flex-md-row flex-column mb-3 mb-md-0"fB>>' +
                '>t' +
                '<"row mx-2"' +
                '<"col-sm-12 col-md-6"i>' +
                '<"col-sm-12 col-md-6"p>' +
                '>',
            // Buttons with Dropdown
            buttons: [
                {
                    extend: 'collection',
                    className: 'btn btn-label-secondary dropdown-toggle mx-3 waves-effect waves-light',
                    text: '<i class="ti ti-screen-share me-1 ti-xs"></i>گرفتن خروجی',
                    buttons: [
                        {
                            extend: 'print',
                            text: '<i class="ti ti-printer me-2" ></i>چاپ',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [1, 2, 3, 4, 5],
                                // prevent avatar to be print
                                format: {
                                    body: function (inner, coldex, rowdex) {
                                        if (inner.length <= 0) return inner;
                                        var el = $.parseHTML(inner);
                                        var result = '';
                                        $.each(el, function (index, item) {
                                            if (item.classList !== undefined && item.classList.contains('user-name')) {
                                                result = result + item.lastChild.firstChild.textContent;
                                            } else if (item.innerText === undefined) {
                                                result = result + item.textContent;
                                            } else result = result + item.innerText;
                                        });
                                        return result;
                                    }
                                }
                            },
                            customize: function (win) {
                                //customize print view for dark
                                $(win.document.body)
                                    .css('color', headingColor)
                                    .css('border-color', borderColor)
                                    .css('background-color', bodyBg);
                                $(win.document.body)
                                    .find('table')
                                    .addClass('compact')
                                    .css('color', 'inherit')
                                    .css('border-color', 'inherit')
                                    .css('background-color', 'inherit');
                            }
                        },
                        {
                            extend: 'csv',
                            text: '<i class="ti ti-file-text me-2" ></i>Csv',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [1, 2, 3, 4, 5],
                                // prevent avatar to be display
                                format: {
                                    body: function (inner, coldex, rowdex) {
                                        if (inner.length <= 0) return inner;
                                        var el = $.parseHTML(inner);
                                        var result = '';
                                        $.each(el, function (index, item) {
                                            if (item.classList !== undefined && item.classList.contains('user-name')) {
                                                result = result + item.lastChild.firstChild.textContent;
                                            } else if (item.innerText === undefined) {
                                                result = result + item.textContent;
                                            } else result = result + item.innerText;
                                        });
                                        return result;
                                    }
                                }
                            }
                        },
                        {
                            extend: 'excel',
                            text: '<i class="ti ti-file-spreadsheet me-2"></i>Excel',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [1, 2, 3, 4, 5],
                                // prevent avatar to be display
                                format: {
                                    body: function (inner, coldex, rowdex) {
                                        if (inner.length <= 0) return inner;
                                        var el = $.parseHTML(inner);
                                        var result = '';
                                        $.each(el, function (index, item) {
                                            if (item.classList !== undefined && item.classList.contains('user-name')) {
                                                result = result + item.lastChild.firstChild.textContent;
                                            } else if (item.innerText === undefined) {
                                                result = result + item.textContent;
                                            } else result = result + item.innerText;
                                        });
                                        return result;
                                    }
                                }
                            }
                        },
                        {
                            extend: 'pdf',
                            text: '<i class="ti ti-file-code-2 me-2"></i>Pdf',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [1, 2, 3, 4, 5],
                                // prevent avatar to be display
                                format: {
                                    body: function (inner, coldex, rowdex) {
                                        if (inner.length <= 0) return inner;
                                        var el = $.parseHTML(inner);
                                        var result = '';
                                        $.each(el, function (index, item) {
                                            if (item.classList !== undefined && item.classList.contains('user-name')) {
                                                result = result + item.lastChild.firstChild.textContent;
                                            } else if (item.innerText === undefined) {
                                                result = result + item.textContent;
                                            } else result = result + item.innerText;
                                        });
                                        return result;
                                    }
                                }
                            }
                        },
                        {
                            extend: 'copy',
                            text: '<i class="ti ti-copy me-2" ></i>کپی',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [1, 2, 3, 4, 5],
                                // prevent avatar to be display
                                format: {
                                    body: function (inner, coldex, rowdex) {
                                        if (inner.length <= 0) return inner;
                                        var el = $.parseHTML(inner);
                                        var result = '';
                                        $.each(el, function (index, item) {
                                            if (item.classList !== undefined && item.classList.contains('user-name')) {
                                                result = result + item.lastChild.firstChild.textContent;
                                            } else if (item.innerText === undefined) {
                                                result = result + item.textContent;
                                            } else result = result + item.innerText;
                                        });
                                        return result;
                                    }
                                }
                            }
                        }
                    ]
                },
                {
                    text: '<i class="ti ti-plus me-0 me-sm-1 ti-xs"></i><span class="d-none d-sm-inline-block">افزودن کاربر</span>',
                    className: 'add-new btn btn-primary waves-effect waves-light',
                    attr: {
                        'data-bs-toggle': 'offcanvas',
                        'data-bs-target': '#offcanvasAddUser'
                    }
                }
            ]
            ,
            // For responsive popup
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
            },
        });
    }


    // Delete Record
    $('.datatables-users tbody').on('click', '.delete-record', function () {


        dt_user.row($(this).parents('tr')).remove().draw();



    });
    // Active Or DeActive User
    $('body').on('click', '.BanUser', function () {

        var id = $(this).attr("data-id");
        Swal.fire({
            title: 'هشدار',
            text: "مطمئنی میخای وضعیت کاربر رو تغییر بدی ؟!",
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

                $.ajax({
                    url: "/App/Admin/BanUser?id=" + id,
                    type: "get",
                    dataType: "json",
                    success: function (res) {
                        dt_user.ajax.reload(null, false);
                    }
                })

            }
        });
    });

    // Active Or DeActive Robot
    $('body').on('click', '.StartBot', function () {

        var id = $(this).attr("data-id");

        AjaxGet("/App/Admin/StartBot?user_id=" + id).then(res => {

            console.log(res);
            eval(res.data);

        });


    });

    // Edit User
    $('body').on('click', '.EditUser', function () {

        BodyBlockUI();

        var id = $(this).attr("data-id");

        $(".dtr-bs-modal").modal("hide");

        AjaxGet('/App/Admin/Edit?id=' + id).then(res => {
            BodyUnblockUI();
            if (res.status == "success") {
                var data = res.data;
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var input = $('input[name=' + key + ']');
                        if (Array.isArray(data[key])) {

                            console.log("1111");
                            SelectPlans("#userPlan", data[key]);
                        }
                        else {
                            input.val(data[key]);
                        }


                    }
                }

                showOffcanvas();
            }

        });
    });

    function showOffcanvas() {
        var offcanvasElement = document.getElementById('offcanvasAddUser');
        var offcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasElement);
        offcanvas.show();
    }


    //لیست تعرفه ها
    function Plans(selectId) {
        var $select = $(selectId);

        $.ajax({
            url: "/App/Admin/Select2Plans",
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
        console.log("select shod");
        $(selectId).val(Ids).trigger('change');
    }



    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);


    const phoneMaskList = document.querySelectorAll('.phone-mask'),
        addNewUserForm = document.getElementById('addNewUserForm');

    // Phone Number
    if (phoneMaskList) {
        phoneMaskList.forEach(function (phoneMask) {
            new Cleave(phoneMask, {
                phone: true,
                phoneRegionCode: 'US'
            });
        });
    }
    const fv = FormValidation.formValidation(addNewUserForm, {
        fields: {
            userUsername: {
                validators: {
                    notEmpty: {
                        message: 'نام کاربری را وارد کنید'
                    }
                }
            },
            userLimit: {
                validators: {
                    notEmpty: {
                        message: 'محدودیت را وارد کنید'
                    }
                }
            }
        },
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


    fv.on('core.form.invalid', function (e) {

        if ($("#userPlan").val().length == 0) {
            $("#userPlanMessage").removeClass("d-none");
            $("#userPlan").addClass("is-invalid");
        }
    });
    fv.on('core.form.valid', function (e) {

        if ($("#userPlan").val().length != 0) {
            $("#userPlanMessage").addClass("d-none");
            $("#userPlan").removeClass("is-invalid");
        }
        else {
            $("#userPlanMessage").removeClass("d-none");
            $("#userPlan").addClass("is-invalid");
            return;
        }

        BodyBlockUI();
        AjaxFormPost('/App/Admin/CreateOrEdit', "#addNewUserForm").then(res => {

            eval(res.data);
            if (res.status == "success") {
                BodyUnblockUI();
                document.getElementById('addNewUserForm').reset();
                dt_user.ajax.reload(null, false);
                // بستن offcanvas پس از موفقیت آمیز بودن ارسال فرم
                var offcanvasElement = document.getElementById('offcanvasAddUser');
                var offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);
                offcanvas.hide();
                
            }

        });
    });

    fv.on('core.form.notvalidated', function (event) {
        console.log(event);
    });

    $("#userPlan").on("change", function () {

        if ($("#userPlan").val().length != 0) {
            $("#userPlanMessage").addClass("d-none");
            $("#userPlan").removeClass("is-invalid");
        }
        else {
            $("#userPlanMessage").removeClass("d-none");
            $("#userPlan").addClass("is-invalid");
        }

    });

});

// Validation & Phone mask



