let currentAlert = null;
let zIndexSeed = 1000;
const THEMES = {
    success: { alert: "alert-success", icon: "bi-check-circle-fill" },
    info:    { alert: "alert-info",    icon: "bi-info-circle-fill" },
    warning: { alert: "alert-warning", icon: "bi-exclamation-triangle-fill" },
    danger:  { alert: "alert-danger",  icon: "bi-x-circle-fill" }
};

function build (text, kind) {
    const { alert, icon } = THEMES[kind] ?? THEMES.info;
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

export function show(text, kind = "info", ms = 5000) {
    const cont = document.getElementById("notify-bar");
    
    if (currentAlert) hide(currentAlert);

    const alert = build(text, kind);
    alert.style.zIndex = ++zIndexSeed;
    cont.appendChild(alert);
    
    requestAnimationFrame(() => alert.classList.add("mb-show"));
    
    const tId = setTimeout(() => hide(alert), ms);
    alert._timeoutId = tId;
    currentAlert = alert;
}

function hide(el) {
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

export function clear() {
    document.querySelectorAll(".message-bar-item").forEach(hide);
}