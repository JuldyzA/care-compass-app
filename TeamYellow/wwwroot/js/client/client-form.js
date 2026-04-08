document.addEventListener('DOMContentLoaded', function () {
    const statusCheckbox = document.getElementById('Status');
    const statusLabel = document.querySelector('.status-label');

    if (statusCheckbox && statusLabel) {
        statusCheckbox.addEventListener('change', function () {
            statusLabel.textContent = this.checked ? 'Active' : 'Inactive';
        });
    }
});