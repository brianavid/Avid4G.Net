var controlHammer = null;

function AddControlHammerActions(controlUnderButtons) {
    if (!controlUnderButtons)
    {
        $(".tvControlPane").each(function () {
            $(this).height($(window).height() - getTop(this))
        });
    }

    if (!controlHammer) {
        controlHammer = $(".tvControlPane").hammer();
    }

    controlHammer.on("touch", ".tvAction", function (e) {
        $.ajax({
            url: "/Tv/Action?command=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            cache: false
        });
    });

    controlHammer.on("tap", ".tvProgrammeRecord", function (e) {
        $.ajax({
            url: "/Tv/RecordNow",
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            cache: false
        });
    });
}

var buttonsHammer = null;

function AddButtonsHammerActions(controlHeight) {
    $("#tvButtonsPane").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#tvButtonsPane h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });

    if (!buttonsHammer) {
        buttonsHammer = $(".tvButtons").hammer();
    }

    EnableDragScroll(buttonsHammer)

    buttonsHammer.on("touch", ".tvAction", function (e) {
        $.ajax({
            url: "/Tv/Action?command=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            cache: false
        });
    });
}

function SetChannelsHeight()
{
    $(".tvChannels").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".tvChannels h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });
}

var channelsHammer = null;

function AddChannelsHammerActions() {
    SetChannelsHeight()

    if (!channelsHammer) {
        channelsHammer = $(".tvChannels").hammer({ prevent_default: true });
    }

    EnableDragScroll(channelsHammer)

    channelsHammer.on("hold", ".tvChannel", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Tv/NowAndNext?channelName=" + encodeURIComponent(this.id),
            success: function (data) {
                $(".tvChannelNowNext", which).html(data)
            },
            cache: false
        });
    });

    channelsHammer.on("tap", ".tvChannel", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Tv/ChangeChannel?channelName=" + encodeURIComponent(this.id),
            success: function (data) {
                DisplayRunningOnControlPad(true)
            },
            cache: false
        });
    });
}

function ResizeButtons()
{
    var controlHeight = 0;

    $("#tvControlPane").each(function () {
        controlHeight = $(this).height();
    })

    $("#tvButtonsPane").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#tvButtonsPane h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });
}

function DisplayRunningOnControlPad(jump) {
    var controlDisplay = document.getElementById("tvControlPane");

    if (controlDisplay != null) {
        ReplacePane("tvControlPane", "/Tv/ControlPane", "none", ResizeButtons);
    }
    else if (jump) {
        LinkTo("/Tv/Watch");
    }
}

function DisplayTvChannels() {
    var channelsDisplay = document.getElementById("tvChannels");
    if (channelsDisplay != null) {
        $(".tvChannels").hide()
        $.ajax({
            url: "/Tv/ChannelsPane",
            success: function (data) {
                $(".tvChannels").html(data)
                $(".tvChannels").show()
                SetChannelsHeight();
            },
            cache: false
        });
    }
}

function DisplayTvRadio() {
    var channelsDisplay = document.getElementById("tvChannels");
    if (channelsDisplay != null) {
        $(".tvChannels").hide()
        $.ajax({
            url: "/Tv/RadioPane",
            success: function (data) {
                $(".tvChannels").html(data)
                $(".tvChannels").show()
                SetChannelsHeight();
            },
            cache: false
        });
    }
}

$(function () {
    var controlHeight = 0;
    var controlUnderButtons = false;

    $("#tvControlPane").each(function () {
        controlHeight = $(this).height();
    })

    $("#tvButtonsPane").each(function () {
        controlUnderButtons = true;
    })

    AddControlHammerActions(controlUnderButtons)
    AddButtonsHammerActions(controlHeight)
    AddChannelsHammerActions()

    $("#goTvWatch").click(function () {
        DisplayRunningOnControlPad(true)
    });

    $("#goTvTv").click(function () {
        LinkTo("/Tv/Channels")
    });

    $("#goTvRadio").click(function () {
        LinkTo("/Tv/Radio")
    });

    $("#goTvControls").click(function () {
        LinkTo("/Tv/Buttons")
    });

    $("#displayTvTv").click(DisplayTvChannels);

    $("#displayTvRadio").click(DisplayTvRadio);

    // update again every few seconds
    setInterval("DisplayRunningOnControlPad()", 2000);
})