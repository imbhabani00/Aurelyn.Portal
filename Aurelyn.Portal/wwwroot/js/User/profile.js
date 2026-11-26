(function initProfile() {
    const form = document.getElementById("pfForm");
    const save = document.getElementById("pfSave");
    if (!form || !save) return;

    const $id = id => document.getElementById(id);

    const allIds = [
        "firstName", "middleName", "lastName", "email", "phoneNumber",
        "addressLine1", "addressLine2", "city", "region", "zipCode", "country"
    ];
    const optional = ["middleName", "addressLine2"];

    const ids = allIds.filter(id => $id(id));
    const required = ids.filter(id => !optional.includes(id));

    const rules = {
        email: [/^[^\s@]+@[^\s@]+\.[^\s@]+$/, "Please enter a valid email address."],
        phoneNumber: [/^[0-9+\-\s()]{7,20}$/, "Please enter a valid phone number."],
        zipCode: [/^.{3,}$/, "Please enter a valid postal code."]
    };

    const getValues = () =>
        Object.fromEntries(ids.map(id => [id, $id(id).value.trim()]));

    let initial = getValues();
    let saving = false;

    const labelOf = id =>
        (document.querySelector(`label[for="${id}"]`)?.textContent ?? id)
            .replace("*", "")
            .trim();

    function renderSave() {
        const values = getValues();
        const dirty = ids.some(id => values[id] !== initial[id]);
        save.disabled = saving || !dirty;
    }

    // Close (x) unlocks only when the SAVED profile is complete
    function renderClose() {
        const closeBtn = document.getElementById("modalClose");
        if (!closeBtn) return;
        closeBtn.disabled = !required.every(id => initial[id]);
    }

    function setError(id, message = "") {
        const field = $id(id).closest(".pf-field");
        if (!field) return;
        field.classList.toggle("invalid", !!message);
        const err = field.querySelector(".pf-err");
        if (err) err.textContent = message;
    }

    function validate() {
        const values = getValues();
        let valid = true;

        ids.forEach(id => {
            let message = "";

            if (required.includes(id) && !values[id]) {
                message = "This field is required.";
            } else if (values[id] && rules[id] && !rules[id][0].test(values[id])) {
                message = rules[id][1];
            }

            setError(id, message);
            if (message) valid = false;
        });

        return valid;
    }

    function updateProgress() {
        renderSave();
        renderClose();

        const values = getValues();
        const missing = required.filter(id => !values[id]);
        const pct = Math.round(((required.length - missing.length) / required.length) * 100);

        const pctEl = $id("pfPct");
        const fillEl = $id("pfFill");
        const checklist = $id("pfChecklist");

        if (pctEl) pctEl.textContent = pct + "%";
        if (fillEl) fillEl.style.width = pct + "%";

        if (checklist) {
            checklist.innerHTML = "";

            missing.forEach(id => {
                const btn = document.createElement("button");
                btn.type = "button";
                btn.className = "pf-pill";
                btn.textContent = "Add " + labelOf(id);
                btn.onclick = () => {
                    $id(id).focus();
                    $id(id).scrollIntoView({ behavior: "smooth", block: "center" });
                };
                checklist.appendChild(btn);
            });
        }
    }

    function updateHeader() {
        const { firstName = "", lastName = "" } = getValues();
        const nameEl = $id("pfFullName");
        const initialsEl = $id("pfInitials");

        if (nameEl) nameEl.textContent = `${firstName} ${lastName}`.trim();
        if (initialsEl) initialsEl.textContent = (firstName.charAt(0) + lastName.charAt(0)).toUpperCase();
    }

    ids.forEach(id => {
        const onChange = () => {
            setError(id);
            updateProgress();
        };
        $id(id).addEventListener("input", onChange);
        $id(id).addEventListener("change", onChange);
    });

    // ===== AJAX SAVE =====
    form.addEventListener("submit", function (e) {
        e.preventDefault();

        if (!validate()) {
            showError("Please correct the highlighted fields.");
            return;
        }

        const { region, ...rest } = getValues();
        const data = {
            ...rest,
            state: region,
            __RequestVerificationToken: $('input[name="__RequestVerificationToken"]', form).val()
        };

        saving = true;
        save.disabled = true;
        save.textContent = "Saving...";

        $.ajax({
            url: "/User/UpdateProfile",
            type: "POST",
            data: data,
            dataType: "json",
            headers: {
                "X-Requested-With": "XMLHttpRequest"
            },
            success: function (result) {
                const message = result.message ?? result.Message;
                const ok = result.status || result.Status || result.success || result.Success;

                if (!ok) {
                    showError(message || "Failed to update profile.");
                    return;
                }

                initial = getValues();
                updateHeader();
                showSuccess(message || "Profile updated successfully.");

                const redirect = result.redirectUrl ?? result.RedirectUrl;
                if (redirect) {
                    setTimeout(() => (window.location.href = redirect), 1000);
                }
            },
            error: function (xhr) {
                console.error("UpdateProfile failed:", xhr.status, xhr.responseText);
                showError("Update failed (HTTP " + xhr.status + ").");
            },
            complete: function () {
                saving = false;
                save.textContent = "Save changes";
                updateProgress();
            }
        });
    });

    updateProgress();
})();