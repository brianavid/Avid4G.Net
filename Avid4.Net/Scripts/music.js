var last_track = 9999;

var PositionMS = 0;
var DurationMS = 0;
var slidingTime = new Date(0);
var posSlider = null;
var lastDisplayUpdate = new Date();

function updateSlider() {
    var now = new Date();
    if (now.getTime() - slidingTime.getTime() > 5 * 1000 && DurationMS > 0 && PositionMS <= DurationMS) {
        var sliderValue = Math.round((PositionMS * 200) / DurationMS);
        $("#musicPosSlider").val(sliderValue)
    }
}

function UpdateJrmcDisplayPlayingInformation() {
    var now = new Date();
    if (now.getTime() - lastDisplayUpdate.getTime() > 10 * 1000) {
        lastDisplayUpdate = now;
        return;
    }

    lastDisplayUpdate = now;
    if (overlayVisible || !navigator.onLine) {
        return;
    }

    $.ajax({
        type: "GET",
        url: "/Music/GetPlayingInfo",
        timeout: 700,
        cache: false,
        success: function (xml) {
            if (xml != null) {
                var root = xml.getElementsByTagName("Response");

                // see if response is valid
                if ((root != null) && (root.length == 1) && (xml.documentElement.getAttribute("Status") == "OK")) {

                    // get all items
                    var items = xml.getElementsByTagName("Item");
                    if (items != null) {

                        // loop items
                        for (var i = 0; i < items.length; i++) {

                            // parse values
                            var name = items[i].getAttribute("Name");
                            var value = items[i].childNodes[0].nodeValue;

                            // get corresponding element
                            var element = document.getElementById("PlaybackInfo." + name);
                            if (element != null) {

                                // update element
                                if ((element.src != null) && (element.src != ""))
                                    element.src = JrmcHost + value; // image
                                else
                                    element.innerHTML = value; // text
                            }

                            if (name == "PlayingNowPosition") {
                                if (last_track != value) {
                                    var track = document.getElementById("Track_" + last_track);
                                    if (track != null) {
                                        track.style.backgroundColor = "#FFFFFF";
                                    }
                                    track = document.getElementById("Track_" + value);
                                    if (track != null) {
                                        track.style.backgroundColor = "#DDDDDD";
                                    }
                                    if (last_track == 9999) {
                                        window.location.hash = "#Track_" + value;
                                        window.scrollTo(0, window.window.pageYOffset - 100);
                                    }
                                    last_track = value;
                                }
                            }

                            if (name == "PositionMS") {
                                PositionMS = parseInt(value);
                                updateSlider();
                            }

                            if (name == "DurationMS") {
                                DurationMS = parseInt(value);
                                updateSlider();
                            }
                        }
                    }
                }
            }
        },
    });
}

function UpdateQueue(jump)
{
    last_track = 9999;
    var queueDisplay = document.getElementById("musicPlaybackQueueItems");
    if (queueDisplay != null) {
        ReplacePane("musicPlaybackQueueItems", "/Music/QueuePane", "none",
            function () {
                if (jump)
                {
                    ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Library", "clear")
                }
            });
    }
    else if (jump) {
        LinkTo("/Music/Playing");
    }
}

function testDisplay(s) {
    $("#display").text(s);
    console.log(s);
}

var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".musicPlayback").hammer();
    }

    EnableDragScroll(controlHammer)

    controlHammer.on("tap", "#musicPrev", function () {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Previous"),
            cache: false
        })
    });

    controlHammer.on("tap", "#musicPlayPause", function () {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayPause"),
            cache: false
        })
    });

    controlHammer.on("tap", "#musicNext", function () {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Next"),
            cache: false
        })
    });

    controlHammer.on("tap", "#musicMinus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Position?Position=10000&Relative=-1"),
            cache: false
        })
    });

    controlHammer.on("tap", "#musicPlus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Position?Position=10000&Relative=1"),
            cache: false
        })
    });

    $("#musicPosSlider").noUiSlider({
        range: [0, 200]
        , start: 0
        , step: 1
        , handles: 1
        , slide: function () {
            slidingTime = new Date();
            var pos = Math.floor($(this).val());
            PositionMS = pos * DurationMS / 200;
            var secs = Math.floor(PositionMS / 1000);
            var mins = Math.floor(secs / 60);
            secs = secs % 60;
            var posText = mins + ":" + (secs < 10 ? "0" : "") + secs;
            secs = Math.floor(DurationMS / 1000);
            mins = Math.floor(secs / 60);
            secs = secs % 60;
            var durText = mins + ":" + (secs < 10 ? "0" : "") + secs;
            var posDisplay = document.getElementById("PlaybackInfo.PositionDisplay");
            if (posDisplay != null)
            {
                $(posDisplay).text(posText + " / " + durText);
            }

            $.ajax({
                url: "/Music/SendMCWS?url=" + escape("Playback/Position?Position=" + PositionMS),
                cache: false
            })
        }
    });

}

