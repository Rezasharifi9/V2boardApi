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

    var Role = window.PanelUserRole || "";
    if (!Role && document.cookie.split(';').length != 0) {
        var Cookies = document.cookie.split(';');
        var RoleCookie = Cookies.find(cookie => cookie.trim().startsWith("Role="));
        if (RoleCookie) {
            Role = RoleCookie.split('=')[1];
        }
    } else if (!Role && document.cookie) {
        Role = document.cookie.split('=')[1];
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

    var OVERDUE_UNPAID_DAYS = 30;

    function formatAgentMoney(value) {
        var n = Number(value) || 0;
        return Math.round(n).toLocaleString('fa-IR');
    }

    function escapeAgentHtml(value) {
        return String(value == null ? '' : value).replace(/[&<>"']/g, function (c) {
            return ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' })[c];
        });
    }

    function isAgentRow(row) {
        return row && row.role != 1;
    }

    function isOverdueUnpaid(row) {
        return isAgentRow(row) && (Number(row.walletValue) || 0) > 0 && (Number(row.daysUnpaid) || 0) > OVERDUE_UNPAID_DAYS;
    }

    function updateAgentSummaryCards(api) {
        if (!api) {
            return;
        }

        var overdueCount = 0;
        var totalDebt = 0;
        var blockedCount = 0;
        var noTelegramCount = 0;
        var overdueRows = [];

        api.rows().every(function () {
            var row = this.data();
            if (!isAgentRow(row)) {
                return;
            }

            totalDebt += Number(row.walletValue) || 0;
            if (row.isBlocked) {
                blockedCount += 1;
            }
            if (!row.telegramActive) {
                noTelegramCount += 1;
            }
            if (isOverdueUnpaid(row)) {
                overdueCount += 1;
                overdueRows.push(row);
            }
        });

        $('#statOverdueCount').text(formatAgentMoney(overdueCount));
        $('#statTotalDebt').text(formatAgentMoney(totalDebt));
        $('#statBlockedCount').text(formatAgentMoney(blockedCount));
        $('#statNoTelegramCount').text(formatAgentMoney(noTelegramCount));
        $('#cardOverdueDebt').data('overdue-rows', overdueRows);
    }

    function renderOverdueDebtModal(rows) {
        var body = $('#overdueDebtBody');
        body.empty();
        if (!rows || !rows.length) {
            body.append('<tr><td colspan="4" class="text-center text-muted py-4">موردی یافت نشد</td></tr>');
            return;
        }

        rows.sort(function (a, b) {
            return (Number(b.daysUnpaid) || 0) - (Number(a.daysUnpaid) || 0);
        });

        rows.forEach(function (row) {
            var username = escapeAgentHtml(row.username || '-');
            var debt = escapeAgentHtml(row.used || formatAgentMoney(row.walletValue) + ' تومان');
            var lastPay = row.lastPaymentDate ? escapeAgentHtml(row.lastPaymentDate) : 'پرداخت نشده';
            var days = formatAgentMoney(row.daysUnpaid) + ' روز';
            body.append(
                '<tr>' +
                '<td><a href="/App/Admin/Details?user_id=' + row.id + '">' + username + '</a></td>' +
                '<td>' + debt + '</td>' +
                '<td dir="ltr">' + lastPay + '</td>' +
                '<td>' + days + '</td>' +
                '</tr>'
            );
        });
    }

    // Users datatable
    if (dt_user_table.length) {
        var dt_user = dt_user_table.DataTable({
            ajax: {
                url: '/App/Admin/_PartialGetAllUsers',
                data: function (d) {
                    d.filterParentId = $('#filterHeadAgentSelect').val() || '';
                },
                dataSrc: 'data'
            },
            columns: [
                { data: 'id' },
                { data: 'username' },
                { data: 'walletValue' },
                { data: 'limit' },
                { data: 'status' },
                { data: 'telegramActive' },
                { data: 'lastPaymentSort' },
                { data: 'sellCount' },
                { data: 'sumSellCount' },
                { data: 'id' }

            ],
            order: [],
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
                        var userId = full['id'];
                        var $name = full['username'] || full['Username'] || '-';
                        var $image = full['profile'];
                        var $output;
                        var nameClass = 'text-body';
                        if ($image) {
                            $output =
                                '<img src="' + assetsPath + 'img/avatars/' + $image + '" alt="profile" class="rounded-circle">';
                        } else {
                            var stateNum = Math.floor(Math.random() * 6);
                            var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
                            var $state = states[stateNum];
                            var nameParts = String($name).trim().split(/\s+/).filter(Boolean);
                            var $initials = nameParts.length > 0 ? nameParts[0].charAt(0) : '?';
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
                            '" class="' + nameClass + ' text-truncate"><span class="fw-medium">' +
                            $name +
                            '</span></a>' +
                            (full['role'] == 3 ? '<small class="text-muted">نماینده کل</small>' : '') +
                            (full['parentUsername'] && $('#filterHeadAgentSelect').val()
                                ? '<small class="text-muted">زیرمجموعه ' + escapeAgentHtml(full['parentUsername']) + '</small>'
                                : '') +
                            '</div>' +
                            '</div>';
                        return $row_output;
                    }
                },
                {
                    // User Use
                    targets: 2,
                    render: function (data, type, full, meta) {
                        if (type === 'sort' || type === 'type') {
                            return full['walletValue'] || 0;
                        }
                        var $used = full['used'];
                        var walletHtml = '<span>' + $used + '</span>';
                        if (Role == '1' && full['role'] != 1) {
                            walletHtml +=
                                ' <a href="javascript:;" class="text-body EditWallet" data-id="' + full['id'] +
                                '" data-username="' + escapeAgentHtml(full['username'] || '') +
                                '" data-wallet="' + (full['walletValue'] || 0) +
                                '" title="تغییر مبلغ کیف پول"><i class="ti ti-pencil ti-xs"></i></a>';
                        }

                        return walletHtml;
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
                        var info = statusObj[$status] || { title: 'نامشخص', class: 'bg-label-secondary' };
                        var blockedBadge = full['isBlocked']
                            ? '<span class="badge bg-label-danger mt-1">اشتراک مسدود</span>'
                            : '';
                        return (
                            '<div class="d-flex flex-column">' +
                            '<span class="badge ' +
                            info.class +
                            '" text-capitalized>' +
                            info.title +
                            '</span>' +
                            blockedBadge +
                            '</div>'
                        );
                    }
                },
                {
                    targets: 5,
                    render: function (data, type, full, meta) {
                        if (type === 'sort' || type === 'type' || type === 'filter') {
                            return full['telegramActive'] ? 1 : 0;
                        }
                        if (full['telegramActive']) {
                            return '<span class="badge bg-label-success">تلگرام فعال</span>';
                        }
                        return '<span class="badge bg-label-secondary">ثبت نشده</span>';
                    }
                },
                {
                    targets: 6,
                    render: function (data, type, full, meta) {
                        if (type === 'sort' || type === 'type') {
                            return full['lastPaymentSort'] || 0;
                        }
                        var label = full['lastPaymentDate'];
                        var overdue = isOverdueUnpaid(full);
                        if (!label) {
                            return overdue
                                ? '<span class="text-danger fw-medium">پرداخت نشده</span>'
                                : '<span class="text-muted">پرداخت نشده</span>';
                        }
                        return overdue
                            ? '<span class="text-danger fw-medium" dir="ltr">' + label + '</span>'
                            : '<span dir="ltr">' + label + '</span>';
                    }
                },
                {
                    // Sell Count
                    targets: 7,
                    render: function (data, type, full, meta) {
                        var $role = full['sellCount'];
                        return "<span class='text-truncate d-flex align-items-center'>" + $role + '</span>';
                    }
                },
                {
                    // Sum Sell
                    targets: 8,
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
                        var username = full['username'] || full['Username'] || '';
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

                        var menuRobot = "";

                        if (Role == "1") {
                            menuRobot = '<a href="javascript:;" class="dropdown-item StartBot" data-id=' + $id + '>' + "تغییر وضعیت ربات" + '</a>';
                        }

                        var menuDelete = "";
                        if (Role == "1") {
                            menuDelete = '<a href="javascript:;" class="dropdown-item DeleteAgent text-danger" data-id=' + $id + ' data-username="' + username + '">حذف نماینده</a>';
                        }

                        var menuBlock = '';
                        if (full['role'] != 1) {
                            if (full['isBlocked']) {
                                menuBlock = '<a href="javascript:;" class="dropdown-item UnblockAgentSubs" data-id=' + $id + ' data-username="' + username + '">رفع مسدودسازی اشتراک‌ها</a>';
                            } else {
                                menuBlock = '<a href="javascript:;" class="dropdown-item BlockAgentSubs" data-id=' + $id + ' data-username="' + username + '">مسدودسازی اشتراک‌های زیرمجموعه</a>';
                            }
                        }

                        var sendMessageBtn = full['role'] != 1
                            ? '<a href="javascript:;" class="text-body SendAgentMessage" data-id=' + $id + ' data-username="' + username + '" title="ارسال پیام"><i class="ti ti-mail ti-sm me-2"></i></a>'
                            : '';

                        return (
                            '<div class="d-flex align-items-center">' +
                            sendMessageBtn +
                            '<a href="javascript:;" class="text-body EditUser" data-id=' + $id + '><i class="ti ti-edit ti-sm me-2"></i></a>' +
                            '<a href="javascript:;" class="text-body dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm mx-1"></i></a>' +
                            '<div class="dropdown-menu dropdown-menu-end m-0">' +
                            '<a href="/App/Admin/Details?user_id=' + userId +
                            '" class="dropdown-item">نمایش</a>' +
                            '<a href="javascript:;" class="dropdown-item BanUser" data-id=' + $id + '>' + $StatusTitle + '</a>' +
                            menuBlock +
                            menuDelete +
                            menuRobot +
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
                loadingRecords: "در حال بارگزاری ..."
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
                                columns: [1, 2, 3, 4, 5, 6, 7, 8],
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
                                columns: [1, 2, 3, 4, 5, 6, 7, 8],
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
                                columns: [1, 2, 3, 4, 5, 6, 7, 8],
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
                                columns: [1, 2, 3, 4, 5, 6, 7, 8],
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
                                columns: [1, 2, 3, 4, 5, 6, 7, 8],
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
            drawCallback: function () {
                updateAgentSummaryCards(this.api());
            },
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

        if ($('#filterHeadAgentSelect').length) {
            var $headSelect = $('#filterHeadAgentSelect');
            if (!$headSelect.parent().hasClass('position-relative')) {
                $headSelect.wrap('<div class="position-relative"></div>');
            }
            $headSelect.select2({
                placeholder: 'نمایندگان مستقیم',
                dropdownParent: $headSelect.parent(),
                allowClear: true,
                width: '100%'
            });
            $headSelect.on('change', function () {
                dt_user.ajax.reload();
            });
        }
    }


    var dt_basic_table = $('.datatables-plan');
    var dt_basic;
    // DataTable with buttons
    // --------------------------------------------------------------------


 
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
                        eval(res.data);
                        if (res.status == "success") {
                            dt_user.ajax.reload(null, false);
                        }
                    }
                })

            }
        });
    });

    $('body').on('click', '.DeleteAgent', function () {
        var id = $(this).attr('data-id');
        var username = $(this).attr('data-username') || '';

        $.ajax({
            url: '/App/Admin/GetDeleteAgentPreview?id=' + id,
            type: 'get',
            dataType: 'json',
            success: function (previewRes) {
                if (previewRes.status !== 'success' || !previewRes.data) {
                    Swal.fire({
                        title: 'خطا',
                        text: previewRes.message || 'امکان حذف این نماینده وجود ندارد.',
                        icon: 'error',
                        confirmButtonText: 'باشه'
                    });
                    return;
                }

                var preview = previewRes.data;
                var confirmText = preview.message || preview.Message || '';
                confirmText = confirmText.replace(/\n/g, '<br>');

                Swal.fire({
                    title: 'حذف نماینده «' + (preview.username || preview.Username || username) + '»',
                    html: '<div class="text-start" style="white-space:normal;line-height:1.8;">' + confirmText + '</div>',
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'بله، حذف شود',
                    cancelButtonText: 'انصراف',
                    customClass: {
                        confirmButton: 'btn btn-danger me-3 waves-effect waves-light',
                        cancelButton: 'btn btn-label-secondary waves-effect waves-light'
                    },
                    buttonsStyling: false
                }).then(function (result) {
                    if (!result.value)
                        return;

                    $.ajax({
                        url: '/App/Admin/DeleteAgent?id=' + id,
                        type: 'post',
                        dataType: 'json',
                        success: function (res) {
                            if (res.data)
                                eval(res.data);
                            if (res.status === 'success')
                                dt_user.ajax.reload(null, false);
                        }
                    });
                });
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

    $('#cardOverdueDebt').on('click', function () {
        renderOverdueDebtModal($(this).data('overdue-rows') || []);
        var modalEl = document.getElementById('modalOverdueDebt');
        var modal = bootstrap.Modal.getOrCreateInstance(modalEl);
        modal.show();
    });

    var currentMessageAgentId = 0;

    $('body').on('click', '.SendAgentMessage', function () {
        currentMessageAgentId = $(this).attr('data-id');
        var username = $(this).attr('data-username') || '';
        $('#sendAgentMessageName').text(username);
        $('#sendAgentMessageText').val('');
        $(".dtr-bs-modal").modal("hide");
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('modalSendAgentMessage'));
        modal.show();
    });

    $('#btnSendAgentMessage').on('click', function () {
        var id = currentMessageAgentId;
        var message = ($('#sendAgentMessageText').val() || '').trim();
        if (!message) {
            Swal.fire({
                title: 'هشدار',
                text: 'متن پیام را وارد کنید',
                icon: 'warning',
                confirmButtonText: 'باشه'
            });
            return;
        }

        BodyBlockUI();
        $.ajax({
            url: '/App/Admin/SendAgentMessage',
            type: 'post',
            dataType: 'json',
            data: { id: id, message: message },
            success: function (res) {
                BodyUnblockUI();
                if (res.data) {
                    eval(res.data);
                }
                if (res.status === 'success' || res.status === 'warning') {
                    var modal = bootstrap.Modal.getInstance(document.getElementById('modalSendAgentMessage'));
                    if (modal) {
                        modal.hide();
                    }
                }
            },
            error: function () {
                BodyUnblockUI();
            }
        });
    });

    function confirmAgentNetworkAction(options) {
        Swal.fire({
            title: options.title,
            html: '<div class="text-start" style="line-height:1.8;">' + options.html + '</div>',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: options.confirmText,
            cancelButtonText: 'انصراف',
            customClass: {
                confirmButton: options.confirmClass + ' me-3 waves-effect waves-light',
                cancelButton: 'btn btn-label-secondary waves-effect waves-light'
            },
            buttonsStyling: false
        }).then(function (result) {
            if (!result.value) {
                return;
            }

            BodyBlockUI();
            $.ajax({
                url: options.url,
                type: 'post',
                dataType: 'json',
                data: { id: options.id },
                success: function (res) {
                    BodyUnblockUI();
                    if (res.data) {
                        eval(res.data);
                    }
                    if (res.status === 'success') {
                        dt_user.ajax.reload(null, false);
                    }
                },
                error: function () {
                    BodyUnblockUI();
                }
            });
        });
    }

    $('body').on('click', '.BlockAgentSubs', function () {
        var id = $(this).attr('data-id');
        var username = $(this).attr('data-username') || '';
        confirmAgentNetworkAction({
            id: id,
            url: '/App/Admin/BlockAgentSubscriptions',
            title: 'مسدودسازی اشتراک‌ها',
            confirmText: 'بله، مسدود شود',
            confirmClass: 'btn btn-danger',
            html: 'تمام اشتراک‌های نماینده «' + escapeAgentHtml(username) + '» و در صورت نماینده کل بودن، اشتراک‌های نمایندگان زیرمجموعه هم مسدود می‌شود.'
        });
    });

    $('body').on('click', '.UnblockAgentSubs', function () {
        var id = $(this).attr('data-id');
        var username = $(this).attr('data-username') || '';
        confirmAgentNetworkAction({
            id: id,
            url: '/App/Admin/UnblockAgentSubscriptions',
            title: 'رفع مسدودسازی اشتراک‌ها',
            confirmText: 'بله، رفع مسدود شود',
            confirmClass: 'btn btn-primary',
            html: 'مسدودسازی اشتراک‌های نماینده «' + escapeAgentHtml(username) + '» و نمایندگان زیرمجموعه برداشته می‌شود.'
        });
    });

    function formatWalletInput(value) {
        var digits = String(Math.round(Number(value) || 0)).replace(/\D/g, '');
        return digits.replace(/\B(?=(\d{3})+(?!\d))/g, ',');
    }

    $('body').on('click', '.EditWallet', function () {
        if (Role != '1') {
            return;
        }

        var id = $(this).attr('data-id');
        var username = $(this).attr('data-username') || '';
        var wallet = $(this).attr('data-wallet') || 0;
        $('.dtr-bs-modal').modal('hide');
        $('#editWalletUserId').val(id);
        $('#editWalletAgentName').text(username);
        $('#Deposit').val(formatWalletInput(wallet));
        var modal = bootstrap.Modal.getOrCreateInstance(document.getElementById('editWallet'));
        modal.show();
    });

    $('#ZeroWallet').on('click', function () {
        $('#Deposit').val(0);
    });

    $('#SaveDeposit').on('click', function () {
        if (Role != '1') {
            return;
        }

        BodyBlockUI();
        AjaxFormPost('/App/Admin/EditWallet', '#editWalletForm').then(function (res) {
            BodyUnblockUI();
            if (res.data) {
                eval(res.data);
            }
            if (res.status == 'success') {
                var modal = bootstrap.Modal.getInstance(document.getElementById('editWallet'));
                if (modal) {
                    modal.hide();
                }
                dt_user.ajax.reload(null, false);
            }
        }).catch(function () {
            BodyUnblockUI();
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

    if (addNewUserForm) {
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

    fv.on('core.form.valid', function (e) {

        BodyBlockUI();
        AjaxFormPost('/App/Admin/CreateOrEdit', "#addNewUserForm").then(res => {
            BodyUnblockUI();
            eval(res.data);
            
            if (res.status == "success") {
                dt_user.ajax.reload(null, false);
                // بستن offcanvas پس از موفقیت آمیز بودن ارسال فرم
                var offcanvasElement = document.getElementById('offcanvasAddUser');
                var offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);
                offcanvas.hide();
                $("input[name='userId']").val(0);
                document.getElementById('addNewUserForm').reset();
            }

        });
    });
    }


    var addNewPlanForm = document.getElementById('addNewPlanForm');
    if (addNewPlanForm) {
    const fv_plan = FormValidation.formValidation(addNewPlanForm, {
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
    fv_plan.on('core.form.valid', function (e) {


        blockUI('.section-block');
        AjaxFormPost('/App/Admin/SetPlan', "#addNewPlanForm").then(res => {

            eval(res.data);
            if (res.status == "success") {
                UnblockUI(".section-block")
                document.getElementById('addNewPlanForm').reset();
                dt_user.ajax.reload(null, false);
                $("#addNewPlan").modal("hide");

            }

        });
    });
    }
});

// Validation & Phone mask



