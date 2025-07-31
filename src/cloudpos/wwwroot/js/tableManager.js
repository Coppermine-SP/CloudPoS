function loadScript(url) {
    return new Promise((resolve, reject) => {
        if (window.Sortable) return resolve();

        const script = document.createElement('script');
        script.src = url;
        script.onload = resolve;
        script.onerror = () => reject(new Error(`Failed to load script: ${url}`));
        document.head.appendChild(script);
    });
}

function notifyBlazor(evt, dotNetHelper) {
    const item = evt.item;
    const tableId = parseInt(item.dataset.tableId);
    const target = evt.to;

    let x = -1, y = -1;
    let containerType = 'list';

    if (target.classList.contains('grid-cell')) {
        x = parseInt(target.dataset.x);
        y = parseInt(target.dataset.y);
        containerType = 'grid';
    }

    dotNetHelper.invokeMethodAsync('UpdateTableState', tableId, x, y, containerType);
}

function bindDragImageHandlers(containerSelector) {
    const items = document.querySelectorAll(`${containerSelector} .table-item`);
    items.forEach(item => {
        item.addEventListener('dragstart', function (e) {
            // 현재 요소 기준 좌상단이 마우스에 붙도록 함
            e.dataTransfer.setDragImage(e.target, 0, 0);
        });
    });
}

function createSortable(container, options) {
    if (!container) return;
    new Sortable(container, options);
}

export async function initializeDragAndDrop(dotNetHelper) {
    try {
        await loadScript('https://cdn.jsdelivr.net/npm/sortablejs@latest/Sortable.min.js');

        const unplacedList = document.getElementById('unplaced-list');
        const gridCells = document.querySelectorAll('.grid-cell');

        if (!unplacedList || gridCells.length === 0) {
            console.error('Drag-and-drop 요소를 찾을 수 없습니다.');
            return;
        }

        const baseOptions = {
            animation: 150,
            group: 'tables',
            forceFallback: true,
            fallbackOnBody: true,
            fallbackClass: 'sortable-fallback-custom',
            onEnd: (evt) => notifyBlazor(evt, dotNetHelper)
        };

        // 미배치 리스트
        createSortable(unplacedList, {
            ...baseOptions,
            group: { name: 'tables', put: true }
        });

        // 그리드 셀
        gridCells.forEach(cell => {
            createSortable(cell, {
                ...baseOptions,
                group: {
                    name: 'tables',
                    put: (to) => to.el.children.length === 0
                }
            });
        });

        // ✅ 고스트 위치 정확히 붙이기
        bindDragImageHandlers('#unplaced-list');
        bindDragImageHandlers('.grid-cell');

    } catch (err) {
        console.error('SortableJS 초기화 실패:', err);
    }
}