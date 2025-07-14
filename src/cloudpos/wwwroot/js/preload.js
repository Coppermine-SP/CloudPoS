const getColorScheme = () => {
    const x = parseInt(localStorage.getItem("preferredColorScheme"));
    if (isNaN(x) || x > 2 || x < 0) return 0;
    return x;
}

function setColorScheme(x){
    let scheme;
    if(x === 1) scheme = "light";
    else if(x === 2) scheme = "dark";
    else scheme = window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";

    document.documentElement.setAttribute('data-bs-theme', scheme);
}

window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', () => {
    const storedTheme = getColorScheme();
    if (storedTheme === 0) setColorScheme(0);
})
setColorScheme(getColorScheme());

let vh = window.innerHeight * 0.01
document.documentElement.style.setProperty('--vh', `${vh}px`)
window.addEventListener('resize', () => {
    let vh = window.innerHeight * 0.01
    document.documentElement.style.setProperty('--vh', `${vh}px`)
});