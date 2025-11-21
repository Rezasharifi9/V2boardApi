
// datatable (jquery)
$(function () {
    var dt_basic_table = $('.datatables-plan');
    var dt_basic;

    // DataTable with buttons
    // --------------------------------------------------------------------

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: '/App/BotFactors/GetFactores',
            columns: [
                { data: '' },
                { data: 'User' },
                { data: 'Date' },
                { data: 'PayMethod' },
                { data: 'Status' },
                { data: 'Price' },
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
                    // User
                    targets: 1,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $name = full['User'];
                        var $id = full['UserId'];

                        // Creates full output for row
                        var $row_output = "<a href='/App/TelegramUsers/Details?user_id=" + $id + "'>" + $name + "</a>";
                        return $row_output;
                    }
                },
                {
                    // Date
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Date = full['Date'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Date + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 3,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $PayMethod = full['PayMethod'];

                        var statusObj = {
                            0: { title: 'کارت به کارت', class: 'bg-label-success' },
                            1: { title: 'درگاه پرداخت', class: 'bg-label-primary' },
                            2: { title: 'هاب اسمارت', class: 'bg-label-danger' },
                            3: { title: 'آرانکس', class: 'bg-label-secondary' },
                        };

                        var $row_output = "<span class='badge " + statusObj[$PayMethod].class + "'>" + statusObj[$PayMethod].title + "</span>";

                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 4,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Traffic = full['Status'];

                        var statusObj = {
                            0: { title: 'پرداخت نشده', class: 'bg-label-danger' },
                            1: { title: 'پرداخت شده', class: 'bg-label-success' },
                        };

                        var $row_output = "<span class='badge " + statusObj[$Traffic].class + "'>" + statusObj[$Traffic].title + "</span>";

                        return $row_output;
                    }
                },
                {
                    // Price
                    targets: 5,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Price = full['Price'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Price + ' ریال' + "</span>";
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
                            '<a data-bs-toggle="popover" title="تائید فاکتور" data-id=' + full["Id"] + ' data-id-price=' + full["Price"] + ' class="btn btn-sm btn-icon item-accpet"><i class="text-primary ti ti-checklist"></i></a>'
                        );
                    }
                }
            ],
            order: [[2, 'desc']],
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
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
        $('div.head-label').html('<h5 class="card-title mb-0">فاکتور ها</h5>');
    }

    $('body').on('click', '.item-accpet', function () {

        //تائید تراکنش

        var factor_id = $(this).attr("data-id");
        var data_price = $(this).attr("data-id-price");

        Swal.fire({
            title: 'هشدار',
            text: "مطمئنی میخای فاکتور به مبلغ " + data_price + " را تائید کنی ؟؟",
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



                AjaxPost("/App/BotFactors/Accept?factor_id=" + factor_id).then(res => {

                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status == "success") {

                        dt_basic.ajax.reload(null, false);
                    }


                });

            }
        });



    });


    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});

