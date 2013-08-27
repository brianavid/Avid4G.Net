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

function LaunchProgram(application, url) {
    $.ajax({
        url: "/Action/Launch?name=" + application,
        success: function (data) {
            window.location = url;
        },
        error: function (xhr, ajaxOptions, thrownError) {
            alert(xhr.status);
            alert(thrownError);
        },
        cache: false
    });
}

function StartSky(application, url, mode) {
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
            alert(xhr.status);
            alert(thrownError);
        },
        cache: false
    });
}

function LaunchProgramWithArgs(application, args, url) {
    $.ajax({
        url: "/Action/Launch?name=" + application + "&args=" + encodeURIComponent(args),
        success: function (data) {
            window.location = url;
        },
        cache: false
    });
}

function LaunchNewProgram(application, args, url) {
    $.ajax({
        url: "/Action/Launch?detach=yes&name=" + application + "&args=" + encodeURIComponent(args),
        success: function (data) {
            window.location = url;
        },
        cache: false
    });
}

function LaunchNewVideo(args, title, url) {
    $.ajax({
        url: "/Action/Launch?name=Video&title=" + encodeURIComponent(title) + "&args=" + encodeURIComponent(args),
        success: function (data) {
            window.location = url;
        },
        cache: false
    });
}

function LaunchNewPhoto(url) {
    $.ajax({
        url: "/Action/Launch?&detach=yes&name=Photo",
        success: function (data) {
            window.location = url;
        },
        cache: false
    });
}

function LinkTo(url) {
    window.location = url;
}

function GoHome() {
    location.href = '/Home/Home';
}

var stackedPaneUrls = [];

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

function getTop(el) {
    return $(el).offset().top;
}

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

function EnableDragScroll(h) {
    var lastY = 0;

    h.on("dragup dragdown", function (e) {
        var g = e.gesture;
        g.preventDefault()
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

        if (g.direction == "up" || g.direction == "down")
        {
            var distance = Math.round(100 * g.velocityY * g.velocityY);
            var delta = g.direction == "up" ? distance : -distance;
            var duration = Math.round(250 * g.velocityY)

            var top = parseInt($(this).scrollTop());
            var newTop = top + delta;
            var scrollEasing = 'easeOutQuad';

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

