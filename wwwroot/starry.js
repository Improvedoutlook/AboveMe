// Starry background JS for AboveMe
// Track last known width to avoid re-rendering on mobile address bar show/hide
window._lastStarryWidth = 0;

window.renderStars = function () {
    const starCount = 120;
    const container = document.getElementById('starry-bg');
    if (!container) return;
    container.innerHTML = '';
    window._lastStarryWidth = window.innerWidth;
    // Use percentage-based positioning so stars stay stable when
    // mobile browsers resize the viewport height (address bar show/hide)
    for (let i = 0; i < starCount; i++) {
        const star = document.createElement('div');
        star.className = 'star';
        const size = Math.random() * 2 + 1;
        star.style.width = `${size}px`;
        star.style.height = `${size}px`;
        star.style.top = `${Math.random() * 100}%`;
        star.style.left = `${Math.random() * 100}%`;
        star.style.opacity = (0.6 + Math.random() * 0.4).toString();
        star.style.animationDuration = `${1.5 + Math.random() * 2}s`;
        container.appendChild(star);
    }
    // Shooting stars logic (moved from starry-shooting-stars.js)
    (function () {
        const STAR_COUNT = 5; // Number of shooting stars at a time
        const MIN_INTERVAL = 2000; // Minimum ms between shooting stars
        const MAX_INTERVAL = 7000; // Maximum ms between shooting stars
        const SHOOTING_STAR_DURATION = 1200; // ms for a shooting star to cross the sky
        const STAR_COLOR = 'rgba(255,255,255,0.85)';
        const STAR_LENGTH = 275; // px (longer)
        const STAR_WIDTH = 1; // px (thinner)
        const STAR_FADE = 0.7; // fade out at end

        let canvas, ctx, width, height;
        let shootingStars = [];

        function randomBetween(a, b) {
            return a + Math.random() * (b - a);
        }

        function createCanvas() {
            if (document.getElementById('shooting-stars-canvas')) return;
            canvas = document.createElement('canvas');
            canvas.id = 'shooting-stars-canvas';
            canvas.style.position = 'fixed';
            canvas.style.top = '0';
            canvas.style.left = '0';
            canvas.style.width = '100vw';
            canvas.style.height = '100vh';
            canvas.style.pointerEvents = 'none';
            canvas.style.zIndex = '1';
            document.body.appendChild(canvas);
            resizeCanvas();
            window.addEventListener('resize', resizeCanvas);
        }

        let lastCanvasWidth = 0;
        function resizeCanvas() {
            const newWidth = window.innerWidth;
            const newHeight = window.innerHeight;
            // Only resize canvas if width changed, to avoid jitter on
            // mobile when address bar shows/hides (height-only change)
            if (canvas && (lastCanvasWidth !== newWidth || !canvas.width)) {
                width = newWidth;
                height = newHeight;
                canvas.width = width;
                canvas.height = height;
                lastCanvasWidth = newWidth;
            } else {
                // Still update the logical height for spawning positions
                height = newHeight;
            }
        }

        function spawnShootingStar() {
            // Appear from random top 1/3 of the sky, random direction
            const startX = randomBetween(width * 0.1, width * 0.9);
            const startY = randomBetween(height * 0.05, height * 0.33);
            const angle = randomBetween(Math.PI * 0.7, Math.PI * 0.95); // mostly left-to-right, slight downward
            const length = randomBetween(STAR_LENGTH * 0.7, STAR_LENGTH * 1.2);
            const duration = randomBetween(SHOOTING_STAR_DURATION * 0.8, SHOOTING_STAR_DURATION * 1.2);
            shootingStars.push({
                startX, startY, angle, length, duration,
                startTime: performance.now(),
                opacity: 0
            });
        }

        function drawShootingStars(now) {
            if (!ctx) return;
            ctx.clearRect(0, 0, width, height);
            shootingStars = shootingStars.filter(star => {
                const elapsed = now - star.startTime;
                if (elapsed > star.duration) return false;
                // Fade in/out
                let fade = 1;
                if (elapsed < 200) fade = elapsed / 200;
                else if (elapsed > star.duration - 400) fade = (star.duration - elapsed) / 400;
                fade = Math.max(0, Math.min(1, fade));
                // Position
                const progress = elapsed / star.duration;
                const x = star.startX + Math.cos(star.angle) * star.length * progress;
                const y = star.startY + Math.sin(star.angle) * star.length * progress;
                ctx.save();
                ctx.globalAlpha = fade * STAR_FADE;
                ctx.strokeStyle = STAR_COLOR;
                ctx.lineWidth = STAR_WIDTH;
                ctx.beginPath();
                ctx.moveTo(x, y);
                ctx.lineTo(x - Math.cos(star.angle) * 30, y - Math.sin(star.angle) * 30);
                ctx.stroke();
                ctx.restore();
                return true;
            });
        }

        function animate(now) {
            drawShootingStars(now);
            requestAnimationFrame(animate);
        }

        function scheduleNextStar() {
            setTimeout(() => {
                if (shootingStars.length < STAR_COUNT) spawnShootingStar();
                scheduleNextStar();
            }, randomBetween(MIN_INTERVAL, MAX_INTERVAL));
        }

        function start() {
            createCanvas();
            canvas = document.getElementById('shooting-stars-canvas');
            if (!canvas) return;
            ctx = canvas.getContext('2d');
            if (!ctx) return;
            requestAnimationFrame(animate);
            scheduleNextStar();
        }

        if (document.readyState === 'complete' || document.readyState === 'interactive') {
            setTimeout(start, 500);
        } else {
            window.addEventListener('DOMContentLoaded', start);
        }
    })();
};

// Only re-render stars when viewport width actually changes.
// Mobile browsers fire resize events when the address bar shows/hides
// (height-only change), which would scatter stars with new random positions.
window.addEventListener('resize', () => {
    if (document.getElementById('starry-bg') && window.innerWidth !== window._lastStarryWidth) {
        window.renderStars();
    }
});
