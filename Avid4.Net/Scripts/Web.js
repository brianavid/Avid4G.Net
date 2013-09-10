var webPadHammer = null;

function AddWebPadHammerActions() {
    $(".webPad").each(function () {
        $(this).height($(window).height() - getTop(this))
    });

    if (!webPadHammer) {
        webPadHammer = $(".webPad").hammer();
    }
    EnableMouseBehaviour(webPadHammer)
}

var browserHammer = null;

var selectedDate = null;
var selectedChannel = null;

function AddBrowserHammerActions() {
    $("#webBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#webBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!browserHammer) {
        browserHammer = $(".webBrowserItems").hammer();
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("swiperight swipeleft", function (e) {
        PopStackedPane("webBrowserItems", function () { ReplacePane("webBrowserItems", "/Web/BrowserPane?mode=iPlayerSelect", "clear") })
        return false;
    })

    browserHammer.on("tap", ".webBrowserChannel", function (e) {
        $(".webBrowserSelectedChannel").removeClass("webBrowserSelectedChannel")
        $(this).addClass("webBrowserSelectedChannel");
        selectedChannel = this.id;

        if (selectedDate != null) {
            ReplacePane("webBrowserItems", "/Web/BrowserPane?mode=iPlayerProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "push")
            selectedDate = null;
            selectedChannel = null;
        }
        return false;
    });

    browserHammer.on("tap", ".webBrowserDate", function (e) {
        $(".webBrowserSelectedDate").removeClass("webBrowserSelectedDate")
        $(this).addClass("webBrowserSelectedDate");
        selectedDate = this.id;

        if (selectedChannel != null) {
            ReplacePane("webBrowserItems", "/Web/BrowserPane?mode=iPlayerProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "push")
            selectedDate = null;
            selectedChannel = null;
        }
        return false;
    });

    browserHammer.on("doubletap", ".webBrowserProgramme", function (e) {
        $.ajax({
            url: "/Web/PlayBBC?pid=" + this.id,
            success: function (data) {
                var webPad = document.getElementById("webPad");
                if (webPad != null) {
                    ReplacePane("webBrowserItems", "/Web/BrowserPane?mode=iPlayerSelect", "clear");
                }
                else {
                    LinkTo("/Web/Mouse");
                }
            },
            cache: false
        });
        return false;
    });

}

$(function () {
    AddWebPadHammerActions();
    AddBrowserHammerActions();
})
