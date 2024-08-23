/**
 * DataTables Basic
 */

'use strict';

let fv,fv1, offCanvasEl;

// datatable (jquery)
$(function () {

    $('[data-bs-toggle="popover"]').tooltip();

    var dt_basic_table = $('.datatables-plan');
    var dt_basic;
    // DataTable with buttons
    // --------------------------------------------------------------------


    if (dt_basic_table.length) {
        dt_basic = dt_basic_table.DataTable({
            ajax: '/App/ServerGroups/_PartialGetAll',
            columns: [
                { data: '' },
                { data: 'ID' },
                { data: 'GroupName' },
                { data: 'Status' },
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
                    // Group ID
                    targets: 1,
                    responsivePriority: 1,
                    render: function (data, type, full, meta) {
                        var $name = full['ID'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Group Name
                    targets: 2,
                    responsivePriority: 2,
                    render: function (data, type, full, meta) {
                        var $name = full['GroupName'];
                        // Creates full output for row
                        var $row_output = "<span>" + $name + "</span>";
                        return $row_output;
                    }
                },
                {
                    // Status
                    targets: 3,
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
                        var statusText = "";
                        var statusIcon = "";
                        var Status = full["Status"];
                        if (Status == "1") {
                            statusText = "غیر فعال کردن";
                            statusIcon = "ti-lock";
                        }
                        else {
                            statusText = "فعال کردن";
                            statusIcon = "ti-lock-off";
                        }


                        return (
                            '<a data-bs-toggle="popover" title="' + statusText + '" class="btn btn-sm btn-icon item-edit change-status" data-id="' + full["id"] + '"><i class="text-primary ti ' + statusIcon +'"></i></a>' +
                            '<button type="button" data-bs-toggle="popover" title="ویرایش" class="btn btn-sm btn-icon item-edit EditGroup" data-id="' + full["ID"] + '" data-bs-target="#modalgroupEdit" data-bs-toggle="modal"><i class="text-primary ti ti-pencil"></i></button>'
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
                searchPlaceholder: 'جستجوی تعرفه',
                loadingRecords: "در حال بارگزاری ..."
            },
            displayLength: 10,
            lengthMenu: [10, 25, 50, 75, 100],
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

    //// Edit Group
    $('body').on('click', '.EditGroup', function () {
        blockUI('#modalgroupEdit .section-block');
        var id = $(this).attr("data-id");
        $("#modalgroupEdit").modal('show');
        AjaxGet('/App/ServerGroups/EditGroup?id=' + id).then(res => {
            console.log(res);
            if (res.status == "success") {
                UnblockUI('#modalgroupEdit .section-block');
                var data = res.data;
                for (var key in data) {
                    if (data.hasOwnProperty(key)) {
                        var input = $('input[name=' + key + ']');

                        if (key == "planGroup") {
                            SelectGroup("#planGroup", data[key]);
                        }
                        else {
                            input.val(data[key]);
                        }


                    }
                }
            }

        });
    });

    //$('body').on('click', '.change-status', function () {

    //    var id = $(this).attr("data-id");
    //    Swal.fire({
    //        title: 'هشدار',
    //        text: "مطمئنی میخای وضعیت تعرفه رو تغییر بدی ؟!",
    //        icon: 'warning',
    //        showCancelButton: true,
    //        confirmButtonText: 'بله',
    //        cancelButtonText: 'بازگشت',
    //        customClass: {
    //            confirmButton: 'btn btn-primary me-3 waves-effect waves-light',
    //            cancelButton: 'btn btn-label-secondary waves-effect waves-light'
    //        },
    //        buttonsStyling: false
    //    }).then(function (result) {
    //        if (result.value) {

    //            $.ajax({
    //                url: "/App/Plan/ChangeStatus?id=" + id,
    //                type: "get",
    //                dataType: "json",
    //                success: function (res) {
    //                    dt_basic.ajax.reload(null, false);
    //                }
    //            })

    //        }
    //    });
    //});



    //// Delete Record
    //$('.datatables-basic tbody').on('click', '.delete-record', function () {
    //    dt_basic.row($(this).parents('tr')).remove().draw();
    //});

    // Filter form control to default size
    // ? setTimeout used for multilingual table initialization
    setTimeout(() => {
        $('.dataTables_filter .form-control').removeClass('form-control-sm');
        $('.dataTables_length .form-select').removeClass('form-select-sm');
    }, 300);


    //$("#updatePlan").click(function () {

    //    Swal.fire({
    //        title: 'مطمئنی ؟',
    //        text: "وضعیت تمامی تعرفه ها تغییر خواهد کرد !!",
    //        icon: 'warning',
    //        showCancelButton: true,
    //        confirmButtonText: 'بله',
    //        cancelButtonText: 'بازگشت',
    //        customClass: {
    //            confirmButton: 'btn btn-primary me-3 waves-effect waves-light',
    //            cancelButton: 'btn btn-label-secondary waves-effect waves-light'
    //        },
    //        buttonsStyling: false
    //    }).then(function (result) {
    //        if (result.value) {
    //            BodyBlockUI();
    //            $.ajax({
    //                url: "/App/Plan/UpdatePlans",
    //                type: "get",
    //                dataType: "json",
    //                success: function (res) {
    //                    BodyUnblockUI();

    //                    eval(res.data);
    //                    if (res.status == "success") {
    //                        dt_basic.ajax.reload(null, false);
    //                    }


    //                }
    //            })

    //        }

    //    });
    //});


    const formAddNewRecord = document.getElementById('groupForm');

    // Form validation for Add new record
    fv = FormValidation.formValidation(formAddNewRecord, {
        fields: {
            groupName: {
                validators: {
                    notEmpty: {
                        message: 'عنوان دسته بندی را وارد کنید'
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
                rowSelector: '.mb-3'
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

    fv.on('core.form.valid', function (e) {
        blockUI('.section-block');
        AjaxFormPost('/App/ServerGroups/CreateOrEdit', "#groupForm").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {

                dt_basic.ajax.reload(null, false);
                document.getElementById('groupForm').reset();
                $("#modalgroup").modal("hide");
            }

        });
    });




    $("#updategroup").click(function () {

        Swal.fire({
            title: 'مطمئنی ؟',
            text: "وضعیت تمامی دسته بندی ها تغییر خواهد کرد !!",
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
                $.ajax({
                    url: "/App/ServerGroups/UpdateGroups",
                    type: "get",
                    dataType: "json",
                    success: function (res) {
                        BodyUnblockUI();

                        eval(res.data);
                        if (res.status == "success") {
                            dt_basic.ajax.reload(null, false);
                        }


                    }
                })

            }

        });
    });



    const formEditRecord = document.getElementById('groupEditForm');

    // Form validation for Add new record
    fv1 = FormValidation.formValidation(formEditRecord, {
        fields: {
            groupName: {
                validators: {
                    notEmpty: {
                        message: 'عنوان دسته بندی را وارد کنید'
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
                rowSelector: '.mb-3'
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

    fv1.on('core.form.valid', function (e) {
        blockUI('.section-block');
        AjaxFormPost('/App/ServerGroups/CreateOrEdit', "#groupEditForm").then(res => {
            UnblockUI('.section-block');
            eval(res.data);
            if (res.status == "success") {

                dt_basic.ajax.reload(null, false);
                document.getElementById('groupEditForm').reset();
                $("#modalgroupEdit").modal("hide");
            }

        });
    });
});

//function SelectGroup(selectId, Ids) {
//    console.log("select shod");
//    $(selectId).val(Ids).trigger('change');
//}

//function showOffcanvas() {
//    var offcanvasElement = document.getElementById('Add-Or-EditPlan');
//    var offcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasElement);
//    offcanvas.show();
//}
