/**
 * CINEMATIC INTRO CONTROLLER — SABORATOR
 * Manages 60 FPS lifecycle, skip interaction, session storage and accessibility.
 */
(function() {
    'use strict';

    var INTRO_KEY = 'saborator_intro_seen_v1';
    var INTRO_DURATION_MS = 3900;
    var FADE_OUT_DURATION_MS = 650;
    var timerId = null;

    function initIntro() {
        var introEl = document.getElementById('cinematicIntro');
        if (!introEl) return;

        // 1. Accessibility: prefers-reduced-motion check
        var prefersReduced = window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        if (prefersReduced) {
            introEl.classList.add('intro-done');
            return;
        }

        // Reset classes to ensure animation starts fresh on every entry
        introEl.classList.remove('intro-done', 'intro-hiding');
        void introEl.offsetWidth; // force reflow

        // Bind Skip Button
        var skipBtn = document.getElementById('introSkipBtn');
        if (skipBtn) {
            skipBtn.addEventListener('click', function(e) {
                e.preventDefault();
                finishIntro(true);
            });
        }

        // En pantallas táctiles no hay tecla Esc: tocar cualquier parte de la intro la salta
        var isTouch = window.matchMedia && window.matchMedia('(hover: none), (pointer: coarse)').matches;
        if (isTouch) {
            introEl.addEventListener('click', function() { finishIntro(true); }, { once: true });
        }

        // Bind Keyboard: Esc or Space to skip
        function onKeyDown(e) {
            if (e.key === 'Escape' || e.key === ' ' || e.keyCode === 27 || e.keyCode === 32) {
                window.removeEventListener('keydown', onKeyDown);
                finishIntro(true);
            }
        }
        window.addEventListener('keydown', onKeyDown);

        // Schedule natural cinematic exit
        timerId = setTimeout(function() {
            window.removeEventListener('keydown', onKeyDown);
            finishIntro(false);
        }, INTRO_DURATION_MS);
    }

    function finishIntro(isFast) {
        if (timerId) {
            clearTimeout(timerId);
            timerId = null;
        }

        var introEl = document.getElementById('cinematicIntro');
        if (!introEl || introEl.classList.contains('intro-done')) return;

        introEl.classList.add('intro-hiding');

        var exitMs = isFast ? 350 : FADE_OUT_DURATION_MS;
        setTimeout(function() {
            introEl.classList.add('intro-done');
            introEl.classList.remove('intro-hiding');
        }, exitMs);
    }

    // Expose replay function globally for footer or interactive links
    window.replayIntro = function() {
        var introEl = document.getElementById('cinematicIntro');
        if (!introEl) return;

        // Reset classes
        introEl.classList.remove('intro-done', 'intro-hiding');

        // Restart animations by triggering DOM reflow
        void introEl.offsetWidth;

        // Reset timer ring if present
        var ring = introEl.querySelector('.skip-timer-circle');
        if (ring) {
            ring.style.animation = 'none';
            void ring.offsetWidth;
            ring.style.animation = '';
        }

        // Run cycle
        var onKeyDown = function(e) {
            if (e.key === 'Escape' || e.key === ' ') {
                window.removeEventListener('keydown', onKeyDown);
                finishIntro(true);
            }
        };
        window.addEventListener('keydown', onKeyDown);

        timerId = setTimeout(function() {
            window.removeEventListener('keydown', onKeyDown);
            finishIntro(false);
        }, INTRO_DURATION_MS);
    };

    // Auto-initialize when DOM is ready or navigated via bfcache
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initIntro);
    } else {
        initIntro();
    }

    window.addEventListener('pageshow', function(e) {
        if (e.persisted) {
            initIntro();
        }
    });
})();
