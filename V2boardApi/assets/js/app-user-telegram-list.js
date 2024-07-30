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

    var dt_user_table = $('.datatables-users'),
        userView = '/App/TelegramUsers/Details?user_id=' + getUrlParameter("user_id"),
        statusObj = {
            0: { title: 'مسدود', class: 'bg-label-danger' },
            1: { title: 'فعال', class: 'bg-label-success' },
        };

    if (dt_user_table.length) {
        var dt_user = dt_user_table.DataTable({
            ajax: '/App/TelegramUsers/_PartialGetAllUsers',
            columns: [
                { data: 'Profile' },
                { data: 'Username' },
                { data: 'FullName' },
                { data: 'SumBuy' },
                { data: 'Wallet' },
                { data: 'Status' },
                { data: 'Invited' },
            ],
            columnDefs: [
                {
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
                    targets: 1,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $name = full['FullName'],
                            $email = full['Username'],
                            $image = full['Profile'],
                            $userId = full["id"];
                        if ($image) {
                            var $output =
                                '<img src="' + assetsPath + 'img/avatars/' + $image + '" alt="Avatar" class="rounded-circle">';
                        } else {
                            var stateNum = Math.floor(Math.random() * 6);
                            var states = ['success', 'danger', 'warning', 'info', 'primary', 'secondary'];
                            var $state = states[stateNum],
                                $name = full['FullName'];
                            let nameParts = $name.split(" ");
                            let $initials = nameParts[0].charAt(0) + "‌" + nameParts[1].charAt(0);
                            $output = '<span class="avatar-initial rounded-circle bg-label-' + $state + '">' + $initials + '</span>';
                        }
                        var $row_output =
                            '<div class="d-flex justify-content-start align-items-center user-name">' +
                            '<div class="avatar-wrapper">' +
                            '<div class="avatar me-3">' +
                            $output +
                            '</div>' +
                            '</div>' +
                            '<div class="d-flex flex-column">' +
                            '<a href="/App/TelegramUsers/Details?user_id=' + $userId + '" class="text-body text-truncate"><span class="fw-medium">' +
                            $name +
                            '</span></a>' +
                            '<small class="text-muted">' +
                            $email +
                            '</small>' +
                            '</div>' +
                            '</div>';
                        return $row_output;
                    }
                },
                {
                    targets: 2,
                    render: function (data, type, full, meta) {
                        var $Wallet = full['Wallet'];
                        return (
                            '<span>' +
                            $Wallet +
                            '</span>'
                        );
                    }
                },
                {
                    targets: 3,
                    render: function (data, type, full, meta) {
                        var $SumBuy = full['SumBuy'];
                        return (
                            '<span>' + $SumBuy + '</span>'
                        );
                    }
                },
                {
                    targets: 4,
                    render: function (data, type, full, meta) {
                        var $status = full['Status'];
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
                    targets: 5,
                    render: function (data, type, full, meta) {
                        var $Invited = full['Invited'];
                        var $InvitedUser = full['InviteUser'];
                        if ($Invited == 1) {
                            return (
                                '<span class="badge bg-label-success" text-capitalized>' +
                                $InvitedUser +
                                '</span>'
                            );
                        }
                        else {
                            return (
                                '<span class="badge bg-label-warning" text-capitalized>' +
                                "دعوت نشده" +
                                '</span>'
                            );
                        }
                    }
                },
                {
                    targets: -1,
                    title: 'عملیات',
                    searchable: false,
                    orderable: false,
                    render: function (data, type, full, meta) {
                        var userId = full["id"];
                        var $status = full['Status'];
                        var $StatusTitle = "";
                        if ($status == 0) {
                            $StatusTitle = "فعال کردن";
                        }
                        else {
                            $StatusTitle = "غیرفعال کردن";
                        }
                        return (
                            '<div class="d-flex align-items-center">' +
                            '<a href="javascript:;" class="text-body dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="ti ti-dots-vertical ti-sm mx-1"></i></a>' +
                            '<div class="dropdown-menu dropdown-menu-end m-0">' +
                            '<a href="/App/TelegramUsers/Details?user_id=' + userId +
                            '" class="dropdown-item">نمایش</a>' +
                            '<a href="javascript:;" class="dropdown-item BanUser" data-id=' + userId + '>' + $StatusTitle + '</a>' +
                            '</div>' +
                            '</div>'
                        );
                    }
                }
            ],
            language: {
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
                            return col.title !== ''
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

    // Active Or DeActive User
    $('body').on('click', '.BanUser', function () {
        //var id = $(this).attr("data-id");
        //Swal.fire({
        //    title: 'هشدار',
        //    text: "مطمئنی میخای وضعیت کاربر رو تغییر بدی ؟!",
        //    icon: 'warning',
        //    showCancelButton: true,
        //    confirmButtonText: 'بله',
        //    cancelButtonText: 'بازگشت',
        //    customClass: {
        //        confirmButton: 'btn btn-primary me-3 waves-effect waves-light',
        //        cancelButton: 'btn btn-label-secondary waves-effect waves-light'
        //    },
        //    buttonsStyling: false
        //}).then(function (result) {
        //    if (result.value) {
        //        $.ajax({
        //            url: "/App/Admin/BanUser?id=" + id,
        //            type: "get",
        //            dataType: "json",
        //            success: function (res) {
        //                dt_user.ajax.reload(null, false);
        //            }
        //        })
        //    }
        //});
    });

    // Active Or DeActive Robot
    $('body').on('click', '.StartBot', function () {
        var id = $(this).attr("data-id");
    });

    // Edit User
    $('body').on('click', '.EditUser', function () { });
});



// Validation & Phone mask



