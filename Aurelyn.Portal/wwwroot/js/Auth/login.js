$(function () {
    var loginForm = $('#loginForm');
    var loginBtn = $('#loginBtn');

    loginForm.on('submit', function (e) {
        e.preventDefault();
        var email = loginForm.find('[name="Email"]').val();
        var password = loginForm.find('[name="Password"]').val();
        if (!email || !email.trim()) {
            showError('Email address is required.');
            return false;
        }

        if (!password || !password.trim()) {
            showError('Password is required.');
            return false;
        }

        setButtonLoading(loginBtn, true);

        var formData = new FormData(loginForm[0]);

        $.ajax({
            url: '/Account/Login',
            type: 'POST',
            data: formData,
            contentType: false,
            processData: false,
            headers: {
                'X-Requested-With': 'XMLHttpRequest'
            },
            success: function (response) {
                if (response.status === true) {

                    showSuccess(response.message || 'OTP sent successfully');

                    if (response.returnUrl) {
                        setTimeout(function () {
                            window.location.href = response.returnUrl;
                        }, 500);
                    }

                } else {

                    showError(response.message || 'Login failed');

                    setButtonLoading(loginBtn, false);
                }
            },

            error: function (xhr) {

                console.log('Login Error:', xhr.status, xhr.responseText);

                setButtonLoading(loginBtn, false);

                showError('Login failed. Please try again.');
            }
        });
    });

});
