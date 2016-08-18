last_track = -1;

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

function UpdatePositionDisplay() {
    var secs = Math.floor(PositionMS / 1000);
    var mins = Math.floor(secs / 60);
    secs = secs % 60;
    var posText = mins + ":" + (secs < 10 ? "0" : "") + secs;
    secs = Math.floor(DurationMS / 1000);
    mins = Math.floor(secs / 60);
    secs = secs % 60;
    var durText = mins + ":" + (secs < 10 ? "0" : "") + secs;
    var posDisplay = document.getElementById("PlaybackInfo.PositionDisplay");
    if (posDisplay != null) {
        $(posDisplay).text(posText + " / " + durText);
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
                            var value = items[i].childNodes.length == 0 ? "" : items[i].childNodes[0].nodeValue;

                            // get corresponding element
                            var element = document.getElementById("PlaybackInfo." + name);
                            if (element != null) {

                                // update element
                                if ((element.src != null) && (element.src != ""))
                                {
                                    if (value[0] == '/')
                                    {
                                        element.src = value; // image
                                    }
                                    else
                                    {
                                        element.src = JrmcHost + value; // image
                                    }
                                }
                                else
                                {
                                    element.innerHTML = value; // text
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

                            if (name == "FileKey") {
                                if (last_track != value) {
                                    var trackFoundInQueue = false;

                                    last_track = value;
                                    $(".musicSelectedQueueItem").removeClass("musicSelectedQueueItem")
                                    $("#" + value + ".musicPlaybackQueueItem").each(function () {
                                        $(this).addClass("musicSelectedQueueItem");

                                        trackFoundInQueue = true;

                                        //  Scroll it into view
                                        var topOffset = $(this).offset().top - $(".musicPlaybackQueueItems").offset().top
                                        $(".musicPlaybackQueueItems").animate({
                                            scrollTop: topOffset - 50
                                        })
                                    })

                                    if (!trackFoundInQueue) {
                                        UpdateQueue(false)
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    });
}

function UpdateQueue(display, id)
{
    last_track = -1;
    var queueDisplay = document.getElementById("musicPlaybackQueueItems");
    if (queueDisplay != null) {
        ReplacePane("musicPlaybackQueueItems", "/Music/QueuePane", "none",
            function () {
                if (display) {
                    display(id)
                }
            });
    }
    else if (display) {
        LinkTo("/Music/Playing");
    }
}

function UpdateSearchTextVisibility()
{
    var isSearch = false
    $("#musicSearchResults").each(function () {
        isSearch = true
    });

    if (isSearch)
    {
        $(".musicBrowserSearchEntry").show()
    }
    else
    {
        $(".musicBrowserSearchEntry").hide()
    }
}

function ReplaceBrowserPane(url, stacking)
{
    ReplacePane("musicBrowserItems", url, stacking, UpdateSearchTextVisibility)
}

function DisplayBrowserAlbumTracksAppend(id) {
    ReplaceBrowserPane("/Music/BrowserPane?mode=AllTracksOnAlbum&append=true&id=" + id, "clear")
}

function DisplayBrowserHome() {
    ReplaceBrowserPane("/Music/BrowserPane?mode=Library", "clear")
}

function testDisplay(s) {
    $("#display").text(s);
    console.log(s);
}

var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".musicPlayback").hammer({ prevent_default: true });
    }

    EnableDragScroll(controlHammer)

    controlHammer.on("touch", "#musicPrev", function () {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Previous"),
            cache: false
        })
    });

    controlHammer.on("touch", "#musicPlayPause", function () {
        var statusElement = document.getElementById("PlaybackInfo.Status");
        var status = statusElement.innerText;
        var playCommand = status == "Stopped" ? "Playback/PlayByIndex?Index=0" : "Playback/PlayPause";
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape(playCommand),
            cache: false
        })
    });

    controlHammer.on("touch", "#musicNext", function () {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Next"),
            cache: false
        })
    });

    controlHammer.on("touch", "#musicMinus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Position?Position=10000&Relative=-1"),
            success: function (data) {
                PositionMS -= 10000;
                UpdatePositionDisplay();
            },
            cache: false
        })
    });

    controlHammer.on("touch", "#musicPlus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/Position?Position=10000&Relative=1"),
            success: function (data) {
                PositionMS += 10000;
                UpdatePositionDisplay();
            },
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
            UpdatePositionDisplay();
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
        queueHammer = $(".musicPlaybackQueueItems").hammer({ prevent_default: true, holdThreshold: 100 });
    }

    EnableDragScroll(queueHammer)

    queueHammer.on("tap", ".musicPlaybackQueueItem", function (e) {

        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByIndex?Index=" + $(".musicPlaybackQueueItem").index(this)),
            cache: false
        })
        return false;
    });

    queueHammer.on("hold", ".musicPlaybackQueueItem", function (e) {
        e.gesture.stopDetect()
        var browserDisplay = document.getElementById("musicBrowserItems");
        if (browserDisplay) {
            ReplaceBrowserPane("/Music/BrowserPane?mode=TrackInfo&id=" + this.id, "push")
        }
        else {
            LinkTo("/Music/Browser?mode=TrackInfo&id=" + this.id);
        }
    });

    queueHammer.on("swipeleft swiperight", ".musicPlaybackQueueItem", function (e) {
        $.ajax({
            url: "/Music/RemoveQueuedTrack/" + this.id,
            cache: false,
            success: function (data) {
                UpdateQueue(false);
            }
        })
    });

}

var browserHammer = null;

var selectedDate = null;
var selectedStation = null;

