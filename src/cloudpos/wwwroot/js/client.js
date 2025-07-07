/*
    client.js - CloudInteractive.CloudPos.Server
    Copyright (C) 2025-2026 Coppermine-SP.
 */

// Modal
export function showModal(title, message, showNoBtn = true) {
    return new Promise(resolve => {

        const tmpl     = document.getElementById('modal-template');
        const fragWrap = document.createElement('div');          // 삭제용 래퍼
        fragWrap.appendChild(tmpl.content.cloneNode(true));
        document.body.appendChild(fragWrap);

        const modalEl  = fragWrap.querySelector('.modal');
        const yesBtn   = fragWrap.querySelector('[data-answer="yes"]');
        const noBtn    = fragWrap.querySelector('[data-answer="no"]');

        modalEl.querySelector('.modal-title h5').textContent = title;
        modalEl.querySelector('.modal-body p').innerHTML = message;   // allow <wbr> or other inline HTML
        if (!showNoBtn) noBtn.classList.add('d-none');

        yesBtn.addEventListener('click', () => { resolve(true);  bsModal.hide(); });
        noBtn .addEventListener('click', () => { resolve(false); bsModal.hide(); });

        modalEl.addEventListener('hidden.bs.modal', () => { fragWrap.remove(); });
        const bsModal = new bootstrap.Modal(modalEl, {
            backdrop: 'static',
            keyboard: false
        });
        bsModal.show();
    });
}

//ColorScheme
const PREFERRED_COLOR_SCHEME_KEY = "preferredColorScheme";
export function getPreferredColorScheme(){
    const value = localStorage.getItem(PREFERRED_COLOR_SCHEME_KEY);

    if(value === "1") return 1;
    else if(value === "2") return 2;
    return 0;
}

export function setPreferredColorScheme(scheme){
    localStorage.setItem(PREFERRED_COLOR_SCHEME_KEY, scheme);
}

export function setColorScheme(scheme){
    if(scheme === 0){
        scheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? 2 : 1;
    }

    document.documentElement.setAttribute('data-bs-theme', scheme === 1 ? "light" : "dark");
}

//Sound
const soundCache = new Map();
export function playSound(url) {
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
    0: { alert: "alert-success", icon: "bi-check-circle-fill" },
    1: { alert: "alert-info",    icon: "bi-info-circle-fill" },
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

export function showNotify(text, kind = "info", ms = 5000) {
    const cont = document.getElementById("notify-bar");

    if (currentAlert) hideNotify(currentAlert);

    const alert = buildNotify(text, kind);
    alert.style.zIndex = ++zIndexSeed;
    cont.appendChild(alert);

    requestAnimationFrame(() => alert.classList.add("mb-show"));

    alert._timeoutId = setTimeout(() => hideNotify(alert), ms);
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