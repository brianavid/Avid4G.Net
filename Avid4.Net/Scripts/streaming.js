﻿var rokuControlHammer = null;

function AddRokuControlHammerActions() {
    if (!rokuControlHammer) {
        rokuControlHammer = $(".rokuControls").hammer({ prevent_default: true });
    }

    rokuControlHammer.on("tap", "#btnHome", function (e) {
        $.get("/Streaming/KeyPress/Home")
        return false;
    });

    rokuControlHammer.on("tap", "#btnBack", function (e) {
        $.get("/Streaming/KeyPress/Back")
        return false;
    });

    rokuControlHammer.on("tap", "#btnOk", function (e) {
        $.get("/Streaming/KeyPress/Select")
        return false;
    });

    rokuControlHammer.on("touch", "#btnUp", function (e) {
        $.get("/Streaming/KeyDown/Up")
        return false;
    });

    rokuControlHammer.on("release", "#btnUp", function (e) {
        $.get("/Streaming/KeyUp/Up")
        return false;
    });

    rokuControlHammer.on("touch", "#btnLeft", function (e) {
        $.get("/Streaming/KeyDown/Left")
        return false;
    });

    rokuControlHammer.on("release", "#btnLeft", function (e) {
        $.get("/Streaming/KeyUp/Left")
        return false;
    });

    rokuControlHammer.on("touch", "#btnRight", function (e) {
        $.get("/Streaming/KeyDown/Right")
        return false;
    });

    rokuControlHammer.on("release", "#btnRight", function (e) {
        $.get("/Streaming/KeyUp/Right")
        return false;
    });

    rokuControlHammer.on("touch", "#btnDown", function (e) {
        $.get("/Streaming/KeyDown/Down")
        return false;
    });

    rokuControlHammer.on("release", "#btnDown", function (e) {
        $.get("/Streaming/KeyUp/Down")
        return false;
    });

    rokuControlHammer.on("touch", "#btnPrev", function (e) {
        $.get("/Streaming/KeyDown/Rev")
        return false;
    });

    rokuControlHammer.on("release", "#btnPrev", function (e) {
        $.get("/Streaming/KeyUp/Rev")
        return false;
    });

    rokuControlHammer.on("touch", "#btnNext", function (e) {
        $.get("/Streaming/KeyDown/Fwd")
        return false;
    });

    rokuControlHammer.on("release", "#btnNext", function (e) {
        $.get("/Streaming/KeyUp/Fwd")
        return false;
    });

    rokuControlHammer.on("tap", "#btnPlayPause", function (e) {
        $.get("/Streaming/KeyPress/Play")
        return false;
    });

    rokuControlHammer.on("tap", "#goRokuText", function (e) {
        var text = document.getElementById("rokuText").value
        $.get("/Streaming/SendText?text=" + encodeURIComponent(text))
        return false;
    });
}

var smartControlHammer = null;

function AddSmartControlHammerActions() {
    if (!smartControlHammer) {
        smartControlHammer = $(".smartControls").hammer({ prevent_default: true });
    }

    smartControlHammer.on("touch", ".tvKey", function (e) {
        $.get("/Streaming/SendTvKey?keyName=" + this.id)
        return false;
    });

    smartControlHammer.on("touch", ".tvPlayPause", function (e) {
        $.get("/Streaming/SmartTvPlayPause")
        return false;
    });
}

var curzonControlHammer = null;

function AddCurzonControlHammerActions() {
    if (!curzonControlHammer) {
        curzonControlHammer = $(".curzonButtons").hammer({ prevent_default: true });
    }

    curzonControlHammer.on("tap", "#curzonEscape", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys={ESC}",
            cache: false
        });
        return false;
    });

    curzonControlHammer.on("tap", "#curzonLeft", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys={BS}",
            cache: false
        });
        return false;
    });

    curzonControlHammer.on("tap", "#curzonEnter", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys=~",
            cache: false
        });
        return false;
    });

    curzonControlHammer.on("tap", "#curzonEnterText", function (e) {
        var text = document.getElementById("curzonEnteredText").value
        $.ajax({
            url: "/Action/SendKeys?keys=" + encodeURIComponent(text),
            cache: false
        });
        return false;
    });

    curzonControlHammer.on("tap", "#curzonMenu", function (e) {
        $.ajax({
            url: "/Action/MouseClick?right=yes",
            cache: false
        });
        return false;
    });
}

var browserHammer = null;

function AddBrowserHammerActions() {
    $("#rokuBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#rokuBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!browserHammer) {
        browserHammer = $(".rokuBrowserItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("tap", ".rokuBrowserApp", function (e) {
        $.ajax({
            url: "/Streaming/RokuLaunch/" + this.id,
            success: function (data) {
                if (document.getElementById("isWide") == null) {
                    LinkTo("/Streaming/Controls")
                }
            },
            cache: false
        });
        return false;
    });
}

$(function () {
    $("#streamingLeftPane").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        $(this).height(h - t)
    });

    AddRokuControlHammerActions()
    AddSmartControlHammerActions()
    AddCurzonControlHammerActions()
    AddBrowserHammerActions();

    $("#goStreamSourceSelect").click(function () {
        LinkTo("/Streaming/Browser")
    });

    $("#goRokuSelect").click(function () {
        if (document.getElementById("isWide") == null &&
            $("#homeTitle").text() == "Roku") {
            LinkTo("/Streaming/Controls")
        } else {
            $.get("/Action/GoRoku", null, function () {
                LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
            })
        }
    });

    $("#goSmartTvSelect").click(function () {
        if (document.getElementById("isWide") == null &&
            $("#homeTitle").text() == "SmartTv") {
            LinkTo("/Streaming/Controls")
        } else {
            $.get("/Action/GoSmart", null, function () {
                LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Controls")
            })
        }
    });

    $("#goChromecastSelect").click(function () {
        $.get("/Action/GoChromecast", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
        })
    });

    $("#goChromecastAudioSelect").click(function () {
        $.get("/Action/GoChromecastAudio", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
        })
    });

    $("#goCurzonSelect").click(function () {
        $.get("/Action/GoCurzon", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Controls")
        })
    });

    $("#goLogFireSelect").click(function () {
        $.get("/Action/GoLogFire", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
        })
    });

})
