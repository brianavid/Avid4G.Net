var mousePadHammer = null;

function AddMousePadHammer() {
    $(".mousePad").each(function () {
        $(this).height($(window).height() - getTop(this))
    });

    if (!mousePadHammer) {
        mousePadHammer = $(".mousePad").hammer();
    }
    EnableMouseBehaviour(mousePadHammer)
}

var mouseEtcButtonsHammer = null;

function AddMouseEtcButtonsHammer() {
    if (!mouseEtcButtonsHammer) {
        mouseEtcButtonsHammer = $(".mouseEtcButtons").hammer();
    }

    mouseEtcButtonsHammer.on("tap", "#mouseEtcEscape", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys={ESC}",
            cache: false
        });
        return false;
    });

    mouseEtcButtonsHammer.on("tap", "#mouseEtcLeft", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys={BS}",
            cache: false
        });
        return false;
    });

    mouseEtcButtonsHammer.on("tap", "#mouseEtcEnter", function (e) {
        $.ajax({
            url: "/Action/SendKeys?keys=~",
            cache: false
        });
        return false;
    });

    mouseEtcButtonsHammer.on("tap", "#mouseEtcEnterText", function (e) {
        var text = document.getElementById("mouseEtcEnteredText").value
        $.ajax({
            url: "/Action/SendKeys?keys="+encodeURIComponent(text),
            cache: false
        });
        return false;
    });

    mouseEtcButtonsHammer.on("tap", "#mouseEtcMenu", function (e) {
        $.ajax({
            url: "/Action/MouseClick?right=yes",
            cache: false
        });
        return false;
    });

}

var panelSwitcher = null;

