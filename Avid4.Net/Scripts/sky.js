var controlHammer = null;

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
    $(".skyControlAction").text(actionText);
    if (timeout)
    {
        actionTextTimeout = setTimeout("$('.skyControlAction').text(''); actionTextTimeout=null", 2000);
    }
}

function AddControlHammerActions(controlUnderButtons) {
    if (!controlUnderButtons)
    {
        $(".skyControlPane").each(function () {
            $(this).height($(window).height() - getTop(this))
        });
    }

    if (!controlHammer) {
        controlHammer = $(".skyControlPane").hammer({prevent_default: true});
    }

    var held = false;
    var paused = false;
    var lastSegment = -1;

    controlHammer.on("tap", function (e) {
        var g = e.gesture;
        g.preventDefault()
        if (paused) {
            paused = false;
            DisplayControlAction("PLAY", true)
            ControlAction("/Sky/Play?speed=1")
        }
        else {
            paused = true;
            DisplayControlAction("PAUSE")
            ControlAction("/Sky/Pause")
        }
    });

    controlHammer.on("hold drag", function (e) {
        var g = e.gesture;
        g.preventDefault()
        $(e.target).attr('oncontextmenu', 'return false');
        held = true;
        var segment = Math.floor((g.touches[0].clientX - getLeft(this)) * 9 / this.clientWidth)
        if (segment != lastSegment)
        {
            switch (segment)
            {
                case 0:
                    DisplayControlAction("<< 30")
                    ControlAction("/Sky/Play?speed=-30")
                    break;
                case 1:
                    DisplayControlAction("<< 12")
                    ControlAction("/Sky/Play?speed=-12")
                    break;
                case 2:
                    DisplayControlAction("<< 6")
                    ControlAction("/Sky/Play?speed=-6")
                    break;
                case 3:
                    DisplayControlAction("<< 2")
                    ControlAction("/Sky/Play?speed=-2")
                    break;
                case 4:
                    DisplayControlAction("PAUSE")
                    ControlAction("/Sky/Pause")
                    break;
                case 5:
                    DisplayControlAction(">> 2")
                    ControlAction("/Sky/Play?speed=2")
                    break;
                case 6:
                    DisplayControlAction(">> 6")
                    ControlAction("/Sky/Play?speed=6")
                    break;
                case 7:
                    DisplayControlAction(">> 12")
                    ControlAction("/Sky/Play?speed=12")
                    break;
                case 8:
                    DisplayControlAction(">> 30")
                    ControlAction("/Sky/Play?speed=30")
                    break;
            }
            lastSegment = segment;
        }
    });

    controlHammer.on("release", function (e) {
        var g = e.gesture;
        g.preventDefault()
        if (held) {
            held = false;
            lastSegment = -1;
            DisplayControlAction("PAUSE")
            ControlAction("/Sky/Pause")
            paused = true;
        }
    });

    controlHammer.on("pinchin", function (e) {
        var g = e.gesture;
        g.preventDefault()
        held = false;
        lastSegment = -1;
        DisplayControlAction("STOP")
        ControlAction("/Sky/Stop")
        paused = true;
        held = false;
    });

}

var buttonsHammer = null;

function AddButtonsHammerActions(controlHeight) {
    $("#skyButtonsPane").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#skyButtonsPane h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });

    if (!buttonsHammer) {
        buttonsHammer = $(".skyButtons").hammer();
    }

    EnableDragScroll(buttonsHammer)

    buttonsHammer.on("tap", ".skySendIR", function (e) {
        $.ajax({
            url: "/Action/SendIR?id=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(false)
            },
            cache: false
        });
    });
}

function SetChannelsHeight()
{
    $(".skyChannels").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".skyChannels h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });
}

var channelsHammer = null;

function AddChannelsHammerActions() {
    SetChannelsHeight()

    if (!channelsHammer) {
        channelsHammer = $(".skyChannels").hammer();
    }

    EnableDragScroll(channelsHammer)

    channelsHammer.on("hold", ".skyChannel", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Sky/NowAndNext?id=" + this.id,
            success: function (data) {
                $(".skyChannelNowNext", which).html(data)
            },
            cache: false
        });
    });

    channelsHammer.on("doubletap", ".skyChannel", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Sky/ChangeChannel?id=" + this.id,
            success: function (data) {
                DisplayRunningOnControlPad(true)
            },
            cache: false
        });
    });
}

function SetRecordingsHeight()
{
    $(".skyRecordings").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".skyRecordings h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });
}

var recordingsHammer = null;

