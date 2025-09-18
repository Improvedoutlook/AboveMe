// Starry background JS for AboveMe
window.renderStars = function () {
    const starCount = 120;
    const container = document.getElementById('starry-bg');
    if (!container) return;
    container.innerHTML = '';
    const w = window.innerWidth;
    const h = window.innerHeight;
    for (let i = 0; i < starCount; i++) {
        const star = document.createElement('div');
        star.className = 'star';
        const size = Math.random() * 2 + 1;
        star.style.width = `${size}px`;
        star.style.height = `${size}px`;
        star.style.top = `${Math.random() * h}px`;
        star.style.left = `${Math.random() * w}px`;
        star.style.opacity = (0.6 + Math.random() * 0.4).toString();
        star.style.animationDuration = `${1.5 + Math.random() * 2}s`;
        container.appendChild(star);
    }
    // Shooting stars logic (moved from starry-shooting-stars.js)
    (function () {
        const STAR_COUNT = 5; // Number of shooting stars at a time
        const MIN_INTERVAL = 2000; // Minimum ms between shooting stars
        const MAX_INTERVAL = 7000; // Maximum ms between shooting stars
        const SHOOTING_STAR_DURATION = 1800; // ms for a shooting star to cross the sky
        const STAR_COLOR = 'rgba(255,255,255,0.85)';
        const STAR_LENGTH = 220; // px (longer)
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

        function resizeCanvas() {
            width = window.innerWidth;
            height = window.innerHeight;
            if (canvas) {
                canvas.width = width;
                canvas.height = height;
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

window.addEventListener('resize', () => {
    if (document.getElementById('starry-bg')) {
        window.renderStars();
    }
});
