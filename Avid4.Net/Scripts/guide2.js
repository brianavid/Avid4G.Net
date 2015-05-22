var browserHammer = null;

var selectedDate = null;
var selectedChannel = null;

function AddBrowserHammerActions() {
    $("#guideBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#guideBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!browserHammer) {
        browserHammer = $(".guideBrowserItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("swiperight swipeleft", function (e) {
        PopStackedPane("guideBrowserItems", function () { ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideRoot", "clear") })
        return false;
    })

    browserHammer.on("doubletap", ".guideTitle", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideRoot", "clear")
        return false;
    });

    browserHammer.on("tap", "#guideFavouritesEpg", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSelectFavouritesEpg", "push")
        return false;
    });

    browserHammer.on("tap", "#guideTvEpg", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSelectTvEpg", "push")
        return false;
    });

    browserHammer.on("tap", "#guideRadioEpg", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSelectRadioEpg", "push")
        return false;
    });

    browserHammer.on("tap", "#guideSchedule", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSchedule", "push")
        return false;
    });

    browserHammer.on("tap", "#guideSeries", function (e) {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSeries", "push")
        return false;
    });

    browserHammer.on("tap", ".guideEpgChannel", function (e) {
        $(".guideEpgSelectedChannel").removeClass("guideEpgSelectedChannel")
        $(this).addClass("guideEpgSelectedChannel");
        selectedChannel = this.id;

        if (selectedDate != null) {
            ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "push")
            selectedDate = null;
            selectedChannel = null;
        }
        return false;
    });

    browserHammer.on("tap", ".guideEpgDate", function (e) {
        $(".guideEpgSelectedDate").removeClass("guideEpgSelectedDate")
        $(this).addClass("guideEpgSelectedDate");
        selectedDate = this.id;

        if (selectedChannel != null) {
            ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "push")
            selectedDate = null;
            selectedChannel = null;
        }
        return false;
    });

    browserHammer.on("tap", ".guideEpgProgramme", function (e) {
        var programItem = this;
        $(".guideEpgProgrammeDescription").remove();
        $(".guideEpgProgrammeCancel").remove();
        $(".guideEpgProgrammeRecord").remove();
        $(".guideEpgProgrammeRecordSeries").remove();

        $.ajax({
            url: "/Guide/Description?id=" + programItem.id,
            success: function (description) {
                if (hasClass(programItem, "guideEpgProgrammeScheduled")) {
                    $(programItem).prepend('<img class="guideEpgProgrammeCancel" id="' + programItem.id + '" src="/Content/Buttons/SmallRound/Exit.png" />')
                } else {
                    $(programItem).prepend('<img class="guideEpgProgrammeRecordSeries" id="' + programItem.id + '" src="/Content/Buttons/SmallRound/Transport.Rec.Series.png" />')
                    $(programItem).prepend('<img class="guideEpgProgrammeRecord" id="' + programItem.id + '" src="/Content/Buttons/SmallRound/Transport.Rec.png" />')
                }
                $(programItem).append('<div class="guideProgrammeInfo guideEpgProgrammeDescription">' + description + '</div>')
                cache: false
            }
        })
    });

    browserHammer.on("tap", ".guideEpgProgrammeRecord", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/Record?id=" + programItem.id,
            success: function (error) {
                if (error == "") {
                    ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSchedule", "none")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    browserHammer.on("tap", ".guideEpgProgrammeRecordSeries", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/RecordSeries?id=" + programItem.id,
            success: function (error) {
                if (error == "") {
                    ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSchedule", "none")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    browserHammer.on("tap", ".guideEpgProgrammeCancel", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/Cancel?id=" + programItem.id,
            success: function (error) {
                if (error == "") {
                    ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideRoot", "clear")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    browserHammer.on("tap", ".guideEpgSeriesCancel", function (e) {
        var seriesItem = this;

        $.ajax({
            url: "/Guide/CancelSeries?id=" + seriesItem.id,
            success: function () {
                ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideSeries", "none")
                cache: false
            }
        })
    });

    $("#remoteHome").click(function () {
        ReplacePane("guideBrowserItems", "/Guide/BrowserPane2?mode=GuideRoot", "clear")
    });

    $("#remoteExit").click(function () {
        close()
    });

}

$(function () {
    AddBrowserHammerActions();
})
