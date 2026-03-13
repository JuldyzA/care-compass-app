"user strict";

document.addEventListener("DOMContentLoaded", () => {
    loadClients();
});

async function loadClients(page = 1) {
    try {
        const response = await fetch(`/Counsellor/ClientTable?page=${page}`);

        if (!response.ok) throw new Error('Network response was not ok');

        const html = await response.text();
        const container = document.getElementById("clientTableContainer");

        if (container) {
            container.innerHTML = html;
        }
    } catch (error) {
        console.error("Failed to load clients:", error);
    }
}