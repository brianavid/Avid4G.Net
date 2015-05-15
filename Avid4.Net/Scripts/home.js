
$(function () {

    $("#selectMusic").mousedown(function () {
        StopSwitching();
        LaunchProgram("Music", "/Music/Playing");
    });

    $("#selectVideo").mousedown(function () {
        StopSwitching();
        var lastRunningProgram = $("#homeTitle").text();
        LaunchProgram( "Video", lastRunningProgram == "Video" ? "/Video/Watch" : "/Video/Recordings");
    });

    $("#selectTV").mousedown(function () {
        StopSwitching();
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", null, "live")
        } else {
            LaunchProgram("TV", "/TV/Channels")
        }
    });

    $("#selectRadio").mousedown(function () {
        StopSwitching();
        var tvService = $("#tvService").text();
        if (tvService == "Sky") {
            StartSky("Sky", null, "radio")
        } else {
            LaunchProgram("TV", "/TV/Radio", "Radio")
        }
    });

    $("#selectSky").mousedown(function () {
        StopSwitching();
        StartSky("Sky", null, "planner")
    });

    $("#selectSpotify").mousedown(function () {
        StopSwitching();
        LaunchProgram("Spotify", "/Spotify/Playing");
    });

    $("#selectWeb").mousedown(function () {
        StopSwitching();
        var lastRunningProgram = $("#homeTitle").text();
        if (lastRunningProgram == "Web")
        {
            LinkTo("/Web/Mouse");
        }
        else
        {
            AllOffJump("/Web/Browser?mode=iPlayerSelect", true);
        }
    });

    $("#selectStream").mousedown(function () {
        StopSwitching();
        var lastRunningProgram = $("#homeTitle").text();
        $.ajax({
            url: "/Action/StartStream",
            success: function () {
                LinkTo(lastRunningProgram == "Roku" ? "/Streaming/Controls" : "/Streaming/Browser");
            },
            cache: false
        });
    });

    $("#selectPhotos").mousedown(function () {
        StopSwitching();
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
        StopSwitching();
        LinkTo("/Guide/Browser?mode=GuideRoot");
    });

});