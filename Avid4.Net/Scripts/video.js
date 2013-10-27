function InitializePositionSlider()
{
    $("#videoPosSlider").noUiSlider({
        range: [0, 200]
        , start: 0
        , step: 1
        , handles: 1
        , slide: function () {
            slidingTime = new Date();
            var pos = Math.floor($(this).val());
            PositionMS = pos * DurationMS / 200;
            var secs = Math.floor(PositionMS / 1000);
            var mins = Math.floor(secs / 60);
            secs = secs % 60;
            var posText = mins + ":" + (secs < 10 ? "0" : "") + secs;
            var posDisplay = document.getElementById("PlaybackInfo.ElapsedTimeDisplay");
            if (posDisplay != null) {
                $(posDisplay).text(posText);
            }

            sendZoom("exSeekTo," + Math.round(PositionMS / 1000));
        }
    });
}

function SetRecordingsHeight() {
    $(".videoRecordings").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".videoRecordings h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });
    $(".videoDvds").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".videoDvds h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });
}

function DisplayVideoRecordings() {
    var DvdsDisplay = document.getElementById("videoDvds");
    if (DvdsDisplay != null) {
        $(".videoRecordings").hide()
        $(".videoDvds").hide()
        $.ajax({
            url: "/Video/RecordingsPane",
            success: function (data) {
                $(".videoRecordings").html(data)
                $(".videoRecordings").show()
                SetRecordingsHeight();
            },
            cache: false
        });
    }
    else {
        LinkTo("/Video/Recordings");
    }
}

function DisplayDvds() {
    var recordingsDisplay = document.getElementById("videoRecordings");
    if (recordingsDisplay != null) {
        $(".videoRecordings").hide()
        $(".videoDvds").hide()
        $.ajax({
            url: "/Video/DVDsPane",
            success: function (data) {
                $(".videoDvds").html(data)
                $(".videoDvds").show()
                SetRecordingsHeight()
            },
            cache: false
        });
    }
    else {
        LinkTo("/Video/DVDs");
    }
}

function DisplayRunningOnWatchPane(jump) {
    var watchDisplay = document.getElementById("videoWatchPane");
    if (watchDisplay != null) {
        ReplacePane("videoWatchPane", "/Video/WatchPane", "none", InitializePositionSlider);
    }
    else if (jump) {
        LinkTo("/Video/Watch");
    }
}

function sendZoom(cmd, onAfter)
{
    $.get("/Video/SendZoom?cmd=" + escape(cmd), null, onAfter)
}

var videoControlHammer = null;

function AddVideoControlsHammerActions() {

    if (!videoControlHammer) {
        videoControlHammer = $(".videoWatchPane").hammer();
    }

    videoControlHammer.on("touch", "#videoBack60", function (e) {
        sendZoom("exSeekBack,60")
    });

    videoControlHammer.on("touch", "#videoBack10", function (e) {
        sendZoom("exSeekBack,10")
    });

    videoControlHammer.on("touch", "#videoForward10", function (e) {
        sendZoom("exSeekAhead,10")
    });

    videoControlHammer.on("touch", "#videoForward60", function (e) {
        sendZoom("exSeekAhead,60")
    });

    videoControlHammer.on("touch", "#videoPlayPause", function (e) {
        sendZoom("fnPlay")
    });

    videoControlHammer.on("touch", "#videoStop", function (e) {
        sendZoom("fnStop")
    });

    videoControlHammer.on("touch", "#videoDvdMenu", function (e) {
        sendZoom("fnDVDRootMenu", UpdateZoomDisplayPlayingInformation)
    });

    videoControlHammer.on("touch", "#videoMenuUp", function (e) {
        sendZoom("fnDVDMenuUp")
    });

    videoControlHammer.on("touch", "#videoMenuLeft", function (e) {
        sendZoom("fnDVDMenuLeft")
    });

    videoControlHammer.on("touch", "#videoMenuSelect", function (e) {
        sendZoom("fnDVDMenuSelect")
    });

    videoControlHammer.on("touch", "#videoMenuRight", function (e) {
        sendZoom("fnDVDMenuRight")
    });

    videoControlHammer.on("touch", "#videoMenuDown", function (e) {
        sendZoom("fnDVDMenuDown")
    });

}

var videoRecordingsListHammer = null;

function AddVideoRecordingsHammerActions() {
    SetRecordingsHeight()

    if (!videoRecordingsListHammer) {
        videoRecordingsListHammer = $(".videoRecordings").hammer();
    }

    EnableDragScroll(videoRecordingsListHammer)

    videoRecordingsListHammer.on("swiperight swipeleft", function (e) {
        PopStackedPane("videoRecordings", DisplayVideoRecordings)
        return false;
    })

    videoRecordingsListHammer.on("tap", ".videoRecording", function (e) {
        e.gesture.preventDefault()
        ReplacePane("videoRecordings", "/Video/Recording?id=" + this.id, "push")
    });

    videoRecordingsListHammer.on("tap", "#videoRecordingPlayFromStart", function () {
        $("#videoRecordingPlayFromStart").text("Playing ...")
        var playUrl = "/Video/PlayRecording?id=" + $("#recordingId").text();
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnWatchPane(true)
            },
            cache: false
        });
    });

    videoRecordingsListHammer.on("tap", "#videoRecordingDelete", function () {
        var title = $(".videoRecordingName").text();
        var when = $(".videoRecordingWhen").text();
        if (confirm("Delete '" + title + "' (" + when + ")")) {
            $("#videoRecordingDelete").text("Deleting ...")
            $.ajax({
                url: "/Video/DeleteRecording?id=" + $("#recordingId").text(),
                success: function (data) {
                    DisplayVideoRecordings()
                },
                cache: false
            });
        }
    });

}

