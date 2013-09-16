﻿
$(function () {

    $("#selectMusic").mousedown(function () {
        LaunchProgram("Music", "/Music/All");
    });

    $("#selectVideo").mousedown(function () {
        LinkTo("/Video/All");
    });

    $("#selectSky").mousedown(function () {
        StartSky("Sky", "/Sky/All", "planner")
    });

    $("#selectSpotify").mousedown(function () {
        LaunchProgram("Spotify", "/Spotify/All");
    });

    $("#selectWeb").mousedown(function () {
        LinkTo("/Web/All");
    });

    $("#selectPhotos").mousedown(function () {
        LaunchNewProgram("Photo", "", "/Photos/All");
    });

    $("#selectEpg").mousedown(function () {
        LinkTo("/Guide/BrowserWide?mode=GuideRoot");
    });

});