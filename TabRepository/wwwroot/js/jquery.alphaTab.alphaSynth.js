(function ($) {
    function loadMidi(element, context, as, score) {
        // invalid score
        if (score == null || !as.Ready) return;

        var midi = AlphaTab.Audio.Generator.MidiFileGenerator.GenerateMidiFile(score);
        element.data('alphaSynthTickCache', midi.TickLookup);
        var ms = new AlphaTab.IO.ByteBuffer();
        midi.WriteTo(ms);
        var bytes = ms.ToArray();
        as.LoadMidiBytes(bytes);
    }

    // extend the api
    var api = $.fn.alphaTab.fn;

    //
    // Plugin 01: Player 
    api.playerInit = function (element, context, args) {
        var as = element.data('alphaSynth');
        if (!as) {
            // initialize alphaSynth
            as = new AlphaSynth.Main.AlphaSynthApi();
            as.On('ready', function (r) {
                as.Ready = r;
                // initial loading 
                if (r) {
                    loadMidi(element, context, as, api.score(element, context));
                }
            });
            element.data('alphaSynth', as);

            element.on('loaded', function () {
                loadMidi(element, context, as, api.score(element, context));
            });
        }
        return as;
    };

    api.player = function (element, context) {
        return element.data('alphaSynth');
    };

    //
    // Plugin 02: Cursors

    var selectionStart = null;
    var selectionEnd = null;
    var selecting = false;

    function getTickCache(element) {
        return element.data('alphaSynthTickCache');
    }
    function getCursorCache(element) {
        return element.data('alphaSynthCursorCache');
    }

    // updates the cursors to highlight the beat at the specified tick position
    api.playerCursorUpdateTick = function (element, context, tick) {
        requestAnimationFrame(function () {
            var cache = getTickCache(element);
            if (cache) {
                var tracks = api.tracks(element, context);
                if (tracks.length > 0) {
                    var beat = cache.FindBeat(tracks, tick);
                    if (beat) {
                        api.playerCursorUpdateBeat(element, context, beat.CurrentBeat, beat.NextBeat, beat.Duration);
                    }
                }
            }
        });
    };

    api.playerCursorSelectRange = function (element, context, startBeat, endBeat) {

        var cache = getCursorCache(element);
        if (!cache) {
            return;
        }

        var selectionWrapper = context.cursorOptions.selectionWrapper;
        selectionWrapper.empty();

        if (startBeat == null || endBeat == null || startBeat.beat == endBeat.beat) {
            return;
        }

        if (!startBeat.bounds) {
            startBeat.bounds = cache.FindBeat(startBeat.beat);
        }

        if (!endBeat.bounds) {
            endBeat.bounds = cache.FindBeat(endBeat.beat);
        }

        var startTick = startBeat.beat.get_AbsoluteStart();
        var endTick = endBeat.beat.get_AbsoluteStart();
        if (endTick < startTick) {
            var t = startBeat;
            startBeat = endBeat;
            endBeat = t;
        }

        var startX = startBeat.bounds.RealBounds.X;
        var endX = endBeat.bounds.RealBounds.X + endBeat.bounds.RealBounds.W;
        if (endBeat.beat.Index == endBeat.beat.Voice.Beats.length - 1) {
            endX = endBeat.bounds.BarBounds.MasterBarBounds.RealBounds.X + endBeat.bounds.BarBounds.MasterBarBounds.RealBounds.W;
        }

        // if the selection goes across multiple staves, we need a special selection highlighting
        if (startBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds != endBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds) {
            // from the startbeat to the end of the staff, 
            // then fill all staffs until the end-beat staff
            // then from staff-start to the end beat (or to end of bar if it's the last beat)

            var staffStartX = startBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds.VisualBounds.X;
            var staffEndX = startBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds.VisualBounds.X + startBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds.VisualBounds.W;

            var startSelection = $('<div></div>').css({
                position: 'absolute',
                top: startBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.Y + 'px',
                left: startX + 'px',
                width: (staffEndX - startX) + 'px',
                height: startBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.H + 'px'
            });
            selectionWrapper.append(startSelection);

            var staffStartIndex = startBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds.Index + 1;
            var staffEndIndex = endBeat.bounds.BarBounds.MasterBarBounds.StaveGroupBounds.Index;
            for (var staffIndex = staffStartIndex; staffIndex < staffEndIndex; staffIndex++) {
                var staffBounds = cache.StaveGroups[staffIndex];

                var middleSelection = $('<div></div>').css({
                    position: 'absolute',
                    top: staffBounds.VisualBounds.Y + 'px',
                    left: staffStartX + 'px',
                    width: (staffEndX - staffStartX) + 'px',
                    height: staffBounds.VisualBounds.H + 'px'
                });
                selectionWrapper.append(middleSelection);
            }

            var endSelection = $('<div></div>').css({
                position: 'absolute',
                top: endBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.Y + 'px',
                left: staffStartX + 'px',
                width: (endX - staffStartX) + 'px',
                height: endBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.H + 'px'
            });
            selectionWrapper.append(endSelection);
        }
        else {
            // if the beats are on the same staff, we simply highlight from the startbeat to endbeat
            var selection = $('<div></div>');
            selection.css({
                position: 'absolute',
                top: startBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.Y + 'px',
                left: startX + 'px',
                width: (endX - startX) + 'px',
                height: startBeat.bounds.BarBounds.MasterBarBounds.VisualBounds.H + 'px'
            });
            selectionWrapper.append(selection);
        }
    };

    // updates the cursors to highlight the specified beat
    api.playerCursorUpdateBeat = function (element, context, beat, nextBeat, duration) {
        if (beat == null) return;

        var cache = getCursorCache(element);
        if (!cache) {
            return;
        }

        var previousBeat = context.cursorOptions.currentBeat;
        var previousCache = context.cursorOptions.cursorCache;
        var previousState = context.cursorOptions.playerState;
        context.cursorOptions.currentBeat = beat;
        context.cursorOptions.cursorCache = cache;
        context.cursorOptions.playerState = playerState;

        if (beat == previousBeat && cache == previousCache && previousState == playerState) {
            return;
        }

        var cursorWrapper = context.cursorOptions.cursors;
        var barCursor = context.cursorOptions.barCursor;
        var beatCursor = context.cursorOptions.beatCursor;

        var beatBoundings = cache.FindBeat(beat);
        if (!beatBoundings) {
            return;
        }

        var barBoundings = beatBoundings.BarBounds.MasterBarBounds;
        barCursor.css({
            top: barBoundings.VisualBounds.Y + 'px',
            left: barBoundings.VisualBounds.X + 'px',
            width: barBoundings.VisualBounds.W + 'px',
            height: barBoundings.VisualBounds.H + 'px'
        });
        beatCursor
            .stop(true, false)
            .css({
                top: barBoundings.VisualBounds.Y + 'px',
                left: (beatBoundings.VisualBounds.X) + 'px',
                width: context.cursorOptions.beatCursorWidth + 'px',
                height: barBoundings.VisualBounds.H + 'px'
            })
        ;

        // if playing, animate the cursor to the next beat
        $('.atHighlight').removeClass('atHighlight');
        if (playerState == 1) {
            $('.b' + beat.Id).addClass('atHighlight');

            var nextBeatX = barBoundings.VisualBounds.X + barBoundings.VisualBounds.W;

            // get position of next beat on same stavegroup
            if (nextBeat) {
                var nextBeatBoundings = cache.FindBeat(nextBeat);
                if (nextBeatBoundings.BarBounds.MasterBarBounds.StaveGroupBounds == barBoundings.StaveGroupBounds) {
                    nextBeatX = nextBeatBoundings.VisualBounds.X;
                }
            }
            beatCursor.animate({
                left: nextBeatX + 'px'
            }, duration, 'linear');

            if (!selecting) {

                // calculate position of whole music wheet within the scroll parent
                var scrollElement = $(context.cursorOptions.scrollElement);
                var scrollElementOffset = scrollElement.offset();
                var elementOffset = element.offset();
                elementOffset = {
                    top: elementOffset.top - scrollElementOffset.top,
                    left: elementOffset.left - scrollElementOffset.left,
                };
                if (context.cursorOptions.autoScroll == 'vertical') {
                    var scrollTop = beatBoundings.RealBounds.Y - elementOffset.top;
                    if (scrollTop != context.cursorOptions.lastScroll) {
                        context.cursorOptions.lastScroll = scrollTop;
                        $(context.cursorOptions.scrollElement).animate({
                            scrollTop: scrollTop + 'px'
                        }, context.cursorOptions.scrollSpeed);
                    }
                }
                else if (context.cursorOptions.autoScroll == 'horizontal-bar') {
                    var padding = beatCursor.offset().left - beatBoundings.VisualBounds.X;
                    if (barBoundings.VisualBounds.X != context.cursorOptions.lastScroll) {
                        var scrollLeft = padding + beatBoundings.RealBounds.X + context.cursorOptions.scrollOffset;
                        context.cursorOptions.lastScroll = barBoundings.VisualBounds.X;
                        $(context.cursorOptions.scrollElement).animate({
                            scrollLeft: scrollLeft + 'px'
                        }, context.cursorOptions.scrollSpeed);
                    }
                }
                else if (context.cursorOptions.autoScroll == 'horizontal-offscreen') {
                    var padding = beatCursor.offset().left - beatBoundings.VisualBounds.X;
                    var elementRight = $(context.cursorOptions.scrollElement).scrollLeft() +
                                       $(context.cursorOptions.scrollElement).width();
                    if ((barBoundings.VisualBounds.X + barBoundings.VisualBounds.W) >= elementRight ||
                         barBoundings.VisualBounds.X < $(context.cursorOptions.scrollElement).scrollLeft()
                    ) {
                        var scrollLeft = padding + beatBoundings.RealBounds.X + context.cursorOptions.scrollOffset;
                        context.cursorOptions.lastScroll = barBoundings.VisualBounds.X;
                        $(context.cursorOptions.scrollElement).animate({
                            scrollLeft: scrollLeft + 'px'
                        }, context.cursorOptions.scrollSpeed);
                    }
                }
            }
        }
    };

    var cursorOptionsDefaults = {
        autoScroll: 'vertical',
        scrollSpeed: 300,
        scrollOffset: 0,
        scrollElement: 'body',
        scrollAdjustment: 0,
        beatCursorWidth: 3,
        handleClick: true
    };


    api.cursorOptions = function (element, context, options) {
        if (options) {
            context.cursorOptions = $.extend(cursorOptionsDefaults, options);
        }
        else {
            return context.cursorOptions;
        }
    };

    api.playerCursor = function (element, context, options) {
        var as = element.data('alphaSynth');
        if (!as) {
            throw new Error('Initialize player with "playerInit" before you init the cursors');
        }

        // prevent double initialization
        if (element.data('alphaSynthCursor')) { return; }
        element.data('alphaSynthCursor', true);

        context.cursorOptions = $.extend(cursorOptionsDefaults, options);

        //
        // Create cursors

        var cursorWrapper = $('<div class="cursors"></div>');
        var selectionWrapper = $('<div class="selectionWrapper"></div>');
        var barCursor = $('<div class="barCursor"></div>');
        var beatCursor = $('<div class="beatCursor"></div>');
        var surface = $('.alphaTabSurface', element);

        // required css styles 
        element.css({ position: 'relative' });
        element.css({ 'text-align': 'left' });
        cursorWrapper.css({ position: 'absolute', "z-index": 1000, display: 'inline', 'pointer-events': 'none' });
        selectionWrapper.css({ position: 'absolute' });
        barCursor.css({ position: 'absolute' });
        beatCursor.css({ position: 'absolute' });

        // store options and created elements for fast access
        context.cursorOptions.cursors = cursorWrapper;
        context.cursorOptions.barCursor = barCursor;
        context.cursorOptions.beatCursor = beatCursor;
        context.cursorOptions.selectionWrapper = selectionWrapper;

        // add cursors to UI
        element.prepend(cursorWrapper);
        cursorWrapper.prepend(barCursor);
        cursorWrapper.prepend(beatCursor);
        cursorWrapper.prepend(selectionWrapper);

        //
        // Hook into events
        var previousTick = 0;
        var playerState = 0;

        // we need to update our position caches if we render a tablature
        element.on('post-rendered', function (e, score) {
            var renderer = api.renderer(element, context);
            element.data('alphaSynthCursorCache', renderer.BoundsLookup);
            api.playerCursorUpdateTick(element, context, previousTick);
            cursorWrapper.css({
                position: 'absolute', "z-index": 1000,
                width: surface.width(), height: surface.height()
            });
        });

        // cursor updating
        as.On('positionChanged', function (currentTime, endTime, currentTick, endTick) {
            previousTick = currentTick;
            console.log(currentTick);
            setTimeout(function () {
                api.playerCursorUpdateTick(element, context, currentTick);
            }, 0); // enqueue cursor update for later to return ExternalInterface call in case of Flash
        });
        as.On('playerStateChanged', function (s) {
            playerState = s;
            setTimeout(function () {
                api.playerCursorUpdateTick(element, context, previousTick);
            }, 0); // enqueue cursor update for later to return ExternalInterface call in case of Flash
        });

        //
        // Click Handling

        if (context.cursorOptions.handleClick) {
            $(context.CanvasElement).on('mousedown', function (e) {
                if (e.which != 1) {
                    return;
                }
                e.preventDefault();

                var parentOffset = $(this).offset();
                var relX = e.pageX - parentOffset.left;
                var relY = e.pageY - parentOffset.top;
                var beat = api.getBeatAtPos(element, context, relX, relY);
                if (beat) {
                    selectionStart = {
                        beat: beat
                    };
                    selectionEnd = null;
                    selecting = true;
                }
            });
            $(context.CanvasElement).on('mousemove', function (e) {
                if (selecting) {
                    var parentOffset = $(this).offset();
                    var relX = e.pageX - parentOffset.left;
                    var relY = e.pageY - parentOffset.top;
                    var beat = api.getBeatAtPos(element, context, relX, relY);
                    if (beat && (selectionEnd == null || selectionEnd.beat != beat)) {
                        selectionEnd = {
                            beat: beat
                        };

                        api.playerCursorSelectRange(element, context, selectionStart, selectionEnd);
                    }
                }
            });
            $(context.CanvasElement).on('mouseup', function (e) {
                e.preventDefault();

                // for the selection ensure start < end
                if (selectionEnd) {
                    var startTick = selectionStart.beat.get_AbsoluteStart();
                    var endTick = selectionEnd.beat.get_AbsoluteStart();
                    if (endTick < startTick) {
                        var t = selectionStart;
                        selectionStart = selectionEnd;
                        selectionEnd = t;
                    }
                }

                // get the start and stop ticks (which consider properly repeats)
                var tickCache = getTickCache(element);
                var realMasterBarStart = tickCache.GetMasterBarStart(selectionStart.beat.Voice.Bar.get_MasterBar());

                // move to selection start
                api.playerCursorUpdateBeat(element, context, selectionStart.beat);
                as.SetPositionTick(realMasterBarStart + selectionStart.beat.Start);

                // set playback range 
                if (selectionEnd && selectionStart.beat != selectionEnd.beat) {
                    var realMasterBarEnd = tickCache.GetMasterBarStart(selectionEnd.beat.Voice.Bar.get_MasterBar());
                    as.SetPlaybackRange(realMasterBarStart + selectionStart.beat.Start, realMasterBarEnd + selectionEnd.beat.Start);
                }
                else {
                    selectionStart = null;
                    as.SetPlaybackRange(-1, -1);
                }
                api.playerCursorSelectRange(element, context, selectionStart, selectionEnd);
                selecting = false;
            });

            element.on('post-rendered', function (e, score) {
                if (selectionStart) {
                    api.playerCursorSelectRange(element, context, selectionStart, selectionEnd);
                }
            });

        }
    }

    api.getBeatAtPos = function (element, context, x, y) {
        var cache = getCursorCache(element);
        return cache.GetBeatAtPos(x, y);
    };


})(jQuery);


