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
                animation: 250, // Збільшено для плавності
                easing: "cubic-bezier(0.25, 1, 0.5, 1)", // Плавний easing
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
// Підтримує: переміщення між цехами + зміна порядку в межах цеху
window.GanttDragDrop = {
    sortableInstances: {},
    dragData: null, // Зберігаємо дані про drag операцію
    dotNetHelperRef: null, // Зберігаємо посилання на .NET helper
    
    initGanttSortable: function (elementId, dotNetHelper, workshopNumber) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.warn('Gantt element not found:', elementId);
            return false;
        }

        // Знищуємо попередній інстанс
        if (this.sortableInstances[elementId]) {
            this.sortableInstances[elementId].destroy();
            delete this.sortableInstances[elementId];
        }

        const self = this;
        self.dotNetHelperRef = dotNetHelper;

        try {
            this.sortableInstances[elementId] = new Sortable(el, {
                animation: 300, // Збільшено для плавності
                easing: "cubic-bezier(0.25, 1, 0.5, 1)", // Плавний easing
                ghostClass: 'gantt-sortable-ghost',
                chosenClass: 'gantt-sortable-chosen',
                dragClass: 'gantt-sortable-drag',
                draggable: '.draggable-bar[data-segment="0"]', // Тільки перші сегменти
                filter: '.gantt-cell, .shipment-line, .segment-continuation',
                preventOnFilter: false,
                group: 'gantt-orders', // Група для переміщення між цехами
                sort: false, // Вимикаємо автоматичне сортування бо бари абсолютно позиціоновані
                forceFallback: true,
                fallbackOnBody: true,
                fallbackTolerance: 3, // Мінімальний рух для початку drag
                swapThreshold: 0.65,
                
                onStart: function (evt) {
                    document.body.classList.add('is-dragging');
                    
                    // Підсвічуємо всі зони для drop
                    document.querySelectorAll('.gantt-dropzone').forEach(zone => {
                        zone.classList.add('drop-target-active');
                    });
                    
                    // Зберігаємо дані про елемент що перетягуємо
                    const item = evt.item;
                    const fromContainer = evt.from;
                    
                    // Отримуємо всі перші сегменти (головні бари) в порядку left
                    const bars = Array.from(fromContainer.querySelectorAll('.draggable-bar[data-segment="0"]'));
                    const sortedBars = bars.sort((a, b) => {
                        const leftA = parseFloat(a.style.left) || 0;
                        const leftB = parseFloat(b.style.left) || 0;
                        return leftA - leftB;
                    });
                    
                    self.dragData = {
                        item: item,
                        fromWorkshop: parseInt(fromContainer.dataset.workshop) || workshopNumber,
                        orderDay: parseInt(item.dataset.orderDay),
                        squareMeters: parseFloat(item.dataset.square),
                        oldIndex: sortedBars.indexOf(item),
                        sortedBars: sortedBars
                    };
                    
                    console.log('🎯 Gantt drag started:', {
                        orderDay: self.dragData.orderDay,
                        workshop: self.dragData.fromWorkshop,
                        oldIndex: self.dragData.oldIndex,
                        square: self.dragData.squareMeters
                    });
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    
                    // Прибираємо підсвітку
                    document.querySelectorAll('.gantt-dropzone').forEach(zone => {
                        zone.classList.remove('drop-target-active');
                    });
                    
                    if (!self.dragData) {
                        console.warn('No drag data available');
                        return;
                    }
                    
                    const fromWorkshop = self.dragData.fromWorkshop;
                    const toContainer = evt.to;
                    const toWorkshop = parseInt(toContainer.dataset.workshop) || fromWorkshop;
                    const orderDay = self.dragData.orderDay;
                    const squareMeters = self.dragData.squareMeters;
                    const oldIndex = self.dragData.oldIndex;
                    
                    console.log('🎯 Gantt drag ended:', {
                        fromWorkshop,
                        toWorkshop,
                        orderDay,
                        oldIndex
                    });
                    
                    // Повертаємо елемент на місце - Blazor сам оновить DOM
                    if (evt.from !== evt.to) {
                        // Переміщення між контейнерами - повертаємо назад
                        evt.from.appendChild(evt.item);
                    }
                    
                    if (fromWorkshop === toWorkshop) {
                        // Переміщення в межах одного цеху
                        const container = evt.to;
                        const containerRect = container.getBoundingClientRect();
                        const dropX = evt.originalEvent ? evt.originalEvent.clientX - containerRect.left : 0;
                        const containerWidth = containerRect.width;
                        
                        // Отримуємо всі бари крім того що перетягуємо
                        const otherBars = self.dragData.sortedBars.filter(b => b !== evt.item);
                        
                        // Визначаємо нову позицію по X-координаті
                        let newIndex = otherBars.length;
                        
                        for (let i = 0; i < otherBars.length; i++) {
                            const bar = otherBars[i];
                            const barLeft = parseFloat(bar.style.left) || 0;
                            const barWidth = parseFloat(bar.style.width) || 0;
                            const barCenterPercent = barLeft + barWidth / 2;
                            const barCenterPx = (barCenterPercent / 100) * containerWidth;
                            
                            if (dropX < barCenterPx) {
                                newIndex = i;
                                break;
                            }
                        }
                        
                        // Коригуємо індекс якщо переміщуємо праворуч
                        if (newIndex > oldIndex) {
                            // newIndex вже правильний
                        }
                        
                        console.log('📦 Same workshop reorder:', {
                            fromWorkshop, oldIndex, newIndex, dropX, containerWidth
                        });
                        
                        if (oldIndex !== newIndex && oldIndex >= 0 && newIndex >= 0) {
                            dotNetHelper.invokeMethodAsync('OnGanttOrderReordered', 
                                fromWorkshop,
                                oldIndex, 
                                newIndex
                            ).then(() => {
                                console.log('✅ OnGanttOrderReordered success');
                            }).catch(err => {
                                console.error('❌ Error calling OnGanttOrderReordered:', err);
                                window.DragDropInterop.showToast('Помилка зміни порядку', 'error');
                            });
                        }
                    } else {
                        // Переміщення між цехами
                        console.log('🔄 Transfer between workshops:', fromWorkshop, '->', toWorkshop);
                        
                        dotNetHelper.invokeMethodAsync('OnGanttOrderTransfer',
                            fromWorkshop,
                            toWorkshop,
                            orderDay,
                            squareMeters
                        ).then(() => {
                            console.log('✅ OnGanttOrderTransfer success');
                        }).catch(err => {
                            console.error('❌ Error calling OnGanttOrderTransfer:', err);
                            window.DragDropInterop.showToast('Помилка переміщення', 'error');
                        });
                    }
                    
                    self.dragData = null;
                }
            });
            
            console.log('✅ Gantt Sortable initialized for:', elementId, 'workshop:', workshopNumber);
            return true;
        } catch (err) {
            console.error('❌ Error initializing Gantt Sortable:', err);
            return false;
        }
    },
    
    // Знищення всіх інстансів
    destroyAll: function() {
        for (const elementId in this.sortableInstances) {
            if (this.sortableInstances[elementId]) {
                this.sortableInstances[elementId].destroy();
            }
        }
        this.sortableInstances = {};
        this.dragData = null;
        console.log('All Gantt Sortable instances destroyed');
    }
};

