var webHammer = null;

function AddWebHammerActions() {
    $(".webPad").each(function () {
        $(this).height($(window).height() - getTop(this))
    });

    if (!webHammer) {
        webHammer = $(".webPad").hammer();
    }
    EnableMouseBehaviour(webHammer)
}

$(function () {
    AddWebHammerActions();
})
