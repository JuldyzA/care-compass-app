document.addEventListener("DOMContentLoaded", () => {
    loadClients();
});

document.addEventListener("click", (e) => {
    const link = e.target.closest(".client-page-link");
    if (link) {
        e.preventDefault();
        const page = parseInt(link.dataset.page, 10) || 1;
        loadClients(page);
    }
});

function showError(message, timeout = 5000) {
    const container = document.getElementById("error-alert");
    if (!container) {
        alert(message);
        return;
    }

    const alertDiv = document.createElement('div');
    alertDiv.className = 'alert alert-danger alert-dismissible fade show';
    alertDiv.role = 'alert';
    alertDiv.innerHTML = `
        <strong><span>${message}</span></strong>
        <button type="button" class="btn-close" aria-label="Close"></button>
    `;

    container.appendChild(alertDiv);

    const closeBtn = alertDiv.querySelector('.btn-close');
    if (closeBtn) {
        closeBtn.addEventListener('click', () => {
            if (window.bootstrap?.Alert) {
                try { window.bootstrap.Alert.getOrCreateInstance(alertDiv).close(); }
                catch { alertDiv.remove(); }
            } else {
                alertDiv.remove();
            }
        });
    }

    // Auto close after timeout
    setTimeout(() => {
        if (window.bootstrap?.Alert) {
            try { window.bootstrap.Alert.getOrCreateInstance(alertDiv).close(); }
            catch { if (alertDiv.parentNode) alertDiv.parentNode.removeChild(alertDiv); }
        } else {
            if (alertDiv.parentNode) alertDiv.parentNode.removeChild(alertDiv);
        }
    }, timeout);
}

function hideDateError() {
    const container = document.getElementById("error-alert");
    if (!container) return;
    const alerts = Array.from(container.querySelectorAll('.alert'));
    alerts.forEach(a => {
        if (window.bootstrap?.Alert) {
            try { window.bootstrap.Alert.getOrCreateInstance(a).close(); }
            catch { if (a.parentNode) a.parentNode.removeChild(a); }
        } else {
            if (a.parentNode) a.parentNode.removeChild(a);
        }
    });
}

function validateDateRange() {
    const startInput = document.getElementById("clientStartDate");
    const endInput = document.getElementById("clientEndDate");
    if (!startInput || !endInput) {
        return true;
    }

    const startVal = startInput.value;
    const endVal = endInput.value;

    // if one or both are empty, consider valid (allow open-ended ranges)
    if (!startVal || !endVal) {
        hideDateError();
        return true;
    }

    const start = new Date(startVal);
    const end = new Date(endVal);

    if (end < start) {
        showError("End date cannot be before Start date.", 5000);
        setTimeout(() => {
            endInput.focus();
        }, 300);
        return false;
    }

    hideDateError();
    return true;
}

function attachFilterHandler() {
    const btn = document.getElementById("clientFilterBtn");
    if (btn) {
        btn.onclick = (ev) => {
            ev.preventDefault();
            if (!validateDateRange()) return;
            loadClients(1);
        };
    }

    const clearBtn = document.getElementById("clientClearBtn");
    if (clearBtn) {
        clearBtn.onclick = (ev) => {
            ev.preventDefault();
            const input = document.getElementById("clientSearchInput");
            if (input) {
                input.value = "";
                input.focus();
            }
            const start = document.getElementById("clientStartDate");
            const end = document.getElementById("clientEndDate");
            if (start) start.value = "";
            if (end) end.value = "";
            hideDateError();
            loadClients(1);
        };
    }

    const startInput = document.getElementById("clientStartDate");
    const endInput = document.getElementById("clientEndDate");
    if (startInput) startInput.onchange = hideDateError;
    if (endInput) endInput.onchange = hideDateError;

    const input = document.getElementById("clientSearchInput");
    if (input) {
        input.onkeydown = (ev) => {
            if (ev.key === "Enter") {
                ev.preventDefault();
                if (!validateDateRange()) return;
                loadClients(1);
            } else if (ev.key === "Escape" || ev.key === "Esc") {
                ev.preventDefault();
                input.value = "";
                input.focus();
                const start = document.getElementById("clientStartDate");
                const end = document.getElementById("clientEndDate");
                if (start) start.value = "";
                if (end) end.value = "";
                hideDateError();
                loadClients(1);
            }
        };
    }

    // sort the column and change the arrow
    attachSortHandlers();
    updateSortIcons();
}

function attachSortHandlers() {
    const container = document.getElementById("clientTableContainer");
    if (!container) return;

    const headers = Array.from(document.querySelectorAll("#clientsTable thead th.sortable"));
    headers.forEach(th => {
        // remove previous handler if any
        th.onclick = null;
        th.style.cursor = 'pointer';
        th.onclick = (ev) => {
            ev.preventDefault();
            const column = th.dataset.column;
            if (!column) return;

            const currentColumn = container.dataset.sortColumn || "";
            const currentDir = (container.dataset.sortDir || "asc").toLowerCase();

            let nextDir = "asc";
            if (currentColumn === column) {
                nextDir = currentDir === "asc" ? "desc" : "asc";
            } else {
                nextDir = "asc";
            }

            container.dataset.sortColumn = column;
            container.dataset.sortDir = nextDir;

            // go to first page when sort changes
            loadClients(1);
        };
    });
}

function updateSortIcons() {
    const container = document.getElementById("clientTableContainer");
    if (!container) return;

    const sortColumn = container.dataset.sortColumn;
    const sortDir = (container.dataset.sortDir || "asc").toLowerCase();

    const headers = Array.from(document.querySelectorAll("#clientsTable thead th"));
    headers.forEach(th => {
        const icon = th.querySelector("i");
        if (!icon) return;
        const column = th.dataset.column;

        // reset to neutral
        icon.className = "bi bi-chevron-expand text-muted";

        if (column && sortColumn && column === sortColumn) {
            if (sortDir === "asc") {
                icon.className = "bi bi-chevron-up text-primary";
            } else {
                icon.className = "bi bi-chevron-down text-primary";
            }
        }
    });
}

async function loadClients(page = 1) {
    const container = document.getElementById("clientTableContainer");
    if (!container) return;

    const isDashboard = container.getAttribute("data-is-dashboard") === "true";

    const input = document.getElementById("clientSearchInput");
    const startInput = document.getElementById("clientStartDate");
    const endInput = document.getElementById("clientEndDate");

    const searchTerm = input ? encodeURIComponent(input.value.trim()) : "";
    const startDate = startInput && startInput.value ? encodeURIComponent(startInput.value) : "";
    const endDate = endInput && endInput.value ? encodeURIComponent(endInput.value) : "";
    const sortColumn = container.dataset.sortColumn || "";
    const sortDir = container.dataset.sortDir || "";

    const params = [];
    if (searchTerm) params.push(`searchTerm=${searchTerm}`);
    if (startDate) params.push(`startDate=${startDate}`);
    if (endDate) params.push(`endDate=${endDate}`);
    if (sortColumn) params.push(`sortColumn=${encodeURIComponent(sortColumn)}`);
    if (sortDir) params.push(`sortDir=${encodeURIComponent(sortDir)}`);

    const query = params.length ? `&${params.join("&")}` : "";
    const url = `/Counsellor/ClientTable?page=${page}&isDashboard=${isDashboard}${query}`;

    try {
        const response = await fetch(url);
        if (!response.ok) throw new Error("Network response was not ok");

        const html = await response.text();
        container.innerHTML = html;

        // Reattach filter/clear/keyboard handlers because the partial was replaced
        attachFilterHandler();
    } catch (error) {
        console.error("Failed to load clients:", error);
    }
}