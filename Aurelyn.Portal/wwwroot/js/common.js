$(function () {
    if (typeof toastr !== "undefined") {
        toastr.options = {
            closeButton: true,
            progressBar: true,
            positionClass: "toast-top-right",
            timeOut: 3500,
            extendedTimeOut: 1000,
            showEasing: "swing",
            hideEasing: "linear",
            showMethod: "fadeIn",
            hideMethod: "fadeOut"
        };
    }

    $(document).on("click", "#sidebarToggle", function () {
        $("#sidebar").addClass("open");
        $("#sidebarOverlay").addClass("open");
        $("body").css("overflow", "hidden");
    });
    $(document).on("click", "#sidebarClose, #sidebarOverlay", closeSidebar);
    $(document).on("click", ".au-nav-link", function () {
        if (window.matchMedia("(max-width: 800px)").matches) {
            closeSidebar();
        }
    });

    $(document).on("click", "#userDropBtn", function (e) {
        e.stopPropagation();
        var $drop = $("#userDrop");
        $drop.toggleClass("open");
        $(this).attr(
            "aria-expanded",
            $drop.hasClass("open") ? "true" : "false"
        );
    });

    $(document).on("click", function (e) {
        if (!$(e.target).closest("#userDropWrap").length) {
            $("#userDrop").removeClass("open");
            $("#userDropBtn").attr("aria-expanded", "false");
        }
    });
    $(document).on("click", "#modalBackdrop, #modalClose", closePopup);
    $(document).on("keydown", function (e) {

        if (e.key === "Escape") {
            closePopup();
            closeSidebar();
        }
    });

    $(document).on("click", "[data-toggle-password]", function () {
        var inputId = $(this).attr("data-toggle-password");
        if (inputId) {
            togglePassword(inputId, this);
        }
    });
});

function closeSidebar() {
    $("#sidebar").removeClass("open");
    $("#sidebarOverlay").removeClass("open");
    $("body").css("overflow", "");
}

function showSuccess(message) {
    if (typeof toastr !== "undefined") {
        toastr.success(message || "Operation successful");
    }
}

function showError(message) {
    if (typeof toastr !== "undefined") {
        toastr.error(message || "Something went wrong");
    }
}

function showWarning(message) {
    if (typeof toastr !== "undefined") {
        toastr.warning(message || "Warning");
    }
}
function showInfo(message) {
    if (typeof toastr !== "undefined") {
        toastr.info(message || "Information");
    }
}

function loadPopup(url, title, size) {
    var $backdrop = $("#modalBackdrop");
    var $container = $("#modalContainer");
    var $box = $("#modalBox");
    var $body = $("#modalBody");

    $box.removeClass("modal-sm modal-md modal-lg modal-xl");
    if (["sm", "md", "lg", "xl"].indexOf(size) !== -1) {
        $box.addClass("modal-" + size);
    }

    $("#modalTitle").text(title || "");
    $body.html('<div class="au-spinner" role="status" aria-label="Loading"></div>');
    $("#modalClose").prop("disabled", false);
    $backdrop.addClass("open");
    $container.addClass("open");

    $.ajax({
        url: url,
        type: "GET",
        cache: false,
        success: function (html) {
            $body.html(html);
            initFormControls($body);
        },
        error: function () {
            $body.html(
                '<p class="text-danger mb-0">' +
                'Unable to load this content. Please try again.' +
                '</p>'
            );
        }
    });
}
function closePopup() {
    if ($("#modalClose").prop("disabled")) return;
    $("#modalBackdrop, #modalContainer").removeClass("open");
    $("#modalBody").empty();
}

