const cache = new Map();

export function play(url) {
    let audioEl = cache.get(url);

    if (!audioEl) {
        audioEl = new Audio(url);
        audioEl.preload = "auto";
        cache.set(url, audioEl);
        console.log("sound cached:", url);
    }

    audioEl.currentTime = 0;
    audioEl.play().catch(err => console.warn("sound play failed:", err));
}