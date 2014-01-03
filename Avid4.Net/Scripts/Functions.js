$.ajaxSetup({
    // Disable caching of AJAX responses */
    cache: false
});


function UpdateVolumeDisplay(displayValue) {
    var volumeDisplay = document.getElementById("volumeDisplay");
    if (volumeDisplay != null) {
        volumeDisplay.innerHTML = displayValue;
    }
}

//  While a player application is still launching and before it has change the view to the 
//  appropriate player view, we overlay a grey mask to indicate that the view is not yet usable
var overlayVisibleForLaunch = false;

function OverlayScreenForLaunch() {
    if (!overlayVisibleForLaunch) {
        overlayVisibleForLaunch = true
        var overlay = document.createElement("div");
        overlay.setAttribute("id", "overlayLaunch");
        overlay.setAttribute("class", "overlay");
        document.body.appendChild(overlay);
    }
}

function RemoveScreenOverlay() {
    if (overlayVisibleForLaunch) {
        overlayVisibleForLaunch = false;
        document.body.removeChild(document.getElementById("overlayLaunch"));
    }
}

//  Cause AVid to launch a player application and afterwards switch the view to the specfied URL
//  During the launch, overlay the screen with a grey mask
function LaunchProgram(application, url) {
    OverlayScreenForLaunch()
    $.ajax({
        url: "/Action/Launch?name=" + application,
        success: function (data) {
            window.location = url;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            RemoveScreenOverlay()
        },
        cache: false
    });
}

//  Cause AVid to launch a new player application (leaving the existing music playing) with specified arguments 
//  and afterwards switch the view to the specfied URL
//  During the launch, overlay the screen with a grey mask
function LaunchNewProgram(application, args, url) {
    OverlayScreenForLaunch()
    $.ajax({
        url: "/Action/Launch?detach=yes&name=" + application + "&args=" + encodeURIComponent(args),
        success: function (data) {
            window.location = url;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            RemoveScreenOverlay()
        },
        cache: false
    });
}

//  Cause AVid to turn on (and switch to) the Sky box and afterwards switch the view to the specfied URL
//  During the launch, overlay the screen with a grey mask
function StartSky(application, url, mode) {
    OverlayScreenForLaunch()
    $.ajax({
        url: "/Action/StartSky?mode=" + mode,
        success: function (data) {
            if (url == null)
            {
                url = data;
            }
            window.location = url;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            RemoveScreenOverlay()
        },
        cache: false
    });
}

//  Switch the entire view to a new URL
function LinkTo(url) {
    window.location = url;
}

//  A stack of pane partial URLs which can be popped (by a side-swip) to return to an ealier pane view
var stackedPaneUrls = [];

//  Replace a pane (an identified <div> within the enire view) with the new partial pane contents of the specfied URL.
//  The replacement can optionally push the URL on the stack to allow it to be popped. Alternatively the stack can be cleared
//  The "onAfter" function can be executed once the view has been replaced
function ReplacePane(paneId, url, stacking, onAfter)
{
    switch (stacking)
    {
        case "none":
            break;
        case "push":
            stackedPaneUrls.push(url);
            break;
        case "clear":
            stackedPaneUrls = [];
            stackedPaneUrls.push(url);
            break;
    }

    console.log("ReplacePane " + url + " [ " + stackedPaneUrls + " ]")

    var pane = document.getElementById(paneId);
    if (pane != null) {
        $.ajax({
            url: url,
            success: function (data) {
                pane.innerHTML = data;
                if (onAfter)
                {
                    onAfter();
                }
            },
            cache: false
        });
    }
}

//  Pop back the contents of the identified pane to an earlier pushed URL
//  The "onAfter" function can be executed once the view has been replaced
function PopStackedPane(paneId, actionIfNothingToPop, onAfter) {
    if (stackedPaneUrls.length > 1)
    {
        stackedPaneUrls.pop()
        ReplacePane(paneId, stackedPaneUrls[stackedPaneUrls.length - 1], "none", onAfter)
    } else if (actionIfNothingToPop)
    {
        actionIfNothingToPop()
    }
}

