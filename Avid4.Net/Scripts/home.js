
$(function () {

    $("#selectMusic").mousedown(function () {
        LaunchProgram("Music", "/Music/Playing");
    });

    $("#selectVideo").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        LaunchProgram( "Video", lastRunningProgram == "Video" ? "/Video/Watch" : "/Video/Recordings");
    });

    $("#selectTV").mousedown(function () {
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", null, "live")
        } else {
            LaunchProgram("TV", "/TV/Channels")
        }
    });

    $("#selectRadio").mousedown(function () {
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", null, "radio")
        } else {
            LaunchProgram("TV", "/TV/Radio")
        }
    });

    $("#selectSky").mousedown(function () {
        StartSky("Sky", null, "planner")
    });

    $("#selectSpotify").mousedown(function () {
        LaunchProgram("Spotify", "/Spotify/Playing");
    });

    $("#selectWeb").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        if (lastRunningProgram == "Web")
        {
            LinkTo("/Web/Mouse");
        }
        else
        {
            AllOffJump("/Web/Browser?mode=iPlayerSelect");
        }
    });

    $("#selectPhotos").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        if (lastRunningProgram == "Photo")
        {
            LinkTo("/Photos/Display");
        }
        else
        {
            LaunchNewProgram("Photo", "", "/Photos/Browse");
        }
    });

    $("#selectEpg").mousedown(function () {
        LinkTo("/Guide/Browser?mode=GuideRoot");
    });

});