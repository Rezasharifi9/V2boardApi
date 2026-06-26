
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


    var Role = (typeof window.UserRole !== 'undefined' ? String(window.UserRole) : '').trim();

    function updateSubscriptionSummary(summary) {
        var $box = $('#subscriptionFilterSummary');
        if (!summary) {
            $box.addClass('d-none');
            return;
        }
        $('#summaryAgentName').text(summary.agentUsername || '-');
        $('#summaryTotalCount').text(summary.totalCount);
        $('#summaryAmountLabel').text(summary.amountLabel || 'جمع مبلغ');
        $('#summaryTotalAmount').text((summary.totalAmountFormatted || '0') + ' تومان');
        var fromDate = $('#filterFromDate').val();
        var toDate = $('#filterToDate').val();
        $('#summaryDateRange').text(fromDate && toDate ? fromDate + ' تا ' + toDate : '-');
        $box.removeClass('d-none');
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

    if (window.ShowAgentFilter && $('#filterAgentSelect').length) {
        var $agentSelect = $('#filterAgentSelect');
        if (!$agentSelect.parent().hasClass('position-relative')) {
            $agentSelect.wrap('<div class="position-relative"></div>');
        }
        $agentSelect.select2({
            placeholder: Role === '1' ? 'همه نمایندگان' : 'انتخاب نماینده زیرمجموعه',
            dropdownParent: $agentSelect.parent(),
            allowClear: true
        });
        if (typeof GetUsersSelectForSubscriptions === 'function') {
            GetUsersSelectForSubscriptions('#filterAgentSelect');
        }
    }

    $('#btnApplySubscriptionFilters').on('click', function () {
        if (dt_basic) dt_basic.ajax.reload();
    });

    $('#btnClearSubscriptionFilters').on('click', function () {
        $('#filterAgentSelect').val('').trigger('change');
        $('#filterFromDate').val('');
        $('#filterToDate').val('');
        $('#filterSortMode').val('');
        $('#subscriptionFilterSummary').addClass('d-none');
        if (dt_basic) dt_basic.ajax.reload();
    });

    // DataTable with buttons
    // --------------------------------------------------------------------

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: {
                url: '/App/Subscriptions/GetAll',
                type: 'POST',
                data: function (d) {
                    d.filterAgentId = $('#filterAgentSelect').val() || '';
                    d.filterFromDate = $('#filterFromDate').val() || '';
                    d.filterToDate = $('#filterToDate').val() || '';
                    d.filterSortLowVolume = $('#filterSortMode').val() === 'lowVolume' ? '1' : '0';
                },
                dataSrc: function (json) {
                    updateSubscriptionSummary(json.summary);
                    return json.data;
                },
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
                { data: 'OnlineUsers', className: "text-center" },
                { data: 'PlanName', className: "text-center" },
                { data: 'ExpireDate', className: "text-center" },
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
                        var $name = full['Name'].split('@')[0];
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
                    // DaysLeft
                    targets: 6,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {

                        var $OnlineUsers = full['OnlineUsers'];
                        var $LimitUsers = full['LimitUsers'];
                        var $Exceeded = full['Exceeded'];

                        if ($OnlineUsers < $LimitUsers && $OnlineUsers >= 1) {
                            var $row_output = "<span class='badge bg-label-warning'>" + $OnlineUsers + "/" + $LimitUsers + "</span>";
                        }
                        else {
                            if ($Exceeded) {
                                var $row_output = "<span class='badge bg-label-danger'>" + $OnlineUsers + "/" + $LimitUsers + "</span>";
                            }
                            else {
                                var $row_output = "<span class='badge bg-label-success'>" + $OnlineUsers + "/" + $LimitUsers + "</span>";
                            }
                        }
                        



                        return $row_output;
                    }
                },
                {
                    // PlanName
                    targets: 7,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $PlanName = full['PlanName'];
                        var $row_output = "<span>" + $PlanName + "</span>";
                        return $row_output;
                    }
                },
                {
                    // ExpireDate
                    targets: 8,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $ExpireDate = full['ExpireDate'];
                        var $row_output = "<span>" + $ExpireDate + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 9,
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
                        var $Suplink = full["BackupLink"];
                        var $IsActive = full["IsActive"];
                        var $DayCount = full["DaysLeft"];
                        var $Volume = full["RemainingVolume"];
                        var $UsedVolume = full["UsedVolume"];
                        var $Name = full["Name"].split('@')[0];
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
                            menu += '<button data-id="' + user_id + '" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit" type="button"><i class="text-primary ti ti-pencil"></i></button>';
                        }

                        return (
                            '<div class="d-flex align-items-center">' +
                            '<button data-bs-toggle="popover" title="QR Code Link" onclick="ShowQRCode(\'' + $link + '\')" class="btn btn-sm btn-icon item-qrcode"><i class="text-primary ti ti-qrcode"></i></button>' +
                            '<button data-bs-toggle="popover" title="QR Code Backup Link" onclick="ShowQRCode(\'' + $Suplink + '\')" class="btn btn-sm btn-icon item-qrcode"><i class="text-primary ti ti-qrcode"></i></button>' +
                            '<a href="javascript:;" class="text-primary dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm mx-1"></i></a>' +
                            '<div class="dropdown-menu dropdown-menu-start m-0">' +
                            (Role == "1" ? '<button data-id="' + user_id + '" class="dropdown-item item-edit">ویرایش</button>' : '') +
                            '<button data-id="' + user_id + '" class="dropdown-item item-refresh">تمدید</button>' +
                            '<button data-id="' + user_id + '" data-name="' + $Name + '" class="dropdown-item item-packages">بسته‌های فعال</button>' +
                            '<button  onclick="copyToClipboard(\'' + $link + '\')"  class="dropdown-item item-copy">کپی لینک اصلی</button>' +
                            '<button  onclick="copyToClipboard(\'' + $Suplink + '\')"  class="dropdown-item item-copy">کپی لینک پشتیبان</button>' +
                            '<button  data-id="' + user_id + '" class="dropdown-item item-unlink">تغییر لینک</button>' +
                            '<button  data-id="' + user_id + '" class="dropdown-item item-changename" data-id2="' + full["Name"] + '">تغییر نام</button>' +
                            '<button class="dropdown-item item-history" data-id="' + user_id + '" data-name="' + $Name + '">تاریخچه مصرف</button>' +
                            '<button data-bs-toggle="popover" data-id="' + $IsActive + '" data-id2="' + user_id + '" class="dropdown-item item-access">' + $state + '</button>' +
                            '<div class="dropdown-divider"></div>' +
                            '<li><button data-used="' + $UsedVolume + '" data-id="' + user_id + '"data-user="' + $Name + '"  data-id-vol="' + $Volume + '" data-id-time="' + full["DaysLeft"] + '" class="dropdown-item text-danger item-delete">حذف</button></li>' +
                            '</ul>' +
                            '</div>' + menu +
                            '</div>'
                        );
                    }
                }
            ],
            order: [6, "desc"],
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
                loadingRecords: "در حال بارگزاری ..."
            },
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
            responsive: {
                details: {
                    display: $.fn.dataTable.Responsive.display.modal({
                        header: function (row) {
                            var data = row.data();
                            return data['Name'].split('@')[0];
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
        $('div.head-label').html('<h5 class="card-title mb-0">اشتراک‌ها</h5>');
    }

    //مربوط به نمایش مودال ویرایش اشتراک
    var expirePickerInstance = null;

    function initExpirePicker(defaultDate) {
        var pickerEl = document.querySelector('#expire-picker');
        if (!pickerEl || typeof flatpickr === 'undefined') return;

        if (expirePickerInstance) {
            expirePickerInstance.destroy();
            expirePickerInstance = null;
        }

        var opts = {
            disableMobile: "true",
            altInput: true,
            altFormat: 'j F Y',
            dateFormat: 'Y/m/d',
            locale: 'fa'
        };
        if (defaultDate) {
            opts.defaultDate = defaultDate;
        }
        expirePickerInstance = flatpickr(pickerEl, opts);
    }

    $('body').on('click', '.item-edit', function () {


        $(".dtr-bs-modal").modal("hide");

        var user_id = $(this).attr("data-id");
        document.getElementById('EditUserForm').reset();
        $("#modalEditSub").modal("show");

        BodyBlockUI();

        AjaxGet('/App/Subscriptions/Edit?user_id=' + user_id).then(res => {

            if (res.status == "success") {

                $("#modalEditSub input[name='user_id']").val(user_id);
                var data = res.data;
                var expireDate = '';
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        if (key == "userExpire") {
                            expireDate = data[key];
                        } else {
                            $('#modalEditSub input[name=' + key + ']').val(data[key]);
                        }
                    }
                }
                initExpirePicker(expireDate);

            }
            else {
                eval(res.data);
            }
            BodyUnblockUI();
        });



    });

    //مربوط به مودال تمدید (رزرو بسته)
    $('body').on('click', '.item-refresh', function () {
        $(".dtr-bs-modal").modal("hide");
        var user_id = $(this).attr("data-id");
        $("#modalRenew").modal("show");
        $("#modalRenew input[name='user_id']").val(user_id);
    });

    //نمایش بسته‌های فعال و رزرو
    var currentPackagesUserId = null;
    var currentPackagesSubName = '';

    function loadPackagesModal(user_id, subName) {
        currentPackagesUserId = user_id;
        currentPackagesSubName = subName || '';

        BodyBlockUI();
        AjaxGet('/App/Subscriptions/GetSubscriptionPackages?user_id=' + user_id).then(res => {
            BodyUnblockUI();
            if (res.status !== "success") {
                showToast("خطا", res.message || "خطا در دریافت اطلاعات", "text-danger");
                return;
            }

            $('#packagesSubName').text(res.data.subscriptionName || currentPackagesSubName);
            var current = res.data.current;
            var currentBadge = current.status === 'فعال' ? 'bg-label-success' : 'bg-label-warning';
            $('#packagesCurrentBody').html(
                '<tr>' +
                '<td>' + current.planName + '</td>' +
                '<td>' + current.totalVolumeGb + '</td>' +
                '<td>' + current.remainingVolumeGb + '</td>' +
                '<td>' + current.expireDate + '</td>' +
                '<td><span class="badge ' + currentBadge + '">' + current.status + '</span></td>' +
                '</tr>'
            );

            var reservedHtml = '';
            if (res.data.reserved && res.data.reserved.length > 0) {
                res.data.reserved.forEach(function (item) {
                    var months = item.months == 0 ? 'نامحدود' : item.months;
                    reservedHtml += '<tr>' +
                        '<td>' + item.planName + '</td>' +
                        '<td>' + item.volumeGb + '</td>' +
                        '<td>' + months + '</td>' +
                        '<td>' + item.reservedDate + '</td>' +
                        '<td><span class="badge bg-label-warning">' + item.status + '</span></td>' +
                        '<td class="text-nowrap">' +
                        '<button type="button" class="btn btn-sm btn-success btn-activate-reserved me-1" data-order-id="' + item.orderId + '">فعال‌سازی</button>' +
                        '<button type="button" class="btn btn-sm btn-label-danger btn-cancel-reserved" data-order-id="' + item.orderId + '">حذف</button>' +
                        '</td>' +
                        '</tr>';
                });
            } else {
                reservedHtml = '<tr><td colspan="6" class="text-center text-muted">بسته رزروی وجود ندارد</td></tr>';
            }
            $('#packagesReservedBody').html(reservedHtml);
            $("#modalPackages").modal("show");
        });
    }

    $('body').on('click', '.item-packages', function () {
        $(".dtr-bs-modal").modal("hide");
        var user_id = $(this).attr("data-id");
        var subName = $(this).attr("data-name");
        loadPackagesModal(user_id, subName);
    });

    function postReservedPackageAction(url, orderId) {
        return new Promise(function (resolve, reject) {
            $.ajax({
                url: url,
                type: 'POST',
                data: {
                    orderId: orderId,
                    __RequestVerificationToken: $('#packagesActionForm input[name="__RequestVerificationToken"]').val()
                },
                success: resolve,
                error: reject
            });
        });
    }

    $('body').on('click', '.btn-activate-reserved', function () {
        var orderId = $(this).data('order-id');
        Swal.fire({
            text: 'آیا مطمئن هستید می‌خواهید اشتراک را فعال کنید؟',
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'بله، فعال می‌کنم',
            cancelButtonText: 'انصراف',
            customClass: {
                confirmButton: 'btn btn-success me-3 waves-effect waves-light',
                cancelButton: 'btn btn-label-secondary waves-effect waves-light'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.value) return;

            BodyBlockUI();
            postReservedPackageAction('/App/Subscriptions/ActivateReservedPackage', orderId).then(function (res) {
                BodyUnblockUI();
                eval(res.data);
                if (res.status === 'success' && currentPackagesUserId) {
                    loadPackagesModal(currentPackagesUserId, currentPackagesSubName);
                    if (dt_basic) dt_basic.ajax.reload(null, false);
                }
            }).catch(function () {
                BodyUnblockUI();
                showToast('خطا', 'خطا در فعال‌سازی اشتراک', 'text-danger');
            });
        });
    });

    $('body').on('click', '.btn-cancel-reserved', function () {
        var orderId = $(this).data('order-id');
        Swal.fire({
            text: 'آیا مطمئن هستید می‌خواهید حذف کنید؟',
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
            if (!result.value) return;

            BodyBlockUI();
            postReservedPackageAction('/App/Subscriptions/CancelReservedPackage', orderId).then(function (res) {
                BodyUnblockUI();
                eval(res.data);
                if (res.status === 'success' && currentPackagesUserId) {
                    loadPackagesModal(currentPackagesUserId, currentPackagesSubName);
                    if (dt_basic) dt_basic.ajax.reload(null, false);
                }
            }).catch(function () {
                BodyUnblockUI();
                showToast('خطا', 'خطا در حذف اشتراک', 'text-danger');
            });
        });
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
        var Used = $(this).attr("data-used");

        if ((vol < 0 || time == 0) || Role == "1" || Role == "4" || Used <= 1) {
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

    //مربوط به نمایش مودال نام اشتراک
    $('body').on('click', '.item-changename', function () {


        $(".dtr-bs-modal").modal("hide");

        var user_id = $(this).attr("data-id");
        var OldName = $(this).attr("data-id2");

        document.getElementById('ChangeNameUserForm').reset();
        $("#modalChangeName").modal("show");

        $("#modalChangeName input[name='user_id']").val(user_id);
        $("#modalChangeName input[name='OldName']").val(OldName);


    });

    //تاریخچه مصرف کاربران
    var dtUsageHistory = null;
    var currentUsageUserId = null;
    var currentUsageSubName = '';
    var usageFromPicker = null;
    var usageToPicker = null;

    function updateUsageHistorySummary(summary) {
        if (!summary) {
            $('#usageHistorySummary').addClass('d-none');
            return;
        }

        $('#usageSummaryDownload').text(summary.TotalDownload || '-');
        $('#usageSummaryUpload').text(summary.TotalUpload || '-');
        $('#usageSummaryTotal').text(summary.Total || '-');
        $('#usageSummaryRange').text((summary.FromDate || '-') + ' تا ' + (summary.ToDate || '-'));
        $('#usageHistorySummary').removeClass('d-none');
    }

    function setDefaultUsageDateRange() {
        var now = new Date();
        var from = new Date();
        from.setDate(from.getDate() - 30);

        if (usageFromPicker) {
            usageFromPicker.setDate(from, false);
        }
        if (usageToPicker) {
            usageToPicker.setDate(now, false);
        }
    }

    function initUsageDatePickers() {
        var fromEl = document.querySelector('#usageHistoryFromDate');
        var toEl = document.querySelector('#usageHistoryToDate');

        if (fromEl && !usageFromPicker) {
            usageFromPicker = flatpickr(fromEl, {
                disableMobile: true,
                locale: 'fa',
                altInput: true,
                altFormat: 'j F Y',
                dateFormat: 'Y/m/d'
            });
        }

        if (toEl && !usageToPicker) {
            usageToPicker = flatpickr(toEl, {
                disableMobile: true,
                locale: 'fa',
                altInput: true,
                altFormat: 'j F Y',
                dateFormat: 'Y/m/d'
            });
        }
    }

    function destroyUsageHistoryTable() {
        if (dtUsageHistory) {
            dtUsageHistory.destroy();
            dtUsageHistory = null;
        }
        updateUsageHistorySummary(null);
    }

    function loadUsageHistoryTable() {
        if (!currentUsageUserId) {
            return;
        }

        var fromDate = $('#usageHistoryFromDate').val();
        var toDate = $('#usageHistoryToDate').val();
        var url = '/App/Subscriptions/GetSubUseage?user_id=' + currentUsageUserId;

        if (fromDate) {
            url += '&fromDate=' + encodeURIComponent(fromDate);
        }
        if (toDate) {
            url += '&toDate=' + encodeURIComponent(toDate);
        }

        destroyUsageHistoryTable();

        dtUsageHistory = $('.usage-history-table').DataTable({
            ajax: {
                url: url,
                dataSrc: function (json) {
                    if (!json || json.status !== 'success') {
                        updateUsageHistorySummary(null);
                        if (json && json.message) {
                            showToast('خطا', json.message, 'text-danger');
                        }
                        return [];
                    }

                    updateUsageHistorySummary(json.summary);
                    return json.data || [];
                }
            },
            columns: [
                {
                    data: 'Date',
                    render: function (data, type, full) {
                        if (type === 'sort' || type === 'type') {
                            return full.DateSort || 0;
                        }
                        return data;
                    }
                },
                { data: 'Download' },
                { data: 'Upload' },
                { data: 'Total' }
            ],
            order: [[0, 'desc']],
            dom:
                '<"row mx-2"' +
                '<"col-sm-12 col-md-6"l>' +
                '<"col-sm-12 col-md-6 d-flex justify-content-md-end justify-content-center"f>' +
                '>t' +
                '<"row mx-2"' +
                '<"col-sm-12 col-md-6"i>' +
                '<"col-sm-12 col-md-6"p>' +
                '>',
            language: {
                sLengthMenu: '_MENU_',
                search: '',
                searchPlaceholder: 'جستجو در جدول...',
                paginate: {
                    first: 'اولین',
                    last: 'آخرین',
                    next: 'بعدی',
                    previous: 'قبلی'
                },
                info: 'نمایش _START_ تا _END_ از _TOTAL_ ورودی',
                lengthMenu: 'نمایش _MENU_ ورودی',
                zeroRecords: 'موردی یافت نشد',
                infoEmpty: 'هیچ موردی موجود نیست',
                infoFiltered: '(فیلتر شده از _MAX_ ورودی)',
                loadingRecords: 'در حال بارگزاری ...'
            },
            displayLength: 10,
            lengthMenu: [10, 25, 50, 100]
        });
    }

    initUsageDatePickers();

    $('body').on('click', '.item-history', function () {
        currentUsageUserId = $(this).attr('data-id');
        currentUsageSubName = $(this).attr('data-name') || '';

        $('.dtr-bs-modal').modal('hide');
        $('#usageHistoryUserName').text(currentUsageSubName ? '(' + currentUsageSubName + ')' : '');
        setDefaultUsageDateRange();
        $('#modalHistoryUse').modal('show');
        loadUsageHistoryTable();
    });

    $('#btnSearchUsageHistory').on('click', function () {
        loadUsageHistoryTable();
    });

    $('#btnExportUsageHistoryPdf').on('click', function () {
        if (!currentUsageUserId) {
            return;
        }

        var fromDate = $('#usageHistoryFromDate').val();
        var toDate = $('#usageHistoryToDate').val();
        var url = '/App/Subscriptions/ExportSubUsagePdf?user_id=' + currentUsageUserId;

        if (fromDate) {
            url += '&fromDate=' + encodeURIComponent(fromDate);
        }
        if (toDate) {
            url += '&toDate=' + encodeURIComponent(toDate);
        }
        if (currentUsageSubName) {
            url += '&subName=' + encodeURIComponent(currentUsageSubName);
        }

        window.open(url, '_blank');
    });

    $('#modalHistoryUse').on('hidden.bs.modal', function () {
        destroyUsageHistoryTable();
        currentUsageUserId = null;
        currentUsageSubName = '';
        $('#usageHistoryUserName').text('');
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
            url: "/App/Plan/Select2UserPlans",
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
                Plans("#userPlan");
            }

        });
    });

    // پایان فرم افزودن اشتراک







    // فرم مربوط به ویرایش اشتراک

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
    fv_edit.on('core.form.invalid', function (e) {



    });
    // اگر فرم صحیح بود
    fv_edit.on('core.form.valid', function (e) {

        blockUI("#modalEditSub .section-block");

        AjaxFormPost('/App/Subscriptions/Edit', "#EditUserForm").then(res => {
            UnblockUI("#modalEditSub .section-block");
            eval(res.data);
            if (res.status == "success") {


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

        AjaxFormPost('/App/Subscriptions/ReservePackage', "#RenewUserForm").then(res => {
            UnblockUI("#modalRenew .section-block");
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('RenewUserForm').reset();
                $("#modalRenew").modal("hide");
                dt_basic.ajax.reload(null, false);
                Plans("#userPlanRenew");
            }

        });
    });

    // پایان فرم تمدید اشتراک

    //فرم مربوط به تغییر نام اشتراک

    const ChangeNameUserForm = document.getElementById('ChangeNameUserForm');


    const fv_Change = FormValidation.formValidation(ChangeNameUserForm, {
        fields: {
            SubName: {
                validators: {
                    notEmpty: {
                        message: 'نام اشتراک را وارد کنید'
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
    fv_Change.on('core.form.valid', function (e) {


        blockUI("#modalChangeName .section-block");

        AjaxFormPost('/App/Subscriptions/EditSubName', "#ChangeNameUserForm").then(res => {
            UnblockUI("#modalChangeName .section-block");
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('ChangeNameUserForm').reset();
                $("#modalChangeName").modal("hide");
                dt_basic.ajax.reload(null, false);
            }

        });
    });


});


