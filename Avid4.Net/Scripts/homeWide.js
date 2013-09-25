
$(function () {

    $("#selectMusic").mousedown(function () {
        LaunchProgram("Music", "/Music/All");
    });

    $("#selectVideo").mousedown(function () {
        LaunchProgram("Video", "/Video/All");
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
            AllOffJump("/Web/All");
        }
    });

    $("#selectPhotos").mousedown(function () {
        LaunchNewProgram("Photo", "", "/Photos/All");
    });

    $("#selectEpg").mousedown(function () {
        LinkTo("/Guide/BrowserWide?mode=GuideRoot");
    });

});