function AddBrowserHammerActions() {
    $("#musicBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#musicBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h-t)
    });

    if (!browserHammer) {
        browserHammer = $(".musicBrowserItems").hammer({ prevent_default: true, holdThreshold: 100 });
    }

    EnableDragScroll(browserHammer)

    browserHammer.on("swiperight swipeleft", function (e) {
        PopStackedPane("musicBrowserItems", function () { ReplaceBrowserPane("/Music/BrowserPane?mode=Library", "clear") }, UpdateSearchTextVisibility)
        return false;
    })

    browserHammer.on("tap", "#musicBrowserLibraryArtists", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=ArtistInitials", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryAlbums", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumInitials", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryComposers", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=Composers", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryPlaylists", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=Playlists", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryLuckyDip", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=LuckyDip", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryRecentAlbums", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=RecentAlbums", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibrarySearch", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=Search", "push")
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryListenAgain", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=ListenAgainSelect", "push")
        return false;
    });

    browserHammer.on("doubletap", ".musicBrowserPlaylist", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playlist/Files?Action=Play&Playlist=" + this.id),
            success: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", ".musicBrowserComposer", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumsOfComposer&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserArtistsInitial", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=ArtistsOfInitial&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserAlbumInitial", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumsOfInitial&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserArtist", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumsOfArtist&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("hold", ".musicBrowserAlbum", function (e) {
        e.gesture.stopDetect()
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumInfo&id=" + this.id, "push")
    });

    browserHammer.on("hold", ".musicBrowserTrack", function (e) {
        e.gesture.stopDetect()
        ReplaceBrowserPane("/Music/BrowserPane?mode=TrackInfo&id=" + this.id, "push")
    });

    browserHammer.on("tap", ".musicBrowserCancel", function (e) {
        PopStackedPane("musicBrowserItems", function () { ReplaceBrowserPane("/Music/BrowserPane?mode=Library", "clear") })
        return false;
    });

    browserHammer.on("doubletap", ".musicBrowserAlbum", function (e) {
        var playButton = $(".playButton:first").text();
        var url = "Playback/PlayByKey?Album=1&Key=";
        var playNow = playButton[0] == '>'
        if (!playNow) {
            url = "Playback/PlayByKey?Album=1&Location=End&Key="
        }

        $.ajax({
            url: "/Music/SendMCWS?url=" + escape(url + this.id),
            success: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryPlayAlbum", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByKey?Album=1&Key=" + $("#TrackInfoId").text()),
            success: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryAppendAlbum", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByKey?Album=1&Location=End&Key=" + $("#TrackInfoId").text()),
            success: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryAlbumTracks", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=Tracks&id=" + $("#TrackInfoId").text(), "clear")
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

    browserHammer.on("tap", "#musicBrowserLibraryPlayTrack", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByKey?Key=" + $("#TrackInfoId").text()),
            success: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryAppendTrack", function (e) {
        $.ajax({
            url: "/Music/SendMCWS?url=" + escape("Playback/PlayByKey?&Location=End&Key=" + $("#TrackInfoId").text()),
            success: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#musicBrowserLibraryTrackAlbum", function (e) {
        ReplaceBrowserPane("/Music/BrowserPane?mode=AlbumInfo&id=" + $("#AlbumInfoId").text(), "clear")
        return false;
    });

    browserHammer.on("tap", ".musicBrowserListenAgainStation", function (e) {
        $(".musicBrowserListenAgainSelectedStation").removeClass("musicBrowserListenAgainSelectedStation")
        $(this).addClass("musicBrowserListenAgainSelectedStation");
        selectedStation = this.id;

        if (selectedDate != null) {
            ReplaceBrowserPane("/Music/BrowserPane?mode=ListenAgainProgrammes&date=" + selectedDate + "&station=" + selectedStation, "push")
            selectedDate = null;
            selectedStation = null;
        }
        return false;
    });

    browserHammer.on("tap", ".musicBrowserListenAgainDate", function (e) {
        $(".musicBrowserListenAgainSelectedDate").removeClass("musicBrowserListenAgainSelectedDate")
        $(this).addClass("musicBrowserListenAgainSelectedDate");
        selectedDate = this.id;

        if (selectedStation != null) {
            ReplaceBrowserPane("/Music/BrowserPane?mode=ListenAgainProgrammes&date=" + selectedDate + "&station=" + selectedStation, "push")
            selectedDate = null;
            selectedStation = null;
        }
        return false;
    });

    browserHammer.on("doubletap", ".musicBrowserListenAgainProgramme", function (e) {
        $.ajax({
            url: "/Music/PlayListenAgain?pid=" + this.id,
            success: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserHome);
            },
            cache: false
        });
        return false;
    });
}

var searchHammer = null;

function AddSearchHammerActions() {
    if (!searchHammer) {
        searchHammer = $(".musicBrowserSearchEntry").hammer();
    }
    searchHammer.on("tap", "#goMusicSearch", function (e) {
        var query = document.getElementById("SearchText").value
        ReplaceBrowserPane("/Music/BrowserPane?mode=Search&query=" + encodeURIComponent(query), "push")
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
        ReplaceBrowserPane("/Music/BrowserPane?mode=Library", "clear")
    });

    AddControlHammerActions()
    AddBrowserHammerActions();
    AddSearchHammerActions();
    AddQueueHammerActions(controlHeight)

    // update information once now
    UpdateJrmcDisplayPlayingInformation();

    // update again every little bit
    jrmcRepeater = setInterval("UpdateJrmcDisplayPlayingInformation()", 2000);
});