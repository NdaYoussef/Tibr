/**
 * TIBR Admin — admin.js
 * Handles: sidebar toggle, reveal animations, KPI counters,
 * active nav highlighting, topbar scroll effect, notifications.
 * Written as plain TypeScript-compatible ES2020 — no framework needed.
 */

document.addEventListener('DOMContentLoaded', () => {

    // ── Sidebar Toggle (mobile) ───────────────────────────────────
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

    toggleBtn?.addEventListener('click', () => {
        sidebar?.classList.contains('open') ? closeSidebar() : openSidebar();
    });

    overlay?.addEventListener('click', closeSidebar);

    // Close on ESC
    document.addEventListener('keydown', (e) => {
        if (e.key === 'Escape') closeSidebar();
    });

    // ── Topbar scroll glass effect ────────────────────────────────
    const topbar = document.getElementById('topbar');
    window.addEventListener('scroll', () => {
        if (!topbar) return;
        if (window.scrollY > 10) {
            topbar.style.boxShadow = '0 4px 24px rgba(0,0,0,0.4)';
        } else {
            topbar.style.boxShadow = 'none';
        }
    }, { passive: true });

    // ── Intersection Observer — reveal animations ─────────────────
    const revealEls = document.querySelectorAll('.reveal');
    if ('IntersectionObserver' in window) {
        const io = new IntersectionObserver(
            (entries) => {
                entries.forEach((entry, idx) => {
                    if (entry.isIntersecting) {
                        // Stagger siblings slightly
                        const delay = idx * 60;
                        setTimeout(() => {
                            entry.target.classList.add('visible');
                        }, delay);
                        io.unobserve(entry.target);
                    }
                });
            },
            { threshold: 0.08, rootMargin: '0px 0px -40px 0px' }
        );
        revealEls.forEach(el => io.observe(el));
    } else {
        // Fallback for old browsers
        revealEls.forEach(el => el.classList.add('visible'));
    }

    // ── KPI Card counter animation ────────────────────────────────
    // Triggered when card enters viewport via IO
    const kpiCards = document.querySelectorAll('.kpi-card');
    if ('IntersectionObserver' in window) {
        const kpiIO = new IntersectionObserver(
            (entries) => {
                entries.forEach(entry => {
                    if (!entry.isIntersecting) return;
                    const el = entry.target.querySelector('[data-count]');
                    if (el && !el.dataset.animated) {
                        animateCounter(el);
                        el.dataset.animated = 'true';
                    }
                    kpiIO.unobserve(entry.target);
                });
            },
            { threshold: 0.3 }
        );
        kpiCards.forEach(card => kpiIO.observe(card));
    }

    function animateCounter(el) {
        const target = parseFloat(el.dataset.count ?? '0');
        const isCurr = el.dataset.currency === 'true';
        const duration = 1600; // ms
        const start = performance.now();

        const tick = (now) => {
            const raw = Math.min((now - start) / duration, 1);
            // Ease out cubic
            const ease = 1 - Math.pow(1 - raw, 3);
            const value = Math.round(target * ease);

            el.textContent = isCurr
                ? value.toLocaleString('en-US') + ' EGP'
                : value.toLocaleString('en-US');

            if (raw < 1) requestAnimationFrame(tick);
        };
        requestAnimationFrame(tick);
    }

    // ── Active sidebar nav link ───────────────────────────────────
    // Already handled by Razor class binding, but keep hover ripple
    document.querySelectorAll('.nav-item').forEach(item => {
        item.addEventListener('click', function () {
            // Ripple effect
            const ripple = document.createElement('span');
            ripple.style.cssText = `
                position:absolute; inset:0; border-radius:inherit;
                background:rgba(242,202,80,0.08);
                animation: rippleFade 0.4s ease forwards;
                pointer-events:none;
            `;
            this.style.position = 'relative';
            this.appendChild(ripple);
            setTimeout(() => ripple.remove(), 400);
        });
    });

    // Inject ripple keyframe once
    if (!document.getElementById('rippleStyle')) {
        const style = document.createElement('style');
        style.id = 'rippleStyle';
        style.textContent = `
            @keyframes rippleFade {
                from { opacity: 1; transform: scale(0.95); }
                to   { opacity: 0; transform: scale(1.02); }
            }
        `;
        document.head.appendChild(style);
    }

    // ── Notification badge count ──────────────────────────────────
    // In a real implementation, fetch from /api/notifications/unread-count
    // For now it reads the badge value already rendered by the layout.
    const notifBtn = document.getElementById('notifBtn');
    const notifCount = document.getElementById('notifCount');

    notifBtn?.addEventListener('click', () => {
        // Placeholder — a real dropdown would appear here
        notifCount && (notifCount.textContent = '0');
        notifCount && (notifCount.style.display = 'none');
    });

    // ── Gold price ticker simulation ──────────────────────────────
    // In production, replace with SignalR or polling from /api/market/price
    const priceEl = document.getElementById('goldPrice');
    if (priceEl) {
        let basePrice = 3500;
        setInterval(() => {
            const delta = (Math.random() - 0.48) * 8;
            basePrice = Math.max(3200, Math.min(3900, basePrice + delta));
            priceEl.textContent = basePrice.toLocaleString('en-US', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            }) + ' EGP';
        }, 6000);
    }

    // ── Table row hover — add keyboard accessibility ──────────────
    document.querySelectorAll('.table-row-anim').forEach(row => {
        row.setAttribute('tabindex', '0');
        row.addEventListener('keydown', (e) => {
            if (e.key === 'Enter') {
                const link = row.querySelector('a');
                link?.click();
            }
        });
    });

    // ── Auto-dismiss alerts after 5 seconds ───────────────────────
    document.querySelectorAll('.alert-tibr').forEach(alert => {
        setTimeout(() => {
            alert.style.transition = 'opacity 0.4s ease, transform 0.4s ease';
            alert.style.opacity = '0';
            alert.style.transform = 'translateY(-8px)';
            setTimeout(() => alert.remove(), 400);
        }, 5000);
    });

    // ── Sidebar badge dynamic update ──────────────────────────────
    // Fetch live counts for sidebar badges (non-blocking)
    updateSidebarBadges();

    async function updateSidebarBadges() {
        try {
            // Products count
            const badgeProducts = document.getElementById('badgeProducts');
            if (badgeProducts) {
                const res = await fetch('/api/Product?pageSize=1&pageNumber=1');
                if (res.ok) {
                    const data = await res.json();
                    badgeProducts.textContent = data?.totalCount ?? '—';
                }
            }
        } catch {
            // Silently fail — badges are cosmetic
        }

        try {
            // Pending orders
            const badgeOrders = document.getElementById('badgeOrders');
            if (badgeOrders) {
                const res = await fetch('/api/Order');
                if (res.ok) {
                    const data = await res.json();
                    const pending = Array.isArray(data)
                        ? data.filter(o => o.orderStatus === 'Pending').length
                        : '—';
                    badgeOrders.textContent = pending;
                }
            }
        } catch { /* silent */ }

        try {
            // Open support tickets
            const badgeSupport = document.getElementById('badgeSupport');
            if (badgeSupport) {
                const res = await fetch('/api/Support');
                if (res.ok) {
                    const data = await res.json();
                    const open = Array.isArray(data)
                        ? data.filter(t => t.status === 'Open' || t.status === 'InProgress').length
                        : '—';
                    badgeSupport.textContent = open;
                }
            }
        } catch { /* silent */ }
    }

    // ── Smooth page transitions ───────────────────────────────────
    // Fade out content before navigating away
    document.querySelectorAll('a[href]:not([target="_blank"]):not([href^="#"]):not([href^="javascript"])').forEach(link => {
        link.addEventListener('click', (e) => {
            const href = link.getAttribute('href');
            if (!href || href.startsWith('http') || href.startsWith('mailto')) return;

            e.preventDefault();
            const content = document.getElementById('pageContent');
            if (content) {
                content.style.transition = 'opacity 0.2s ease';
                content.style.opacity = '0';
                setTimeout(() => {
                    window.location.href = href;
                }, 200);
            } else {
                window.location.href = href;
            }
        });
    });

    // Fade in on load
    const content = document.getElementById('pageContent');
    if (content) {
        content.style.opacity = '0';
        requestAnimationFrame(() => {
            content.style.transition = 'opacity 0.35s ease';
            content.style.opacity = '1';
        });
    }

});