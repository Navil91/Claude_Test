// WorkOrders.js - Work order management functionality

let allWorkOrders = [];
let filteredWorkOrders = [];
let editingWorkOrderId = null;

// Load work orders
async function loadWorkOrders() {
    try {
        allWorkOrders = await api.getWorkOrders();
        filteredWorkOrders = allWorkOrders;
        renderWorkOrders();
    } catch (error) {
        console.error('Error loading work orders:', error);
        showToast('Failed to load work orders', 'error');
        document.getElementById('workOrdersTableBody').innerHTML = `
            <tr><td colspan="7" class="loading" style="color: var(--danger-color);">
                Failed to load work orders: ${error.message}
            </td></tr>
        `;
    }
}

function renderWorkOrders() {
    const tbody = document.getElementById('workOrdersTableBody');

    if (filteredWorkOrders.length === 0) {
        tbody.innerHTML = '<tr><td colspan="7" class="loading">No work orders found</td></tr>';
        return;
    }

    tbody.innerHTML = filteredWorkOrders.map(wo => `
        <tr>
            <td><small>${wo.id.substring(0, 8)}...</small></td>
            <td><strong>${wo.assetId || '-'}</strong></td>
            <td>${truncate(wo.comment, 40)}</td>
            <td>${wo.labourHours || '-'}</td>
            <td>
                ${wo.transcriptText ?
                    `<span class="badge badge-success">Yes</span>` :
                    '<span class="badge badge-warning">No</span>'}
            </td>
            <td>
                ${wo.transcriptConfidence ?
                    `<span class="badge ${getConfidenceBadgeClass(wo.transcriptConfidence)}">
                        ${(wo.transcriptConfidence * 100).toFixed(0)}%
                    </span>` :
                    '-'}
            </td>
            <td>
                <button class="btn btn-secondary" onclick="viewDetails('${wo.id}')">View</button>
                <button class="btn btn-secondary" onclick="editWorkOrder('${wo.id}')">Edit</button>
                <button class="btn btn-danger" onclick="deleteWorkOrder('${wo.id}')">Delete</button>
            </td>
        </tr>
    `).join('');
}

function filterWorkOrders() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();

    filteredWorkOrders = allWorkOrders.filter(wo =>
        (wo.assetId && wo.assetId.toLowerCase().includes(searchTerm)) ||
        (wo.comment && wo.comment.toLowerCase().includes(searchTerm))
    );

    renderWorkOrders();
}

function getConfidenceBadgeClass(confidence) {
    if (confidence >= 0.8) return 'badge-success';
    if (confidence >= 0.6) return 'badge-warning';
    return 'badge-danger';
}

function showCreateModal() {
    editingWorkOrderId = null;
    document.getElementById('modalTitle').textContent = 'Create Work Order';
    document.getElementById('workOrderForm').reset();
    document.getElementById('workOrderGuid').value = '';
    document.getElementById('workOrderModal').classList.add('show');
}

async function editWorkOrder(id) {
    try {
        const wo = await api.getWorkOrderById(id);
        editingWorkOrderId = id;

        document.getElementById('modalTitle').textContent = 'Edit Work Order';
        document.getElementById('workOrderGuid').value = wo.id;
        document.getElementById('assetId').value = wo.assetId || '';
        document.getElementById('comment').value = wo.comment || '';
        document.getElementById('labourHours').value = wo.labourHours || '';

        document.getElementById('workOrderModal').classList.add('show');
    } catch (error) {
        console.error('Error loading work order:', error);
        showToast('Failed to load work order details', 'error');
    }
}

async function saveWorkOrder(event) {
    event.preventDefault();

    const data = {
        assetId: document.getElementById('assetId').value || null,
        comment: document.getElementById('comment').value || null,
        labourHours: parseFloat(document.getElementById('labourHours').value) || null
    };

    try {
        if (editingWorkOrderId) {
            await api.updateWorkOrder(editingWorkOrderId, data);
            showToast('Work order updated successfully', 'success');
        } else {
            await api.createWorkOrder(data);
            showToast('Work order created successfully', 'success');
        }

        closeModal();
        loadWorkOrders();
    } catch (error) {
        console.error('Error saving work order:', error);
        showToast(error.message, 'error');
    }
}

async function deleteWorkOrder(id) {
    if (!confirm('Are you sure you want to delete this work order?')) {
        return;
    }

    try {
        await api.deleteWorkOrder(id);
        showToast('Work order deleted successfully', 'success');
        loadWorkOrders();
    } catch (error) {
        console.error('Error deleting work order:', error);
        showToast('Failed to delete work order', 'error');
    }
}

async function viewDetails(id) {
    try {
        const wo = await api.getWorkOrderById(id);

        const detailsHtml = `
            <div class="result-item">
                <strong>Work Order ID:</strong>
                <p>${wo.id}</p>
            </div>
            <div class="result-item">
                <strong>Asset ID:</strong>
                <p>${wo.assetId || 'N/A'}</p>
            </div>
            <div class="result-item">
                <strong>Comment:</strong>
                <p>${wo.comment || 'N/A'}</p>
            </div>
            <div class="result-item">
                <strong>Labour Hours:</strong>
                <p>${wo.labourHours || 'N/A'}</p>
            </div>
            ${wo.transcriptText ? `
                <div class="result-item">
                    <strong>Transcript Text:</strong>
                    <p class="transcript-text">${wo.transcriptText}</p>
                </div>
                <div class="result-item">
                    <strong>Transcript Confidence:</strong>
                    <p>
                        <span class="badge ${getConfidenceBadgeClass(wo.transcriptConfidence)}">
                            ${(wo.transcriptConfidence * 100).toFixed(2)}%
                        </span>
                    </p>
                </div>
                <div class="result-item">
                    <strong>Locale:</strong>
                    <p>${wo.transcriptLocale || 'N/A'}</p>
                </div>
                <div class="result-item">
                    <strong>Last Transcribed:</strong>
                    <p>${formatDate(wo.lastTranscribedUtc)}</p>
                </div>
            ` : '<p><em>No transcription data available</em></p>'}
        `;

        document.getElementById('workOrderDetails').innerHTML = detailsHtml;
        document.getElementById('detailsModal').classList.add('show');
    } catch (error) {
        console.error('Error loading work order details:', error);
        showToast('Failed to load work order details', 'error');
    }
}

function closeModal() {
    document.getElementById('workOrderModal').classList.remove('show');
}

function closeDetailsModal() {
    document.getElementById('detailsModal').classList.remove('show');
}

// Close modals when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('workOrderModal');
    const detailsModal = document.getElementById('detailsModal');

    if (event.target === modal) {
        closeModal();
    }
    if (event.target === detailsModal) {
        closeDetailsModal();
    }
};

// Initialize page
document.addEventListener('DOMContentLoaded', loadWorkOrders);
