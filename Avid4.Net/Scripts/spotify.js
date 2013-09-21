var spotifyHammer = null;

function AddSpotifyHammerActions() {
    $(".spotifyPad").each(function () {
        $(this).height($(window).height() - getTop(this))
    });

    if (!spotifyHammer) {
        spotifyHammer = $(".spotifyPad").hammer();
    }
    EnableMouseBehaviour(spotifyHammer)
}

var PositionMS = 0;
var DurationMS = 0;
var slidingTime = new Date(0);
var posSlider = null;
var lastDisplayUpdate = new Date();
var lastTrackId = null;

function updateSlider() {
    var now = new Date();
    if (now.getTime() - slidingTime.getTime() > 5 * 1000 && DurationMS > 0 && PositionMS <= DurationMS) {
        var sliderValue = Math.round((PositionMS * 200) / DurationMS);
        $("#spotifyPosSlider").val(sliderValue)
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
    var posDisplay = document.getElementById("SpotifyInfoPlayingNowPositionDisplay");
    if (posDisplay != null) {
        $(posDisplay).text(posText + "/" + durText);
    }
}

function UpdateSpotifyDisplayPlayingInformation() {
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
        url: "/Spotify/GetPlayingInfo",
        timeout: 700,
        cache: false,
        success: function (xml) {
            if (xml != null && xml !="") {
                var track = xml.documentElement;
                $("#SpotifyInfoName").text(track.getAttribute("name"));
                $("#SpotifyInfoArtist").text(track.getAttribute("trackArtists"));
                $("#SpotifyInfoAlbum").text(track.getAttribute("album"));
                $("#SpotifyInfoStatus").text(track.getAttribute("status"));
                $("#SpotifyInfoPositionDisplay").text(track.getAttribute("indexDisplay"));
                $("#SpotifyInfoPlayingNowPositionDisplay").text(track.getAttribute("postionDisplay"));

                PositionMS = parseInt(track.getAttribute("position")) * 1000
                DurationMS = parseInt(track.getAttribute("duration")) * 1000
                updateSlider();

                var trackId = parseInt(track.getAttribute("id"))
                if (trackId != lastTrackId)
                {
                    lastTrackId = trackId;

                    $(".spotifySelectedQueueItem").removeClass("spotifySelectedQueueItem")
                    $("#" + trackId + ".spotifyPlaybackQueueItem").each(function () {
                        $(this).addClass("spotifySelectedQueueItem");

                        //  Scroll it into view
                        var topOffset = $(this).offset().top - $(".spotifyPlaybackQueueItems").offset().top
                        $(".spotifyPlaybackQueueItems").animate({
                            scrollTop: topOffset - 50
                        })
                    })

                    var albumId = parseInt(track.getAttribute("albumid"))
                    if (isNaN(albumId))
                    {
                        $("#SpotifyInfoImageURL").attr("src", "/Content/Spotify.png")
                    }
                    else
                    {
                        $("#SpotifyInfoImageURL").attr("src", "/Spotify/GetAlbumImage/" + albumId)
                    }
                }
            }
        },
    });
}

function UpdateQueue(display,id) {
    lastTrackId = null;
    var queueDisplay = document.getElementById("spotifyPlaybackQueueItems");
    if (queueDisplay != null) {
        ReplacePane("spotifyPlaybackQueueItems", "/Spotify/QueuePane", "none",
            function () {
                if (display) {
                    display(id)
                }
            });
    }
    else if (display) {
        LinkTo("/Spotify/Playing");
    }
}

function DisplayBrowserAlbumTracksAppend(id) {
    ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=TracksOnAlbum&append=true&id=" + id, "clear")
}

function DisplayBrowserHome() {
    ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear")
}

function testDisplay(s) {
    $("#display").text(s);
    console.log(s);
}

var controlHammer = null;

function AddControlHammerActions() {
    if (!controlHammer) {
        controlHammer = $(".spotifyPlayback").hammer();
    }

    EnableDragScroll(controlHammer)

    controlHammer.on("touch", "#spotifyPrev", function () {
        $.ajax({
            url: "/Spotify/Back",
            cache: false
        })
    });

    controlHammer.on("touch", "#spotifyPlayPause", function () {
        $.ajax({
            url: "/Spotify/PlayPause",
            cache: false
        })
    });

    controlHammer.on("touch", "#spotifyNext", function () {
        $.ajax({
            url: "/Spotify/Skip",
            cache: false
        })
    });

    controlHammer.on("touch", "#spotifyMinus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Spotify/Minus10",
            success: function (data) {
                PositionMS -= 10000;
                UpdatePositionDisplay();
            },
            cache: false
        })
    });

    controlHammer.on("touch", "#spotifyPlus10", function (e) {
        e.preventDefault();
        $.ajax({
            url: "/Spotify/Plus10",
            success: function (data) {
                PositionMS += 10000;
                UpdatePositionDisplay();
            },
            cache: false
        })
    });

    $("#spotifyPosSlider").noUiSlider({
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
                url: "/Spotify/SetPosition?pos=" + Math.floor(PositionMS/1000),
                cache: false
            })
        }
    });

}

