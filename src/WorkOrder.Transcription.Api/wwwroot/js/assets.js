// Assets.js - Asset management functionality

let allAssets = [];
let filteredAssets = [];
let editingAssetId = null;

// Load assets
async function loadAssets() {
    try {
        const activeOnly = document.getElementById('activeOnlyCheckbox').checked;
        allAssets = await api.getAssets(activeOnly);
        filteredAssets = allAssets;
        renderAssets();
    } catch (error) {
        console.error('Error loading assets:', error);
        showToast('Failed to load assets', 'error');
        document.getElementById('assetsTableBody').innerHTML = `
            <tr><td colspan="5" class="loading" style="color: var(--danger-color);">
                Failed to load assets: ${error.message}
            </td></tr>
        `;
    }
}

function renderAssets() {
    const tbody = document.getElementById('assetsTableBody');

    if (filteredAssets.length === 0) {
        tbody.innerHTML = '<tr><td colspan="5" class="loading">No assets found</td></tr>';
        return;
    }

    tbody.innerHTML = filteredAssets.map(asset => `
        <tr>
            <td><strong>${asset.assetId}</strong></td>
            <td>${asset.assetName || '-'}</td>
            <td>${asset.assetType || '-'}</td>
            <td>
                <span class="badge ${asset.isActive ? 'badge-success' : 'badge-danger'}">
                    ${asset.isActive ? 'Active' : 'Inactive'}
                </span>
            </td>
            <td>
                <button class="btn btn-secondary" onclick="editAsset('${asset.id}')">Edit</button>
                <button class="btn btn-danger" onclick="deleteAsset('${asset.id}', '${asset.assetId}')">Delete</button>
            </td>
        </tr>
    `).join('');
}

function filterAssets() {
    const searchTerm = document.getElementById('searchInput').value.toLowerCase();

    filteredAssets = allAssets.filter(asset =>
        asset.assetId.toLowerCase().includes(searchTerm) ||
        (asset.assetName && asset.assetName.toLowerCase().includes(searchTerm)) ||
        (asset.assetType && asset.assetType.toLowerCase().includes(searchTerm))
    );

    renderAssets();
}

function showCreateModal() {
    editingAssetId = null;
    document.getElementById('modalTitle').textContent = 'Create Asset';
    document.getElementById('assetForm').reset();
    document.getElementById('assetGuid').value = '';
    document.getElementById('assetId').removeAttribute('readonly');
    document.getElementById('isActive').checked = true;
    document.getElementById('assetModal').classList.add('show');
}

async function editAsset(id) {
    try {
        const asset = await api.getAssetById(id);
        editingAssetId = id;

        document.getElementById('modalTitle').textContent = 'Edit Asset';
        document.getElementById('assetGuid').value = asset.id;
        document.getElementById('assetId').value = asset.assetId;
        document.getElementById('assetId').setAttribute('readonly', 'readonly');
        document.getElementById('assetName').value = asset.assetName || '';
        document.getElementById('assetType').value = asset.assetType || '';
        document.getElementById('isActive').checked = asset.isActive;

        document.getElementById('assetModal').classList.add('show');
    } catch (error) {
        console.error('Error loading asset:', error);
        showToast('Failed to load asset details', 'error');
    }
}

async function saveAsset(event) {
    event.preventDefault();

    const data = {
        assetId: document.getElementById('assetId').value,
        assetName: document.getElementById('assetName').value || null,
        assetType: document.getElementById('assetType').value || null,
        isActive: document.getElementById('isActive').checked
    };

    try {
        if (editingAssetId) {
            await api.updateAsset(editingAssetId, data);
            showToast('Asset updated successfully', 'success');
        } else {
            await api.createAsset(data);
            showToast('Asset created successfully', 'success');
        }

        closeModal();
        loadAssets();
    } catch (error) {
        console.error('Error saving asset:', error);
        showToast(error.message, 'error');
    }
}

async function deleteAsset(id, assetId) {
    if (!confirm(`Are you sure you want to delete asset "${assetId}"? This will mark it as inactive.`)) {
        return;
    }

    try {
        await api.deleteAsset(id, false); // Soft delete
        showToast('Asset deleted successfully', 'success');
        loadAssets();
    } catch (error) {
        console.error('Error deleting asset:', error);
        showToast('Failed to delete asset', 'error');
    }
}

function closeModal() {
    document.getElementById('assetModal').classList.remove('show');
}

// Close modal when clicking outside
window.onclick = function(event) {
    const modal = document.getElementById('assetModal');
    if (event.target === modal) {
        closeModal();
    }
};

// Initialize page
document.addEventListener('DOMContentLoaded', loadAssets);
