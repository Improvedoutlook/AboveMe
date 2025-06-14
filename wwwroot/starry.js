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
    // Shooting stars removed; only sparkling stars remain
};

window.addEventListener('resize', () => {
    if (document.getElementById('starry-bg')) {
        window.renderStars();
    }
});
