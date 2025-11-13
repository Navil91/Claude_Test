// Voice Recorder - Speech-to-Work-Order Integration

let mediaRecorder = null;
let audioChunks = [];
let audioBlob = null;
let isRecording = false;

// Initialize page
document.addEventListener('DOMContentLoaded', () => {
    checkMicrophoneSupport();
});

function checkMicrophoneSupport() {
    if (!navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showToast('Your browser does not support audio recording', 'error');
        document.getElementById('recordBtn').disabled = true;
    }
}

async function toggleRecording() {
    if (isRecording) {
        stopRecording();
    } else {
        await startRecording();
    }
}

async function startRecording() {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });

        mediaRecorder = new MediaRecorder(stream);
        audioChunks = [];

        mediaRecorder.ondataavailable = (event) => {
            audioChunks.push(event.data);
        };

        mediaRecorder.onstop = () => {
            audioBlob = new Blob(audioChunks, { type: 'audio/webm' });
            displayAudioPlayback();
            stream.getTracks().forEach(track => track.stop());
        };

        mediaRecorder.start();
        isRecording = true;

        // Update UI
        document.getElementById('recordBtn').classList.add('recording');
        document.getElementById('recordBtnText').textContent = 'Stop Recording';
        document.getElementById('recordingStatus').textContent = '🔴 Recording...';
        document.getElementById('recordingStatus').style.color = 'var(--danger-color)';

        showToast('Recording started', 'success');
    } catch (error) {
        console.error('Error starting recording:', error);
        showToast('Failed to access microphone. Please allow microphone access.', 'error');
    }
}

function stopRecording() {
    if (mediaRecorder && isRecording) {
        mediaRecorder.stop();
        isRecording = false;

        // Update UI
        document.getElementById('recordBtn').classList.remove('recording');
        document.getElementById('recordBtnText').textContent = 'Start Recording';
        document.getElementById('recordingStatus').textContent = '✓ Recording complete';
        document.getElementById('recordingStatus').style.color = 'var(--success-color)';

        showToast('Recording stopped', 'success');
    }
}

function displayAudioPlayback() {
    const audioUrl = URL.createObjectURL(audioBlob);
    const audioElement = document.getElementById('audioPlayback');
    audioElement.src = audioUrl;

    document.getElementById('audioControls').style.display = 'block';
    document.getElementById('transcribeBtn').disabled = false;
}

function handleFileUpload(event) {
    const file = event.target.files[0];
    if (!file) return;

    // Validate file type
    const allowedTypes = ['audio/wav', 'audio/webm', 'audio/mpeg', 'audio/ogg', 'audio/mp3'];
    if (!allowedTypes.includes(file.type) && !file.name.match(/\.(wav|webm|mp3|ogg)$/i)) {
        showToast('Please upload a valid audio file (WAV, MP3, WEBM, OGG)', 'error');
        return;
    }

    // Validate file size (max 10MB)
    if (file.size > 10 * 1024 * 1024) {
        showToast('File size must be less than 10MB', 'error');
        return;
    }

    audioBlob = file;

    // Display audio playback
    const audioUrl = URL.createObjectURL(file);
    const audioElement = document.getElementById('audioPlayback');
    audioElement.src = audioUrl;

    document.getElementById('audioControls').style.display = 'block';
    document.getElementById('transcribeBtn').disabled = false;
    document.getElementById('recordingStatus').textContent = `📁 File uploaded: ${file.name}`;
    document.getElementById('recordingStatus').style.color = 'var(--success-color)';

    showToast('Audio file uploaded successfully', 'success');
}

function resetRecording() {
    audioBlob = null;
    audioChunks = [];
    document.getElementById('audioControls').style.display = 'none';
    document.getElementById('transcribeBtn').disabled = true;
    document.getElementById('recordingStatus').textContent = '';
    document.getElementById('audioFile').value = '';
    showToast('Ready to record new audio', 'info');
}

