$(function () {

    function getAntiForgeryToken(formSelector) {
        return $(formSelector + ' input[name="__RequestVerificationToken"]').val();
    }

    function showAlert(status, message) {
        Swal.fire({
            title: status === 'success' ? 'موفق' : (status === 'warning' ? 'هشدار' : 'خطا'),
            text: message,
            icon: status === 'success' ? 'success' : (status === 'warning' ? 'warning' : 'error')
        });
    }

    $('#btnSaveGeneral').on('click', function () {
        $.ajax({
            url: '/App/Settings/SaveGeneralSettings',
            type: 'POST',
            dataType: 'json',
            data: {
                __RequestVerificationToken: getAntiForgeryToken('#generalSettingsForm'),
                SubAddress: $('#SubAddress').val(),
                BackupSubAddr: $('#BackupSubAddr').val()
            },
            success: function (res) {
                showAlert(res.status, res.message);
            },
            error: function () {
                showAlert('danger', 'خطا در ذخیره تنظیمات');
            }
        });
    });

    $('#btnTestMysql').on('click', function () {
        var serverIp = $('#ServerIP').val();
        var databaseName = $('#DataBaseName').val();
        var username = $('#MysqlUsername').val();
        var password = $('#MysqlPassword').val();

        if (!serverIp || !databaseName || !username) {
            showAlert('warning', 'آیپی، نام دیتابیس و نام کاربری MySQL را وارد کنید');
            return;
        }

        $.ajax({
            url: '/App/Settings/TestMysqlConnection',
            type: 'POST',
            dataType: 'json',
            data: {
                ServerIP: serverIp,
                DataBaseName: databaseName,
                Username: username,
                Password: password
            },
            success: function (res) {
                showAlert(res.status, res.message);
            },
            error: function () {
                showAlert('danger', 'خطا در تست ارتباط');
            }
        });
    });

    $('#btnSaveMysql').on('click', function () {
        $.ajax({
            url: '/App/Settings/SaveMysqlSettings',
            type: 'POST',
            dataType: 'json',
            data: {
                __RequestVerificationToken: getAntiForgeryToken('#mysqlSettingsForm'),
                ServerAddress: $('#ServerAddress').val(),
                ServerIP: $('#ServerIP').val(),
                Username: $('#MysqlUsername').val(),
                Password: $('#MysqlPassword').val(),
                DataBaseName: $('#DataBaseName').val(),
                ApiToken_V2board: $('#ApiToken_V2board').val()
            },
            success: function (res) {
                showAlert(res.status, res.message);
            },
            error: function () {
                showAlert('danger', 'خطا در ذخیره تنظیمات');
            }
        });
    });

    $('#btnSaveBot').on('click', function () {
        var robotId = $('#Robot_ID').val();
        var channelId = $('#Channel_ID').val();

        if (robotId && robotId.indexOf('@@') !== -1) {
            showAlert('warning', 'آیدی ربات را بدون @@ وارد کنید');
            return;
        }
        if (channelId && channelId.indexOf('@@') !== -1) {
            showAlert('warning', 'آیدی کانال را بدون @@ وارد کنید');
            return;
        }

        $.ajax({
            url: '/App/Settings/SaveBotServerSettings',
            type: 'POST',
            dataType: 'json',
            data: {
                __RequestVerificationToken: getAntiForgeryToken('#botSettingsForm'),
                Discount_Percent: $('#Discount_Percent').val() || null,
                AdminTelegramUniqID: $('#AdminTelegramUniqID').val() || null,
                Channel_ID: channelId,
                BotbaseAddress: $('#BotbaseAddress').val(),
                Robot_Token: $('#Robot_Token').val(),
                Robot_ID: robotId,
                BotID: $('#BotID').val() || null
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
