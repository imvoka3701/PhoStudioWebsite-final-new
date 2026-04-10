/* --- GLOBAL UI AUDIO SYSTEM --- */
window.playUISound = function(type) {
    try {
        const AudioContext = window.AudioContext || window.webkitAudioContext;
        if (!AudioContext) return;
        const ctx = new AudioContext();
        const osc = ctx.createOscillator();
        const gain = ctx.createGain();
        osc.connect(gain);
        gain.connect(ctx.destination);

        if (type === 'success') {
            osc.type = 'sine';
            osc.frequency.setValueAtTime(523.25, ctx.currentTime); // C5
            osc.frequency.exponentialRampToValueAtTime(1046.50, ctx.currentTime + 0.1); // C6
            gain.gain.setValueAtTime(0, ctx.currentTime);
            gain.gain.linearRampToValueAtTime(0.08, ctx.currentTime + 0.05);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.5);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.5);
        } else if (type === 'delete') {
            osc.type = 'triangle';
            osc.frequency.setValueAtTime(200, ctx.currentTime);
            osc.frequency.exponentialRampToValueAtTime(50, ctx.currentTime + 0.2);
            gain.gain.setValueAtTime(0, ctx.currentTime);
            gain.gain.linearRampToValueAtTime(0.1, ctx.currentTime + 0.05);
            gain.gain.exponentialRampToValueAtTime(0.001, ctx.currentTime + 0.3);
            osc.start(ctx.currentTime);
            osc.stop(ctx.currentTime + 0.3);
        }
    } catch(e) {}
};

