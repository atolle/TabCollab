var searchTimeout = undefined;
$(document).on('keyup', '.search-form input', function (e) {
    if (searchTimeout) {
        clearTimeout(searchTimeout);
    }

    var thisForm = $(this);

    searchTimeout = setTimeout(function () {
        $.ajax({
            type: "POST",
            url: "/Friends/Search",
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
    clearSearchSuggestions();
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
        var input = document.createElement("li");
        input.className = "list-group-item";
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
