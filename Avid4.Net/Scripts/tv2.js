function ControlAction(action)
{
    console.log(action)
    $.ajax({
        url: action,
        success: function (data) {
            console.log(data)
        },
        async: false,
        cache: false
    });
}

var actionTextTimeout = null

function DisplayControlAction(actionText, timeout)
{
    if (actionTextTimeout != null)
    {
        clearTimeout(actionTextTimeout)
        actionTextTimeout = null
    }
    $(".tvControlAction").text(actionText);
    if (timeout)
    {
        actionTextTimeout = setTimeout("$('.tvControlAction').text(''); actionTextTimeout=null", 2000);
    }
}

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
            url: "/Tv2/Action?command=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            async: false,
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
            url: "/Tv2/Action?command=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            async: false,
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
            url: "/Tv2/NowAndNext?channelName=" + this.id,
            success: function (data) {
                $(".tvChannelNowNext", which).html(data)
            },
            cache: false
        });
    });

    channelsHammer.on("doubletap", ".tvChannel", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Tv2/ChangeChannel?channelName=" + this.id,
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
        ReplacePane("tvControlPane", "/Tvs/ControlPane", "none", ResizeButtons);
    }
    else if (jump) {
        LinkTo("/Tv2/Watch");
    }
}

function DisplayTvChannels() {
    var channelsDisplay = document.getElementById("tvChannels");
    if (channelsDisplay != null) {
        $(".tvChannels").hide()
        $.ajax({
            url: "/Tv2/ChannelsPane",
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
            url: "/Tv2/RadioPane",
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
        LinkTo("/Tv2/Channels")
    });

    $("#goTvRadio").click(function () {
        LinkTo("/Tv2/Radio")
    });

    $("#goTvControls").click(function () {
        LinkTo("/Tv2/Buttons")
    });

    $("#displayTvTv").click(DisplayTvChannels);

    $("#displayTvRadio").click(DisplayTvRadio);

    // update again every minute
    setInterval("DisplayRunningOnControlPad()", 60000);
})