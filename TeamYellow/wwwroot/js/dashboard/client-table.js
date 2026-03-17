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

async function loadClients(page = 1) {
    const container = document.getElementById("clientTableContainer");
    if (!container) return;

    const isDashboard = container.getAttribute("data-is-dashboard") === "true";

    try {
        const response = await fetch(`/Counsellor/ClientTable?page=${page}&isDashboard=${isDashboard}`);
        if (!response.ok) throw new Error("Network response was not ok");

        const html = await response.text();
        container.innerHTML = html;
    } catch (error) {
        console.error("Failed to load clients:", error);
    }
}