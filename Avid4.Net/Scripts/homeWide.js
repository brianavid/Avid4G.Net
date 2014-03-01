
$(function () {

    $("#selectMusic").mousedown(function () {
        LaunchProgram("Music", "/Music/All");
    });

    $("#selectVideo").mousedown(function () {
        LaunchProgram("Video", "/Video/All");
    });

    $("#selectTV").mousedown(function () {
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", "/Sky/All", "live")
        } else {
            LaunchProgram("TV", "/TV/All")
        }
    });

    $("#selectSky").mousedown(function () {
        StartSky("Sky", "/Sky/All", "planner")
    });

    $("#selectSpotify").mousedown(function () {
        LaunchProgram("Spotify", "/Spotify/All");
    });

    $("#selectWeb").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        if (lastRunningProgram == "Web") {
            LinkTo("/Web/All");
        }
        else {
            AllOffJump("/Web/All", true);
        }
    });

    $("#selectPhotos").mousedown(function () {
        LaunchNewProgram("Photo", "", "/Photos/All");
    });

    $("#selectEpg").mousedown(function () {
        LinkTo("/Guide/BrowserWide?mode=GuideRoot");
    });

});