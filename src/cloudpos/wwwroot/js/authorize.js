var hiddenForm;
var authCodeInput;
var authCodeContainer;
let originalScrollY     = 0;
let keyboardIsVisible   = false;
const RESTORE_DELAY_MS  = 150;

function rememberScroll() {
    if (!keyboardIsVisible) {          
        keyboardIsVisible = true;
        originalScrollY = window.scrollY;
    }
}

function restoreScroll() {
    setTimeout(() => {
        // authCodeContainer 안의 어떤 <input>도 포커스를 갖고 있지 않은 경우 = 키보드 닫힘
        if (
            keyboardIsVisible &&
            !authCodeContainer.contains(document.activeElement)
        ) {
            window.scrollTo({ top: originalScrollY, behavior: "smooth" });
            keyboardIsVisible = false;
        }
    }, RESTORE_DELAY_MS);
}

document.addEventListener("DOMContentLoaded", function(event) {
    hiddenForm = document.getElementById("auth-hidden-form");
    authCodeInput = document.getElementById("auth-hidden-form-authcode");
    authCodeContainer = document.getElementById("authcode-container");

    const inputs = authCodeContainer.querySelectorAll('input[type="text"]');
    inputs.forEach((input, index) => {
        input.addEventListener('focus',  rememberScroll);
        input.addEventListener('blur',   restoreScroll);
        input.addEventListener('input', (e) => {
            input.value = input.value.toUpperCase().replace(/[^A-Z0-9]/, '');

            if (input.value && index < inputs.length - 1) {
                inputs[index + 1].focus();
            }

            if (Array.from(inputs).every(i => i.value)) {
                const code = Array.from(inputs).map(i => i.value).join('');
                authCodeInput.value = code;
                hiddenForm.submit();
            }
        });
        input.addEventListener('keydown', (e) => {
            if (e.key === 'Backspace' && input.value === '' && index > 0) {
                inputs[index - 1].focus();
            }
        });
    });
    
    if (window.visualViewport) {
        let lastHeight = window.visualViewport.height;
        window.visualViewport.addEventListener('resize', () => {
            if (
                keyboardIsVisible &&
                window.visualViewport.height - lastHeight > 100
            ) {
                restoreScroll();
            }
            lastHeight = window.visualViewport.height;
        });
    }
});