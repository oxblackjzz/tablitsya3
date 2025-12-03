// Функція для експорту файлів (CSV, текстові файли)
window.downloadFile = function(filename, base64Content) {
    const link = document.createElement('a');
    link.download = filename;
    link.href = `data:text/plain;base64,${base64Content}`;
 document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
};

// Функції для прокрутки до елемента
window.scrollToElement = function (selector) {
    const element = document.querySelector(selector);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        
        // Додати тимчасове підсвічування
        element.style.transition = 'box-shadow 0.3s ease-in-out';
        element.style.boxShadow = '0 0 20px rgba(255, 193, 7, 0.8)';
    
        setTimeout(() => {
            element.style.boxShadow = '';
        }, 1000);
    }
};

// Функція для експорту графіка Ганта (для майбутньої реалізації)
window.exportGanttChart = async function (workshopNumber) {
    const element = document.querySelector(`[data-workshop="${workshopNumber}"]`);
    if (!element) {
        console.error(`Gantt chart for workshop ${workshopNumber} not found`);
        return;
    }
    
    // Тут можна додати html2canvas або іншу бібліотеку експорту
    console.log(`Export gantt chart for workshop ${workshopNumber}`);
};

// Функція для підтвердження дії
window.confirmAction = function (message) {
    return confirm(message);
};

// ============================================
// DRAG & DROP ДЛЯ ДІАГРАМИ ГАНТА
// ============================================

