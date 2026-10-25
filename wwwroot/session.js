window.pvSession = {
    channel: typeof BroadcastChannel !== 'undefined' ? new BroadcastChannel('pv-session') : null,
    async request(path, body) {
        try {
            const r = await fetch(path, { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json', 'X-PV-Client': '1' }, body: JSON.stringify(body ?? {}) });
            if (!r.ok) { const data = await r.json().catch(() => ({})); return data.message || (r.status === 429 ? 'Espere un minuto antes de volver a intentar.' : 'No se pudo completar el acceso.'); }
            return null;
        } catch { return 'No se pudo conectar con la aplicación.'; }
    },
    login(body) { return this.request('/api/auth/login', body); },
    async logout() {
        const error = await this.request('/api/auth/logout');
        if (!error) { this.channel?.postMessage('logout'); localStorage.setItem('pv-logout', Date.now().toString()); }
        return error;
    },
    watch() {
        if (this.watching) return;
        this.watching = true;
        this.channel && (this.channel.onmessage = e => { if (e.data === 'logout') location.reload(); });
        window.addEventListener('storage', e => { if (e.key === 'pv-logout') location.reload(); });
        this.timer = setInterval(async () => {
            if (document.hidden) return;
            try { if ((await fetch('/api/auth/session', { credentials: 'same-origin', cache: 'no-store' })).status === 401) location.reload(); } catch {}
        }, 15000);
        document.addEventListener('visibilitychange', async () => {
            if (!document.hidden) { try { if ((await fetch('/api/auth/session', { cache:'no-store' })).status === 401) location.reload(); } catch {} }
        });
    }
};
window.pvPrint = () => window.print();
