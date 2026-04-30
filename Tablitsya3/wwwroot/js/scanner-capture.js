// Глобальний захоплювач сканера штрих-кодів (HID-клавіатура).
// Призначення: дозволити сканування QR/штрихкодів без видимого <input>,
// щоб оператор не міг випадково "скинути" буфер під час промислового сканування.
//
// Принцип роботи:
//  - Слухаємо keydown на window.
//  - Якщо клавіші приходять швидше за порогову паузу (typingThresholdMs)
//    і завершуються Enter/CR/LF — це сканер.
//  - Інакше це звичайний користувач і ми ігноруємо ввід.
//  - Не активуємось, якщо фокус у редагованому полі (input/textarea/contenteditable),
//    щоб не заважати редагуванню.

(function () {
    if (window.__scannerCapture) return;

    const state = {
        buffer: '',
        lastKeyTime: 0,
        dotNetRef: null,
        callbackName: null,
        typingThresholdMs: 50,   // сканери дають < 30мс між символами; людина — > 80мс
        minLength: 4,            // мінімальна довжина "коду"
        active: false,
        prefix: '',
        suffix: ''
    };

    function isEditableTarget(target) {
        if (!target) return false;
        const tag = (target.tagName || '').toUpperCase();
        if (tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT') return true;
        if (target.isContentEditable) return true;
        return false;
    }

    function flushScan() {
        let code = state.buffer;
        state.buffer = '';
        if (!code || code.length < state.minLength) return;

        if (state.prefix && code.startsWith(state.prefix)) {
            code = code.substring(state.prefix.length);
        }
        if (state.suffix && code.endsWith(state.suffix)) {
            code = code.substring(0, code.length - state.suffix.length);
        }

        if (state.dotNetRef && state.callbackName) {
            try {
                state.dotNetRef.invokeMethodAsync(state.callbackName, code);
            } catch (e) {
                console.error('[scanner-capture] invokeMethodAsync failed', e);
            }
        }
    }

    function onKeyDown(e) {
        if (!state.active) return;
        if (isEditableTarget(e.target)) return;

        const now = performance.now();
        const delta = now - state.lastKeyTime;
        state.lastKeyTime = now;

        // Якщо пауза між клавішами велика — це не сканер, скидаємо
        if (delta > 500 && state.buffer.length > 0) {
            state.buffer = '';
        }

        if (e.key === 'Enter' || e.key === 'NumpadEnter') {
            // Завершення сканування
            if (state.buffer.length >= state.minLength && delta < state.typingThresholdMs * 5) {
                e.preventDefault();
                flushScan();
            } else {
                state.buffer = '';
            }
            return;
        }

        // Ігноруємо службові клавіші
        if (e.key.length !== 1) return;
        // Тільки видимі ASCII + кирилиця
        state.buffer += e.key;
        // Не даємо браузеру додавати символи в фокус
        e.preventDefault();
    }

    window.__scannerCapture = {
        start: function (dotNetRef, callbackName, options) {
            state.dotNetRef = dotNetRef;
            state.callbackName = callbackName || 'OnScanCapturedAsync';
            if (options) {
                if (typeof options.typingThresholdMs === 'number') state.typingThresholdMs = options.typingThresholdMs;
                if (typeof options.minLength === 'number') state.minLength = options.minLength;
                if (typeof options.prefix === 'string') state.prefix = options.prefix;
                if (typeof options.suffix === 'string') state.suffix = options.suffix;
            }
            if (!state.active) {
                window.addEventListener('keydown', onKeyDown, true);
                state.active = true;
            }
        },
        stop: function () {
            if (state.active) {
                window.removeEventListener('keydown', onKeyDown, true);
                state.active = false;
            }
            state.dotNetRef = null;
            state.buffer = '';
        },
        // Програмне сканування (для тестування або ручного вводу адміна)
        simulate: function (code) {
            state.buffer = code;
            flushScan();
        }
    };
})();
