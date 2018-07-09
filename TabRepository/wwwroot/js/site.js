var searchTimeout = undefined;
var bodyClick = false;
var notificationCount = 0;

$(document).on('keyup', '.search-form input', function (e) {
    if (searchTimeout) {
        clearTimeout(searchTimeout);
    }

    var thisForm = $(this);

    searchTimeout = setTimeout(function () {
        $.ajax({
            type: "POST",
            url: "/Friends/FuzzySearch",
            data: { searchString: thisForm[0].value },
            success: function (result) {
                addSearchSuggestions(result);
            },
            error: function (error) {
                console.log(error);
            }
        });
    }, 500);
});

$(document).on('blur', '.search-form input', function (e) {
    setTimeout(function () {
        clearSearchSuggestions();
    }, 500);
});

$(document).on('click', '.search-btn', function (e) {
    $(this).closest('form').submit();
});

function clearSearchSuggestions() {
    var container = document.getElementsByClassName("search-results");
    container = container[0];

    while (container.hasChildNodes()) {
        container.removeChild(container.lastChild);
    }
}

function addSearchSuggestions(suggestions) {
    var container = document.getElementsByClassName("search-results");
    container = container[0];

    clearSearchSuggestions();

    for (var i = 0; i < suggestions.length; i++) {
        var input = document.createElement("a");
        input.className = "btn list-group-item";
        input.setAttribute('href', '/Friends/Search?search=' + suggestions[i]);
        input.innerHTML = suggestions[i];
        input.name = suggestions[i];
        container.appendChild(input);
    }
}

// Sets active class on navbar links based on selected page
$(document).ready(function () {
    $('li.active').removeClass('active');
    $('a[href="' + location.pathname + '"]').closest('li').addClass('active');
});

$(document).ready(GetNotificationPanel());

function GetNotificationPanel() {
    $.ajax({
        url: '/Notifications/GetNotificationPanel',
        type: 'GET',
    })
        .then(function (data) {
            notificationCount = data.count;
            if (data.count > 0) {
                if ($('.notifications').is(':hidden')) {
                    $('.notifications-btn').css("color", "red");
                }
                else {
                    $('.notifications-btn').css("color", "gray");
                }
                $('.notifications').html(data.html);
            }
            else {
                $('.notifications-btn').css("color", "gray");
                $('.notifications').hide();
            }
        })
        .fail(function (error) {

        })
};

$(document).on('click', '.notifications-btn', function (e) {
    if (notificationCount > 0) {
        $('.notifications').toggle();
        if ($('.notifications').is(':hidden')) {
            $('.notifications-btn').css("color", "red");
        }
        else {
            $('.notifications-btn').css("color", "gray");
        }
    }
});

$(document.body).click(function (e) {
    if (notificationCount > 0) {        
        if (e.target != $('.notifications-btn')[0] && !$(e.target).hasClass('.notification-delete-btn') && !$(e.target).hasClass('.notification-delete-all-btn')) {
            $('.notifications').hide();
        }
        if ($('.notifications').is(':hidden')) {
            $('.notifications-btn').css("color", "red");
        }
        else {
            $('.notifications-btn').css("color", "gray");
        }
    }
});

$(document).on('click', '.notification-delete-btn', function (e) {
    e.preventDefault();

    var notificationId = { notificationId: $(this).attr('data-notification-id') };

    $.ajax({
        url: '/Notifications/DeleteNotificationForUser',
        type: 'POST',
        data: notificationId,
        dataType: 'json'
    })
    .then(function () {        
        GetNotificationPanel();
    })
    .fail(function (error) {

    })
});

$(document).on('click', '.notification-delete-all-btn', function (e) {
    e.preventDefault();

    $.ajax({
        url: '/Notifications/DeleteAllNotificationsForUser',
        type: 'POST'
    })
    .then(function () {
        GetNotificationPanel();
    })
    .fail(function (error) {

    })
});