$(function () {
    AddMousePadHammer()
    AddMouseEtcButtonsHammer()

    $(window).bind('orientationchange', function (event) {
        if (panelSwitcher != null)
            window.location.reload(true);
    });

    $("#volumeUp").mousedown(function () {
        $.ajax({
            url: "/Action/VolumeUp",
            success: function (data) {
                UpdateVolumeDisplay(data);
            },
            cache: false
        });
        return false;
    });

    $("#volumeDown").mousedown(function () {
        $.ajax({
            url: "/Action/VolumeDown",
            success: function (data) {
                UpdateVolumeDisplay(data);
            },
            cache: false
        });
        return false;
    });

    $("#volumeMute").mousedown(function () {
        $.ajax({
            url: "/Action/VolumeMute",
            success: function (data) {
                UpdateVolumeDisplay(data);
            },
            cache: false
        });
        return false;
    });

    $("#goBack").click(function () {
        history.go(-1);
    });

    $("#goHome").click(function () {
        location.href = '/Home/Home';
    });

    $("#goHomeWide").click(function () {
        location.href = '/Home/Wide';
    });

    $("#allOff").click(function () {
        AllOffJump("/Home/Home");
    });

    $("#allOffWide").click(function () {
        AllOffJump("/Home/Wide");
    });

    $("#toggleSettings").click(function () {
        $(".actionMenuOverlay").show()
        $(".actionMenu").show()
        $(".actionMenuSub").hide();
    });

    function HideActionMenu()
    {
        $(".actionMenuOverlay").hide()
        $(".actionMenu").hide()
    }

    $(".actionMenuOverlay").click(function () {
        HideActionMenu()
    });

    $("#actionMenuScreenOn").click(function () {
        $.ajax({
            url: "/Action/ScreenOn",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuVisualOn").click(function () {
        $.ajax({
            url: "/Action/VisualOn",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuLogFire").click(function () {
        $(".actionMenu").hide()
        $.ajax({
            url: "/Action/LogFire",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuScreenOff").click(function () {
        $.ajax({
            url: "/Action/ScreenOff",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuSoundTV").click(function () {
        $(".actionMenuSub").show();
    });

    $(".actionMenuSoundModeItem").click(function () {
        var mode = this.innerText
        $.ajax({
            url: "/Action/SoundTV?mode=" + mode,
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuSoundRooms").click(function () {
        $.ajax({
            url: "/Action/SoundRooms",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuNothingRunning").click(function () {
        $.ajax({
            url: "/Action/AllOff?keep=true",
            success: function (data) {
                location.href = '/Home/Home';
            },
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuNothingRunningWide").click(function () {
        $.ajax({
            url: "/Action/AllOff?keep=true",
            success: function (data) {
                location.href = '/Home/Wide';
            },
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuRebuildMediaDb").click(function () {
        $(".actionMenu").hide()
        $.ajax({
            url: "/Action/RebuildMediaDb",
            success: HideActionMenu,
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuMouseEtc").click(function () {
        location.href = document.getElementById("isWide") != null ? '/Home/MouseEtcWide' : '/Home/MouseEtc';
    });

    $("#actionMenuRecycleApp").click(function () {
        $(".actionMenu").hide()
        $.ajax({
            url: "/Action/RecycleApp",
            success: function (data) {
                location.href = '/Home/Home';
            },
            error: HideActionMenu,
            cache: false
        });
    });

    $("#actionMenuRecycleAppWide").click(function () {
        $(".actionMenu").hide()
        $.ajax({
            url: "/Action/RecycleApp",
            success: function (data) {
                location.href = '/Home/Wide';
            },
            error: HideActionMenu,
            cache: false
        });
    });

    panelSwitcher = setInterval('SwitchPanelAfterWake(' + (document.getElementById("isWide") != null) + ')', 1000);
});


var overlayVisible = false;
var lastWake = new Date();
if (document.referrer == null || document.referrer == "")
{
    lastWake = new Date(0);
}
else
{
    var viewedRunningProgram = $("#topBarTitle").text()
    var currentRunningProgram = $("#homeTitle").text();

    if (viewedRunningProgram != "" &&
        viewedRunningProgram != currentRunningProgram)
    {
        lastWake = new Date(0);
    }
}

function OverlayScreen() {
    if (!overlayVisible) {
        overlayVisible = true
        var overlay = document.createElement("div");
        overlay.setAttribute("id", "overlay");
        overlay.setAttribute("class", "overlay");
        document.body.appendChild(overlay);
    }
}

//  The top bar (and therefore every view) has this handler function (SwitchPanelAfterWake) that determines if 
//  it is being viewed for the first time in over a minute. This case will occur when (for example) a new 
//  controlling device is awoken from its sleeping state. When this case occurs, the view is automatically 
//  switched to a suitable default view for the currently running player application, 
//  as the view displayed when the device last update may no longer be appropriate.
function SwitchPanelAfterWake(isWide) {
    var now = new Date();
    if (!navigator.onLine) {
        OverlayScreen();
        return;
    }
    //$("#homeTitle").text(now.getSeconds() % 2 == 0 ? "Tick": "Tock");
    if (overlayVisible || now.getTime() - lastWake.getTime() > 1 * 60 * 1000) {
        //$("#homeTitle").text("Wait");
        $.ajax({
            type: "GET",
            url: "/Action/GetRunning",
            timeout: 700,
            cache: false,
            success: function (newRunningProgram) {
                lastWake = now;
                //$("#homeTitle").text("OK");
                if (overlayVisible) {
                    overlayVisible = false;
                    document.body.removeChild(document.getElementById("overlay"));
                }
                var lastRunningProgram = $("#topBarTitle").text()
                if (panelSwitcher != null && // in case the response arrives after the switcher has been cancelled
                    lastRunningProgram != newRunningProgram)
                {
                    switch (newRunningProgram) {
                        default:
                            window.location = isWide ? "/Home/Wide" : "/Home/Home";
                            break;
                        case "TV":
                        case "Radio":
                            window.location = isWide ? "/Tv/All" : "/Tv/Watch";
                            break;
                        case "Sky":
                            window.location = isWide ? "/Sky/All" : "/Sky/Watch";
                            break;
                        case "Music":
                            window.location = isWide ? "/Music/All" : "/Music/Playing";
                            break;
                        case "Video":
                            window.location = isWide ? "/Video/All" : "/Video/Watch";
                            break;
                        case "Spotify":
                            window.location = isWide ? "/Spotify/All" : "/Spotify/Playing";
                            break;
                        case "Web":
                            window.location = isWide ? "/Web/All" : "/Web/Mouse";
                            break;
                        case "Photo":
                            window.location = isWide ? "/Photos/All" : "/Photos/Display";
                            break;
                    }
                }
            },
            error: function (jqXHR, textStatus, errorThrown) {
                //$("#homeTitle").text("Error");
                OverlayScreen();
            }
        });
    }
}

function StopSwitching() {
    if (panelSwitcher != null)
    {
        clearInterval(panelSwitcher)
        panelSwitcher = null;
    }
}