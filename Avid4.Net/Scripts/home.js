
$(function () {

    $("#selectMusic").mousedown(function () {
        LaunchProgram("Music", "/Music/Playing");
    });

    $("#selectVideo").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        LinkTo(lastRunningProgram == "Video" ? "/Video/Watch" : "/Video/Recordings");
    });

    $("#selectTV").mousedown(function () {
        StartSky("TV", null, "live")
    });

    $("#selectRadio").mousedown(function () {
        StartSky("Sky", null, "radio")
    });

    $("#selectSky").mousedown(function () {
        StartSky("Sky", null, "planner")
    });

    $("#selectSpotify").mousedown(function () {
        LaunchProgram("Spotify", "/Spotify/Playing");
    });

    $("#selectWeb").mousedown(function () {
        var lastRunningProgram = $("#homeTitle").text();
        LinkTo(lastRunningProgram == "Web" ? "/Web/Mouse" : "/Web/Browser?mode=iPlayerSelect");
    });

    $("#selectPhotos").css({ opacity: 0.5 });
    $("#selectEpg").css({ opacity: 0.5 });

});