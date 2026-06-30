/**
 * TIBR Admin — admin.js
 */

// ═══════════════════════════════════════════════════════════════
// THEME SYSTEM — TOP LEVEL (global, available immediately)
// ═══════════════════════════════════════════════════════════════

const TIBR_THEME_KEY = 'tibr-admin-theme';

function tibr_getTheme() {
    return localStorage.getItem(TIBR_THEME_KEY) || 'light';
}

function tibr_applyTheme(theme) {
    document.documentElement.setAttribute('data-theme', theme);
    localStorage.setItem(TIBR_THEME_KEY, theme);
    // Rebuild charts after CSS variables settle (short delay)
    setTimeout(tibr_rebuildAllCharts, 100);
}

function tibr_toggleTheme() {
    tibr_applyTheme(tibr_getTheme() === 'dark' ? 'light' : 'dark');
}

// ── Chart colour helpers — top level ───────────────────────────

function getChartColors() {
    const isDark = tibr_getTheme() === 'dark';
    return {
        gold: isDark ? '#f2ca50' : '#c5860a',
        goldAlpha: isDark ? 'rgba(242,202,80,0.20)' : 'rgba(197,134,10,0.15)',
        goldGrad0: isDark ? 'rgba(242,202,80,0.30)' : 'rgba(197,134,10,0.25)',
        goldGrad1: isDark ? 'rgba(242,202,80,0.00)' : 'rgba(197,134,10,0.00)',
        blue: isDark ? '#3b82f6' : '#2563eb',
        blueAlpha: isDark ? 'rgba(59,130,246,0.20)' : 'rgba(37,99,235,0.12)',
        green: isDark ? '#22c55e' : '#16a34a',
        red: isDark ? '#ef4444' : '#dc2626',
        grid: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.06)',
        text: isDark ? '#7a7a7a' : '#6b7280',
        tooltip: isDark ? '#1e1e2e' : '#ffffff',
        tooltipBorder: isDark ? '#f2ca50' : '#c5860a',
        tooltipText: isDark ? '#e8e6e3' : '#111827',
    };
}

// Chart registry — maps id → builder function
window._tibr_chartBuilders = {};

function registerChart(id, builderFn) {
    window._tibr_chartBuilders[id] = builderFn;
}

function tibr_rebuildAllCharts() {
    Object.values(window._tibr_chartBuilders).forEach(fn => {
        try { fn(); } catch (_) { /* chart not on this page — skip silently */ }
    });
}

// Expose on window so inline scripts can call them directly
window.getChartColors = getChartColors;
window.registerChart = registerChart;
window.tibr_getTheme = tibr_getTheme;
window.tibr_applyTheme = tibr_applyTheme;
window.tibr_toggleTheme = tibr_toggleTheme;
window.tibr_rebuildAllCharts = tibr_rebuildAllCharts;

// ═══════════════════════════════════════════════════════════════
// DOM-DEPENDENT CODE — inside DOMContentLoaded
// ═══════════════════════════════════════════════════════════════

