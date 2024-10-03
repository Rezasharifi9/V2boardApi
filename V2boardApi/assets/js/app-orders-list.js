
// datatable (jquery)
$(function () {
    var dt_basic_table = $('.datatables-plan');
    var dt_basic;

    // DataTable with buttons
    // --------------------------------------------------------------------

    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: '/App/Orders/GetOrders',
            columns: [
                { data: '' },
                { data: 'SubName' },
                { data: 'UserCreator' },
                { data: 'Plan' },
                { data: 'CreateDate' },
                { data: 'ActiveDate' },
                { data: 'Status' },
                { data: 'Price' },
                { data: '' },
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
                        var $name = full['SubName'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // $UserCreator
                    targets: 2,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $UserCreator = full['UserCreator'];
                        // Creates full output for row
                        var $row_output = "<span>" + $UserCreator + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Plan
                    targets: 3,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Plan = full['Plan'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Plan + "</span>";
                        return $row_output;
                    }
                },
                {
                    // CreateDate
                    targets: 4,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $CreateDate = full['CreateDate'];

                        var $row_output = "<span>" + $CreateDate + "</span>";

                        return $row_output;
                    }
                },
                {
                    // ActiveDate
                    targets: 5,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $ActiveDate = full['ActiveDate'];

                        var $row_output = "<span>" + $ActiveDate + "</span>";

                        return $row_output;
                    }
                },
                {
                    // CreateDate
                    targets:6,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Status = full['Status'];

                        var statusObj = {
                            0: { title: 'در صف رزرو', class: 'bg-label-warning' },
                            1: { title: 'انجام شده', class: 'bg-label-success' },
                        };

                        var $row_output = "<span class='badge " + statusObj[$Status].class + "'>" + statusObj[$Status].title + "</span>";

                        return $row_output;
                    }
                },
                {
                    // Price
                    targets: 7,
                    responsivePriority: 4,
                    render: function (data, type, full, meta) {
                        var $Price = full['Price'];
                        // Creates full output for row
                        var $row_output = "<span>" + $Price + ' ءتء' + "</span>";
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

    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);
});