async function transcribeAudio() {
    if (!audioBlob) {
        showToast('Please record or upload audio first', 'error');
        return;
    }

    const locale = document.getElementById('locale').value;
    const statusDiv = document.getElementById('transcriptionStatus');

    try {
        // Disable button and show loading
        document.getElementById('transcribeBtn').disabled = true;
        statusDiv.textContent = '⏳ Transcribing audio...';
        statusDiv.style.color = 'var(--warning-color)';

        // Create a temporary work order ID for transcription
        const tempWorkOrderId = '00000000-0000-0000-0000-000000000000';

        // Convert blob to file with proper extension
        const audioFile = new File([audioBlob], 'recording.webm', { type: audioBlob.type });

        // Call transcription API
        const result = await api.transcribeAudio(tempWorkOrderId, audioFile, locale);

        // Display results
        displayTranscriptionResults(result);

        statusDiv.textContent = '✓ Transcription complete!';
        statusDiv.style.color = 'var(--success-color)';
        showToast('Transcription completed successfully', 'success');
    } catch (error) {
        console.error('Transcription error:', error);
        statusDiv.textContent = `❌ Transcription failed: ${error.message}`;
        statusDiv.style.color = 'var(--danger-color)';
        showToast(error.message, 'error');
        document.getElementById('transcribeBtn').disabled = false;
    }
}

function displayTranscriptionResults(result) {
    // Show result section
    document.getElementById('resultSection').style.display = 'block';

    // Display transcript text
    document.getElementById('transcriptText').textContent = result.text || 'No text recognized';

    // Display extracted fields
    document.getElementById('extractedAsset').textContent = result.fields?.asset || 'Not found';
    document.getElementById('extractedHours').textContent = result.fields?.hours || 'Not found';

    // Display confidence with color coding
    const confidence = result.fields?.confidence || 0;
    const confidenceElement = document.getElementById('extractedConfidence');
    confidenceElement.textContent = `${(confidence * 100).toFixed(1)}%`;

    if (confidence >= 0.8) {
        confidenceElement.className = 'extracted-value confidence-badge badge-success';
    } else if (confidence >= 0.6) {
        confidenceElement.className = 'extracted-value confidence-badge badge-warning';
    } else {
        confidenceElement.className = 'extracted-value confidence-badge badge-danger';
    }

    // Display comment
    document.getElementById('extractedComment').textContent = result.fields?.comment || result.text;

    // Display metadata
    const metadata = result.metadata || {};
    const metadataHtml = `
        <div style="display: grid; gap: 0.5rem;">
            <p><strong>Vendor:</strong> ${metadata.vendor || 'N/A'}</p>
            <p><strong>Language:</strong> ${metadata.language || 'N/A'}</p>
            <p><strong>Duration:</strong> ${metadata.durationSeconds || 'N/A'} seconds</p>
            <p><strong>Processing Time:</strong> ${metadata.processingTimeMs || 'N/A'} ms</p>
            <p><strong>Used LLM Fallback:</strong> ${metadata.usedLlmFallback ? 'Yes' : 'No'}</p>
            ${result.audioUri ? `<p><strong>Audio URI:</strong> <a href="${result.audioUri}" target="_blank">View</a></p>` : ''}
        </div>
    `;
    document.getElementById('metadataContent').innerHTML = metadataHtml;

    // Store result for saving
    window.transcriptionResult = result;

    // Scroll to results
    document.getElementById('resultSection').scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

async function saveExtractedWorkOrder() {
    if (!window.transcriptionResult) {
        showToast('No transcription result to save', 'error');
        return;
    }

    const result = window.transcriptionResult;

    const workOrderData = {
        assetId: result.fields?.asset || null,
        comment: result.fields?.comment || result.text,
        labourHours: result.fields?.hours || null
    };

    try {
        const savedWorkOrder = await api.createWorkOrder(workOrderData);

        showToast('Work order created successfully!', 'success');

        // Redirect to work orders page after 2 seconds
        setTimeout(() => {
            window.location.href = '/workorders.html';
        }, 2000);
    } catch (error) {
        console.error('Error saving work order:', error);
        showToast(error.message, 'error');
    }
}

function resetForm() {
    // Reset audio
    resetRecording();

    // Hide results
    document.getElementById('resultSection').style.display = 'none';

    // Clear stored result
    window.transcriptionResult = null;

    // Reset transcription status
    document.getElementById('transcriptionStatus').textContent = '';

    // Re-enable transcribe button
    document.getElementById('transcribeBtn').disabled = true;

    showToast('Form reset', 'info');
}
