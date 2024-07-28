/**
 *  Pages Authentication
 */

'use strict';
const formAuthentication = document.querySelector('#formAuthentication');

document.addEventListener('DOMContentLoaded', function (e) {
  (function () {
    // Form validation for Add new record
    if (formAuthentication) {
      const fv = FormValidation.formValidation(formAuthentication, {
        fields: {
          userUsername: {
            validators: {
              notEmpty: {
                message: 'لطفا نام کاربری را وارد کنید'
              }
            }
          },
          userPassword: {
            validators: {
              notEmpty: {
                message: 'رمز عبور را وارد کنید'
              }
            }
          }
        },
        plugins: {
          trigger: new FormValidation.plugins.Trigger(),
          bootstrap5: new FormValidation.plugins.Bootstrap5({
            eleValidClass: '',
            rowSelector: '.mb-3'
          }),
          submitButton: new FormValidation.plugins.SubmitButton(),
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

            var username = $("input[name='userUsername']").val();
            var password = $("input[name='userPassword']").val();
            var remember = $("input[name='userRemember']").val();

            BodyBlockUI();
            $.ajax({
                url: "/App/Admin/Login",
                type: "post",
                dataType: "json",
                data: { userUsername: username, userPassword: password, userRemember: remember },
                success: function (res) {
                    BodyUnblockUI();
                    if (res.status == "success") {
                        location.replace(res.redirectURL);
                    }
                    else {
                        eval(res.data);
                    }
                }
            });

        });
    }

    //  Two Steps Verification
    const numeralMask = document.querySelectorAll('.numeral-mask');

    // Verification masking
    if (numeralMask.length) {
      numeralMask.forEach(e => {
        new Cleave(e, {
          numeral: true
        });
      });
    }
  })();

});

$("#userRemember").change(function () {

    if ($(this).is(":checked")) {
        $(this).val(true);
    }
    else {
        $(this).val(false);
    }
});
         