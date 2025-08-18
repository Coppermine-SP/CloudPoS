// Components/TableObjectManager.razor.js

// ---- Utilities --------------------------------------------------------------
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
    const tableId = parseInt(item?.dataset?.tableId ?? "-1", 10);
    const target = evt.to;

    let x = -1, y = -1;
    let containerType = 'list';

    if (target && target.classList.contains('grid-cell')) {
        x = parseInt(target.dataset.x, 10);
        y = parseInt(target.dataset.y, 10);
        containerType = 'grid';
    }

    dotNetHelper.invokeMethodAsync('UpdateTableState', tableId, x, y, containerType);
}

// ---- Idempotent bindings ----------------------------------------------------
let sortableRegistry = new WeakMap();   // container -> Sortable instance
let globalDragStarted = false;

function createSortable(container, options) {
    if (!container) return;
    if (sortableRegistry.has(container)) return; // already bound
    const sortable = new Sortable(container, options);
    sortableRegistry.set(container, sortable);
}

function destroySortable(container) {
    const existing = sortableRegistry.get(container);
    if (existing) {
        try { existing.destroy(); } catch { /* noop */ }
        sortableRegistry.delete(container);
    }
}

// 전역 dragstart 한 번만 바인딩 (아이템별 리스너 누적 방지)
function ensureGlobalDragImageHandler() {
    if (globalDragStarted) return;
    globalDragStarted = true;

    document.addEventListener('dragstart', function (e) {
        const item = e.target?.closest?.('.table-item');
        if (!item || !e.dataTransfer) return;

        try {
            // 마우스 좌상단에 붙도록
            e.dataTransfer.setDragImage(item, 0, 0);
        } catch {
            // 일부 브라우저/OS에서 제한될 수 있음 → 무시
        }
    }, { capture: true, passive: true });
}

// ---- Public API -------------------------------------------------------------
export async function initializeDragAndDrop(dotNetHelper) {
    try {
        await loadScript('https://cdn.jsdelivr.net/npm/sortablejs@latest/Sortable.min.js');

        const unplacedList = document.getElementById('unplaced-list');
        const gridCells = document.querySelectorAll('.grid-cell');
        const isTouchDevice = 'ontouchstart' in window || navigator.maxTouchPoints > 0;

        if (!unplacedList || gridCells.length === 0) {
            console.error('Drag-and-drop 요소를 찾을 수 없습니다.');
            return;
        }

        ensureGlobalDragImageHandler();

        const baseOptions = {
            animation: 150,
            group: 'tables',
            forceFallback: isTouchDevice,
            fallbackOnBody: true,
            fallbackClass: 'sortable-fallback-custom',
            onEnd: (evt) => {
                // 셀에 2개 이상 들어갔으면 즉시 롤백 (Blazor 강제 재렌더 불필요)
                const to = evt.to;
                if (to && to.classList.contains('grid-cell') && to.children.length > 1) {
                    // 원래 자리로 복구
                    if (evt.from) evt.from.appendChild(evt.item);
                    return;
                }
                notifyBlazor(evt, dotNetHelper);
            }
        };

        // 미배치 리스트 (멱등 바인딩)
        createSortable(unplacedList, {
            ...baseOptions,
            group: { name: 'tables', put: true }
        });

        // 그리드 각 셀 (멱등 바인딩)
        gridCells.forEach(cell => {
            createSortable(cell, {
                ...baseOptions,
                group: {
                    name: 'tables',
                    // 이미 1개 있으면 드롭 불가
                    put: (to) => to.el.children.length === 0
                }
            });
        });

    } catch (err) {
        console.error('SortableJS 초기화 실패:', err);
    }
}

export function disposeDragAndDrop() {
    try {
        const unplacedList = document.getElementById('unplaced-list');
        if (unplacedList) destroySortable(unplacedList);
        document.querySelectorAll('.grid-cell').forEach(destroySortable);
    } catch {
        // ignore
    }
}