// Drag & Drop для модального вікна зміни порядку
window.ReorderModalDragDrop = {
    sortableInstance: null,
    
    init: function (elementId, dotNetHelper) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.warn('Reorder modal element not found:', elementId);
            return false;
        }

        // Знищуємо попередній інстанс
        if (this.sortableInstance) {
            this.sortableInstance.destroy();
            this.sortableInstance = null;
        }

        try {
            this.sortableInstance = new Sortable(el, {
                animation: 250, // Збільшено для плавності
                easing: "cubic-bezier(0.25, 1, 0.5, 1)", // Плавний easing
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                handle: '.drag-handle-cell',
                filter: '.no-drag, button',
                preventOnFilter: false,
                forceFallback: true,
                
                onStart: function (evt) {
                    document.body.classList.add('is-dragging');
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    
                    if (evt.oldIndex !== evt.newIndex) {
                        // Оновлюємо номери в таблиці
                        const rows = el.querySelectorAll('.reorder-item');
                        rows.forEach((row, index) => {
                            const numCell = row.querySelector('.order-number');
                            if (numCell) {
                                numCell.textContent = index + 1;
                            }
                            row.dataset.index = index;
                        });
                        
                        // Викликаємо .NET метод
                        dotNetHelper.invokeMethodAsync('OnItemReordered', 
                            evt.oldIndex, 
                            evt.newIndex
                        ).then(() => {
                            console.log('Reorder modal: item moved', evt.oldIndex, '->', evt.newIndex);
                        }).catch(err => {
                            console.error('Error calling OnItemReordered:', err);
                        });
                    }
                }
            });
            
            console.log('✅ Reorder Modal Sortable initialized');
            return true;
        } catch (err) {
            console.error('❌ Error initializing Reorder Modal Sortable:', err);
            return false;
        }
    },
    
    destroy: function () {
        if (this.sortableInstance) {
            this.sortableInstance.destroy();
            this.sortableInstance = null;
        }
    }
};

// Автоматична ініціалізація при завантаженні Blazor
console.log('✅ DragDropInterop loaded');
