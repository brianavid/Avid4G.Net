
$(function () {

    $("#selectMusic").mousedown(function () {
        StopSwitching();
        LaunchProgram("Music", "/Music/All");
    });

    $("#selectVideo").mousedown(function () {
        StopSwitching();
        LaunchProgram("Video", "/Video/All");
    });

    $("#selectTV").mousedown(function () {
        StopSwitching();
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", "/Sky/All", "live")
        } else {
            LaunchProgram("TV", "/TV/All")
        }
    });

    $("#selectSky").mousedown(function () {
        StopSwitching();
        StartSky("Sky", "/Sky/All", "planner")
    });

    $("#selectSpotify").mousedown(function () {
        StopSwitching();
        LaunchProgram("Spotify", "/Spotify/All");
    });

    $("#selectWeb").mousedown(function () {
        StopSwitching();
        var lastRunningProgram = $("#homeTitle").text();
        if (lastRunningProgram == "Web") {
            LinkTo("/Web/All");
        }
        else {
            AllOffJump("/Web/All", true);
        }
    });

    $("#selectStream").mousedown(function () {
        StopSwitching();
        $.ajax({
            url: "/Action/StartStream",
            success: function () {
                LinkTo("/Roku/All");
            },
            cache: false
        });
    });

    $("#selectPhotos").mousedown(function () {
        StopSwitching();
        LaunchNewProgram("Photo", "", "/Photos/All");
    });

    $("#selectEpg").mousedown(function () {
        StopSwitching();
        LinkTo("/Guide/BrowserWide?mode=GuideRoot");
    });

});