function loadDropdown(url, controlId, prefillValue, defaultSelect) {
    var $select = $("#" + controlId);
    if (!$select.length) return;
    $select.prop("disabled", true);
    $select.html('<option value="">Loading...</option>');
    var accessToken = getAccessToken();
    $.ajax({
        url: url,
        type: "GET",
        dataType: "json",
        headers: accessToken
            ? { Authorization: "Bearer " + accessToken }
            : {},
        success: function (response) {
            if (typeof response === "string") {

                try {
                    response = JSON.parse(response);
                }
                catch (e) {
                    response = [];
                }
            }

            var data = [];
            if (Array.isArray(response)) {
                data = response;
            }
            else if (response && Array.isArray(response.lookupData)) {
                data = response.lookupData;
            }
            else if (response && Array.isArray(response.response)) {
                data = response.response;
            }
            else if (response && Array.isArray(response.data)) {

                data = response.data;

            }
            $select.empty();
            $select.append(
                $("<option>", {
                    value: "",
                    text: defaultSelect || "— Select —"
                })
            );

            $.each(data, function (_, item) {
                var id =
                    item.id ??
                    item.Id ??
                    item.value ??
                    item.Value ??
                    "";
                var name =
                    item.name ??
                    item.Name ??
                    item.text ??
                    item.Text ??
                    "";
                var $option = $("<option>", {
                    value: id,
                    text: name
                });
                $option.data(
                    "code",
                    item.code ?? item.Code ?? ""
                );
                $option.data("item", item);
                if (
                    prefillValue !== null &&
                    prefillValue !== undefined &&
                    String(id) === String(prefillValue)
                ) {
                    $option.prop("selected", true);
                }
                $select.append($option);
            });
            $select.prop("disabled", false);
            $select.trigger("dropdown:loaded");
        },
        error: function () {
            $select.html(
                '<option value="">Failed to load</option>'
            );
            $select.prop("disabled", false);
            showError("Unable to load dropdown values.");
        }
    });
}

function getDropdownCode(controlId) {

    return $("#" + controlId + " option:selected").data("code") || "";

}

function getDropdownItem(controlId) {

    return $("#" + controlId + " option:selected").data("item") || null;

}
function getAccessToken() {
    return $('meta[name="access-token"]').attr("content") || "";
}

function getAntiForgeryToken($form) {
    var token = $form && $form.length
        ? $form.find('input[name="__RequestVerificationToken"]').val()
        : $('input[name="__RequestVerificationToken"]').first().val();
    return token ||
        $('meta[name="request-verification-token"]').attr("content") ||
        "";

}

function ajaxPost(url, data, successFn, errorFn) {
    var $form = data && data.jquery ? data : null;
    var token = getAntiForgeryToken($form);
    $.ajax({
        url: url,
        type: "POST",
        data: data,
        headers: token
            ? { RequestVerificationToken: token }
            : {},
        success: function (response) {
            if (typeof successFn === "function") {
                successFn(response);
            }
        },
        error: function (xhr) {
            if (typeof errorFn === "function") {
                errorFn(xhr);
            }
            else {
                showError("Request failed. Please try again.");
            }
        }
    });
}

function ajaxPostJson(url, jsonData, successFn, errorFn) {
    var token = getAntiForgeryToken();
    $.ajax({
        url: url,
        type: "POST",
        contentType: "application/json; charset=utf-8",
        data: JSON.stringify(jsonData),
        headers: token
            ? { RequestVerificationToken: token }
            : {},
        success: function (response) {
            if (typeof successFn === "function") {
                successFn(response);
            }
        },
        error: function (xhr) {
            if (typeof errorFn === "function") {
                errorFn(xhr);
            }
            else {
                showError("Request failed. Please try again.");
            }
        }
    });
}

function showConfirm(message, onConfirm, confirmText) {
    var safeMessage = $("<div>")
        .text(message || "Are you sure you want to continue?")
        .html();
    $("#modalBox")
        .removeClass("modal-lg modal-xl")
        .addClass("modal-sm");
    $("#modalTitle").text("Please confirm");
    $("#modalBody").html(
        '<p class="mb-4">' + safeMessage + '</p>' +
        '<div class="d-flex justify-content-end gap-2">' +
        '<button type="button" class="btn btn-light" id="confirmCancelBtn">' +
        'Cancel</button>' +
        '<button type="button" class="btn btn-primary" id="confirmOkBtn">' +
        $("<div>").text(confirmText || "Confirm").html() +
        '</button>' +
        '</div>'
    );
    $("#modalBackdrop, #modalContainer").addClass("open");
    $("#confirmCancelBtn").on("click", closePopup);
    $("#confirmOkBtn").on("click", function () {
        closePopup();
        if (typeof onConfirm === "function") {
            onConfirm();
        }
    });
}

