var profilesHammer = null;

function AddProfilesHammerActions() {
    if (!profilesHammer) {
        profilesHammer = $(".securityProfileItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(profilesHammer)

    profilesHammer.on("tap", ".securityProfileItem", function (e) {
        $.ajax({
            url: "/Security/LoadProfile/" + this.id,
            cache: false
        });
        return false;
    });
}

function CheckUsingDefaultProfile() {
    $.ajax({
        type: "GET",
        url: "/Security/IsDefault",
        timeout: 700,
        cache: false,
        success: function (isDefault) {
            if (isDefault != "Yes")
                window.location = "/Security/Away";
        }
    })
}

var defaultChecker = null;

function CheckUsingDefaultProfileWhenFocussed() {
    window.onfocus = function () {
        if (defaultChecker == null) {
            defaultChecker = setInterval(CheckUsingDefaultProfile, 10000)
        }
    }
    window.onblur = function () {
        if (defaultChecker != null) {
            clearInterval(defaultChecker)
            defaultChecker = null
        }
    }
    window.focus();
}

$(function () {
    AddProfilesHammerActions()
})