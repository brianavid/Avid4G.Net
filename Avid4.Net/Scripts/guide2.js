var selectorDateHammer = null;
var selectorChannelHammer = null;

var selectedDate = null;
var selectedChannel = null;

function ResetSelectors() {
    $(".guideEpgSelectedChannel").removeClass("guideEpgSelectedChannel")
    $(".guideEpgSelectedDate").removeClass("guideEpgSelectedDate")
    selectedDate = null;
    selectedChannel = null;

}

function ScrollListingsToEnd() {
    $(".guideBrowserItems").each(function () {
        ScrollToEnd($(this))
    })
}

function AddSelectorHammerActions() {
    $("#guideChannels").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".guideChannelsPane h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    $("#guideDates").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".guideDates h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    $(".guideSelectorItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log(".guideSelectorItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    selectorDateHammer = $("#guideDates").hammer({ prevent_default: true });
    selectorChannelHammer = $("#guideChannels").hammer({ prevent_default: true });

    EnableDragScroll(selectorDateHammer)
    EnableDragScroll(selectorChannelHammer)

    selectorChannelHammer.on("tap", ".guideEpgChannel", function (e) {
        $(".guideEpgSelectedChannel").removeClass("guideEpgSelectedChannel")
        $(this).addClass("guideEpgSelectedChannel");
        selectedChannel = this.id;

        if (selectedDate != null) {
            $(".guideOverlayListings").show()
            $(".guideOverlaySelectors").html("")
            ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "clear", ScrollListingsToEnd)
        }
        return false;
    });

    selectorDateHammer.on("tap", ".guideEpgDate", function (e) {
        $(".guideEpgSelectedDate").removeClass("guideEpgSelectedDate")
        $(this).addClass("guideEpgSelectedDate");
        selectedDate = this.id;

        if (selectedChannel != null) {
            $(".guideOverlayListings").show()
            $(".guideOverlaySelectors").html("")
            ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideProgrammes&date=" + selectedDate + "&channel=" + selectedChannel, "clear", ScrollListingsToEnd)
        }
        return false;
    });
}

var listingsHammer = null;

function AddListingsHammerActions() {
    $("#guideBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#guideBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!listingsHammer) {
        listingsHammer = $(".guideBrowserItems").hammer({ prevent_default: true });
    }

    EnableDragScroll(listingsHammer)

    listingsHammer.on("tap", ".guideEpgProgramme", function (e) {
        var programItem = this;
        $(".guideEpgProgrammeDescription").remove();
        $(".guideEpgProgrammeCancel").remove();
        $(".guideEpgProgrammeRecord").remove();
        $(".guideEpgProgrammeRecordSeries").remove();

        $.ajax({
            url: "/Guide/Description?id=" + programItem.id + "&channelName=" + $("#ChannelName").text(),
            success: function (description) {
                if (!hasClass(programItem, "guideEpgProgrammeScheduled")) {
                    $(programItem).prepend('<img class="guideEpgProgrammeRecordSeries" id="' + programItem.id + '" src="/Content/Buttons/SmallRound/Transport.Rec.Series.png" />')
                    $(programItem).prepend('<img class="guideEpgProgrammeRecord" id="' + programItem.id + '" src="/Content/Buttons/SmallRound/Transport.Rec.png" />')
                }
                $(programItem).append('<div class="guideProgrammeInfo guideEpgProgrammeDescription">' + description + '</div>')
                cache: false
            }
        })
    });

    listingsHammer.on("tap", ".guideEpgProgrammeRecord", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/Record?id=" + programItem.id + "&channelName=" + $("#ChannelName").text(),
            success: function (error) {
                if (error == "") {
                    $(".guideSelectorItems").html("")
                    ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSchedule", "none")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    listingsHammer.on("tap", ".guideEpgProgrammeRecordSeries", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/RecordSeries?id=" + programItem.id + "&channelName=" + $("#ChannelName").text(),
            success: function (error) {
                if (error == "") {
                    $(".guideSelectorItems").html("")
                    ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSchedule", "none")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    listingsHammer.on("tap", ".guideScheduledRecording", function (e) {
        $(".guideEpgProgrammeCancel").addClass("startHidden");
        $(this).children("img").removeClass("startHidden");

        return false;
    });

    listingsHammer.on("tap", ".guideSeriesDefinitions", function (e) {
        $(".guideEpgSeriesCancel").addClass("startHidden");
        $(this).children("img").removeClass("startHidden");

        return false;
    });

    listingsHammer.on("tap", ".guideEpgProgrammeCancel", function (e) {
        var programItem = this;

        $.ajax({
            url: "/Guide/Cancel?id=" + programItem.id,
            success: function (error) {
                if (error == "") {
                    $(".guideSelectorItems").html("")
                    ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSchedule", "none")
                }
                else {
                    alert(error)
                }
                cache: false
            }
        })
    });

    listingsHammer.on("tap", ".guideEpgSeriesCancel", function (e) {
        var seriesItem = this;

        $.ajax({
            url: "/Guide/CancelSeries?id=" + seriesItem.id,
            success: function () {
                $(".guideSelectorItems").html("")
                ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSeries", "none")
                cache: false
            }
        })
    });

}

$(function () {

    $("#guideTvEpg").click(function () {
        ResetSelectors()
        $(".guideOverlayListings").hide()
        $(".guideBrowserItems").html("")
        ReplacePane("guideSelectorItems", "/Guide/SelectorPane?mode=GuideSelectTvEpg", "clear", AddSelectorHammerActions)
    });

    $("#guideRadioEpg").click(function () {
        ResetSelectors()
        $(".guideOverlayListings").hide()
        $(".guideBrowserItems").html("")
        ReplacePane("guideSelectorItems", "/Guide/SelectorPane?mode=GuideSelectRadioEpg", "clear", AddSelectorHammerActions)
    });

    $("#guideSchedule").click(function () {
        $(".guideOverlayListings").show()
        $(".guideSelectorItems").html("")
        ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSchedule", "clear")
    });

    $("#guideSeries").click(function () {
        $(".guideOverlayListings").show()
        $(".guideSelectorItems").html("")
        ReplacePane("guideBrowserItems", "/Guide/ListingsPane?mode=GuideSeries", "clear")
    });

    AddListingsHammerActions();

    StopSwitching()
})
