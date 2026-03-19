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

function attachFilterHandler() {
    const btn = document.getElementById("clientFilterBtn");
    if (btn) {
        // Use onclick to avoid duplicate listeners when the partial is replaced
        btn.onclick = (ev) => {
            ev.preventDefault();
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
            loadClients(1);
        };
    }

    // Listen for Enter and Escape on the search input to trigger filter / clear
    const input = document.getElementById("clientSearchInput");
    if (input) {
        input.onkeydown = (ev) => {
            if (ev.key === "Enter") {
                ev.preventDefault();
                loadClients(1);
            } else if (ev.key === "Escape" || ev.key === "Esc") {
                ev.preventDefault();
                input.value = "";
                input.focus();
                loadClients(1);
            }
        };
    }
}

async function loadClients(page = 1) {
    const container = document.getElementById("clientTableContainer");
    if (!container) return;

    const isDashboard = container.getAttribute("data-is-dashboard") === "true";

    const input = document.getElementById("clientSearchInput");
    const searchTerm = input ? encodeURIComponent(input.value.trim()) : "";
    const searchQuery = searchTerm ? `&searchTerm=${searchTerm}` : "";
    const url = `/Counsellor/ClientTable?page=${page}&isDashboard=${isDashboard}${searchQuery}`;

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