
// datatable (jquery)

let fv;
var dt_basic_table = $('.datatables-links');
var dt_basic;

$(function () {


    // DataTable with buttons
    // --------------------------------------------------------------------

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: '/App/PaymentLinks/GetLinks',
            columns: [
                { data: '' },
                { data: 'Hash' },
                { data: 'Authority' },
                { data: 'Amount' },
                { data: 'Description' },
                { data: 'CreateDate' },
                { data: 'Status' },
                { data: 'PayWebLink' },
                { data: 'PayTelLink' },
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
                    // Hash
                    targets: 1,
                    responsivePriority: 3,
                    render: function (data, type, full, meta) {
                        var $Hash = full['Hash'];
                        return (
                            "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                            "<i class='ti-3d-cube-sphere'></i>" +
                            $Hash +
                            '</span>'
                        );
                    }
                },
                {
                    // Authority
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Authority = full['Authority'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Authority + "</span>";
                        return $row_output;
                    }
                },
                {
                    // amount
                    targets: 3,
                    responsivePriority: 5,
                    render: function (data, type, full, meta) {
                        var $amount = full['Amount'];
                        // Creates full output for row
                        var $row_output = "<span>" + $amount + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Description
                    targets: 4,
                    responsivePriority: 6,
                    render: function (data, type, full, meta) {
                        var $Description = full['Description'];
                        return (
                            "<span data-bs-toggle='tooltip' data-bs-html='true' title='<span>" +
                            "<i class='ti-3d-cube-sphere'></i>" +
                            $Description +
                            '</span>'
                        );
                    }
                },
                {
                    // CreateDate
                    targets: 5,
                    responsivePriority: 5,
                    render: function (data, type, full, meta) {
                        var $CreateDate = full['CreateDate'];
                        // Creates full output for row
                        var $row_output = "<span>" + $CreateDate + "</span>";
                        return $row_output;
                    }
                },
                {
                    // $PayWebLink
                    targets: 6,
                    responsivePriority: 7,
                    render: function (data, type, full, meta) {

                        var $PayWebLink = full['PayWebLink'];
                        var $row_output = "<a href='" + $PayWebLink +"'>" + "Pay" + "</a>";

                        // Creates full output for row
                        return $row_output;
                    }
                },
                {
                    // $PayTelLink
                    targets: 7,
                    responsivePriority: 7,
                    render: function (data, type, full, meta) {

                        var $PayTelLink = full['PayTelLink'];
                        var $row_output = "<a href='" + $PayTelLink + "'>" + "Pay" + "</a>";

                        // Creates full output for row
                        return $row_output;
                    }
                },
                {
                    // Status
                    targets:8,
                    responsivePriority: 8,
                    render: function (data, type, full, meta) {
                        var $Status = full['Status'];

                        var statusObj = {
                            1: { title: 'Paid', class: 'bg-label-success' },
                            0: { title: 'Unpaid', class: 'bg-label-danger' }
                        };

                        var $row_output = "<span class='badge " + statusObj[$Status].class + "'>" + statusObj[$Status].title + "</span>";

                        return $row_output;
                    }
                },
                {
                    // Actions
                    targets: -1,
                    title: 'Opration',
                    orderable: false,
                    searchable: false,
                    render: function (data, type, full, meta) {
                        return (
                            '<a data-bs-toggle="popover" title="Accept" data-id=' + full["Id"] + ' class="btn btn-sm btn-icon item-accpet"><i class="text-primary ti ti-checklist"></i></a>'
                        );
                    }
                }
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
            // Buttons with Dropdown
            order: [[2, 'desc']],
            displayLength: 6,
            lengthMenu: [6, 25, 50, 75, 100],
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
            buttons: [
                {
                    text: '<i class="ti ti-plus me-md-1"></i><span class="d-md-inline-block d-none">افزودن لینک پرداخت</span>',
                    className: 'btn btn-primary waves-effect waves-light',
                    action: function (e, dt, button, config) {
                        $("#modalAddpay").modal("show");
                    }
                }
            ],
        });
    }

    const payform = document.getElementById('payform');

    // Form validation for Add new record
    fv = FormValidation.formValidation(payform, {
        fields: {
            payAmount: {
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

        AjaxFormPost('/App/PaymentLinks/CreateLink', "#payform").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {

                document.getElementById('payform').reset();
                $("#modalAddpay").modal("hide");
                dt_basic.ajax.reload(null, false);

            }
            else {
                eval(res.data);
            }

        });
    });


    $('body').on('click', '.item-accpet', function () {

        //تائید تراکنش

        var pay_id = $(this).attr("data-id");

        Swal.fire({
            title: 'هشدار',
            text: "مطمئنی میخای این پرداخت رو تایید کنی ؟",
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



                AjaxPost("/App/PaymentLinks/Accept?pay_id=" + pay_id).then(res => {

                    BodyUnblockUI();
                    eval(res.data);
                    if (res.status == "success") {

                        dt_basic.ajax.reload(null, false);
                    }


                });

            }
        });



    });
});

