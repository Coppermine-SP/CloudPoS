const key = "preferredColorScheme";
export function getPreferredColorScheme(){
    const value = localStorage.getItem(key);

    if(value === "1") return 1;
    else if(value === "2") return 2;
    return 0;
}

export function setPreferredColorScheme(scheme){
    localStorage.setItem(key, scheme);
}

export function setColorScheme(scheme){
    if(scheme === 0){
        scheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? 2 : 1;
    }

    document.documentElement.setAttribute('data-bs-theme', scheme === 1 ? "light" : "dark");
}