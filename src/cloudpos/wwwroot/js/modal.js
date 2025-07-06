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