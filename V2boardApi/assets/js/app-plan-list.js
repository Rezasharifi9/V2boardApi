/**
 * DataTables Basic
 */

'use strict';

let fv, offCanvasEl;

// datatable (jquery)
$(function () {
    var dt_basic_table = $('.datatables-plan');
    var dt_basic;
    var select2 = $('#planGroup');
    // DataTable with buttons
    // --------------------------------------------------------------------


    if (select2.length) {
        var $this = select2;
        $this.wrap('<div class="position-relative"></div>').select2({
            placeholder: 'انتخاب گروه مجوز',
            dropdownParent: $this.parent(),
            allowClear: false
        });

        Groups("#planGroup");
    }

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: '/App/Plan/_PartialGetAllPlans',
            columns: [
                { data: 'id' },
                { data: 'PlanName' },
                { data: 'DayesCount' },
                { data: 'Traffic' },
                { data: 'Price' },
                { data: 'Status' },
                { data: '' }
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
                    // Plan Name
                    targets: 1,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $name = full['PlanName'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Dayes Count
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $DayesCount = full['DayesCount'];
                        // Creates full output for row
                        var $row_output = "<span>" + $DayesCount + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Traffic
                    targets: 3,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Traffic = full['Traffic'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Traffic + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Price
                    targets: 4,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Price = full['Price'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Price + "</span>";
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
                            '<div class="d-inline-block">' +
                            '<a href="javascript:;" class="btn btn-sm btn-icon dropdown-toggle hide-arrow" data-bs-toggle="dropdown"><i class="text-primary ti ti-dots-vertical"></i></a>' +
                            '<ul class="dropdown-menu dropdown-menu-end m-0">' +
                            '<li><a href="javascript:;" class="dropdown-item">جزئیات</a></li>' +
                            '<li><a href="javascript:;" class="dropdown-item">بایگانی</a></li>' +
                            '<div class="dropdown-divider"></div>' +
                            '<li><a href="javascript:;" class="dropdown-item text-danger delete-record">حذف</a></li>' +
                            '</ul>' +
                            '</div>' +
                            '<a href="javascript:;" class="btn btn-sm btn-icon item-edit"><i class="text-primary ti ti-pencil"></i></a>'
                        );
                    }
                }
            ],
            order: [[2, 'desc']],
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



    // Delete Record
    $('.datatables-basic tbody').on('click', '.delete-record', function () {
        dt_basic.row($(this).parents('tr')).remove().draw();
    });

    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);


    function Groups(selectId) {
        var $select = $(selectId);
        $.ajax({
            url: "/App/Plan/GetSelectGroups",
            type: "get",
            dataType: "json",
            async: true,
            success: function (res) {
                console.log(res);
                // پاک کردن گزینه‌های قبلی
                $select.empty();

                // افزودن گزینه‌های جدید
                $.each(res.data, function (index, item) {
                    var newOption = new Option(item.Name, item.id, false, false);
                    $select.append(newOption);
                });
            },
            error: function (xhr, status, error) {
                console.error("An error occurred: " + status + " " + error);
            }
        });

    }

    const formAddNewRecord = document.getElementById('planForm');

    // Form validation for Add new record
    fv = FormValidation.formValidation(formAddNewRecord, {
        fields: {
            planName: {
                validators: {
                    notEmpty: {
                        message: 'نامه تعرفه الزامی است'
                    }
                }
            },
            planTraffic: {
                validators: {
                    notEmpty: {
                        message: 'ترافیک تعرفه الزامی است'
                    }
                }
            },
            planPrice: {
                validators: {
                    notEmpty: {
                        message: 'قیمت تعرفه الزامی است'
                    }
                }
            },
            planGroup: {
                validators: {
                    notEmpty: {
                        message: 'گروه مجوز الزامی است'
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
                rowSelector: '.col-sm-12'
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

    fv.on('core.form.invalid', function (e) {

        if ($("#planGroup").val().length == 0) {
            $("#planGroupMessage").removeClass("d-none");
            $("#planGroup").addClass("is-invalid");
        }
    });

    fv.on('core.form.valid', function (e) {


        if ($("#planGroup").val().length != 0) {
            $("#planGroupMessage").addClass("d-none");
            $("#planGroup").removeClass("is-invalid");
        }
        else {
            $("#planGroupMessage").removeClass("d-none");
            $("#planGroup").addClass("is-invalid");
            return;
        }

        blockUI('.section-block');
        AjaxFormPost('/App/Plan/CreateOrEdit', "#planForm").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {
                
                dt_basic.ajax.reload(null, false);
                // بستن offcanvas پس از موفقیت آمیز بودن ارسال فرم
                var offcanvasElement = document.getElementById('Add-Or-EditPlan');
                var offcanvas = bootstrap.Offcanvas.getInstance(offcanvasElement);
                offcanvas.hide();
                document.getElementById('planForm').reset();
            }

        });
    });
});