document.addEventListener('DOMContentLoaded', () => {

    // ── Wire ALL [data-theme-toggle] buttons ───────────────────
    // Uses event delegation on document so buttons added later also work
    document.addEventListener('click', e => {
        const btn = e.target.closest('[data-theme-toggle]');
        if (btn) tibr_toggleTheme();
    });

    // ── Sidebar Toggle (mobile) ────────────────────────────────
    const sidebar = document.getElementById('sidebar');
    const overlay = document.getElementById('sidebarOverlay');
    const toggleBtn = document.getElementById('sidebarToggle');

    function openSidebar() {
        sidebar?.classList.add('open');
        overlay?.classList.add('open');
        document.body.style.overflow = 'hidden';
    }
    function closeSidebar() {
        sidebar?.classList.remove('open');
        overlay?.classList.remove('open');
        document.body.style.overflow = '';
    }

    toggleBtn?.addEventListener('click', openSidebar);
    overlay?.addEventListener('click', closeSidebar);
    document.addEventListener('keydown', e => { if (e.key === 'Escape') closeSidebar(); });

    // ── Topbar scroll glass effect ─────────────────────────────
    const topbar = document.getElementById('topbar');
    window.addEventListener('scroll', () => {
        if (!topbar) return;
        topbar.style.boxShadow = window.scrollY > 10
            ? '0 2px 12px rgba(0,0,0,0.10)'
            : 'none';
    }, { passive: true });

    // ── Intersection Observer — reveal animations ──────────────
    const revealEls = document.querySelectorAll('.reveal');
    if ('IntersectionObserver' in window) {
        const io = new IntersectionObserver((entries) => {
            entries.forEach((entry, idx) => {
                if (entry.isIntersecting) {
                    setTimeout(() => entry.target.classList.add('visible'), idx * 60);
                    io.unobserve(entry.target);
                }
            });
        }, { threshold: 0.08, rootMargin: '0px 0px -40px 0px' });
        revealEls.forEach(el => io.observe(el));
    } else {
        revealEls.forEach(el => el.classList.add('visible'));
    }

    // ── KPI counter animation ──────────────────────────────────
    if ('IntersectionObserver' in window) {
        const kpiIO = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (!entry.isIntersecting) return;
                const el = entry.target.querySelector('[data-count]');
                if (el && !el.dataset.animated) {
                    animateCounter(el);
                    el.dataset.animated = 'true';
                }
                kpiIO.unobserve(entry.target);
            });
        }, { threshold: 0.3 });
        document.querySelectorAll('.kpi-card').forEach(c => kpiIO.observe(c));
    }

    function animateCounter(el) {
        const target = parseFloat(el.dataset.count ?? '0');
        const isCurr = el.dataset.currency === 'true';
        const duration = 1600;
        const start = performance.now();
        const tick = now => {
            const raw = Math.min((now - start) / duration, 1);
            const ease = 1 - Math.pow(1 - raw, 3);
            const val = Math.round(target * ease);
            el.textContent = isCurr ? val.toLocaleString() + ' EGP' : val.toLocaleString();
            if (raw < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    }

    // ── Nav ripple effect ──────────────────────────────────────
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', function () {
            const ripple = document.createElement('span');
            ripple.style.cssText = `position:absolute;inset:0;border-radius:inherit;
                background:rgba(197,134,10,0.08);
                animation:tibrRipple 0.4s ease forwards;pointer-events:none;`;
            this.style.position = 'relative';
            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 400);
        });
    });

    if (!document.getElementById('tibrRippleStyle')) {
        const s = document.createElement('style');
        s.id = 'tibrRippleStyle';
        s.textContent = `@keyframes tibrRipple{from{opacity:1;transform:scale(.95)}to{opacity:0;transform:scale(1.02)}}`;
        document.head.appendChild(s);
    }

    // ── Notification badge ─────────────────────────────────────
    const notifBtn = document.getElementById('notifBtn');
    const notifCount = document.getElementById('notifCount');
    notifBtn?.addEventListener('click', () => {
        if (notifCount) { notifCount.textContent = '0'; notifCount.style.display = 'none'; }
    });

    // ── Live gold price ticker (simulation) ───────────────────
    const priceEl = document.getElementById('goldPrice');
    if (priceEl) {
        let base = 2342;
        setInterval(() => {
            const delta = (Math.random() - 0.48) * 8;
            base = Math.max(2100, Math.min(2600, base + delta));
            priceEl.textContent = '$' + base.toLocaleString('en-US', { maximumFractionDigits: 0 });
        }, 6000);
    }

    // ── Table row keyboard accessibility ───────────────────────
    document.querySelectorAll('.table-row-anim').forEach(row => {
        row.setAttribute('tabindex', '0');
        row.addEventListener('keydown', e => {
            if (e.key === 'Enter') row.querySelector('a')?.click();
        });
    });

    // ── Auto-dismiss alerts ────────────────────────────────────
    document.querySelectorAll('.alert-tibr').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-8px)';
            setTimeout(() => alert.remove(), 400);
        }, 5000);
    });

    // ── Sidebar badge counts ───────────────────────────────────
    updateSidebarBadges();

    async function updateSidebarBadges() {
        try {
            const b = document.getElementById('badgeProducts');
            if (b) {
                const r = await fetch('/api/Product?pageSize=1&pageNumber=1');
                if (r.ok) { const d = await r.json(); b.textContent = d?.totalCount ?? '—'; }
            }
        } catch { /* silent */ }
        try {
            const b = document.getElementById('badgeOrders');
            if (b) {
                const r = await fetch('/api/Order');
                if (r.ok) {
                    const d = await r.json();
                    b.textContent = Array.isArray(d)
                        ? d.filter(o => o.orderStatus === 'Pending').length : '—';
                }
            }
        } catch { /* silent */ }
        try {
            const b = document.getElementById('badgeSupport');
            if (b) {
                const r = await fetch('/api/Support');
                if (r.ok) {
                    const d = await r.json();
                    b.textContent = Array.isArray(d)
                        ? d.filter(t => t.status === 'Open' || t.status === 'Pending').length : '—';
                }
            }
        } catch { /* silent */ }
    }

    // ── Smooth page transitions ────────────────────────────────
    const content = document.getElementById('pageContent');
    if (content) {
        content.style.opacity = '0';
        requestAnimationFrame(() => {
            content.style.transition = 'opacity 0.35s ease';
            content.style.opacity = '1';
        });
    }

    document.querySelectorAll(
        'a[href]:not([target="_blank"]):not([href^="#"]):not([href^="javascript"]):not([href^="mailto"])'
    ).forEach(link => {
        link.addEventListener('click', e => {
            const href = link.getAttribute('href');
            if (!href || href.startsWith('http')) return;
            e.preventDefault();
            if (content) {
                content.style.transition = 'opacity 0.2s ease';
                content.style.opacity = '0';
                setTimeout(() => { window.location.href = href; }, 200);
            } else {
                window.location.href = href;
            }
        });
    });

});