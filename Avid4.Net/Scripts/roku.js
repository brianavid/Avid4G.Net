var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".rokuControls").hammer();
    }

    controlHammer.on("tap", "#btnHome", function (e) {
        $.get("/Roku/KeyPress/Home")
        return false;
    });

    controlHammer.on("tap", "#btnBack", function (e) {
        $.get("/Roku/KeyPress/Back")
        return false;
    });

    controlHammer.on("tap", "#btnOk", function (e) {
        $.get("/Roku/KeyPress/Select")
        return false;
    });

    controlHammer.on("touch", "#btnUp", function (e) {
        $.get("/Roku/KeyDown/Up")
        return false;
    });

    controlHammer.on("release", "#btnUp", function (e) {
        $.get("/Roku/KeyUp/Up")
        return false;
    });

    controlHammer.on("touch", "#btnLeft", function (e) {
        $.get("/Roku/KeyDown/Left")
        return false;
    });

    controlHammer.on("release", "#btnLeft", function (e) {
        $.get("/Roku/KeyUp/Left")
        return false;
    });

    controlHammer.on("touch", "#btnRight", function (e) {
        $.get("/Roku/KeyDown/Right")
        return false;
    });

    controlHammer.on("release", "#btnRight", function (e) {
        $.get("/Roku/KeyUp/Right")
        return false;
    });

    controlHammer.on("touch", "#btnDown", function (e) {
        $.get("/Roku/KeyDown/Down")
        return false;
    });

    controlHammer.on("release", "#btnDown", function (e) {
        $.get("/Roku/KeyUp/Down")
        return false;
    });

    controlHammer.on("touch", "#btnPrev", function (e) {
        $.get("/Roku/KeyDown/Rev")
        return false;
    });

    controlHammer.on("release", "#btnPrev", function (e) {
        $.get("/Roku/KeyUp/Rev")
        return false;
    });

    controlHammer.on("touch", "#btnNext", function (e) {
        $.get("/Roku/KeyDown/Fwd")
        return false;
    });

    controlHammer.on("release", "#btnNext", function (e) {
        $.get("/Roku/KeyUp/Fwd")
        return false;
    });

    controlHammer.on("tap", "#btnPlayPause", function (e) {
        $.get("/Roku/KeyPress/Play")
        return false;
    });

    controlHammer.on("tap", "#goRokuText", function (e) {
        var text = document.getElementById("rokuText").value
        $.get("/Roku/SendText?text=" + encodeURIComponent(text))
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
            url: "/Roku/Launch/" + this.id,
            success: function (data) {
                LinkTo("/Roku/Controls")
            },
            cache: false
        });
        return false;
    });
}

$(function () {
    AddControlHammerActions()
    AddBrowserHammerActions();

    $("#goRokuControls").click(function () {
        LinkTo("/Roku/Controls")
    });

    $("#goRokuSelect").click(function () {
        LinkTo("/Roku/Browser")
    });

})
