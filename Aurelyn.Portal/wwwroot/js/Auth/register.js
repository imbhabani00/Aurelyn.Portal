$(function () {
    var registerForm = $('#registerForm');
    var registerBtn = registerForm.find('button[type="submit"]');

    registerForm.on('submit', function (e) {
        e.preventDefault();

        var firstName = registerForm.find('[name="FirstName"]').val();
        var lastName = registerForm.find('[name="LastName"]').val();
        var email = registerForm.find('[name="Email"]').val();
        var password = registerForm.find('[name="Password"]').val();
        var confirm = registerForm.find('[name="ConfirmPassword"]').val();

        if (!firstName || !firstName.trim()) { showError('First name is required.'); return false; }
        if (!lastName || !lastName.trim()) { showError('Last name is required.'); return false; }
        if (!email || !email.trim()) { showError('Email address is required.'); return false; }
        if (!password) { showError('Password is required.'); return false; }
        if (password !== confirm) { showError('Passwords do not match.'); return false; }

        setButtonLoading(registerBtn, true);

        $.ajax({
            url: '/Account/Register',
            type: 'POST',
            data: new FormData(registerForm[0]),
            contentType: false,
            processData: false,
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            success: function (response) {
                debugger
                if (response.status === true) {
                    showSuccess(response.message || 'Account created.');
                    if (response.returnUrl) {
                        setTimeout(function () {
                            window.location.href = response.returnUrl;
                        }, 500);
                    }
                } else {
                    showError(response.message || 'Registration failed');
                    setButtonLoading(registerBtn, false);
                }
            },
            error: function (xhr) {
                console.log('Register Error:', xhr.status, xhr.responseText);
                setButtonLoading(registerBtn, false);
                showError('Registration failed. Please try again.');
            }
        });
    });
});