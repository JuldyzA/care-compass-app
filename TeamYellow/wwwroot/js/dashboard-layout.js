// Configuration
// Screen width breakpoint for auto-collapse (Bootstrap lg breakpoint)
const BREAKPOINT = 992;

const sidebarToggle = document.getElementById('sidebarToggle');
const sidebar = document.getElementById('sidebar');
const toggleIcon = sidebarToggle.querySelector('i');

/**
 * Update sidebar collapsed state and icon
 */
function updateSidebarState(shouldCollapse) {
    if (shouldCollapse) {
        sidebar.classList.add('collapsed');
        sidebarToggle.setAttribute('aria-expanded', 'false');
        // Update icon to chevron-right (>) when collapsed
        toggleIcon.classList.remove('bi-chevron-left');
        toggleIcon.classList.add('bi-chevron-right');
    } else {
        sidebar.classList.remove('collapsed');
        sidebarToggle.setAttribute('aria-expanded', 'true');
        // Update icon to chevron-left (<) when expanded
        toggleIcon.classList.remove('bi-chevron-right');
        toggleIcon.classList.add('bi-chevron-left');
    }
}

/**
 * Handle responsive sidebar behavior based on screen size
 */
function handleResponsiveSidebar() {
    const isSmallScreen = window.innerWidth < BREAKPOINT;
    
    // Small screens: collapse
    // Large screens: expand (default)
    updateSidebarState(isSmallScreen);
}

/**
 * Manual toggle handler
 */
sidebarToggle.addEventListener('click', () => {
    const willBeCollapsed = !sidebar.classList.contains('collapsed');
    updateSidebarState(willBeCollapsed);
});

/**
 * Debounced resize handler for better performance
 */
let resizeTimeout;
function handleResize() {
    clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        handleResponsiveSidebar();
    }, 150);
}

// Window resize listener
window.addEventListener('resize', handleResize);

handleResponsiveSidebar();

/**
 * Set active nav link based on current URL
 * Handles both exact matches and controller/action patterns
 */
function setActiveNavLink() {
    const currentPath = window.location.pathname.toLowerCase();
    const links = document.querySelectorAll('.sidebar-nav .nav-link');

    links.forEach(link => {
        link.classList.remove('active');
        link.removeAttribute('aria-current');
    });

    let activeLink = null;

    links.forEach(link => {
        const href = link.getAttribute('href').toLowerCase();
        const page = link.getAttribute('data-page');

        if (currentPath === href) {
            activeLink = link;
            return;
        }

        if (currentPath === href + '/' || currentPath + '/' === href) {
            activeLink = link;
            return;
        }

        if (page === 'dashboard' &&
            (currentPath === '/counsellor' ||
                currentPath === '/counsellor/' ||
                currentPath === '/counsellor/index')) {
            activeLink = link;
            return;
        }

        if (currentPath.startsWith(href + '/') && href !== '/counsellor') {
            activeLink = link;
            return;
        }
    });

    if (activeLink) {
        activeLink.classList.add('active');
        activeLink.setAttribute('aria-current', 'page');
    }
}

setActiveNavLink();

// Keyboard navigation for sidebar
document.querySelectorAll('.sidebar-nav .nav-link').forEach((link, index, links) => {
    link.addEventListener('keydown', (e) => {
        if (e.key === 'ArrowDown') {
            e.preventDefault();
            const nextLink = links[index + 1];
            if (nextLink) nextLink.focus();
        } else if (e.key === 'ArrowUp') {
            e.preventDefault();
            const prevLink = links[index - 1];
            if (prevLink) prevLink.focus();
        }
    });
});

// Close dropdowns on Escape key
document.addEventListener('keydown', (e) => {
    if (e.key === 'Escape') {
        const dropdowns = document.querySelectorAll('.dropdown-menu.show');
        dropdowns.forEach(dropdown => {
            bootstrap.Dropdown.getInstance(dropdown.previousElementSibling)?.hide();
        });
    }
});

// Clean up on page unload
window.addEventListener('beforeunload', () => {
    window.removeEventListener('resize', handleResize);
});