(()=>{
    const zoneId = 'alert-zone';
    let zone = document.getElementById(zoneId);
    if(!zone){
        zone = document.createElement('div');
        zone.id = zoneId;
        document.body.appendChild(zone);
    }
    
    function animateReorder(oldRects){
        Array.from(zone.children).forEach(el=>{
            const first = oldRects.get(el);
            if(!first) return;
            const last = el.getBoundingClientRect();
            const dy = first.top - last.top;
            if(!dy) return;
            el.style.transform = `translateY(${dy}px)`;
            el.getBoundingClientRect();          // re‑flow, reset
            el.style.transition = 'transform .4s ease';
            el.style.transform = '';
            el.addEventListener('transitionend',()=>el.style.transition='',
                {once:true});
        });
    }
    
    function removeCard(card){
        card.classList.add('alert-exit');
        requestAnimationFrame(()=>{
            card.classList.add('alert-exit-active');
            card.classList.remove('alert-exit');
        });
        card.addEventListener('transitionend',()=>card.remove(),{once:true});
    }
    function showAlertCard({
                               theme='warning',
                               title='',
                               html='',
                               icon='bi-info-circle-fill',
                               duration=10000
                           }={})
    {
        const oldRects = new Map(
            Array.from(zone.children).map(el=>[el, el.getBoundingClientRect()])
        );
        
        const themeClass = {
            warning     : 'text-warning',
            call        : 'text-info',
            transaction : 'text-success'
        }[theme] ?? '';
        
        const card = document.createElement("div")
        card.className = "card shadow-sm alert-card rounded-0";
        card.innerHTML =
            `<div class="d-flex flex-column p-3">
                <div class="d-flex align-items-center alert-title order-bottom rounded-1 mb-2 ${themeClass}">
                    <i class="alert-icon ${icon} bi me-2"></i>
                    <p class="alert-title m-0 flex-fill">${title}</p>
                    <button type="button" class="btn-close ms-2" style="font-size: 8px;"></button>
                </div>
                <div class="flex-fill">
                    <div class="alert-content small">${html}</div>
                </div>
            </div>`
        card.querySelector('.btn-close')
            .addEventListener('click',()=>removeCard(card));
        if(duration>0) setTimeout(()=>removeCard(card), duration);

        card.classList.add('alert-enter');
        zone.insertBefore(card, zone.firstChild);  
        requestAnimationFrame(()=>{
            card.classList.add('alert-enter-active');
            card.classList.add('card-flash-animation');
            card.classList.remove('alert-enter');
            
            card.addEventListener('animationend', () => {
                card.classList.remove('card-flash-animation');
                card.style.backgroundColor = 'var(--bs-tertiary-bg)';
            }, { once: true });
        });
        animateReorder(oldRects);
    }
    
    window.showAlertCard = showAlertCard;
})();