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

$(function () {
    AddProfilesHammerActions()
})