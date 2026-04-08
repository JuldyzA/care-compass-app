// Configuration
// Screen width breakpoint for auto-collapse (Bootstrap xl breakpoint)
const BREAKPOINT = 1200;

const sidebarToggle = document.getElementById('sidebarToggle');
const sidebar = document.getElementById('sidebar');
const toggleIcon = sidebarToggle.querySelector('i');

// Track if user has manually toggled the sidebar
let manualToggle = false;
let initialLoad = true; // Track initial page load

/**
 * Trigger ApexCharts resize event
 */
function resizeApexCharts() {
    // Skip on initial load to avoid double render
    if (initialLoad) {
        initialLoad = false;
        return;
    }
    
    // Dispatch custom event immediately
    window.dispatchEvent(new Event('sidebar-toggled'));
    
    // Also dispatch after transition completes (300ms CSS transition)
    setTimeout(() => {
        window.dispatchEvent(new Event('sidebar-toggled'));
    }, 350);
}

/**
 * Update sidebar collapsed state and icon
 */
function updateSidebarState(shouldCollapse, isManual = false) {
    if (isManual) {
        manualToggle = true;
    }

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
    
    // Trigger chart resize after sidebar state changes
    resizeApexCharts();
}

/**
 * Handle responsive sidebar behavior based on screen size
 */
function handleResponsiveSidebar() {
    // Don't auto-collapse/expand if user has manually toggled
    if (manualToggle) {
        return;
    }

    const isSmallScreen = window.innerWidth < BREAKPOINT;
    
    // Small screens: collapse
    // Large screens: expand (default)
    updateSidebarState(isSmallScreen, false);
}

/**
 * Manual toggle handler
 */
sidebarToggle.addEventListener('click', () => {
    const willBeCollapsed = !sidebar.classList.contains('collapsed');
    updateSidebarState(willBeCollapsed, true);
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

// Initial setup
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

        // Match Account/Manage pages to Account sidebar link
        if (href.includes('/user/account') &&
            currentPath.includes('/account/manage')) {
            activeLink = link;
            return;
        }

        // Match Profile subpages (EditProfile) to Profile sidebar link
        if (href.includes('/user/profile') &&
            currentPath.includes('/user/editprofile')) {
            activeLink = link;
            return;
        }

        // Match Counsellor subpages (CreateClient, ClientDetail, EditClient, Email/Password change)
        if (href.includes('/counsellor/clients') &&
            (currentPath.includes('/counsellor/createclient') ||
             currentPath.includes('/counsellor/clientdetail') ||
             currentPath.includes('/counsellor/editclient'))) {
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
            const toggleEl = dropdown.previousElementSibling;
            if (!toggleEl) {
                return;
            }
            const dropdownInstance = bootstrap.Dropdown.getOrCreateInstance(toggleEl);
            dropdownInstance.hide();
        });
    }
});

// Clean up on page unload
window.addEventListener('beforeunload', () => {
    window.removeEventListener('resize', handleResize);
});

// Configure sidebar dropdown to appear to the right
document.addEventListener('DOMContentLoaded', function () {
    const sidebarDropdownToggle = document.getElementById('userMenuDropdown');
    
    if (sidebarDropdownToggle) {
        // Initialize Bootstrap dropdown with custom Popper configuration
        const dropdown = new bootstrap.Dropdown(sidebarDropdownToggle, {
            popperConfig: function(defaultConfig) {
                return {
                    ...defaultConfig,
                    placement: 'right-start',
                    strategy: 'fixed',
                    modifiers: [
                        {
                            name: 'offset',
                            options: {
                                offset: [0, 8], // 8px gap from sidebar
                            },
                        },
                        {
                            name: 'preventOverflow',
                            options: {
                                boundary: 'viewport',
                                padding: 8,
                            },
                        },
                    ],
                };
            }
        });
    }
});