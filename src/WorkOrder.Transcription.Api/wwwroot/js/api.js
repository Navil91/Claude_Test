// API Base Configuration
const API_BASE_URL = window.location.origin;

// API Helper Functions
const api = {
    // Assets
    async getAssets(activeOnly = false) {
        const response = await fetch(`${API_BASE_URL}/api/assets?activeOnly=${activeOnly}`);
        if (!response.ok) throw new Error('Failed to fetch assets');
        return await response.json();
    },

    async getAssetById(id) {
        const response = await fetch(`${API_BASE_URL}/api/assets/${id}`);
        if (!response.ok) throw new Error('Failed to fetch asset');
        return await response.json();
    },

    async createAsset(data) {
        const response = await fetch(`${API_BASE_URL}/api/assets`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to create asset');
        }
        return await response.json();
    },

    async updateAsset(id, data) {
        const response = await fetch(`${API_BASE_URL}/api/assets/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to update asset');
        }
        return await response.json();
    },

    async deleteAsset(id, hardDelete = false) {
        const response = await fetch(`${API_BASE_URL}/api/assets/${id}?hardDelete=${hardDelete}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error('Failed to delete asset');
    },

    // Work Orders
    async getWorkOrders() {
        const response = await fetch(`${API_BASE_URL}/api/workorders`);
        if (!response.ok) throw new Error('Failed to fetch work orders');
        return await response.json();
    },

    async getWorkOrderById(id) {
        const response = await fetch(`${API_BASE_URL}/api/workorders/${id}`);
        if (!response.ok) throw new Error('Failed to fetch work order');
        return await response.json();
    },

    async createWorkOrder(data) {
        const response = await fetch(`${API_BASE_URL}/api/workorders`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to create work order');
        }
        return await response.json();
    },

    async updateWorkOrder(id, data) {
        const response = await fetch(`${API_BASE_URL}/api/workorders/${id}`, {
            method: 'PUT',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        });
        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to update work order');
        }
        return await response.json();
    },

    async deleteWorkOrder(id) {
        const response = await fetch(`${API_BASE_URL}/api/workorders/${id}`, {
            method: 'DELETE'
        });
        if (!response.ok) throw new Error('Failed to delete work order');
    },

    async getWorkOrderStatistics() {
        const response = await fetch(`${API_BASE_URL}/api/workorders/statistics`);
        if (!response.ok) throw new Error('Failed to fetch statistics');
        return await response.json();
    },

    // Transcription
    async transcribeAudio(workOrderId, audioFile, locale = 'en-GB') {
        const formData = new FormData();
        formData.append('audio', audioFile);

        const response = await fetch(`${API_BASE_URL}/api/workorders/${workOrderId}/transcribe?locale=${locale}`, {
            method: 'POST',
            body: formData
        });

        if (!response.ok) {
            const error = await response.json();
            throw new Error(error.error || 'Failed to transcribe audio');
        }
        return await response.json();
    }
};

// Utility Functions
function showToast(message, type = 'info') {
    // Simple toast notification
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.textContent = message;
    toast.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 1rem 1.5rem;
        background: ${type === 'success' ? '#10b981' : type === 'error' ? '#ef4444' : '#3b82f6'};
        color: white;
        border-radius: 0.5rem;
        box-shadow: 0 10px 15px -3px rgba(0, 0, 0, 0.1);
        z-index: 9999;
        animation: slideIn 0.3s ease;
    `;
    document.body.appendChild(toast);

    setTimeout(() => {
        toast.style.animation = 'slideOut 0.3s ease';
        setTimeout(() => toast.remove(), 300);
    }, 3000);
}

function formatDate(dateString) {
    if (!dateString) return '-';
    const date = new Date(dateString);
    return date.toLocaleString();
}

function truncate(str, length = 50) {
    if (!str) return '-';
    return str.length > length ? str.substring(0, length) + '...' : str;
}

// Add toast animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
`;
document.head.appendChild(style);
