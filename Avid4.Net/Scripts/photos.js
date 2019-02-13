var touchRepeater = null;
var touchStartTime = null;
var touchStartTicks = 0;

function DoJrmcAction(action)
{
        $.ajax({
            url: "/Music/SendMCWS?url=" + action,
            cache: false
        })

}

function DoRepeatedArrows(mcc) {
    var nowTime = new Date();
    var nowTicks = nowTime.getTime();
    if (nowTicks - touchStartTicks > 200) {
        DoJrmcAction("Control/MCC?Command=" + mcc, true);
    }
}

function StartRepeat(mcc) {
    DoJrmcAction("Control/MCC?Command=" + mcc, true);
    touchStartTime = new Date();
    touchStartTicks = touchStartTime.getTime();
    touchRepeater = setInterval("DoRepeatedArrows('" + mcc + "')", 50);
}

function StopRepeat() {
    if (touchRepeater != null) {
        clearInterval(touchRepeater)
        touchRepeater = null;
    }
}

var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".photosDisplay").hammer({ prevent_default: true });
    }

    controlHammer.on("touch", "#btnUp", function (e) {
        StartRepeat("28023")
        return false;
    });

    controlHammer.on("touch", "#btnLeft", function (e) {
        StartRepeat("28025")
        return false;
    });

    controlHammer.on("touch", "#btnRight", function (e) {
        StartRepeat("28026")
        return false;
    });

    controlHammer.on("touch", "#btnDown", function (e) {
        StartRepeat("28024")
        return false;
    });

    controlHammer.on("release", function (e) {
        StopRepeat();
        return false;
    });

    controlHammer.on("touch", "#btnPlus", function (e) {
        DoJrmcAction("Control/MCC?Command=28000", true);
        return false;
    });

    controlHammer.on("touch", "#btnMinus", function (e) {
        DoJrmcAction("Control/MCC?Command=28001", true);
        return false;
    });

    controlHammer.on("touch", "#btnPrev", function (e) {
        DoJrmcAction("Playback/Previous", true);
        return false;
    });

    controlHammer.on("touch", "#btnNext", function (e) {
        DoJrmcAction("Playback/Next", true);
        return false;
    });

    controlHammer.on("touch", "#btnPlayPause", function (e) {
        DoJrmcAction("Playback/PlayPause", true);
        return false;
    });

}

var imagesHammer = null;

function AddImagesHammerActions(controlHeight) {
    $(".photosImagesGrid").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#photosImagesGrid h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });

    if (!imagesHammer) {
        imagesHammer = $(".photosImagesGrid").hammer({ prevent_default: true });
    }

    EnableDragScroll(imagesHammer)

    imagesHammer.on("doubletap", ".photoGridImage", function (e) {
        var url = "Playback/PlayByKey?Album=1&Key=";

        $.ajax({
            url: "/Music/SendMCWS?url=" + encodeURIComponent("Playback/PlayByIndex?Index=" + $(".photoGridImage").index(this)),
            cache: false
        });
        return false;
    });
}

function UpdateImages() {
    var gridDisplay = document.getElementById("photosImagesGrid");
    if (gridDisplay != null) {
        ReplacePane("photosImagesGrid", "/Photos/ImagesPane", "none");
    }
    else {
        LinkTo("/Photos/Display");
    }
}

var browserHammer = null;

function AddBrowserHammerActions() {
    $("#photosBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#photosBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!browserHammer) {
        browserHammer = $(".photosBrowserItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("doubletap", ".photosBrowserAlbum", function (e) {
        var url = "Playback/PlayByKey?Album=1&Key=";

        $.ajax({
            url: "/Music/SendMCWS?url=" + encodeURIComponent(url + this.id),
            success: function (data) {
                UpdateImages();
            },
            error: function (data) {
                UpdateImages();
            },
            cache: false
        });
        return false;
    });
}

$(function () {
    var controlHeight = 0;

    $("#photosDisplayPane").each(function () {
        controlHeight = $(this).height();
    })

    AddControlHammerActions()
    AddImagesHammerActions(controlHeight);
    AddBrowserHammerActions();

    $("#goPhotosDisplay").click(function () {
        LinkTo("/Photos/Display")
    });

    $("#goPhotosImages").click(function () {
        LinkTo("/Photos/Images")
    });

    $("#goPhotosSelect").click(function () {
        LinkTo("/Photos/Browse")
    });

})
