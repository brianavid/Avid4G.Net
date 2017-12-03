var profilesHammer = null;

function AddProfilesHammerActions() {
    $(".securityProfileItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".securityProfileItems h=" + h + "; t=" + t + "; c=" + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!profilesHammer) {
        profilesHammer = $(".securityProfileItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(profilesHammer)

    profilesHammer.on("tap", ".securityProfileItem", function (e) {
        $.ajax({
            url: "/Security/LoadProfile/" + this.id,
            cache: false,
            success: function () {
                LinkTo("/Security/GetSchedule")
            }
        });
        return false;
    });
}

var zonesHammer = null;

function AddZonesHammerActions() {
    $(".securityZoneItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".securityZoneItems h=" + h + "; t=" + t + "; c=" + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!zonesHammer) {
        zonesHammer = $(".securityZoneItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(zonesHammer)

    zonesHammer.on("tap", ".securityZoneItem", function (e) {
        $(".securityZoneOn").addClass("startHidden");
        $(".securityZoneOff").addClass("startHidden");
        $(this).children("img").removeClass("startHidden");

        return false;
    });

    zonesHammer.on("tap", ".securityZoneOn", function (e) {
        var zone = this;

        $.ajax({
            url: "/Security/TurnZoneOn/" + zone.id,
            cache: false
        });
        return false;
    });

    zonesHammer.on("tap", ".securityZoneOff", function (e) {
        var zone = this;

        $.ajax({
            url: "/Security/TurnZoneOff/" + zone.id,
            cache: false
        });
        return false;
    });
}

var scheduleHammer = null;

function AddScheduleHammerActions() {
    $(".securityScheduleItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".securityScheduleItems h=" + h + "; t=" + t + "; c=" + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!scheduleHammer) {
        scheduleHammer = $(".securityScheduleItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(scheduleHammer)
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
    $("#goSecurityProfiles").click(function () {
        LinkTo("/Security/GetProfiles")
    });

    $("#goSecurityZones").click(function () {
        LinkTo("/Security/GetZones")
    });

    $("#goSecuritySchedule").click(function () {
        LinkTo("/Security/GetSchedule")
    });

    $("#securityAwayToday").click(function () {
        LinkTo("/Security/AwayToday")
    });

    $("#securityAwayTrip").click(function () {
        LinkTo("/Security/AwayTrip")
    });

    AddProfilesHammerActions()
    AddZonesHammerActions()
    AddScheduleHammerActions()
})