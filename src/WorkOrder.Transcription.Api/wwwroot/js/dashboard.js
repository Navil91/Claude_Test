// Dashboard.js - Main dashboard functionality

let allAssets = [];
let allWorkOrders = [];

// Load dashboard data
async function loadDashboard() {
    try {
        // Load assets and work orders in parallel
        const [assets, workOrders, stats] = await Promise.all([
            api.getAssets(false),
            api.getWorkOrders(),
            api.getWorkOrderStatistics()
        ]);

        allAssets = assets;
        allWorkOrders = workOrders;

        // Update statistics
        updateStatistics(assets, stats);

        // Load recent work orders
        loadRecentWorkOrders(workOrders);
    } catch (error) {
        console.error('Error loading dashboard:', error);
        showToast('Failed to load dashboard data', 'error');
    }
}

function updateStatistics(assets, stats) {
    document.getElementById('totalAssets').textContent = assets.length;
    document.getElementById('totalWorkOrders').textContent = stats.totalWorkOrders || 0;
    document.getElementById('totalHours').textContent = (stats.totalLabourHours || 0).toFixed(1);
    document.getElementById('withTranscription').textContent = stats.withTranscription || 0;
}

function loadRecentWorkOrders(workOrders) {
    const container = document.getElementById('recentWorkOrders');

    if (workOrders.length === 0) {
        container.innerHTML = '<p class="loading">No work orders yet. Create your first one!</p>';
        return;
    }

    // Show last 5 work orders
    const recent = workOrders.slice(-5).reverse();

    container.innerHTML = recent.map(wo => `
        <div class="recent-workorder">
            <h4>Work Order ${wo.id.substring(0, 8)}</h4>
            <p><strong>Asset:</strong> ${wo.assetId || 'N/A'}</p>
            <p><strong>Comment:</strong> ${truncate(wo.comment, 100)}</p>
            <p><strong>Labour Hours:</strong> ${wo.labourHours || 'N/A'}</p>
            ${wo.transcriptText ? `
                <p><strong>Transcription:</strong> ${truncate(wo.transcriptText, 80)}</p>
                <span class="badge ${getConfidenceBadgeClass(wo.transcriptConfidence)}">
                    Confidence: ${((wo.transcriptConfidence || 0) * 100).toFixed(0)}%
                </span>
            ` : ''}
        </div>
    `).join('');
}

function getConfidenceBadgeClass(confidence) {
    if (!confidence) return 'badge-warning';
    if (confidence >= 0.8) return 'badge-success';
    if (confidence >= 0.6) return 'badge-warning';
    return 'badge-danger';
}

// Initialize dashboard on page load
document.addEventListener('DOMContentLoaded', loadDashboard);