function AddRecordingsHammerActions() {
    SetRecordingsHeight()

    if (!recordingsHammer) {
        recordingsHammer = $(".skyRecordings").hammer();
    }

    EnableDragScroll(recordingsHammer)

    recordingsHammer.on("hold", ".skyRecording", function (e) {
        e.gesture.preventDefault()
        var which = this;
        $.ajax({
            url: "/Sky/RecordingDescription?id=" + this.id,
            success: function (data) {
                $(".skyRecordingDescription", which).text(data)
            },
            cache: false
        });
    });

    recordingsHammer.on("tap", ".skyRecording", function (e) {
        e.gesture.preventDefault()
        ReplacePane("skyRecordings", "/Sky/Recording?id=" + this.id, "clear", SetupRecordingActions)
    });

    recordingsHammer.on("tap", ".skyRecordingGroup", function (e) {
        e.gesture.preventDefault()
        ReplacePane("skyRecordings", "/Sky/RecordingsPane?title=" + escape(this.id), "clear")
    });


    recordingsHammer.on("tap", "#skyRecordingPlayFromStart", function () {
        $("#skyRecordingPlayFromStart").text("Playing ...")
        var playUrl = "/Sky/PlayRecording?id=" + $("#recordingId").text() + "&start=" + $("#positionStartMinutes").text();
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnControlPad(true)
            },
            cache: false
        });
    });

    recordingsHammer.on("tap", "#skyRecordingPlayFromTime", function () {
        $("#skyRecordingPlayFromTime").text("Playing ...")
        var playUrl = "/Sky/PlayRecording?id=" + $("#recordingId").text() + "&start=" + $("#positionMinutes").text();
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnControlPad(true)
            },
            cache: false
        });
    });

    recordingsHammer.on("tap", "#skyRecordingDelete", function () {
        var title = $(".skyRecordingName").text();
        var when = $(".skyRecordingWhen").text();
        if (confirm("Delete '" + title + "' (" + when + ")")) {
            $("#skyRecordingDelete").text("Deleting ...")
            $.ajax({
                url: "/Sky/DeleteRecording?id=" + $("#recordingId").text(),
                success: function (data) {
                    DisplaySkyRecordings(true)
                },
                cache: false
            });
        }
    });
}

function DisplayRunningOnControlPad(jump) {
    var controlDisplay = document.getElementById("skyControlPad");
    if (controlDisplay != null) {
        ReplacePane("skyControlPane", "/Sky/ControlPane", "none");
    }
    else if (jump) {
        LinkTo("/Sky/Watch");
    }
}

function DisplaySkyChannels() {
    var channelsDisplay = document.getElementById("skyChannels");
    if (channelsDisplay != null) {
        $(".skyRecordings").hide()
        $(".skyChannels").hide()
        $.ajax({
            url: "/Sky/ChannelsPane",
            success: function (data) {
                $(".skyChannels").html(data)
                $(".skyChannels").show()
                SetChannelsHeight();
            },
            cache: false
        });
    }
    else {
        LinkTo("/Sky/Recordings");
    }
}

function DisplaySkyRadio() {
    var channelsDisplay = document.getElementById("skyChannels");
    if (channelsDisplay != null) {
        $(".skyRecordings").hide()
        $(".skyChannels").hide()
        $.ajax({
            url: "/Sky/RadioPane",
            success: function (data) {
                $(".skyChannels").html(data)
                $(".skyChannels").show()
                SetChannelsHeight();
            },
            cache: false
        });
    }
    else {
        LinkTo("/Sky/Recordings");
    }
}

function DisplaySkyRecordings(forceUpdate) {
    var recordingsDisplay = document.getElementById("skyRecordings");
    if (recordingsDisplay != null) {
        var isVisible = $(recordingsDisplay).is(":visible")
        $(".skyChannels").hide()
        $(".skyRecordings").hide()
        $.ajax({
            url: "/Sky/RecordingsPane" + (forceUpdate || isVisible ? "?refresh=yes" : ""),
            success: function (data) {
                $(".skyRecordings").html(data)
                $(".skyRecordings").show()
                SetRecordingsHeight()
            },
            cache: false
        });
    }
    else {
        LinkTo("/Sky/Recordings");
    }
}

function SetupRecordingActions()
{
    $(".skyRecordingPositionSlider").noUiSlider({
        range: [0, $("#durationMinutes").text()]
        , start: $("#positionMinutes").text()
        , step: 1
        , handles: 1
        , slide: function () {
            var pos = Math.floor($(this).val());
            var mins = pos % 60;
            var hours = Math.floor(pos / 60);
            var posDisplay = hours + ":" + (mins < 10 ? "0" : "") + mins;
            $("#positionValueDisplay").text(posDisplay);
            $("#positionMinutes").text(pos);
        }
    });

}

$(function () {
    var controlHeight = 0;
    var controlUnderButtons = false;

    $("#skyControlPane").each(function () {
        controlHeight = $(this).height();
    })

    $("#skyButtonsPane").each(function () {
        controlUnderButtons = true;
    })

    AddControlHammerActions(controlUnderButtons)
    AddButtonsHammerActions(controlHeight)
    AddChannelsHammerActions()
    AddRecordingsHammerActions()

    $("#goSkyWatch").click(function () {
        DisplayRunningOnControlPad(true)
    });

    $("#goSkyLive").click(function () {
        LinkTo("/Sky/Live")
    });

    $("#goSkyRadio").click(function () {
        LinkTo("/Sky/Radio")
    });

    $("#goSkyRecordings").click(function () {
        $.ajax({
            url: "/Sky/Stop",
            success: function (data) {
                LinkTo("/Sky/Recordings?refresh=yes")
            },
            async: false,
            cache: false
        });
    });

    $("#goSkyControls").click(function () {
        LinkTo("/Sky/Buttons")
    });

    $("#displaySkyLive").click(DisplaySkyChannels);

    $("#displaySkyRadio").click(DisplaySkyRadio);

    $("#displaySkyRecordings").click(DisplaySkyRecordings);

    $(".skyWatchSendIR").click( function (e) {
        if ($(this).hasClass("skyWatchEnableRecord")) {
            $(".skyWatchRecord").show()
        }
        if ($(this).hasClass("skyWatchDisableRecord")) {
            $(".skyWatchRecord").hide()
        }
        $.ajax({
            url: "/Action/SendIR?id=" + this.id,
            cache: false
        });
    });

    // update again every minute
    setInterval("DisplayRunningOnControlPad()", 60000);
})