var queueHammer = null;

function AddQueueHammerActions(controlHeight) {
    $("#musicPlaybackQueueItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#musicPlaybackQueueItems h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });

    if (!queueHammer) {
        queueHammer = $(".musicPlaybackQueueItems").hammer();
    }

    EnableDragScroll(queueHammer)

    queueHammer.on("tap", ".musicPlaybackQueueItem", function (e) {
        var index = this.id.substring(6);
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByIndex?Index=" + index),
            cache: false
        })
        return false;
    });

}

var browserHammer = null;

function AddBrowserHammerActions() {
    $("#musicBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#musicBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h-t)
    });

    if (!browserHammer) {
        browserHammer = $(".musicBrowserItems").hammer();
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("swiperight", function (e) {
        PopStackedPane("musicBrowserItems", function () { ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Library", "clear") })
        return false;
    })

    browserHammer.on("pinchin", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Library", "clear")
        return false;
    })

    browserHammer.on("tap", "#musicBrowserLibraryArtists", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=ArtistInitials", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryAlbums", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=AlbumInitials", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryComposers", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Composers", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryPlaylists", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Playlists", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryLuckyDip", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=LuckyDip", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibrarySearch", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Search", "push")
        return false;
    });

    browserHammer.on("tap", "#goMusicSearch", function (e) {
        var query = document.getElementById("SearchText").value
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Search&query=" + encodeURIComponent(query), "push")
        return false;
    });

    browserHammer.on("doubletap", ".musicBrowserPlaylist", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playlist/Files?Action=Play&Playlist=" + this.id),
            success: function (data) {
                UpdateQueue(playNow);
            },
            error: function (data) {
                UpdateQueue(playNow);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", ".musicBrowserComposer", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=AlbumsOfComposer&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserArtistsInitial", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=ArtistsOfInitial&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserAlbumInitial", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=AlbumsOfInitial&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserArtist", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=AlbumsOfArtist&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("swipeleft", ".musicBrowserAlbum", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Tracks&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("swiperight", ".musicBrowserSearchTrack", function (e) {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=AlbumsOfTrack&id=" + this.id, "clear")
        return false;
    });

    browserHammer.on("hold", ".musicBrowserAlbum, .musicBrowserTrack", function (e) {
        $(".playButton").text("+ ")
    });

    browserHammer.on("doubletap", ".musicBrowserAlbum", function (e) {
        var playButton = $(".playButton:first").text();
        var url = "Playback/PlayByKey?Album=1&Key=";
        var playNow = playButton[0] == '>'
        if (!playNow)
        {
            url = "Playback/PlayByKey?Album=1&Location=End&Key="
        }

        $.ajax({
            url: "/Music/SendMCWS?url=" + escape(url + this.id),
            success: function (data) {
                UpdateQueue(playNow);
            },
            error: function (data) {
                UpdateQueue(playNow);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("doubletap", ".musicBrowserTrack", function (e) {
        var playButton = $(".playButton:first").text();
        var url = "Playback/PlayByKey?Key=";
        var playNow = playButton[0] == '>'
        if (!playNow) {
            url = "Playback/PlayByKey?Location=End&Key="
        }

        $.ajax({
            url: "/Music/SendMCWS?url=" + escape(url + this.id),
            success: function (data) {
                if (playNow) {
                    $(".playButton").text("+ ")
                }
                UpdateQueue(false);
            },
            error: function (data) {
                UpdateQueue(false);
            },
            cache: false
        });
        return false;
    });

}

$(function () {
    var controlHeight = 0;

    $("#musicControlPane").each(function () {
        controlHeight = $(this).height();
    })

    $("#goMusicPlaying").click(function () {
        LinkTo("/Music/Playing")
    });

    $("#goMusicQueue").click(function () {
        LinkTo("/Music/Queue")
    });

    $("#goMusicLibrary").click(function () {
        LinkTo("/Music/Browser?mode=Library")
    });

    $("#goMusicLibraryPane").click(function () {
        ReplacePane("musicBrowserItems", "/Music/BrowserPane?mode=Library", "clear")
    });

    AddControlHammerActions()
    AddBrowserHammerActions();
    AddQueueHammerActions(controlHeight)

    // update information once now
    UpdateJrmcDisplayPlayingInformation();

    // update again every little bit
    jrmcRepeater = setInterval("UpdateJrmcDisplayPlayingInformation()", 2000);
});