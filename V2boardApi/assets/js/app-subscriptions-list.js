
// datatable (jquery)
$(function () {
    var dt_basic_table = $('.datatables-plan'),
        select2 = $('#userPlan'),
        dt_basic;

    if (select2.length) {
        var $this = select2;
        $this.wrap('<div class="position-relative"></div>').select2({
            placeholder: 'انتخاب تعرفه',
            dropdownParent: $this.parent(),
            allowClear: false
        });

        $("#userPlanRenew").wrap('<div class="position-relative"></div>').select2({
            placeholder: 'انتخاب تعرفه',
            dropdownParent: $("#userPlanRenew").parent(),
            allowClear: false
        });

        Plans("#userPlanRenew");
        Plans("#userPlan");

    }
    

    var Role = "";
    if (document.cookie.split(';').length != 0) {
        var Cookies = document.cookie.split(';');
        var RoleCookie = Cookies.find(cookie => cookie.trim().startsWith("Role="));
        if (RoleCookie) {
            Role = RoleCookie.split('=')[1];
        }
    } else {
        Role = document.cookie;
        Role = Role.split('=')[1];

    }

    // DataTable with buttons
    // --------------------------------------------------------------------

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: {
                url: '/App/Subscriptions/GetAll',
                type: 'POST',
                error: function (jqXHR, textStatus, errorThrown) {

                    location.replace(location.href);

                },
            },
            initComplete: function (setting, json) {

                //تولتیپ کردن بعد از لود دیتا
                $('[data-bs-toggle="popover"]').tooltip();
            },
            drawCallback: function (settings) {
                //تولتیپ کردن بعد از تغییر صفحه یا سرچ
                $('[data-bs-toggle="popover"]').tooltip();
            },
            processing: true,
            serverSide: true,
            paging: true,
            pageLength: 10,
            columns: [
                { data: 'id' },
                { data: 'Name', width: '200px', className: "text-center" },
                { data: 'TotalVolume', className: "text-center" },
                { data: 'UsedVolume', className: "text-center" },
                { data: 'RemainingVolume', className: "text-center" },
                { data: 'DaysLeft', className: "text-center" },
                { data: 'PlanName', className: "text-center" },
                { data: 'IsActive', className: "text-center" },
                { data: '', width: '80px', className: "text-center" },
            ],
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
                    // SubName
                    targets: 1,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $name = full['Name'];
                        var $IsOnline = full['IsOnline'];
                        var $LastTimeOnline = full['LastTimeOnline'];
                        // Creates full output for row
                        var $row_output = "";
                        var $OnlineState = "";
                        if ($IsOnline == true) {
                            $OnlineState += '<i class="ti ti-circle-filled fs-tiny me-2 text-success"></i>';

                            $row_output += "<span data-bs-toggle='popover' data-bs-html='true' title='<span>" +
                                '<span class="fw-medium">آنلاین</span> ' +
                                "</span>'" +
                                "<span>" + "<span>" + $OnlineState + "</span>" +
                                $name +
                                '</span>';
                        }
                        else {
                            $OnlineState += '<i class="ti ti-circle-filled fs-tiny me-2 text-danger"></i>';
                            $row_output += "<span data-bs-toggle='popover' data-bs-html='true' title='<span>" +
                                '<span class="fw-medium">اخرین آنلاین :</span> ' +
                                $LastTimeOnline +
                                "</span>'" +
                                "<span>" + "<span>" + $OnlineState + "</span>" +
                                $name +
                                '</span>';
                        }




                        return $row_output;
                    }
                },
                {
                    // TotalVolume
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $TotalVolume = full['TotalVolume'];
                        // Creates full output for row
                        var $row_output = "<span>" + $TotalVolume + "</span>";
                        return $row_output;
                    }
                },
                {
                    // UsedVolume
                    targets: 3,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $UsedVolume = full['UsedVolume'];
                        // Creates full output for row
                        var $row_output = "<span>" + $UsedVolume + "</span>";
                        return $row_output;
                    }
                },
                {
                    // RemainingVolume
                    targets: 4,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $RemainingVolume = full['RemainingVolume'];

                        var $row_output = "<span>" + $RemainingVolume + "</span>";

                        return $row_output;
                    }
                },
                {
                    // DaysLeft
                    targets: 5,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $DaysLeft = full['DaysLeft'];
                        if ($DaysLeft == -1) {
                            var $row_output = "<span>" + "بدون محدودیت" + "</span>";
                        }
                        else {
                            var $row_output = "<span>" + $DaysLeft + "</span>";
                        }

                        return $row_output;
                    }
                },
                {
                    // PlanName
                    targets: 6,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $PlanName = full['PlanName'];
                        var $row_output = "<span>" + $PlanName + "</span>";
                        return $row_output;
                    }
                },
                {
                    // DaysLeft
                    targets: 7,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $IsActive = full['IsActive'];

                        var statusObj = {
                            1: { title: 'فعال', class: 'bg-label-success' },
                            2: { title: 'پایان تاریخ اشتراک', class: 'bg-label-danger' },
                            3: { title: 'اتمام حجم', class: 'bg-label-danger' },
                            4: { title: 'مسدود', class: 'bg-label-danger' },
                            5: { title: 'نزدیک به پایان تاریخ اشتراک', class: 'bg-label-warning' },
                        };

                        var $row_output = "<span class='badge " + statusObj[$IsActive].class + "'>" + statusObj[$IsActive].title + "</span>";

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
                        var user_id = full["id"];
                        var $link = full["SubLink"];
                        var $IsActive = full["IsActive"];
                        var $DayCount = full["DaysLeft"];
                        var $Volume = full["RemainingVolume"];
                        var $state = "";
                        if (($DayCount == -1 && $Volume <= 2) || ((($DayCount > -1) && $DayCount <= 2) || $Volume <= 2)) {
                            $stateRenew = "ti-refresh";
                            $stateRenewText = "تمدید";
                        }
                        else {
                            $stateRenew = "ti-refresh-alert",
                                $stateRenewText = "تمدید";
                        }

                        if ($IsActive == 1) {
                            $state = "مسدود";
                        }
                        else if ($IsActive == 4) {
                            $state = "رفع مسدودی";
                        }
                        else {
                            $state = "مسدود";

                        }

                        

                        var menu = "";
                        if (Role == "1") {
                            menu += '<button data-id="' + user_id + '" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit" type="buttton"><i class="text-primary ti ti-pencil"></i></button>';
                        }

                        return (
                            '<div class="d-flex align-items-center">' +
                            '<button data-bs-toggle="popover" title="QR Code" onclick="ShowQRCode(\'' + $link + '\')" class="btn btn-sm btn-icon item-qrcode"><i class="text-primary ti ti-qrcode"></i></button>' +
                            '<a href="javascript:;" class="text-primary dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm mx-1"></i></a>' +
                            '<div class="dropdown-menu dropdown-menu-start m-0">' +
                            '<button  data-id="' + full["DaysLeft"] + '" data-id2="' + user_id + '" data-id3="' + $Volume + '" class="dropdown-item item-refresh">تمدید</button>' +
                            '<button  onclick="copyToClipboard(\'' + $link + '\')"  class="dropdown-item item-copy">کپی</button>' +
                            '<button  data-id="' + user_id + '" class="dropdown-item item-unlink">تغییر لینک</button>' +
                            '<button class="dropdown-item item-report">تاریخچه مصرف</button>' +
                            '<button data-bs-toggle="popover" data-id="' + $IsActive + '" data-id2="' + user_id + '" class="dropdown-item item-access">' + $state +'</button>' +
                            '<div class="dropdown-divider"></div>' +
                            '<li><button data-id="' + user_id + '"data-user="' + full["Name"]+'"  data-id-vol="' + $Volume + '" data-id-time="' + full["DaysLeft"] + '" class="dropdown-item text-danger item-delete">حذف</button></li>' +
                            '</ul>' +
                            '</div>' + menu +
                            '</div>'
                        );
                    }
                }
            ],
            order: [6, "desc"],
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return 'جزئیات ' + data['Name'];
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
        $('div.head-label').html('<h5 class="card-title mb-0">فاکتور ها</h5>');
    }

    //مربوط به نمایش مودال ویرایش اشتراک
    $('body').on('click', '.item-edit', function () {


        $(".dtr-bs-modal").modal("hide");

        var user_id = $(this).attr("data-id");
        ShowEditSubForm(user_id);



    });

    //مربوط به مودال تمدید اشتراک
    $('body').on('click', '.item-refresh', function () {

        var DayLeft = $(this).attr("data-id");
        var Volume = $(this).attr("data-id3");

        if ((DayLeft == -1 && Volume <= 2) || (((DayLeft > -1) && DayLeft <= 2) || Volume <= 2)) {

            $(".dtr-bs-modal").modal("hide");

            var user_id = $(this).attr("data-id2");

            $("#modalRenew").modal("show");

            $("#modalRenew input[name='user_id']").val(user_id);
        }
        else {
            if (DayLeft == -1) {
                showToast("هشدار", "امکان تمدید برای حجم باقی مانده کمتر از 2 گیگ فعال می شود ", "text-warning");
            }
            else {
                showToast("هشدار", "امکان تمدید تا " + (DayLeft - 2) + " روز دیگر یا حجم باقی مانده کمتر از " + (2) + " گیگ" + " فعال می شود", "text-warning");
            }
        }




    });

    //مربوط به مسدود و رفع مسدود اشتراک
    $('body').on('click', '.item-access', function () {

        BodyBlockUI();
        var active = $(this).attr("data-id");
        var user_id = $(this).attr("data-id2");

        var Status = true;

        if (active == "4") {
            Status = false;
        }

        AjaxGet("/App/Subscriptions/BanUser?user_id=" + user_id + "&" + "status=" + Status).then(res => {

            BodyUnblockUI();
            eval(res.data);
            if (res.status == "success") {
                dt_basic.ajax.reload(null, false);
            }

        });



    });

    //تغییر لینک اشتراک
    $('body').on('click', '.item-unlink', function () {

        var user_id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "مطمئنی میخای لینک رو تغییر بدی ؟!",
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



                AjaxGet("/App/Subscriptions/Reset?user_id=" + user_id).then(res => {

                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status == "success") {

                        dt_basic.ajax.reload(null, false);
                    }


                });

            }
        });

    });


    //حذف اشتراک
    $('body').on('click', '.item-delete', function () {

        var user_id = $(this).attr("data-id");
        var vol = $(this).attr("data-id-vol");
        var time = $(this).attr("data-id-time");
        var Name = $(this).attr("data-user");

        if ((vol < 0 || time == 0) || Role == "1") {
            Swal.fire({
                title: 'هشدار',
                text: "مطمئنی میخای لینک " + Name + " رو حذف کنی !؟",
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

                    AjaxGet("/App/Subscriptions/delete?user_id=" + user_id).then(res => {

                        BodyUnblockUI();
                        eval(res.data);
                        if (res.status == "success") {

                            dt_basic.ajax.reload(null, false);
                        }


                    });

                }
            });
        }
        else {
            showToast("هشدار", "حذف لینک بعد پایان مدت زمان یا اتمام حجم فعال می شود", "text-warning");
        }


    });



    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);

    function Plans(selectId) {
        var $select = $(selectId);

        $.ajax({
            url: "/App/Admin/Select2UserPlans",
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


    // فرم مربوط به افزودن اشتراک
    const addNewUserForm = document.getElementById('addNewUserForm');


    const fv = FormValidation.formValidation(addNewUserForm, {
        fields: {
            userSubname: {
                validators: {
                    notEmpty: {
                        message: 'نام اشتراک را وارد کنید'
                    }
                }
            },
            userTraffic: {
                validators: {
                    notEmpty: {
                        message: 'ترافیک را وارد کنید'
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

    //اگر فرم صحیح نبود
    fv.on('core.form.invalid', function (e) {

        if ($("#userPlan").val() != null) {
            if ($("#userPlan").val().length == 0) {
                $("#userPlanMessage").removeClass("d-none");
                $("#userPlan").addClass("is-invalid");
            }
        }
        else {
            $("#userPlanMessage").removeClass("d-none");
            $("#userPlan").addClass("is-invalid");
        }

    });
    // اگر فرم صحیح بود
    fv.on('core.form.valid', function (e) {

        if ($("#userPlan").val() != null) {
            if ($("#userPlan").val().length != 0) {
                $("#userPlanMessage").addClass("d-none");
                $("#userPlan").removeClass("is-invalid");
            }
            else {
                $("#userPlanMessage").removeClass("d-none");
                $("#userPlan").addClass("is-invalid");
                return;
            }
        }
        else {
            $("#userPlanMessage").removeClass("d-none");
            $("#userPlan").addClass("is-invalid");
            return;
        }


        blockUI("#modalCenter .section-block");

        AjaxFormPost('/App/Subscriptions/CreateUser', "#addNewUserForm").then(res => {
            UnblockUI("#modalCenter .section-block");
            eval(res.data);
            if (res.status == "success") {
                document.getElementById('addNewUserForm').reset();
                $("#modalCenter").modal("hide");
                dt_basic.ajax.reload(null, false);
            }

        });
    });

    // پایان فرم افزودن اشتراک







    // فرم مربوط به ویرایش اشتراک



    // تابع مربوط به نمایش اطلاعات اشتراک برای ویرایش
    function ShowEditSubForm(user_id) {

        $("#modalEditSub").modal("show");

        BodyBlockUI();

        AjaxGet('/App/Subscriptions/Edit?user_id=' + user_id).then(res => {

            if (res.status == "success") {

                $("#modalEditSub input[name='user_id']").val(user_id);
                var data = res.data;
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var input = $('input[name=' + key + ']');

                        if (key == "userExpire") {
                            picker = document.querySelector('#expire-picker'),

                                picker.flatpickr({
                                    disableMobile: "true",
                                    altInput: true,
                                    altFormat: 'j F Y',
                                    dateFormat: 'Y/m/d',
                                    locale: 'fa',
                                    defaultDate: data[key]

                                });
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


    }

    // اتمام نمایش اطلاعات

    const EditUserForm = document.getElementById('EditUserForm');


    const fv_edit = FormValidation.formValidation(EditUserForm, {
        fields: {
            userSubname: {
                validators: {
                    notEmpty: {
                        message: 'نام اشتراک را وارد کنید'
                    }
                }
            },
            userTraffic: {
                validators: {
                    notEmpty: {
                        message: 'ترافیک را وارد کنید'
                    }
                }
            },
            userExpire: {
                validators: {
                    notEmpty: {
                        message: 'تاریخ انقضا را وارد کنید'
                    }
                }
            },
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

    //اگر فرم صحیح نبود
    fv_edit.on('core.form.invalid', function (e) {



    });
    // اگر فرم صحیح بود
    fv_edit.on('core.form.valid', function (e) {


        blockUI("#modalEditSub .section-block");

        AjaxFormPost('/App/Subscriptions/Edit', "#EditUserForm").then(res => {
            UnblockUI("#modalEditSub .section-block");
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('EditUserForm').reset();
                $("#modalEditSub").modal("hide");
                dt_basic.ajax.reload(null, false);
            }

        });
    });

    // پایان فرم ویرایش اشتراک



    // فرم مربوط به تمدید اشتراک
    const RenewUserForm = document.getElementById('RenewUserForm');


    const fv_Renew = FormValidation.formValidation(RenewUserForm, {
        fields: {
            userExpire: {
                validators: {
                    notEmpty: {
                        message: 'تاریخ انقضا را وارد کنید'
                    }
                }
            },
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

    // اگر فرم صحیح بود
    fv_Renew.on('core.form.valid', function (e) {


        blockUI("#modalRenew .section-block");

        AjaxFormPost('/App/Subscriptions/Renew', "#RenewUserForm").then(res => {
            UnblockUI("#modalRenew .section-block");
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('RenewUserForm').reset();
                $("#modalRenew").modal("hide");
                dt_basic.ajax.reload(null, false);
            }

        });
    });

    // پایان فرم تمدید اشتراک


});