document.addEventListener('DOMContentLoaded', () => {
    // 1. Initialize AOS (Animate On Scroll)
    if (typeof AOS !== 'undefined') {
        AOS.init({
            duration: 1200,
            easing: 'ease-out-cubic',
            once: true,
            offset: 100
        });
    }

    // 2. Navbar Scroll Effect
    const navbar = document.querySelector('.navbar-premium');
    if (navbar) {
        window.addEventListener('scroll', () => {
            if (window.scrollY > 50) {
                navbar.classList.add('scrolled');
            } else {
                navbar.classList.remove('scrolled');
            }
        });
    }

    // 3. Lightbox Gallery Logic
    const masonryItems = document.querySelectorAll('.masonry-item');
    const lightbox = document.getElementById('lightbox');
    const lightboxImg = document.getElementById('lightbox-img');
    const lightboxClose = document.getElementById('lightbox-close');

    if (lightbox && masonryItems.length > 0) {
        masonryItems.forEach(item => {
            item.addEventListener('click', () => {
                const imgSrc = item.querySelector('.pf-img').getAttribute('src');
                lightboxImg.setAttribute('src', imgSrc);
                lightbox.classList.add('active');
            });
        });

        lightboxClose.addEventListener('click', () => {
            lightbox.classList.remove('active');
            setTimeout(() => {
                lightboxImg.setAttribute('src', '');
            }, 400); // clear src after animation out
        });

        // Close when clicking outside image
        lightbox.addEventListener('click', (e) => {
            if (e.target === lightbox) {
                lightboxClose.click();
            }
        });
    }

    // 4. Booking Slot Selection & Flash Pulse
    const timeSlots = document.querySelectorAll('.time-slot.available');
    const slotSummary = document.getElementById('selected-time-summary');
    const timeInput = document.getElementById('booking-time-input');
    const summaryCard = document.querySelector('.booking-card.sticky-top');

    if (timeSlots.length > 0) {
        timeSlots.forEach(slot => {
            slot.addEventListener('click', function () {
                // Double check if slot is still available (not booked)
                if (this.classList.contains('available') && !this.classList.contains('booked')) {
                    timeSlots.forEach(s => s.classList.remove('selected'));
                    this.classList.add('selected');

                    const time = this.getAttribute('data-time');
                    if (slotSummary) {
                        slotSummary.textContent = time;
                        // Trigger Pulse on Summary Card
                        if (summaryCard) {
                            summaryCard.classList.remove('summary-pulse');
                            void summaryCard.offsetWidth; // trigger reflow
                            summaryCard.classList.add('summary-pulse');
                        }
                    }
                    if (timeInput) timeInput.value = time;
                } else {
                    console.warn('Attempted to select a booked time slot:', this.getAttribute('data-time'));
                }
            });
        });
    }

    // 5. Basic Form Validation (excludes auth forms which manage themselves)
    const forms = document.querySelectorAll('.needs-validation');
    Array.prototype.slice.call(forms).forEach(function (form) {
        // Allow login and register forms to use standard bootstrap validation before submitting to MVC
        form.addEventListener('submit', function (event) {
            if (!form.checkValidity()) {
                event.preventDefault();
                event.stopPropagation();
            }
            form.classList.add('was-validated');
        }, false);
    });

    /* --- NEW CINEMATIC LOGIC --- */

    // 6. Loading Screen Out
    const loader = document.getElementById('loader');
    window.addEventListener('load', () => {
        if (loader) {
            setTimeout(() => {
                loader.classList.add('hidden');
            }, 500); // 500ms guaranteed cinematic pause before fade out
        }
    });

    // 7. Page Transitions Hijack
    document.querySelectorAll('a').forEach(link => {
        link.addEventListener('click', function (e) {
            const target = this.getAttribute('href');
            // Intercept only internal standard links, not anchors, new tabs, or empty links
            if (target && target.endsWith('.html') && !this.hasAttribute('target')) {
                e.preventDefault();
                document.body.classList.add('page-transitioning');
                setTimeout(() => {
                    window.location.href = target;
                }, 400); // Wait for the fade out to complete
            }
        });
    });

    // 8. Custom Interaction Cursor
    const cursor = document.querySelector('.custom-cursor');
    if (cursor && window.innerWidth > 768) {
        document.addEventListener('mousemove', (e) => {
            cursor.style.left = e.clientX + 'px';
            cursor.style.top = e.clientY + 'px';
        });

        // Hover effect for interactive elements
        const interactables = document.querySelectorAll('a, button, input, textarea, select, .time-slot, .masonry-item, .service-card');
        interactables.forEach(el => {
            el.addEventListener('mouseenter', () => cursor.classList.add('hovering'));
            el.addEventListener('mouseleave', () => cursor.classList.remove('hovering'));
        });
    }

    // 9. Parallax Scroll Effect for Hero
    const heroBg = document.querySelector('.hero-bg');
    if (heroBg && window.innerWidth > 768) {
        window.addEventListener('scroll', () => {
            const scrolled = window.pageYOffset;
            heroBg.style.transform = `scale(1.05) translateY(${scrolled * 0.3}px)`;
        });
    }

    /* ========================================================= */
    /* 10. REALISTIC VIETNAMESE WORKFLOW SIMULATION (localStorage) */
    /* ========================================================= */

    /* ========================================================= */
    /* 10. REALISTIC SaaS WORKFLOW ENGINE & DATA MODELING        */
    /* ========================================================= */

    // 10A0. RESET BROKEN STORAGE (CRITICAL)
    localStorage.removeItem('pho_auth_user');
    localStorage.removeItem('Role');
    localStorage.removeItem('userRole');
    const requiredCaches = ['users', 'services', 'bookings', 'albums', 'locks', 'payments', 'photo_notes'];
    try {
        requiredCaches.forEach(cacheName => {
            const data = localStorage.getItem(cacheName);
            if (!data) {
                localStorage.setItem(cacheName, JSON.stringify([]));
                console.log(`[SaaS Core] Initialized clear LocalStorage schema: ${cacheName}`);
            } else {
                // Validate if JSON corrupted
                const parsed = JSON.parse(data);
                if(!Array.isArray(parsed) && cacheName !== 'photo_notes') {
                    throw new Error(`${cacheName} is not an array.`);
                }
            }
        });
        
        let notesCheck = localStorage.getItem('photo_notes');
        if(!notesCheck || notesCheck === '[]') localStorage.setItem('photo_notes', JSON.stringify({}));

    } catch(e) {
        console.error("[SaaS Recovery] Data Corruption Detected in DB. Executing Factory Reset System.", e);
        requiredCaches.forEach(cacheName => localStorage.removeItem(cacheName));
        localStorage.removeItem('username');
        localStorage.removeItem('role');
        sessionStorage.removeItem('redirected');
        requiredCaches.forEach(cacheName => {
            if(cacheName === 'photo_notes') localStorage.setItem(cacheName, JSON.stringify({}));
            else localStorage.setItem(cacheName, JSON.stringify([]));
        });
        window.location.reload();
        return;
    }

    // Seed Data Engine — Safe Merge: always ensures system accounts exist
    // while PRESERVING all user-registered accounts.
    function seedBaseData() {
        // --- Services ---
        let services = JSON.parse(localStorage.getItem('services') || '[]');
        if (services.length === 0) {
            services = [
                { id: 'srv_1', name: 'Gói Kỷ Yếu Cơ Bản', price: 1500000, desc: 'Chụp hình kỷ yếu ngoại cảnh 1 buổi', active: true },
                { id: 'srv_2', name: 'Gói Phóng Sự Cưới', price: 5000000, desc: 'Phóng sự cưới cao cấp nửa ngày', active: true },
                { id: 'srv_3', name: 'Gói Gia Đình VIP', price: 3000000, desc: 'Chụp gia đình trong Studio (Full concept)', active: true }
            ];
            localStorage.setItem('services', JSON.stringify(services));
        }

        // --- Users: SAFE MERGE ---
        // Read current users from localStorage
        const currentUsers = JSON.parse(localStorage.getItem('users') || '[]');
        
        const systemAccounts = [
            { username: 'admin',        pass: '123', password: '123', role: 'Admin' },
            { username: 'user',         pass: '123', password: '123', role: 'User' },
            { username: 'photographer', pass: '123', password: '123', role: 'Photographer' }
        ];
        const systemUsernames = ['admin', 'user', 'photographer'];

        // Keep ALL non-system accounts (user-registered) exactly as they are
        const registeredUsers = currentUsers.filter(
            u => !systemUsernames.includes(u.username.toLowerCase())
        );

        // Merge: system accounts first, then all registered users
        // System accounts are always re-applied so their data stays canonical
        const mergedUsers = [...systemAccounts, ...registeredUsers];
        localStorage.setItem('users', JSON.stringify(mergedUsers));
        
        console.log(`[SaaS Core] seedBaseData: ${registeredUsers.length} tài khoản đăng ký được giữ lại.`);
    }
    seedBaseData();


    
    // Safety purge of corrupted keys from local testing
    localStorage.removeItem('pho_auth_user');
    localStorage.removeItem('Role');
    localStorage.removeItem('userRole');
    localStorage.removeItem('pho_bookings'); // Wipe v1 bookings array

    // SaaS LOGIC REMOVED -> Fully replaced by ASP.NET Core MVC Backend

    // 10F. GLOBAL BACKGROUND CRON (LOCK CLEARING)
    setInterval(() => {
        let locks = JSON.parse(localStorage.getItem('locks') || '[]');
        const checkCount = locks.length;
        if(checkCount > 0) {
            const now = Date.now();
            locks = locks.filter(l => now < l.expireAt);
            if(locks.length !== checkCount) {
                localStorage.setItem('locks', JSON.stringify(locks));
                console.log("[SaaS Architecture] CRON System cleared expired 15-minute booking locks. Mutating UI states...");
                // Emit custom event if booking page is open so it rerenders
                window.dispatchEvent(new Event('locks_updated'));
            }
        }
    }, 5000); // Check every 5 seconds silently

    // 10G. DEBUG MODE PANEL NUIT INJECT
    const enableDebug = true;
    if (enableDebug) {
        const debugPanel = document.createElement('div');
        debugPanel.id = "system-debug-panel";
        debugPanel.style.position = 'fixed';
        debugPanel.style.bottom = '20px';
        debugPanel.style.left = '20px';
        debugPanel.style.zIndex = '9999';
        
        debugPanel.innerHTML = `
            <button class="btn btn-sm btn-dark border border-secondary" type="button" data-bs-toggle="collapse" data-bs-target="#debugCollapse" aria-expanded="false" aria-controls="debugCollapse">
                <i class="bi bi-bug me-1 text-danger"></i> System Debug
            </button>
            <div class="collapse mt-2" id="debugCollapse">
                <div class="card card-body bg-dark border-secondary p-2 glass-panel" style="width: 250px;">
                    <button class="btn btn-sm btn-outline-warning mb-2 w-100" id="debug-view-db"><i class="bi bi-database"></i> View Realtime DB</button>
                    <button class="btn btn-sm btn-outline-danger w-100" id="debug-reset-sys"><i class="bi bi-arrow-clockwise"></i> Factory Reset</button>
                </div>
            </div>
        `;
        document.body.appendChild(debugPanel);

        document.getElementById('debug-view-db').addEventListener('click', () => {
            const out = {
                users: JSON.parse(localStorage.getItem('users')),
                services: JSON.parse(localStorage.getItem('services')),
                bookings: JSON.parse(localStorage.getItem('bookings')),
                locks: JSON.parse(localStorage.getItem('locks')),
                albums: JSON.parse(localStorage.getItem('albums')),
            };
            console.dir(out);
            alert("Đã dump toàn bộ Database vào vĩnh diện Developer Console (F12)!");
        });

        document.getElementById('debug-reset-sys').addEventListener('click', () => {
            if(confirm("DANGER: FACTORY WIPE. Điều này sẽ xóa vĩnh viễn hệ thống giả lập?")) {
                localStorage.clear();
                window.location.reload();
            }
        });
    }


});
