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
        input.setAttribute('href', '/Friends/Search?searchString=' + suggestions[i].username + '&exact=true');
        input.innerHTML = suggestions[i].username;
        input.name = suggestions[i].username;
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
                    $('.notifications-btn').css("color", "rgb(134,192,144)");
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
            $('.notifications-btn').css("color", "rgb(134,192,144)");
        }
        else {
            $('.notifications-btn').css("color", "gray");
        }
    }
});

$(document.body).click(function (e) {
    if (notificationCount > 0) {
        if (e.target !== $('.notifications-btn')[0] && !$(e.target).hasClass('notification-read-btn') && !$(e.target).hasClass('.notification-read-all-btn')) {
            $('.notifications').hide();
        }
        if ($('.notifications').is(':hidden')) {
            $('.notifications-btn').css("color", "rgb(134,192,144)");
        }
        else {
            $('.notifications-btn').css("color", "gray");
        }
    }
});

$(document).on('click', '.notification-read-btn', function (e) {
    e.preventDefault();

    var notificationId = { notificationId: $(this).attr('data-notification-id') };

    $.ajax({
        url: '/Notifications/ReadNotification',
        type: 'POST',
        data: notificationId,
        dataType: 'json'
    }).then(function () {
        GetNotificationPanel();
    }).fail(function (error) {

    });
});

$(document).on('click', '.notification-read-all-btn', function (e) {
    e.preventDefault();

    $.ajax({
        url: '/Notifications/ReadAllNotifications',
        type: 'POST'
    })
    .then(function () {
        GetNotificationPanel();
    })
    .fail(function (error) {

    });
});

function showFailureBootbox(action, error) {
    bootbox.dialog({
        message: "An error occurred while " + action + ".<br /><br />Error: " + error,
        buttons: {
            confirm: {
                label: 'Close',
                className: 'btn-default'
            },
        }
    });
}

let deferredPrompt,
    $btnInstallClass = $('.btn-install'),
    $btnInstall = $('#btn-install'),
    $bannerInstall = $('#banner-install');

window.addEventListener('beforeinstallprompt', (e) => {
    // Prevent Chrome 67 and earlier from automatically showing the prompt
    e.preventDefault();
    // Stash the event so it can be triggered later.
    deferredPrompt = e;

    // Only show install button/banner on touch screens
    if ('ontouchstart' in window) {
        $btnInstall.show();
        $bannerInstall.show();
    }
});

$btnInstallClass.on('click', (e) => {
    // hide our user interface that shows our A2HS button
    $btnInstall.hide();
    $bannerInstall.hide();
    // Show the prompt
    deferredPrompt.prompt();
    // Wait for the user to respond to the prompt
    deferredPrompt.userChoice
        .then((choiceResult) => {
            if (choiceResult.outcome === 'accepted') {
                console.log('User accepted the A2HS prompt');
            } else {
                console.log('User dismissed the A2HS prompt');
            }
            deferredPrompt = null;
        });
});

if ('serviceWorker' in navigator) {
    window.addEventListener('load', function () {
        navigator.serviceWorker.register('/sw.js').then(function (registration) {
            // Registration was successful
            console.log('ServiceWorker registration successful with scope: ', registration.scope);
        }, function (err) {
            // registration failed :(
            console.log('ServiceWorker registration failed: ', err);
        });
    });
}

function imageUpload() {
    var $uploadCrop;

    // Make sure we don't already have a Croppie instantiated
    $('#image-upload').croppie('destroy');

    function readFile(input) {
        // Make sure the image size is less than 1MB
        if (input.files.length) {
            if (input.files[0].size > 1000000) {
                $("#image-error").html("Image size limit is 1 MB");
                return;
            }
            else {
                $("#image-error").html("");
                $('#image-upload-container').show();
            }
        }
        else {
            $("#image-error").html("");
            $('#image-upload-conatiner').hide();
        }

        if (input.files && input.files[0]) {
            var reader = new FileReader();

            reader.onload = function (e) {
                $('.image-upload').addClass('ready');
                $uploadCrop.croppie('bind', {
                    url: e.target.result
                }).then(function () {
                    console.log('jQuery bind complete');
                });

            };

            reader.readAsDataURL(input.files[0]);
        }
        else {
            swal("Sorry - you're browser doesn't support the FileReader API");
        }
    }    
    
    $('#Image').on('change', function () {
        if (!this.files.length) {
            $('#image-upload').croppie('destroy');
            return;
        }

        readFile(this);

        // Initialize Croppie
        $uploadCrop = $('#image-upload').croppie({
            viewport: {
                width: 230,
                height: 230,
                type: 'square'
            },
            enableExif: true,
            enableOrientation: true
        });

        // Add rotate and delete buttons after Croppie is initialized, which dynamically adds elements to the DOM
        $('.cr-slider-wrap').append('<div id="image-buttons"><a id="image-rotate" data-deg="90" data-toggle="tooltip" title="Rotate Image" class="white-icon"><i class="fa fa-undo fa-lg" /></a><a id="image-delete" data-deg="90" data-toggle="tooltip" title="Rotate Image" class="white-icon"><i class="fa fa-times fa-lg" /></a></div>');
        
        $('#image-rotate').on('click', function (ev) {
            $uploadCrop.croppie('rotate', parseInt($(this).data('deg')));
        });

        $('#image-delete').on('click', function (ev) {
            $('#Image').val('');
            $('#image-upload').croppie('destroy');
        });
    });
}