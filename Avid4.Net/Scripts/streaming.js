var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".rokuControls").hammer();
    }

    controlHammer.on("tap", "#btnHome", function (e) {
        $.get("/Streaming/KeyPress/Home")
        return false;
    });

    controlHammer.on("tap", "#btnBack", function (e) {
        $.get("/Streaming/KeyPress/Back")
        return false;
    });

    controlHammer.on("tap", "#btnOk", function (e) {
        $.get("/Streaming/KeyPress/Select")
        return false;
    });

    controlHammer.on("touch", "#btnUp", function (e) {
        $.get("/Streaming/KeyDown/Up")
        return false;
    });

    controlHammer.on("release", "#btnUp", function (e) {
        $.get("/Streaming/KeyUp/Up")
        return false;
    });

    controlHammer.on("touch", "#btnLeft", function (e) {
        $.get("/Streaming/KeyDown/Left")
        return false;
    });

    controlHammer.on("release", "#btnLeft", function (e) {
        $.get("/Streaming/KeyUp/Left")
        return false;
    });

    controlHammer.on("touch", "#btnRight", function (e) {
        $.get("/Streaming/KeyDown/Right")
        return false;
    });

    controlHammer.on("release", "#btnRight", function (e) {
        $.get("/Streaming/KeyUp/Right")
        return false;
    });

    controlHammer.on("touch", "#btnDown", function (e) {
        $.get("/Streaming/KeyDown/Down")
        return false;
    });

    controlHammer.on("release", "#btnDown", function (e) {
        $.get("/Streaming/KeyUp/Down")
        return false;
    });

    controlHammer.on("touch", "#btnPrev", function (e) {
        $.get("/Streaming/KeyDown/Rev")
        return false;
    });

    controlHammer.on("release", "#btnPrev", function (e) {
        $.get("/Streaming/KeyUp/Rev")
        return false;
    });

    controlHammer.on("touch", "#btnNext", function (e) {
        $.get("/Streaming/KeyDown/Fwd")
        return false;
    });

    controlHammer.on("release", "#btnNext", function (e) {
        $.get("/Streaming/KeyUp/Fwd")
        return false;
    });

    controlHammer.on("tap", "#btnPlayPause", function (e) {
        $.get("/Streaming/KeyPress/Play")
        return false;
    });

    controlHammer.on("tap", "#goRokuText", function (e) {
        var text = document.getElementById("rokuText").value
        $.get("/Streaming/SendText?text=" + encodeURIComponent(text))
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

    AddControlHammerActions()
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

    $("#goChromecastSelect").click(function () {
        $.get("/Action/GoChromecast", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
        })
    });

    $("#goLogFireSelect").click(function () {
        $.get("/Action/GoLogFire", null, function () {
            LinkTo(document.getElementById("isWide") != null ? "/Streaming/All" : "/Streaming/Browser")
        })
    });

})