window.GanttDragDrop = {
    draggedElement: null,
    draggedOrderData: null,
    ghostElement: null,
    
    // Ініціалізація drag & drop для діаграми Ганта
    initialize: function (workshopNumber, dotNetHelper) {
        console.log(`Initializing Drag & Drop for workshop ${workshopNumber}`);
        
        const ganttContainer = document.querySelector(`[data-workshop="${workshopNumber}"]`);
        if (!ganttContainer) {
            console.warn(`Gantt container for workshop ${workshopNumber} not found`);
            return;
        }
        
        // Знайти всі бари замовлень
        const orderBars = ganttContainer.querySelectorAll('.gantt-bar');
        
        orderBars.forEach(bar => {
            // Зробити бар draggable
            bar.setAttribute('draggable', 'true');
            bar.style.cursor = 'move';
            
            // Додати обробники подій
            bar.addEventListener('dragstart', (e) => this.handleDragStart(e, dotNetHelper));
            bar.addEventListener('dragend', (e) => this.handleDragEnd(e));
        });
        
        // Додати drop zones на днях календаря
        const daySeparators = ganttContainer.querySelectorAll('.day-separator');
        daySeparators.forEach(day => {
            day.addEventListener('dragover', (e) => this.handleDragOver(e));
            day.addEventListener('drop', (e) => this.handleDrop(e, dotNetHelper));
            day.addEventListener('dragenter', (e) => this.handleDragEnter(e));
            day.addEventListener('dragleave', (e) => this.handleDragLeave(e));
        });
    },
    
    // Початок перетягування
    handleDragStart: function (event, dotNetHelper) {
        this.draggedElement = event.target;
        
        // Отримати дані замовлення з data-атрибутів
        const orderDay = this.draggedElement.getAttribute('data-order-day');
        const workshopNumber = this.draggedElement.closest('[data-workshop]').getAttribute('data-workshop');
        
        this.draggedOrderData = {
            orderDay: parseInt(orderDay),
            workshopNumber: parseInt(workshopNumber)
        };
        
        // Зберегти дані для передачі
        event.dataTransfer.effectAllowed = 'move';
        event.dataTransfer.setData('text/plain', JSON.stringify(this.draggedOrderData));
        
        // Візуальний ефект
        this.draggedElement.style.opacity = '0.5';
        this.draggedElement.classList.add('dragging');
        
        // Створити ghost елемент
        this.createGhostElement();
        
        console.log('Drag started:', this.draggedOrderData);
    },
    
    // Завершення перетягування
    handleDragEnd: function (event) {
        if (this.draggedElement) {
            this.draggedElement.style.opacity = '1';
            this.draggedElement.classList.remove('dragging');
        }
        
        // Видалити підсвічування з усіх днів
        document.querySelectorAll('.day-separator').forEach(day => {
            day.classList.remove('drag-over');
        });
        
        // Видалити ghost елемент
        if (this.ghostElement && this.ghostElement.parentNode) {
            this.ghostElement.parentNode.removeChild(this.ghostElement);
        }
        
        this.draggedElement = null;
        this.draggedOrderData = null;
        this.ghostElement = null;
        
        console.log('Drag ended');
    },
    
    // Перетягування над drop zone
    handleDragOver: function (event) {
        event.preventDefault();
        event.dataTransfer.dropEffect = 'move';
    },
    
    // Вхід в drop zone
    handleDragEnter: function (event) {
        const day = event.currentTarget;
        if (day.classList.contains('day-separator')) {
            day.classList.add('drag-over');
        }
    },
    
    // Вихід з drop zone
    handleDragLeave: function (event) {
        const day = event.currentTarget;
        if (day.classList.contains('day-separator')) {
            day.classList.remove('drag-over');
        }
    },
    
    // Відпускання (drop)
    handleDrop: async function (event, dotNetHelper) {
        event.preventDefault();
        
        const dropTarget = event.currentTarget;
        dropTarget.classList.remove('drag-over');
        
        // Отримати дату, на яку відпустили
        const newDateStr = dropTarget.getAttribute('data-date');
        
        if (!newDateStr || !this.draggedOrderData) {
            console.warn('Missing data for drop operation');
            return;
        }
        
        console.log('Dropped on date:', newDateStr, 'Order:', this.draggedOrderData);
        
        // Викликати .NET метод для оновлення замовлення
        try {
            await dotNetHelper.invokeMethodAsync('UpdateOrderDate', 
                this.draggedOrderData.workshopNumber,
                this.draggedOrderData.orderDay,
                newDateStr
            );
            
            // Показати успішне повідомлення
            this.showNotification('? Замовлення перенесено на ' + newDateStr, 'success');
        } catch (error) {
            console.error('Error updating order date:', error);
            this.showNotification('? Помилка при переносі замовлення', 'error');
        }
    },
    
    // Створити ghost елемент для візуального feedback
    createGhostElement: function () {
        this.ghostElement = document.createElement('div');
        this.ghostElement.className = 'gantt-drag-ghost';
        this.ghostElement.textContent = `Замовлення №${this.draggedOrderData.orderDay}`;
        this.ghostElement.style.cssText = `
            position: fixed;
            top: -1000px;
            left: -1000px;
            background: rgba(13, 110, 253, 0.9);
            color: white;
            padding: 8px 16px;
            border-radius: 4px;
            font-weight: bold;
            box-shadow: 0 4px 12px rgba(0,0,0,0.3);
            pointer-events: none;
            z-index: 10000;
        `;
        document.body.appendChild(this.ghostElement);
    },
    
    // Показати повідомлення
    showNotification: function (message, type) {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type === 'success' ? 'success' : 'danger'} gantt-notification`;
        notification.textContent = message;
        notification.style.cssText = `
            position: fixed;
            top: 20px;
            right: 20px;
            z-index: 10001;
            min-width: 300px;
            animation: slideInRight 0.3s ease-out;
        `;
        
        document.body.appendChild(notification);
        
        setTimeout(() => {
            notification.style.animation = 'slideOutRight 0.3s ease-out';
            setTimeout(() => {
                if (notification.parentNode) {
                    notification.parentNode.removeChild(notification);
                }
            }, 300);
        }, 3000);
    },
    
    // Очистити всі обробники
    cleanup: function (workshopNumber) {
        const ganttContainer = document.querySelector(`[data-workshop="${workshopNumber}"]`);
        if (!ganttContainer) return;
        
        const orderBars = ganttContainer.querySelectorAll('.gantt-bar');
        orderBars.forEach(bar => {
            bar.removeAttribute('draggable');
            bar.style.cursor = '';
            bar.classList.remove('dragging');
        });
        
        console.log(`Drag & Drop cleanup for workshop ${workshopNumber}`);
    }
};

// Ініціалізація при завантаженні DOM
window.addEventListener('DOMContentLoaded', function () {
    console.log('Production Planning App initialized');
});
