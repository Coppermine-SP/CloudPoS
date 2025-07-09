export function initCategoryScroller(wrapperId) {
    const wrapper   = document.getElementById(wrapperId);
    if (!wrapper) return;

    const scroller  = wrapper.querySelector('#category-container');
    const arrowL    = wrapper.querySelector('#cat-arrow-left');
    const arrowR    = wrapper.querySelector('#cat-arrow-right');
    const fadeL     = wrapper.querySelector('#cat-fade-left');
    const fadeR     = wrapper.querySelector('#cat-fade-right');
    
    const refresh = () => {
        const max = scroller.scrollWidth - scroller.clientWidth;
        const atStart = scroller.scrollLeft <= 0;
        const atEnd   = scroller.scrollLeft >= max - 1;      // 오차 보정

        [arrowL, fadeL].forEach(el => el.classList.toggle('d-none', atStart));
        [arrowR, fadeR].forEach(el => el.classList.toggle('d-none', atEnd  ));
    };

    /* --- 초기 상태 계산 --- */
    refresh();

    /* --- 스크롤/리사이즈 시 업데이트 --- */
    scroller.addEventListener('scroll', refresh);
    window.addEventListener('resize', refresh);

    /* --- 화살표 클릭 → 150px씩 부드럽게 이동 --- */
    arrowL.addEventListener('click', () => scroller.scrollBy({ left: -150, behavior: 'smooth' }));
    arrowR.addEventListener('click', () => scroller.scrollBy({ left:  150, behavior: 'smooth' }));
    enableDragScroll(scroller);
}

function enableDragScroll(scroller){
    let isDown=false, startX=0, scrollX=0, moved=false, raf=0;

    /* (A) 포인터타입이 'mouse' 또는 'pen'인 경우에만 드래그‑스크롤 기능 사용, 
       단, .category-item에서 발생한 클릭은 무시하여 Blazor가 처리하게 한다 */
    scroller.addEventListener('pointerdown', e => {
        // Only start a drag if the pointer is a mouse/pen **and**
        // the original element isn't a category button itself.
        if (e.pointerType !== 'mouse' && e.pointerType !== 'pen') return;
        if (e.target.closest('.category-item')) return;   // allow normal click‑selection

        isDown  = true;
        startX  = e.clientX;
        scrollX = scroller.scrollLeft;
        scroller.classList.add('dragging');
        scroller.setPointerCapture(e.pointerId);
    });

    scroller.addEventListener('pointermove', e => {
        if (!isDown) return;

        const dx = e.clientX - startX;
        if (Math.abs(dx) > 5) moved = true;

        /* (B) requestAnimationFrame 한정으로 좌표 갱신 – 과도한 reflow 방지 */
        if (!raf){
            raf = requestAnimationFrame(() => {
                scroller.scrollLeft = scrollX - dx;
                raf = 0;
            });
        }
    });

    const stop = e => {
        if (!isDown) return;
        isDown = false;
        scroller.classList.remove('dragging');
        scroller.releasePointerCapture(e.pointerId);
        if (moved) { e.preventDefault(); e.stopPropagation(); moved=false; }
    };
    scroller.addEventListener('pointerup',     stop);
    scroller.addEventListener('pointercancel', stop);
    scroller.addEventListener('wheel', e => {
        if (Math.abs(e.deltaX) < Math.abs(e.deltaY)) {
            scroller.scrollBy({ left: e.deltaY, behavior: 'auto' });
            e.preventDefault();
        }
    }, { passive:false });
}

export function scrollToCategory(wrapperId, categoryId) {
    const wrapper  = document.getElementById(wrapperId);
    if (!wrapper) return;

    const scroller = wrapper.querySelector('#category-container');
    const target   = scroller.querySelector(`[data-cat-id="${categoryId}"]`);
    if (!target) return;

    /* 가운데쯤으로 오도록 스크롤 */
    target.scrollIntoView({ behavior: 'smooth', block: 'nearest', inline: 'center' });
}