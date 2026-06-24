/**
 * SweetCakeShop — Chat: GetChatHistory + SendMessage, DB + Gemini
 */
window.SweetCakeAiChat = function (config) {
    const fab = document.getElementById(config.fabId);
    const panel = document.getElementById(config.panelId);
    const closeBtn = document.getElementById(config.closeId);
    const input = document.getElementById(config.inputId);
    const sendBtn = document.getElementById(config.sendId);
    const messages = document.getElementById(config.messagesId);
    const quickRepliesEl = config.quickRepliesId ? document.getElementById(config.quickRepliesId) : null;
    const dragHandle = config.headerId ? document.getElementById(config.headerId) : null;

    if (!fab || !panel) return;

    let historyLoaded = false;
    let pollTimer = null;

    if (config.startOpen) {
        panel.classList.add('is-open');
        fab.classList.add('is-active');
    }

    function setOpen(open) {
        panel.classList.toggle('is-open', open);
        fab.classList.toggle('is-active', open);
        if (open) {
            startPolling();
        } else {
            stopPolling();
        }
    }

    function getClientContext() {
        const productIdRaw = panel.dataset.productId;
        const productId = productIdRaw && productIdRaw !== '' ? parseInt(productIdRaw, 10) : null;
        return {
            pageUrl: window.location.pathname + window.location.search,
            productId: Number.isFinite(productId) ? productId : null
        };
    }

    fab.addEventListener('click', async () => {
        const willOpen = !panel.classList.contains('is-open');
        setOpen(willOpen);
        if (willOpen && !historyLoaded && config.historyUrl) {
            await loadHistory();
        }
    });

    closeBtn?.addEventListener('click', () => setOpen(false));

    if (config.draggable !== false && dragHandle) {
        initDraggable(panel, dragHandle);
    }

    function initDraggable(panelEl, handleEl) {
        let dragging = false;
        let startX = 0;
        let startY = 0;
        let startLeft = 0;
        let startTop = 0;

        function ensurePositioned() {
            if (panelEl.dataset.positioned === '1') return;
            const rect = panelEl.getBoundingClientRect();
            panelEl.style.left = rect.left + 'px';
            panelEl.style.top = rect.top + 'px';
            panelEl.style.right = 'auto';
            panelEl.style.bottom = 'auto';
            panelEl.dataset.positioned = '1';
        }

        function onPointerDown(e) {
            if (e.target.closest('.btn-close')) return;
            dragging = true;
            ensurePositioned();
            panelEl.classList.add('is-dragging');
            const rect = panelEl.getBoundingClientRect();
            startLeft = rect.left;
            startTop = rect.top;
            const point = e.touches ? e.touches[0] : e;
            startX = point.clientX;
            startY = point.clientY;
            e.preventDefault();
        }

        function onPointerMove(e) {
            if (!dragging) return;
            const point = e.touches ? e.touches[0] : e;
            const dx = point.clientX - startX;
            const dy = point.clientY - startY;
            const w = panelEl.offsetWidth;
            const h = panelEl.offsetHeight;
            const maxLeft = window.innerWidth - w - 8;
            const maxTop = window.innerHeight - h - 8;
            const left = Math.min(Math.max(8, startLeft + dx), maxLeft);
            const top = Math.min(Math.max(8, startTop + dy), maxTop);
            panelEl.style.left = left + 'px';
            panelEl.style.top = top + 'px';
            e.preventDefault();
        }

        function onPointerUp() {
            if (!dragging) return;
            dragging = false;
            panelEl.classList.remove('is-dragging');
        }

        handleEl.addEventListener('mousedown', onPointerDown);
        handleEl.addEventListener('touchstart', onPointerDown, { passive: false });
        document.addEventListener('mousemove', onPointerMove);
        document.addEventListener('mouseup', onPointerUp);
        document.addEventListener('touchmove', onPointerMove, { passive: false });
        document.addEventListener('touchend', onPointerUp);
    }

    function escapeHtml(s) {
        const d = document.createElement('div');
        d.textContent = s;
        return d.innerHTML;
    }

    function renderMarkdown(text) {
        let html = escapeHtml(text);
        html = html.replace(/\*\*(.+?)\*\*/g, '<strong>$1</strong>');
        html = html.replace(/\*(.+?)\*/g, '<em>$1</em>');
        html = html.replace(/^[-•] (.+)$/gm, '<li>$1</li>');
        html = html.replace(/(<li>.*<\/li>\n?)+/gs, m => '<ul class="mb-0 ps-3">' + m + '</ul>');
        html = html.replace(/\n/g, '<br>');
        return html;
    }

    function renderProductCards(products) {
        if (!products || !products.length) return '';
        const cards = products.map(p => {
            const img = p.imageUrl || p.ImageUrl;
            const url = p.detailUrl || p.DetailUrl || '/Products/IndexPro';
            const name = p.name || p.Name;
            const price = p.price ?? p.Price;
            const imgHtml = img
                ? `<img src="${escapeHtml(img)}" alt="" class="ai-product-thumb" />`
                : '<span class="ai-product-thumb ai-product-thumb--empty">🍰</span>';
            return `<a class="ai-product-card" href="${escapeHtml(url)}">
                ${imgHtml}
                <span class="ai-product-card-body">
                    <span class="ai-product-name">${escapeHtml(name)}</span>
                    <span class="ai-product-price">${Number(price).toLocaleString('vi-VN')} VND</span>
                </span>
            </a>`;
        }).join('');
        return `<div class="ai-product-cards">${cards}</div>`;
    }

    function appendMsg(sender, text, products, scroll = true) {
        const normalizedSender = (sender || '').toLowerCase();
        const role = normalizedSender === 'user' ? 'user' : 'bot';
        const wrap = document.createElement('div');
        wrap.className = 'ai-msg ' + role;
        const label = role === 'user' ? 'Bạn' : (normalizedSender === 'admin' ? 'CSKH' : 'Trợ lý');
        const bubble = document.createElement('div');
        bubble.className = 'bubble';
        if (role === 'bot') {
            bubble.innerHTML = renderMarkdown(text) + renderProductCards(products);
        } else {
            bubble.textContent = text;
        }
        wrap.innerHTML = '<div class="small text-muted mb-1">' + label + '</div>';
        wrap.appendChild(bubble);
        messages.appendChild(wrap);
        if (scroll) messages.scrollTop = messages.scrollHeight;
    }

    function clearMessages() {
        messages.innerHTML = '';
    }

    function showTyping() {
        const wrap = document.createElement('div');
        wrap.className = 'ai-msg bot ai-typing-indicator';
        wrap.id = config.typingId || 'aiTypingIndicator';
        wrap.innerHTML = '<div class="small text-muted mb-1">Trợ lý</div><div class="bubble"><span class="typing-dots"><span></span><span></span><span></span></span></div>';
        messages.appendChild(wrap);
        messages.scrollTop = messages.scrollHeight;
    }

    function removeTyping() {
        document.getElementById(config.typingId || 'aiTypingIndicator')?.remove();
    }

    function renderQuickReplies(items) {
        if (!quickRepliesEl || !items?.length) return;
        quickRepliesEl.innerHTML = '';
        items.forEach(text => {
            const btn = document.createElement('button');
            btn.type = 'button';
            btn.className = 'quick-reply-btn';
            btn.textContent = text;
            btn.addEventListener('click', () => {
                input.value = text;
                send();
            });
            quickRepliesEl.appendChild(btn);
        });
    }

    function startPolling() {
        if (!config.historyUrl || config.pollHistory === false || pollTimer) return;
        pollTimer = window.setInterval(() => {
            if (panel.classList.contains('is-open')) {
                loadHistory(true);
            }
        }, config.pollIntervalMs || 5000);
    }

    function stopPolling() {
        if (!pollTimer) return;
        window.clearInterval(pollTimer);
        pollTimer = null;
    }

    async function loadHistory(silent) {
        try {
            const res = await fetch(config.historyUrl, { credentials: 'include' });
            const data = await res.json();
            clearMessages();
            const list = data.messages || data.Messages || [];
            list.forEach(m => {
                const sender = m.sender || m.Sender;
                appendMsg(sender, m.content || m.Content, m.products || m.Products, false);
            });
            messages.scrollTop = messages.scrollHeight;
            if (!silent) {
                renderQuickReplies(data.quickReplies || data.QuickReplies || []);
            }
            historyLoaded = true;
        } catch {
            if (silent) return;
            appendMsg('model', 'Chào anh/chị! 🍰 Em tư vấn bánh SweetCakeShop — hỏi em bất cứ điều gì về menu nhé!');
        }
    }

    async function send() {
        const msg = input.value.trim();
        if (!msg) return;

        if (!panel.classList.contains('is-open')) {
            setOpen(true);
        }

        appendMsg('user', msg);
        input.value = '';
        sendBtn.disabled = true;
        input.disabled = true;
        showTyping();

        try {
            const ctx = getClientContext();
            const body = config.useAdminPayload
                ? { message: msg }
                : { userMessage: msg, pageUrl: ctx.pageUrl, productId: ctx.productId };
            const res = await fetch(config.sendUrl, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                credentials: 'include',
                body: JSON.stringify(body)
            });
            const data = await res.json();
            removeTyping();
            const text = data.reply ?? data.Reply ?? 'Không có phản hồi.';
            const products = data.products ?? data.Products;
            appendMsg('model', text, products);
            const qr = data.quickReplies ?? data.QuickReplies;
            if (qr?.length) renderQuickReplies(qr);
            if (config.onCartAction && text.toLowerCase().includes('giỏ')) {
                try { config.onCartAction(); } catch (_) { }
            }
        } catch {
            removeTyping();
            appendMsg('model', 'Không thể kết nối trợ lý AI. Vui lòng thử lại sau giây lát.');
        } finally {
            sendBtn.disabled = false;
            input.disabled = false;
            input.focus();
        }
    }

    sendBtn?.addEventListener('click', send);
    input?.addEventListener('keydown', e => {
        if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            send();
        }
    });

    if (config.defaultQuickReplies?.length) {
        renderQuickReplies(config.defaultQuickReplies);
    }
};