var queueHammer = null;

function AddQueueHammerActions(controlHeight) {
    $("#spotifyPlaybackQueueItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#spotifyPlaybackQueueItems h=" + h + "; t=" + t + "; c=" + controlHeight + " => " + (h - t - controlHeight))
        $(this).height(h - t - controlHeight)
    });

    if (!queueHammer) {
        queueHammer = $(".spotifyPlaybackQueueItems").hammer();
    }

    EnableDragScroll(queueHammer)

    queueHammer.on("tap", ".spotifyPlaybackQueueItem", function (e) {
        $.ajax({
            url: "/Spotify/SkipToQueuedTrack/" + this.id,
            cache: false
        })
        return false;
    });

    queueHammer.on("hold", ".spotifyPlaybackQueueItem", function (e) {
        var browserDisplay = document.getElementById("spotifyBrowserItems");
        if (browserDisplay) {
            ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=TrackInfo&id=" + this.id, "push")
        }
        else {
            LinkTo("/Spotify/Browser?mode=TrackInfo&id=" + this.id);
        }
    });

    queueHammer.on("swipeleft swiperight", ".spotifyPlaybackQueueItem", function (e) {
        $.ajax({
            url: "/Spotify/RemoveQueuedTrack/" + this.id,
            cache: false,
            success: function (data) {
                UpdateQueue(false);
            },
        })
    });

}

var browserHammer = null;