var videoDvdsListHammer = null;

function AddVideoDvdsHammerActions() {
    if (!videoDvdsListHammer) {
        videoDvdsListHammer = $(".videoDvds").hammer();
    }

    EnableDragScroll(videoDvdsListHammer)

    videoDvdsListHammer.on("doubletap", ".videoDvdDrive", function () {
        var playUrl = "/Video/PlayDvdDisk?drive=" + this.id + "&title=" + escape($(".videoRecordingTitle", this).text());
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnWatchPane(true)
            },
            cache: false
        });
    });

    videoDvdsListHammer.on("doubletap", ".videoBluRay", function () {
        var playUrl = "/Video/PlayBluRayFile?path=" + escape(this.id) + "&title=" + escape($(".videoRecordingTitle", this).text());
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnWatchPane(true)
            },
            cache: false
        });
    });

    videoDvdsListHammer.on("doubletap", ".videoDvdDirectory", function () {
        var playUrl = "/Video/PlayDvdDirectory?path=" + escape(this.id) + "&title=" + escape($(".videoRecordingTitle", this).text());
        $.ajax({
            url: playUrl,
            success: function (data) {
                DisplayRunningOnWatchPane(true)
            },
            cache: false
        });
    });

}

var PositionMS = 0;
var DurationMS = 0;
var slidingTime = new Date(0);
var posSlider = null;
var lastDisplayUpdate = new Date();

function updateSlider() {
    var now = new Date();
    if (now.getTime() - slidingTime.getTime() > 5 * 1000 && DurationMS > 0 && PositionMS <= DurationMS) {
        var sliderValue = Math.round((PositionMS * 200) / DurationMS);
        $("#videoPosSlider").val(sliderValue)
    }
}

function UpdateZoomDisplayPlayingInformation() {
    var now = new Date();
    if (now.getTime() - lastDisplayUpdate.getTime() > 10 * 1000) {
        lastDisplayUpdate = now;
        return;
    }

    lastDisplayUpdate = now;
    if (overlayVisible || !navigator.onLine) {
        return;
    }

    $.ajax({
        type: "GET",
        url: "/Video/GetPlayingInfo",
        timeout: 700,
        success: function (xml) {
            if (xml != null) {
                var root = xml.getElementsByTagName("Response");

                // see if response is valid
                if ((root != null) && (root.length == 1) && (xml.documentElement.getAttribute("Status") == "OK")) {

                    // get all items
                    var items = xml.getElementsByTagName("Item");
                    if (items != null) {

                        // loop items
                        for (var i = 0; i < items.length; i++) {

                            // parse values
                            var name = items[i].getAttribute("Name");
                            var value = items[i].childNodes[0].nodeValue;

                            // get corresponding element
                            var element = document.getElementById("PlaybackInfo." + name);
                            if (element != null) {

                                // update element
                                if ((element.src != null) && (element.src != ""))
                                    element.src = JrmcHost + value; // image
                                else
                                    element.innerHTML = value; // text
                            }

                            if (name == "PositionMS") {
                                PositionMS = parseInt(value);
                                updateSlider();
                            }

                            if (name == "DurationMS") {
                                DurationMS = parseInt(value);
                                updateSlider();
                            }

                            if (name == "Mode") {
                                if (value == "Menu") {
                                    $("#videoPlaying").hide();
                                    $("#videoMenu").show();
                                }
                                else {
                                    $("#videoPlaying").show();
                                    $("#videoMenu").hide();
                                }
                            }
                        }
                    }
                }
            }
        },
        error: function (jqXHR, textStatus, errorThrown) {
        }
    });
}

$(function () {
    InitializePositionSlider()

    SetRecordingsHeight()

    AddVideoControlsHammerActions();
    AddVideoRecordingsHammerActions();
    AddVideoDvdsHammerActions();

    $("#goVideoWatch").click(function () {
        LinkTo("/Video/Watch")
    });

    $("#goVideoRecordings").click(function () {
        LinkTo("/Video/Recordings")
    });

    $("#goVideoDVDs").click(function () {
        LinkTo("/Video/DVDs")
    });

    $("#displayVideoRecordings").click(DisplayVideoRecordings);

    $("#displayDVDs").click(DisplayDvds);

    $("#actionMenuVideoScreenFix").click(function () {
        sendZoom("fnFullscreen")
    });

    // update information once now
    UpdateZoomDisplayPlayingInformation();

    // update again every little bit
    zoomRepeater = setInterval("UpdateZoomDisplayPlayingInformation()", 2000);
})