//  Turn off all player applications and wwitch the entire view to a new URL 
function AllOffJump(url) {
    $.ajax({
        url: "/Action/AllOff",
        success: function (data) {
            window.location = url;
        },
        cache: false
    });
    return false;
}

//  Get the top offset for the specified DOM object (a pane)
function getTop(el) {
    return $(el).offset().top;
}

//  Get the left offset for the specified DOM object (a pane)
function getLeft(el) {
    for (var lx = 0;
            el != null;
            lx += el.offsetLeft, el = el.offsetParent) {
        if (el.leftMargin != undefined) {
            lx += parseInt(el.leftMargin);
        }
    };
    return lx;
}

//  For a Hammer.js object attached to a pane (a <div>) interpret vertical 
//  dragging and swiping as scrolling the contents of that pane with attractve "easing"
function EnableDragScroll(h) {
    var lastY = 0;

    h.on("dragup dragdown", function (e) {
        var g = e.gesture;
        g.preventDefault()
        $(this).stop(true)
        var max = $(this)[0].scrollHeight - $(this).innerHeight();

        var deltaY = Math.round(g.deltaY);
        if (deltaY != lastY)
        {
            var top = parseInt($(this).scrollTop());
            top = top + lastY - deltaY;
            if (top < 0)
            {
                top = 0;
            }
            if (top > max)
            {
                top = max;
            }

            $(this).scrollTop(top);
            lastY = deltaY;
        }
    })

    h.on("swipeup swipedown release", function (e) {
        if (lastY == 0)
        {
            return;
        }

        var g = e.gesture;
        g.preventDefault()
        var max = $(this)[0].scrollHeight - $(this).innerHeight();

        $(this).stop(true)

        if (g.direction == "up" || g.direction == "down")
        {
            var distance = Math.round(400 * g.velocityY);
            var delta = g.direction == "up" ? distance : -distance;
            var duration = 1500;

            var top = parseInt($(this).scrollTop());
            var newTop = top + delta;
            var scrollEasing = 'easeOutCubic';

            if (newTop < 0)
            {
                duration *= (top / distance);
                newTop = 0;
                scrollEasing = 'easeOutBounce';
            }
            if (newTop > max)
            {
                duration *= ((max-top) / distance);
                newTop = max;
                scrollEasing = 'easeOutBounce';
            }

            if (top != newTop)
            {
                $(this).animate({
                    scrollTop: newTop,
                    easing: scrollEasing
                }, duration)
            }
        }
        lastY = 0;
    })
}

//  For a Hammer.js object attached to a pane (a <div>) interpret draging and tapping within the pane as though
//  it were on a touch pad or laptop mouse pad, moving Avid's on-screen cursor and sending mouse click events.
function EnableMouseBehaviour(h) {
    var lastX = 0;
    var lastY = 0;

    h.on("drag", function (e) {
        var g = e.gesture;
        g.preventDefault()

        var deltaX = Math.round(g.deltaX);
        var deltaY = Math.round(g.deltaY);
        console.log(e.type + " " + deltaX + "," + deltaY);
        if (deltaX != lastX || deltaY != lastY) {
            console.log("Mouse move " + (deltaX-lastX) + "," + (deltaY-lastY));
            MouseMove((deltaX - lastX), (deltaY - lastY))
            lastX = deltaX;
            lastY = deltaY;
        }
    })

    h.on("release", function (e) {
        var g = e.gesture;
        g.preventDefault()

        console.log(e.type);
        lastX = 0;
        lastY = 0;
    })

    h.on("tap", function (e) {
        var g = e.gesture;
        g.preventDefault()

        console.log(e.type);
        console.log("Mouse click");
        MouseClick(false);
    })
}

function MouseMove(dx, dy) {
    $.ajax(
        {
            url: "/Action/MouseMove?dx=" + dx + "&dy=" + dy,
            type: 'GET',
            async: false,
            cache: false,
            timeout: 5000
        });
}

function MouseClick(right) {
    $.ajax(
        {
            url: "/Action/MouseClick" + (right ? "?right=true" : ""),
            type: 'GET',
            async: false,
            cache: false,
            timeout: 5000
        });
}

