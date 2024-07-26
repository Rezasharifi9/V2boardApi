/**
 * DataTables Basic
 */

'use strict';

let fv, offCanvasEl;
document.addEventListener('DOMContentLoaded', function (e) {
    (function () {
        const formAddNewRecord = document.getElementById('form-add-new-record');

        setTimeout(() => {
            const newRecord = document.querySelector('.create-new'),
                offCanvasElement = document.querySelector('#add-new-record');

            // To open offCanvas, to add new record
            if (newRecord) {
                newRecord.addEventListener('click', function () {
                    offCanvasEl = new bootstrap.Offcanvas(offCanvasElement);
                    // Empty fields on offCanvas open
                    (offCanvasElement.querySelector('.dt-full-name').value = ''),
                        (offCanvasElement.querySelector('.dt-post').value = ''),
                        (offCanvasElement.querySelector('.dt-email').value = ''),
                        (offCanvasElement.querySelector('.dt-date').value = ''),
                        (offCanvasElement.querySelector('.dt-salary').value = '');
                    // Open offCanvas with form
                    offCanvasEl.show();
                });
            }
        }, 200);

        // Form validation for Add new record
        fv = FormValidation.formValidation(formAddNewRecord, {
            fields: {
                basicFullname: {
                    validators: {
                        notEmpty: {
                            message: 'عنوان الزامی است'
                        }
                    }
                },
                basicPost: {
                    validators: {
                        notEmpty: {
                            message: 'فیلد سمت الزامی است'
                        }
                    }
                },
                basicEmail: {
                    validators: {
                        notEmpty: {
                            message: 'ایمیل الزامی است'
                        },
                        emailAddress: {
                            message: 'فرمت ایمیل نادرست است'
                        }
                    }
                },
                basicDate: {
                    validators: {
                        notEmpty: {
                            message: 'تاریخ عضویت الزامی است'
                        },
                        date: {
                            format: 'MM/DD/YYYY',
                            message: 'فرمت تاریخ نادرست است'
                        }
                    }
                },
                basicSalary: {
                    validators: {
                        notEmpty: {
                            message: 'حقوق پایه الزامی است'
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

        // FlatPickr Initialization & Validation
        const flatpickrDate = document.querySelector('[name="basicDate"]');

        if (flatpickrDate) {
            flatpickrDate.flatpickr({
                disableMobile: "true",
                enableTime: false,
                // See https://flatpickr.js.org/formatting/
                dateFormat: 'Y/m/d',
                locale: 'fa',
                // After selecting a date, we need to revalidate the field
                onChange: function () {
                    fv.revalidateField('basicDate');
                }
            });
        }
    })();
});

// datatable (jquery)
$(function () {
    var dt_basic_table = $('.datatables-plan');
    var dt_basic;

    // DataTable with buttons
    // --------------------------------------------------------------------

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
            dom: '<"card-header flex-column flex-md-row"<"head-label text-center"><"dt-action-buttons text-end pt-3 pt-md-0"B>><"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6 d-flex justify-content-center justify-content-md-end"f>>t<"row"<"col-sm-12 col-md-6"i><"col-sm-12 col-md-6"p>>',
            displayLength: 7,
            lengthMenu: [7, 10, 25, 50, 75, 100],
            buttons: [
                {
                    extend: 'collection',
                    className: 'btn btn-label-primary dropdown-toggle me-2 waves-effect waves-light',
                    text: '<i class="ti ti-file-export me-sm-1"></i> <span class="d-none d-sm-inline-block">گرفتن خروجی</span>',
                    buttons: [
                        {
                            extend: 'print',
                            text: '<i class="ti ti-printer me-1"></i>چاپ',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [3, 4, 5, 6, 7],
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
                            },
                            customize: function (win) {
                                //customize print view for dark
                                $(win.document.body)
                                    .css('color', config.colors.headingColor)
                                    .css('border-color', config.colors.borderColor)
                                    .css('background-color', config.colors.bodyBg);
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
                            text: '<i class="ti ti-file-text me-1"></i>Csv',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [3, 4, 5, 6, 7],
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
                            text: '<i class="ti ti-file-spreadsheet me-1"></i>Excel',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [3, 4, 5, 6, 7],
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
                            text: '<i class="ti ti-file-description me-1"></i>Pdf',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [3, 4, 5, 6, 7],
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
                            text: '<i class="ti ti-copy me-1"></i>کپی',
                            className: 'dropdown-item',
                            exportOptions: {
                                columns: [3, 4, 5, 6, 7],
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
                    text: '<i class="ti ti-plus me-sm-1"></i> <span class="d-none d-sm-inline-block">افزودن رکورد</span>',
                    className: 'create-new btn btn-primary waves-effect waves-light'
                }
            ],
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

    // Add New record
    // ? Remove/Update this code as per your requirements
    var count = 101;
    // On form submit, if form is valid
    fv.on('core.form.valid', function () {
        var $new_name = $('.add-new-record .dt-full-name').val(),
            $new_post = $('.add-new-record .dt-post').val(),
            $new_email = $('.add-new-record .dt-email').val(),
            $new_date = $('.add-new-record .dt-date').val(),
            $new_salary = $('.add-new-record .dt-salary').val();

        if ($new_name != '') {
            dt_basic.row
                .add({
                    id: count,
                    full_name: $new_name,
                    post: $new_post,
                    email: $new_email,
                    start_date: $new_date,
                    salary: $new_salary + 'ءتء',
                    status: 5
                })
                .draw();
            count++;

            // Hide offcanvas using javascript method
            offCanvasEl.hide();
        }
    });

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
});

