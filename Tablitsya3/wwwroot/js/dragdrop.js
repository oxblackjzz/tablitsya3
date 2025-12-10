// Drag & Drop functionality using SortableJS
// https://sortablejs.github.io/Sortable/

window.DragDropInterop = {
    sortableInstances: {},

    // Ініціалізує Sortable для таблиці замовлень
    initSortable: function (elementId, dotNetHelper, workshopNumber) {
        const el = document.getElementById(elementId);
        if (!el) {
            console.error('Element not found:', elementId);
            return false;
        }

        // Знищуємо попередній інстанс якщо є
        if (this.sortableInstances[elementId]) {
            this.sortableInstances[elementId].destroy();
        }

        try {
            this.sortableInstances[elementId] = new Sortable(el, {
                animation: 150,
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                dragClass: 'sortable-drag',
                handle: '.drag-handle',
                filter: '.no-drag',
                preventOnFilter: true,
                
                onStart: function (evt) {
                    // Додаємо клас до body для стилізації
                    document.body.classList.add('is-dragging');
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    
                    if (evt.oldIndex !== evt.newIndex) {
                        // Викликаємо метод C# для обробки зміни порядку
                        dotNetHelper.invokeMethodAsync('OnOrderReordered', 
                            workshopNumber,
                            evt.oldIndex, 
                            evt.newIndex
                        ).catch(err => console.error('Error calling OnOrderReordered:', err));
                    }
                }
            });
            
            console.log('Sortable initialized for:', elementId);
            return true;
        } catch (err) {
            console.error('Error initializing Sortable:', err);
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

    // Ініціалізує drag & drop між цехами
    initCrossWorkshopDrag: function (containerIds, dotNetHelper) {
        const containers = containerIds.map(id => document.getElementById(id)).filter(el => el);
        
        if (containers.length === 0) {
            console.error('No containers found for cross-workshop drag');
            return false;
        }

        containers.forEach((container, index) => {
            const elementId = containerIds[index];
            
            if (this.sortableInstances[elementId]) {
                this.sortableInstances[elementId].destroy();
            }

            this.sortableInstances[elementId] = new Sortable(container, {
                group: 'workshops', // Дозволяє перетягування між групами
                animation: 150,
                ghostClass: 'sortable-ghost',
                chosenClass: 'sortable-chosen',
                handle: '.drag-handle',
                
                onAdd: function (evt) {
                    // Елемент перенесено в інший цех
                    const fromWorkshop = parseInt(evt.from.dataset.workshop);
                    const toWorkshop = parseInt(evt.to.dataset.workshop);
                    const oldIndex = evt.oldIndex;
                    const newIndex = evt.newIndex;
                    
                    dotNetHelper.invokeMethodAsync('OnOrderMovedBetweenWorkshops',
                        fromWorkshop,
                        toWorkshop,
                        oldIndex,
                        newIndex
                    ).catch(err => console.error('Error calling OnOrderMovedBetweenWorkshops:', err));
                },
                
                onEnd: function (evt) {
                    document.body.classList.remove('is-dragging');
                    
                    // Якщо перетягування в межах одного цеху
                    if (evt.from === evt.to && evt.oldIndex !== evt.newIndex) {
                        const workshopNumber = parseInt(evt.to.dataset.workshop);
                        dotNetHelper.invokeMethodAsync('OnOrderReordered',
                            workshopNumber,
                            evt.oldIndex,
                            evt.newIndex
                        ).catch(err => console.error('Error calling OnOrderReordered:', err));
                    }
                }
            });
        });
        
        console.log('Cross-workshop drag initialized for:', containerIds);
        return true;
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
