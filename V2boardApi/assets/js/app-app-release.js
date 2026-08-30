$(function () {

    function getAntiForgeryToken() {
        return $('#appReleaseForm input[name="__RequestVerificationToken"]').val();
    }

    function showAlert(status, message) {
        Swal.fire({
            title: status === 'success' ? 'موفق' : (status === 'warning' ? 'هشدار' : 'خطا'),
            text: message,
            icon: status === 'success' ? 'success' : (status === 'warning' ? 'warning' : 'error')
        });
    }

    function addChangelogRow(value) {
        var $row = $('<div class="input-group mb-2 changelog-row"></div>');
        $row.append('<span class="input-group-text"><i class="ti ti-list"></i></span>');
        $row.append($('<input type="text" class="form-control changelog-item" placeholder="مثلاً رفع باگ اتصال" />').val(value || ''));
        $row.append('<button type="button" class="btn btn-label-danger btn-changelog-remove" title="حذف"><i class="ti ti-trash"></i></button>');
        $('#changelogList').append($row);
    }

    function collectChangelogItems() {
        var items = [];
        $('#changelogList .changelog-item').each(function () {
            var text = ($(this).val() || '').trim();
            if (text)
                items.push(text);
        });
        return items;
    }

    $(document).on('click', '.btn-changelog-remove', function () {
        var $list = $('#changelogList');
        if ($list.find('.changelog-row').length <= 1) {
            $list.find('.changelog-item').val('');
            return;
        }
        $(this).closest('.changelog-row').remove();
    });

    $('#btnAddChangelog').on('click', function () {
        addChangelogRow('');
        $('#changelogList .changelog-item').last().focus();
    });

    $('#btnSaveAppRelease').on('click', function () {
        $.ajax({
            url: '/App/Settings/SaveAppRelease',
            type: 'POST',
            dataType: 'json',
            traditional: true,
            data: {
                __RequestVerificationToken: getAntiForgeryToken(),
                DownloadUrl: $('#DownloadUrl').val(),
                Version: $('#Version').val(),
                VersionCode: $('#VersionCode').val() || null,
                Changelog: collectChangelogItems(),
                ForceInstall: $('#ForceInstall').is(':checked')
            },
            success: function (res) {
                showAlert(res.status, res.message);
            },
            error: function () {
                showAlert('danger', 'خطا در ذخیره تنظیمات');
            }
        });
    });
});
