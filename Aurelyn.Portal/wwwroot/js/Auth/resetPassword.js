$(function () {
    var sendCodeForm = $('#sendCodeForm');
    var resetForm = $('#resetForm');
    var resetSub = $('#resetSub');
    var sendBtn = sendCodeForm.find('button[type="submit"]');
    var resetBtn = resetForm.find('button[type="submit"]');
    var defaultSub = "We'll email a 6-digit code to your registered address.";
    var strong = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,72}$/;

    // STEP 1: email -> code sent
    sendCodeForm.on('submit', function (e) {
        e.preventDefault();

        var email = $('#resetEmail').val();
        if (!email || !email.trim()) {
            showError('Email address is required.');
            return false;
        }

        setButtonLoading(sendBtn, true);

        $.ajax({
            url: '/Account/SendResetCode',
            type: 'POST',
            data: new FormData(sendCodeForm[0]), 
            contentType: false,
            processData: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (response) {
                debugger
                setButtonLoading(sendBtn, false);

                if (response.status === true) {
                    showSuccess(response.message || 'Code sent.');

                    $('#resetEmailHidden').val(email.trim());
                    resetSub.html(
                        'Enter the code sent to <strong>' + $('<div>').text(email).html() +
                        '</strong> and choose a new password.'
                    );
                    sendCodeForm.hide();
                    resetForm.show().find('input[name="Code"]').trigger('focus');
                } else {
                    showError(response.message || response.errorMessage || 'Unable to send the code.');
                }
            },
            error: function (xhr) {
                console.log('SendResetCode Error:', xhr.status, xhr.responseText);
                setButtonLoading(sendBtn, false);
                showError('Unable to send the code. Please try again.');
            }
        });
    });

    // STEP 2: code + new password -> reset
    resetForm.on('submit', function (e) {
        e.preventDefault();

        var code = resetForm.find('[name="Code"]').val();
        var newPassword = resetForm.find('[name="NewPassword"]').val();
        var confirm = resetForm.find('[name="ConfirmPassword"]').val();

        if (!code || code.length !== 6) { showError('Enter the 6-digit code.'); return false; }
        if (!newPassword) { showError('New password is required.'); return false; }
        if (!strong.test(newPassword)) {
            showError('Password must have 8-72 characters with uppercase, lowercase, number and special character.');
            return false;
        }
        if (newPassword !== confirm) { showError('Passwords do not match.'); return false; }

        setButtonLoading(resetBtn, true);

        $.ajax({
            url: '/Account/ResetPassword',
            type: 'POST',
            data: new FormData(resetForm[0]),
            contentType: false,
            processData: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (response) {
                debugger
                if (response.status === true) {
                    showSuccess(response.message || 'Password reset successfully.');
                    setTimeout(function () {
                        window.location.href = response.returnUrl || '/Account/Login';
                    }, 800);
                } else {
                    showError(response.message || response.errorMessage || 'Unable to reset the password.');
                    setButtonLoading(resetBtn, false);
                }
            },
            error: function (xhr) {
                console.log('ResetPassword Error:', xhr.status, xhr.responseText);
                setButtonLoading(resetBtn, false);
                showError('Unable to reset the password. Please try again.');
            }
        });
    });

    // "Send again": back to step 1
    $('#useAnotherEmail').on('click', function () {
        resetForm.hide();
        sendCodeForm.show();
        resetSub.text(defaultSub);
    });

    // Code field accepts digits only
    $('input[name="Code"]').on('input', function () {
        this.value = this.value.replace(/\D/g, '');
    });
});