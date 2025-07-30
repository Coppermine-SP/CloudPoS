/*
    client.js - CloudInteractive.CloudPos.Server
    Copyright (C) 2025-2026 Coppermine-SP.
 */

// Blazor connection
Blazor.start({
    reconnectionHandler: {
        onConnectionDown: (options, error) => {
            console.error("Blazor connection down:", error);
            showNotify("서버 연결을 복구하는 중입니다.", 2, Infinity);
            scheduleRetry();
            return true; 
        },
        onConnectionUp: () => 
        {
            console.log("Blazor connection up.");
            showNotify("서버와 연결되었습니다.", 1);
        }
    }
});
window.isPwaDisplayMode = () => {

    const isPwa = window.matchMedia('(display-mode: standalone)').matches ||
                  window.matchMedia('(display-mode: fullscreen)').matches ||
                  window.matchMedia('(display-mode: minimal-ui)').matches;
    const isPwaForIos = window.navigator.standalone === true;
    return isPwa || isPwaForIos;
};

async function scheduleRetry(attempt = 1) {
    if (attempt > 25) {
        showNotify("서버 연결에 실패했습니다. 페이지를 다시 로드하십시오.", 3, Infinity);
        return;
    }
    try {
        const result = await Blazor.reconnect()
        if (!result) location.reload();
    }
    catch(err){
        console.error("Blazor connection failed. Retrying in 2 seconds...", err);
        setTimeout(() => {scheduleRetry(attempt + 1)}, 2000);
    }
}

//ColorScheme
const PREFERRED_COLOR_SCHEME_KEY = "preferredColorScheme";
function getPreferredColorScheme(){
    const value = localStorage.getItem(PREFERRED_COLOR_SCHEME_KEY);

    if(value === "1") return 1;
    else if(value === "2") return 2;
    return 0;
}

function setPreferredColorScheme(scheme){
    localStorage.setItem(PREFERRED_COLOR_SCHEME_KEY, scheme);
}

//Sound
const soundCache = new Map();
function playSound(url) {
    let audioEl = soundCache.get(url);

    if (!audioEl) {
        audioEl = new Audio(url);
        audioEl.preload = "auto";
        soundCache.set(url, audioEl);
        console.log("sound cached:", url);
    }

    audioEl.currentTime = 0;
    audioEl.play().catch(err => console.warn("sound play failed:", err));
}

//NotifyBar
let currentAlert = null;
let zIndexSeed = 800;
const NOTIFY_THEMES = {
    0: { alert: "alert-info",    icon: "bi-info-circle-fill" },
    1: { alert: "alert-success", icon: "bi-check-circle-fill" },
    2: { alert: "alert-warning", icon: "bi-exclamation-triangle-fill" },
    3: { alert: "alert-danger",  icon: "bi-x-circle-fill" }
};

function buildNotify (text, kind) {
    const { alert, icon } = NOTIFY_THEMES[kind] ?? NOTIFY_THEMES.info;
    const el = document.createElement("div");
    el.className = `alert ${alert} shadow notify-bar-item d-flex align-items-center mt-2 mb-1 py-2`;
    el.style.borderRadius = "28px";
    el.innerHTML =
        `<div class="d-flex align-items-center justify-content-center me-3">
             <span class="bi ${icon}"></span>
         </div>
         <div class="flex-fill">${text}</div>`;
    return el;
}

function showNotify(text, kind = 0, ms = 5000) {
    const cont = document.getElementById("notify-bar");
    if(cont == null) return;

    if (currentAlert) hideNotify(currentAlert);

    const alert = buildNotify(text, kind);
    alert.style.zIndex = ++zIndexSeed;
    cont.appendChild(alert);

    requestAnimationFrame(() => alert.classList.add("mb-show"));

    if(ms !== Infinity) {
        alert._timeoutId = setTimeout(() => hideNotify(alert), ms);
    }
    currentAlert = alert;
}

function hideNotify(el) {
    if (!el || el._hiding) return;
    el._hiding = true;
    if (el._timeoutId) clearTimeout(el._timeoutId);
    el.classList.remove("mb-show");

    const removeNow = () => {
        if (currentAlert === el) currentAlert = null;
        el.remove();
    };

    const TRANSITION_FALLBACK = 350; // ms
    const fallbackId = setTimeout(removeNow, TRANSITION_FALLBACK);

    el.addEventListener("transitionend", () => {
        clearTimeout(fallbackId);
        removeNow();
    }, { once:true });
}

function generateQrCode(data){
    let qrcode = new QRCode("qr-code", {
        text: data,
        width: 256,
        height: 256,
        colorDark : "#000000",
        colorLight : "#ffffff",
        correctLevel : QRCode.CorrectLevel.H
    });
}

window.blazorModal = {
    show: (element) => {
        const modal = bootstrap.Modal.getOrCreateInstance(element, {
            backdrop: 'static',
            keyboard: false
        });
        modal.show();
    },
    hide: (element) => {
        const modal = bootstrap.Modal.getOrCreateInstance(element);
        modal.hide();
    }
};