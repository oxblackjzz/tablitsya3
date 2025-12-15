// Drag & Drop functionality using SortableJS
// https://sortablejs.github.io/Sortable/

window.DragDropInterop = {
    sortableInstances: {},

    // Ініціалізує Sortable для таблиці замовлень
    initSortable: function (elementId, dotNetHelper, workshopNumber) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.warn('Element not found:', elementId);
            return false;
        }

        // Знищуємо попередній інстанс якщо є
        if (this.sortableInstances[elementId]) {
            this.sortableInstances[elementId].destroy();
            delete this.sortableInstances[elementId];
        }

        try {
            this.sortableInstances[elementId] = new Sortable(el, {
                animation: 150,
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                handle: '.drag-handle', // Тільки за іконку можна тягнути
                filter: '.no-drag, input, button, select, a, .btn',
                preventOnFilter: false, // Дозволяємо click на фільтрованих елементах
                forceFallback: true, // Краще працює з Blazor
                
                onStart: function (evt) {
                    document.body.classList.add('is-dragging');
                    console.log('Drag started:', evt.oldIndex);
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    console.log('Drag ended:', evt.oldIndex, '->', evt.newIndex);
                    
                    if (evt.oldIndex !== evt.newIndex) {
                        // ВАЖЛИВО: Скасовуємо DOM зміни - повертаємо елемент на місце
                        // Blazor сам оновить DOM після StateHasChanged
                        const item = evt.item;
                        const parent = evt.from;
                        
                        if (evt.oldIndex < evt.newIndex) {
                            // Елемент був переміщений вниз - повертаємо його перед елементом на старій позиції
                            const refNode = parent.children[evt.oldIndex];
                            if (refNode) {
                                parent.insertBefore(item, refNode);
                            }
                        } else {
                            // Елемент був переміщений вгору
                            const refNode = parent.children[evt.oldIndex + 1];
                            if (refNode) {
                                parent.insertBefore(item, refNode);
                            } else {
                                parent.appendChild(item);
                            }
                        }
                        
                        // Тепер викликаємо .NET метод
                        dotNetHelper.invokeMethodAsync('OnOrderReordered', 
                            workshopNumber,
                            evt.oldIndex, 
                            evt.newIndex
                        ).then(() => {
                            console.log('OnOrderReordered called successfully');
                        }).catch(err => {
                            console.error('Error calling OnOrderReordered:', err);
                        });
                    }
                }
            });
            
            console.log('✅ Sortable initialized for:', elementId, 'workshop:', workshopNumber);
            return true;
        } catch (err) {
            console.error('❌ Error initializing Sortable:', err);
            return false;
        }
    },

    // Знищує Sortable інстанс
    destroySortable: function (elementId) {
        if (this.sortableInstances[elementId]) {
            this.sortableInstances[elementId].destroy();
            delete this.sortableInstances[elementId];
            console.log('Sortable destroyed for:', elementId);
        }
    },

    // Показує toast повідомлення
    showToast: function (message, type) {
        // Створюємо контейнер якщо немає
        let container = document.getElementById('toast-container');
        if (!container) {
            container = document.createElement('div');
            container.id = 'toast-container';
            container.className = 'toast-container position-fixed top-0 end-0 p-3';
            container.style.zIndex = '9999';
            document.body.appendChild(container);
        }

        // Створюємо toast
        const toastId = 'toast-' + Date.now();
        const bgClass = type === 'success' ? 'bg-success' : 
                       type === 'error' ? 'bg-danger' : 
                       type === 'warning' ? 'bg-warning' : 'bg-info';
        
        const toastHtml = `
            <div id="${toastId}" class="toast show ${bgClass} text-white" role="alert">
                <div class="toast-header ${bgClass} text-white">
                    <strong class="me-auto">
                        ${type === 'success' ? '✅' : type === 'error' ? '❌' : type === 'warning' ? '⚠️' : 'ℹ️'}
                        Повідомлення
                    </strong>
                    <button type="button" class="btn-close btn-close-white" onclick="document.getElementById('${toastId}').remove()"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;
        
        container.insertAdjacentHTML('beforeend', toastHtml);
        
        // Автоматично видаляємо через 3 секунди
        setTimeout(() => {
            const toast = document.getElementById(toastId);
            if (toast) {
                toast.classList.add('fade');
                setTimeout(() => toast.remove(), 300);
            }
        }, 3000);
    },

    // Підтвердження дії
    confirm: function (message) {
        return confirm(message);
    }
};

// Drag & Drop для Gantt діаграми
window.GanttDragDrop = {
    sortableInstances: {},
    dotNetHelperRef: null,
    
    initGanttSortable: function (elementId, dotNetHelper, workshopNumber) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.warn('Gantt element not found:', elementId);
            return false;
        }

        // Зберігаємо dotNetHelper
        this.dotNetHelperRef = dotNetHelper;

        // Знищуємо попередній інстанс
        if (this.sortableInstances[elementId]) {
            this.sortableInstances[elementId].destroy();
            delete this.sortableInstances[elementId];
        }

        try {
            this.sortableInstances[elementId] = new Sortable(el, {
                animation: 150,
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                draggable: '.draggable-bar',  // Тільки бари
                filter: '.gantt-cell, .shipment-line',  // Виключаємо клітинки та лінії
                group: 'gantt-orders',
                sort: true,
                forceFallback: true,
                fallbackOnBody: true,
                swapThreshold: 0.65,
                
                onStart: function (evt) {
                    document.body.classList.add('is-dragging');
                    document.querySelectorAll('.gantt-dropzone').forEach(zone => {
                        zone.classList.add('drop-target-active');
                    });
                    console.log('Gantt drag started:', evt.oldIndex, 'item:', evt.item);
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    document.querySelectorAll('.gantt-dropzone').forEach(zone => {
                        zone.classList.remove('drop-target-active');
                    });
                    
                    const fromWorkshop = parseInt(evt.from.dataset.workshop);
                    const toWorkshop = parseInt(evt.to.dataset.workshop);
                    const orderDay = parseInt(evt.item.dataset.orderDay);
                    const squareMeters = parseFloat(evt.item.dataset.square);
                    
                    // Рахуємо реальні індекси (тільки серед draggable-bar)
                    const fromBars = Array.from(evt.from.querySelectorAll('.draggable-bar'));
                    const toBars = Array.from(evt.to.querySelectorAll('.draggable-bar'));
                    
                    const oldIndex = fromBars.indexOf(evt.item);
                    const newIndex = toBars.indexOf(evt.item);
                    
                    console.log('Gantt drag ended:', fromWorkshop, '->', toWorkshop, 'oldIndex:', oldIndex, 'newIndex:', newIndex, 'orderDay:', orderDay);
                    
                    // ВАЖЛИВО: Скасовуємо DOM зміни - Blazor сам оновить
                    // Повертаємо елемент на старе місце
                    if (evt.from === evt.to && oldIndex !== newIndex && oldIndex >= 0 && newIndex >= 0) {
                        // В межах одного контейнера - переміщення порядку
                        console.log('Reordering within workshop:', fromWorkshop);
                        dotNetHelper.invokeMethodAsync('OnGanttOrderReordered', 
                            fromWorkshop,
                            oldIndex, 
                            newIndex
                        ).then(() => {
                            console.log('OnGanttOrderReordered success');
                        }).catch(err => console.error('Error:', err));
                    } else if (fromWorkshop !== toWorkshop) {
                        // Переміщення між цехами
                        console.log('Transferring between workshops:', fromWorkshop, '->', toWorkshop);
                        dotNetHelper.invokeMethodAsync('OnGanttOrderTransfer',
                            fromWorkshop,
                            toWorkshop,
                            orderDay,
                            squareMeters
                        ).then(() => {
                            console.log('OnGanttOrderTransfer success');
                        }).catch(err => console.error('Error:', err));
                    }
                }
            });
            
            console.log('✅ Gantt Sortable initialized for:', elementId, 'workshop:', workshopNumber);
            return true;
        } catch (err) {
            console.error('❌ Error initializing Gantt Sortable:', err);
            return false;
        }
    }
};

// Автоматична ініціалізація при завантаженні Blazor
console.log('✅ DragDropInterop loaded');
