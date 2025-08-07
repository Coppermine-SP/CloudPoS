export function showOffcanvas(elementId) {
    const toggleButton = document.getElementById('offcanvas-toggle-button');

    if (toggleButton && toggleButton.offsetParent !== null) {
        const offcanvasElement = document.getElementById(elementId);
        if (offcanvasElement) {
            const bsOffcanvas = bootstrap.Offcanvas.getOrCreateInstance(offcanvasElement);
            bsOffcanvas.show();
        }
    }
}