function reloadPartial(url, containerId) {
    var $container = $("#" + containerId);
    if (!$container.length) return;
    $container.html(
        '<div class="au-spinner" role="status" aria-label="Loading"></div>'
    );
    $.ajax({
        url: url,
        type: "GET",
        cache: false,
        success: function (html) {
            $container.html(html);
            initFormControls($container);
        },
        error: function () {
            $container.html(
                '<p class="text-danger">' +
                'Unable to reload this section.' +
                '</p>'
            );
        }
    });
}

function serializeForm(formId) {
    var result = {};
    $("#" + formId).serializeArray().forEach(function (item) {
        result[item.name] = item.value;
    });
    return result;
}

function resetForm(formId) {
    var $form = $("#" + formId);
    if (!$form.length) return;
    $form[0].reset();
    $form.find(".is-invalid").removeClass("is-invalid");
    $form.find(".field-error, .invalid-feedback").empty();
}

function initFormControls($context) {

    if (!$context || !$context.length) return;
    if (window.bootstrap && bootstrap.Tooltip) {

        $context.find('[data-bs-toggle="tooltip"]').each(function () {
            bootstrap.Tooltip.getOrCreateInstance(this);
        });
    }
}

function setButtonLoading($btn, loading, loadingText) {

    if (!$btn || !$btn.length) return;


    if (loading) {

        if (!$btn.data("original-html")) {
            $btn.data("original-html", $btn.html());
        }

        $btn.prop("disabled", true).addClass("is-loading");
        if (loadingText) {
            $btn.text(loadingText);
        }
        else {
            $btn.find(".btn-spinner").removeClass("d-none hidden");
        }
    }
    else {
        $btn.prop("disabled", false).removeClass("is-loading");
        var original = $btn.data("original-html");
        if (original) {
            $btn.html(original);
        }
        else {
            $btn.find(".btn-spinner").addClass("d-none hidden");
        }
    }
}

function handleApiResponse(response, successMessage) {
    if (!response) {
        showError("No response from server.");
        return null;
    }
    if (response.status !== true) {
        showError(response.message || "Operation failed.");
        return null;
    }
    if (successMessage) {
        showSuccess(successMessage);
    }
    return response.response;
}

function formatDate(dateStr) {
    if (!dateStr) return "—";
    var date = new Date(dateStr);
    if (Number.isNaN(date.getTime())) {
        return dateStr;
    }
    return date.toLocaleDateString("en-IN", {
        day: "2-digit",
        month: "short",
        year: "numeric"
    });
}

function formatCurrency(amount, currency) {
    if (
        amount === null ||
        amount === undefined ||
        amount === ""
    ) {
        return "—";
    }
    return new Intl.NumberFormat("en-IN", {
        style: "currency",
        currency: currency || "INR",
        minimumFractionDigits: 2
    }).format(Number(amount));
}

function formatNumber(value) {
    if (
        value === null ||
        value === undefined ||
        value === ""
    ) {
        return "—";
    }
    return new Intl.NumberFormat("en-IN").format(Number(value));
}

function togglePassword(inputId, button) {
    var $input = $("#" + inputId);
    if (!$input.length) return;
    var show = $input.attr("type") === "password";
    $input.attr("type", show ? "text" : "password");
    $(button).attr(
        "aria-label",
        show ? "Hide password" : "Show password"
    );
    $(button).attr(
        "aria-pressed",
        show ? "true" : "false"
    );
    $(button).toggleClass("password-visible", show);
}

function updateStrength(value) {
    value = value || "";
    var score = 0;
    if (value.length >= 8) score++;
    if (/[A-Z]/.test(value)) score++;
    if (/[0-9]/.test(value)) score++;
    if (/[^A-Za-z0-9]/.test(value)) score++;
    var colors = [
        "",
        "#e45b70",
        "#e9a23b",
        "#35a58a",
        "#625ee8"
    ];
    var labels = [
        "",
        "Weak",
        "Fair",
        "Good",
        "Strong"
    ];
    for (var i = 1; i <= 4; i++) {

        $("#s" + i).css(
            "background",
            i <= score ? colors[score] : "#e3e5f5"
        );

    }
    $("#strengthLabel")
        .text(value.length ? labels[score] : "")
        .css("color", colors[score] || "");
}

// function googleSignIn() {

//     window.location.href =
//         "/Account/GoogleLogin?returnUrl=" +
//         encodeURIComponent(window.location.pathname);

// }