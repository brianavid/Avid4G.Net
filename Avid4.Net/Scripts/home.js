
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
        LaunchProgramWithArgs("Web", "-k -nomerge http://beta.bbc.co.uk/iplayer/bigscreen/", "/Web/Mouse");
    });

    $("#selectPhotos").css({ opacity: 0.5 });
    $("#selectEpg").css({ opacity: 0.5 });

});