function AddBrowserHammerActions() {
    $("#spotifyBrowserItems").each(function () {
        var h = $(window).height() - 24
        var t = $(this).offset().top
        console.log("#spotifyBrowserItems h=" + h + "; t=" + t + " => " + (h - t))
        $(this).height(h - t)
    });

    if (!browserHammer) {
        browserHammer = $(".spotifyBrowserItems").hammer();
    }

    EnableDragScroll(browserHammer)

    function copyState()
    {
        var state = "";
        $("#ArtistInfoId").each(function () {
            state += "&artistInfoId=" + $(this).text()
        })
        $("#AlbumInfoId").each(function () {
            state += "&albumInfoId=" + $(this).text()
        })
        $("#TrackInfoId").each(function () {
            state += "&trackInfoId=" + $(this).text()
        })
        $("#PlaylistName").each(function () {
            state += "&name=" + encodeURIComponent($(this).text())
        })
        return state;
    }

    browserHammer.on("swiperight swipeleft", function (e) {
        PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
        return false;
    })

    browserHammer.on("tap", "#spotifyBrowserLibraryArtists", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchArtists", "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryAlbums", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchAlbums", "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibrarySearchTracks", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchTracks", "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryPlaylists", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Playlists", "push")
        return false;
    });

    browserHammer.on("tap", "#goSpotifyArtistSearch", function (e) {
        var query = document.getElementById("ArtistSearchText").value
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchArtists&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", "#goSpotifyAlbumSearch", function (e) {
        var query = document.getElementById("AlbumSearchText").value
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchAlbums&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", "#goSpotifyTrackSearch", function (e) {
        var query = document.getElementById("TrackSearchText").value
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchTracks&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserPlaylist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=AlbumsOfPlayist&name=" + encodeURIComponent(this.id), "push")
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserArtist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=AlbumsOfArtist&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserCancel", function (e) {
        PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
        return false;
    });

    browserHammer.on("hold", ".spotifyBrowserArtist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=ArtistInfo&id=" + this.id, "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryArtistBiography", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=ArtistBiography&id=" + $("#ArtistInfoId").text(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryArtistAlbums", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=AlbumsOfArtist&id=" + $("#ArtistInfoId").text(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibrarySimilarArtists", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=GetSimiliarArtists&id=" + $("#ArtistInfoId").text(), "push")
        return false;
    });

    browserHammer.on("doubletap", ".spotifyBrowserAlbum", function (e) {
        $.ajax({
            url: "/Spotify/PlayAlbum?id=" + this.id,
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

    browserHammer.on("hold", ".spotifyBrowserAlbum", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=AlbumInfo&id=" + this.id + copyState(), "push")
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryPlayAlbum", function (e) {
        $.ajax({
            url: "/Spotify/PlayAlbum?id=" + $("#AlbumInfoId").text(),
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

    browserHammer.on("tap", "#spotifyBrowserLibraryAppendAlbum", function (e) {
        $.ajax({
            url: "/Spotify/PlayAlbum?append=true&id=" + $("#AlbumInfoId").text(),
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

    browserHammer.on("tap", "#spotifyBrowserLibraryAlbumTracks", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=TracksOnAlbum&id=" + $("#AlbumInfoId").text() + copyState(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryAlbumArtist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=ArtistInfo&id=" + $("#ArtistInfoId").text(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryAddAlbumToPlaylist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=PlayListsAdd" + copyState(), "none")
        return false;
    });

    browserHammer.on("doubletap", ".spotifyBrowserTrack", function (e) {
        var playButton = $(".playButton:first").text();
        var append = playButton[0] == '+'
        $.ajax({
            url: "/Spotify/PlayTrack?id=" + this.id + (append ? "&append=true" : ""),
            success: function (data) {
                if (!append) {
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

    browserHammer.on("hold", ".spotifyBrowserTrack", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=TrackInfo&id=" + this.id + copyState(), "push")
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryPlayTrack", function (e) {
        var albumId = $("#AlbumInfoId").text()
        $.ajax({
            url: "/Spotify/PlayTrack?id=" + $("#TrackInfoId").text(),
            success: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend, albumId);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend, albumId);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryAppendTrack", function (e) {
        var albumId = $("#AlbumInfoId").text()
        $.ajax({
            url: "/Spotify/PlayTrack?append=true&id=" + $("#TrackInfoId").text(),
            success: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend, albumId);
            },
            error: function (data) {
                UpdateQueue(DisplayBrowserAlbumTracksAppend, albumId);
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryTrackAlbum", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=AlbumInfo&id=" + $("#AlbumInfoId").text(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryTrackArtist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=ArtistInfo&id=" + $("#ArtistInfoId").text(), "push")
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserLibraryAddTrackToPlaylist", function (e) {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=PlayListsAdd" + copyState(), "none")
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserPlaylistAddTrack", function (e) {
        $.ajax({
            url: "/Spotify/AddTrackToPlaylist?id=" + $("#TrackInfoId").text() + "&name=" + this.id,
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserPlaylistAddTrackNew", function (e) {
        var query = document.getElementById("spotifyBrowserPlaylistNewName").value
        $.ajax({
            url: "/Spotify/AddTrackToPlaylist?id=" + $("#TrackInfoId").text() + "&name=" + query,
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserPlaylistRemoveTrack", function (e) {
        $.ajax({
            url: "/Spotify/RemoveTrackFromPlayList?id=" + $("#TrackInfoId").text() + "&name=" + $("#PlaylistName").text(),
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserPlaylistAddAlbum", function (e) {
        $.ajax({
            url: "/Spotify/AddAlbumToPlaylist?id=" + $("#AlbumInfoId").text() + "&name=" + this.id,
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", ".spotifyBrowserPlaylistRemoveAlbum", function (e) {
        $.ajax({
            url: "/Spotify/RemoveAlbumFromPlayList?id=" + $("#AlbumInfoId").text() + "&name=" + $("#PlaylistName").text(),
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });

    browserHammer.on("tap", "#spotifyBrowserPlaylistAddAlbumNew", function (e) {
        var query = document.getElementById("spotifyBrowserPlaylistNewName").value
        $.ajax({
            url: "/Spotify/AddAlbumToPlaylist?id=" + $("#AlbumInfoId").text() + "&name=" + query,
            success: function (data) {
                PopStackedPane("spotifyBrowserItems", function () { ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear") })
            },
            cache: false
        });
        return false;
    });
}

function HandleSearchKeyPresses()
 {
    $("#ArtistSearchText").keypress(function (e) {
         if (e.keyCode == 13)
         {
             var query = document.getElementById("ArtistSearchText").value
             ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchArtists&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
             return false;
         }
     })

     $("#AlbumSearchText").keypress(function (e) {
         if (e.keyCode == 13) {
             var query = document.getElementById("AlbumSearchText").value
             ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchAlbums&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
             return false;
         }
     })

     $("#TrackSearchText").keypress(function (e) {
         if (e.keyCode == 13) {
             var query = document.getElementById("TrackSearchText").value
             ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=SearchTracks&query=" + encodeURIComponent(query), "push", HandleSearchKeyPresses)
             return false;
         }
     })
 }

$(function () {
    var controlHeight = 0;

    $("#spotifyControlPane").each(function () {
        controlHeight = $(this).height();
    })

    $("#goSpotifyPlaying").click(function () {
        LinkTo("/Spotify/Playing")
    });

    $("#goSpotifyQueue").click(function () {
        LinkTo("/Spotify/Queue")
    });

    $("#goSpotifyLibrary").click(function () {
        LinkTo("/Spotify/Browser?mode=Library")
    });

    $("#goSpotifyLibraryPane").click(function () {
        ReplacePane("spotifyBrowserItems", "/Spotify/BrowserPane?mode=Library", "clear")
    });

    AddSpotifyHammerActions();

    AddControlHammerActions()
    AddBrowserHammerActions();
    AddQueueHammerActions(controlHeight)

    // update information once now
    UpdateSpotifyDisplayPlayingInformation();

    // update again every little bit
    setInterval("UpdateSpotifyDisplayPlayingInformation()", 2000);
});