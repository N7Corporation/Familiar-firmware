// Familiar Handler Console - WebSocket Audio Client

class FamiliarClient {
    constructor() {
        this.audioDownlinkWs = null;
        this.audioUplinkWs = null;
        this.videoWs = null;
        this.audioContext = null;
        this.mediaStream = null;
        this.mediaRecorder = null;
        this.isRecording = false;
        this.isPttActive = false;
        this.token = null;

        this.init();
    }

    async init() {
        // Check for existing token
        this.token = localStorage.getItem('familiar_token');

        if (this.token && await this.validateToken()) {
            this.showMainUI();
            this.setupEventListeners();
            await this.checkStatus();
            this.connectWebSockets();
            setInterval(() => this.checkStatus(), 10000);
        } else {
            this.showLoginUI();
        }

        this.setupLoginListeners();
    }

    setupLoginListeners() {
        const loginForm = document.getElementById('login-form');
        const logoutBtn = document.getElementById('logout-btn');

        loginForm.addEventListener('submit', async (e) => {
            e.preventDefault();
            await this.login();
        });

        logoutBtn.addEventListener('click', () => this.logout());
    }

    async login() {
        const pinInput = document.getElementById('pin-input');
        const errorDiv = document.getElementById('login-error');
        const loginBtn = document.getElementById('login-btn');

        const pin = pinInput.value.trim();
        if (!pin) {
            errorDiv.textContent = 'Please enter a PIN';
            return;
        }

        loginBtn.disabled = true;
        errorDiv.textContent = '';

        try {
            const response = await fetch('/api/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ pin })
            });

            if (response.ok) {
                const data = await response.json();
                this.token = data.token;
                localStorage.setItem('familiar_token', this.token);

                this.showMainUI();
                this.setupEventListeners();
                await this.checkStatus();
                this.connectWebSockets();
                setInterval(() => this.checkStatus(), 10000);
            } else if (response.status === 401) {
                errorDiv.textContent = 'Invalid PIN';
            } else if (response.status === 429) {
                errorDiv.textContent = 'Too many attempts. Please wait.';
            } else {
                errorDiv.textContent = 'Login failed. Please try again.';
            }
        } catch (error) {
            console.error('Login error:', error);
            errorDiv.textContent = 'Connection error. Please try again.';
        } finally {
            loginBtn.disabled = false;
            pinInput.value = '';
        }
    }

    async validateToken() {
        try {
            const response = await fetch('/api/auth/validate', {
                headers: { 'Authorization': `Bearer ${this.token}` }
            });
            return response.ok;
        } catch {
            return false;
        }
    }

    logout() {
        this.token = null;
        localStorage.removeItem('familiar_token');

        // Close WebSocket connections
        if (this.audioDownlinkWs) {
            this.audioDownlinkWs.close();
            this.audioDownlinkWs = null;
        }
        if (this.audioUplinkWs) {
            this.audioUplinkWs.close();
            this.audioUplinkWs = null;
        }
        if (this.videoWs) {
            this.videoWs.close();
            this.videoWs = null;
        }

        this.showLoginUI();
    }

    showLoginUI() {
        document.getElementById('login-modal').style.display = 'flex';
        document.getElementById('main-container').style.display = 'none';
        document.getElementById('pin-input').focus();
    }

    showMainUI() {
        document.getElementById('login-modal').style.display = 'none';
        document.getElementById('main-container').style.display = 'block';
    }

    getAuthHeaders() {
        return {
            'Authorization': `Bearer ${this.token}`,
            'Content-Type': 'application/json'
        };
    }

    setupEventListeners() {
        // PTT Button
        const pttBtn = document.getElementById('ptt-btn');
        pttBtn.addEventListener('mousedown', () => this.startPtt());
        pttBtn.addEventListener('mouseup', () => this.stopPtt());
        pttBtn.addEventListener('mouseleave', () => this.stopPtt());
        pttBtn.addEventListener('touchstart', (e) => {
            e.preventDefault();
            this.startPtt();
        });
        pttBtn.addEventListener('touchend', (e) => {
            e.preventDefault();
            this.stopPtt();
        });

        // Volume control
        const volumeSlider = document.getElementById('volume');
        volumeSlider.addEventListener('input', (e) => {
            const value = e.target.value;
            document.getElementById('volume-value').textContent = `${value}%`;
            this.setVolume(value / 100);
        });

        // TTS
        document.getElementById('tts-speak').addEventListener('click', () => this.speak());
        document.getElementById('tts-meshtastic').addEventListener('click', () => this.sendMeshtastic());

        // Quick messages
        document.getElementById('quick-messages').addEventListener('click', (e) => {
            if (e.target.dataset.msg) {
                document.getElementById('tts-text').value = e.target.dataset.msg;
                this.speak();
            }
        });

        // Video controls
        document.getElementById('video-subscribe').addEventListener('click', () => this.toggleVideoStream());
        document.getElementById('video-snapshot').addEventListener('click', () => this.takeSnapshot());
        document.getElementById('video-record').addEventListener('click', () => this.toggleRecording());
    }

    async checkStatus() {
        try {
            const response = await fetch('/api/status', {
                headers: this.getAuthHeaders()
            });

            if (response.status === 401) {
                this.logout();
                return;
            }

            const status = await response.json();

            document.getElementById('sys-uptime').textContent = this.formatUptime(status.uptimeSeconds);
            document.getElementById('sys-camera').textContent = status.cameraAvailable ? 'Available' : 'Not available';

            // Show/hide video panel based on camera availability
            document.getElementById('video-panel').style.display =
                status.cameraAvailable ? 'block' : 'none';

            // Meshtastic status
            const meshResponse = await fetch('/api/meshtastic/status', {
                headers: this.getAuthHeaders()
            });
            const meshStatus = await meshResponse.json();
            document.getElementById('mesh-connected').textContent = meshStatus.connected ? 'Connected' : 'Disconnected';
            document.getElementById('mesh-nodes').textContent = meshStatus.nodeCount;
        } catch (error) {
            console.error('Status check failed:', error);
        }
    }

    formatUptime(seconds) {
        const hours = Math.floor(seconds / 3600);
        const mins = Math.floor((seconds % 3600) / 60);
        return `${hours}h ${mins}m`;
    }

    connectWebSockets() {
        this.connectAudioDownlink();
        this.connectAudioUplink();
    }

    connectAudioDownlink() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.audioDownlinkWs = new WebSocket(
            `${protocol}//${window.location.host}/ws/audio/down?access_token=${encodeURIComponent(this.token)}`
        );

        this.audioDownlinkWs.onopen = () => {
            console.log('Audio downlink connected');
            this.sendJson(this.audioDownlinkWs, { type: 'start' });
            this.updateConnectionStatus(true);
            document.getElementById('ptt-btn').disabled = false;
        };

        this.audioDownlinkWs.onclose = () => {
            console.log('Audio downlink disconnected');
            this.updateConnectionStatus(false);
            document.getElementById('ptt-btn').disabled = true;
            if (this.token) {
                setTimeout(() => this.connectAudioDownlink(), 3000);
            }
        };

        this.audioDownlinkWs.onerror = (error) => {
            console.error('Audio downlink error:', error);
        };

        this.audioDownlinkWs.onmessage = (event) => {
            if (typeof event.data === 'string') {
                const msg = JSON.parse(event.data);
                console.log('Downlink message:', msg);
            }
        };
    }

    connectAudioUplink() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.audioUplinkWs = new WebSocket(
            `${protocol}//${window.location.host}/ws/audio/up?access_token=${encodeURIComponent(this.token)}`
        );

        this.audioUplinkWs.onopen = () => {
            console.log('Audio uplink connected');
            this.sendJson(this.audioUplinkWs, { type: 'subscribe' });
        };

        this.audioUplinkWs.onclose = () => {
            console.log('Audio uplink disconnected');
            if (this.token) {
                setTimeout(() => this.connectAudioUplink(), 3000);
            }
        };

        this.audioUplinkWs.onerror = (error) => {
            console.error('Audio uplink error:', error);
        };

        this.audioUplinkWs.onmessage = async (event) => {
            if (event.data instanceof Blob) {
                // Audio data from cosplayer - play it
                await this.playAudioBlob(event.data);
                this.animateMeter('uplink-meter');
            } else {
                const msg = JSON.parse(event.data);
                console.log('Uplink message:', msg);

                // Handle speaking state changes
                if (msg.type === 'speaking') {
                    this.updateCosplayerSpeaking(msg.active);
                }
            }
        };
    }

    updateCosplayerSpeaking(isSpeaking) {
        const indicator = document.getElementById('cosplayer-speaking');
        const text = indicator.querySelector('.speaking-text');

        if (isSpeaking) {
            indicator.classList.add('active');
            text.textContent = 'Cosplayer Speaking';
        } else {
            indicator.classList.remove('active');
            text.textContent = 'Cosplayer Silent';
        }
    }

    async playAudioBlob(blob) {
        if (!this.audioContext) {
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
        }

        try {
            const arrayBuffer = await blob.arrayBuffer();
            const audioBuffer = await this.audioContext.decodeAudioData(arrayBuffer);
            const source = this.audioContext.createBufferSource();
            source.buffer = audioBuffer;
            source.connect(this.audioContext.destination);
            source.start();
        } catch (error) {
            // PCM data - need to convert
            console.debug('Raw PCM audio received');
        }
    }

    animateMeter(meterId) {
        const meter = document.getElementById(meterId);
        meter.classList.add('active');
        setTimeout(() => meter.classList.remove('active'), 100);
    }

    updateConnectionStatus(connected) {
        const status = document.getElementById('connection-status');
        status.textContent = connected ? 'Connected' : 'Disconnected';
        status.className = `status ${connected ? 'connected' : 'disconnected'}`;
    }

    async startPtt() {
        if (this.isPttActive) return;
        this.isPttActive = true;

        const pttBtn = document.getElementById('ptt-btn');
        pttBtn.classList.add('active');

        // Get microphone access
        if (!this.mediaStream) {
            try {
                this.mediaStream = await navigator.mediaDevices.getUserMedia({
                    audio: {
                        echoCancellation: true,
                        noiseSuppression: true,
                        sampleRate: 16000
                    }
                });
            } catch (error) {
                console.error('Microphone access denied:', error);
                this.isPttActive = false;
                pttBtn.classList.remove('active');
                return;
            }
        }

        // Start recording and sending audio
        this.startAudioCapture();
    }

    stopPtt() {
        if (!this.isPttActive) return;
        this.isPttActive = false;

        const pttBtn = document.getElementById('ptt-btn');
        pttBtn.classList.remove('active');

        this.stopAudioCapture();
    }

    startAudioCapture() {
        if (!this.mediaStream) return;

        const audioContext = new (window.AudioContext || window.webkitAudioContext)({
            sampleRate: 16000
        });

        const source = audioContext.createMediaStreamSource(this.mediaStream);
        const processor = audioContext.createScriptProcessor(4096, 1, 1);

        processor.onaudioprocess = (e) => {
            if (!this.isPttActive) return;

            const inputData = e.inputBuffer.getChannelData(0);
            const pcmData = new Int16Array(inputData.length);

            for (let i = 0; i < inputData.length; i++) {
                pcmData[i] = Math.max(-32768, Math.min(32767, inputData[i] * 32768));
            }

            if (this.audioDownlinkWs && this.audioDownlinkWs.readyState === WebSocket.OPEN) {
                this.audioDownlinkWs.send(pcmData.buffer);
                this.animateMeter('downlink-meter');
            }
        };

        source.connect(processor);
        processor.connect(audioContext.destination);

        this.captureContext = audioContext;
        this.captureProcessor = processor;
    }

    stopAudioCapture() {
        if (this.captureProcessor) {
            this.captureProcessor.disconnect();
            this.captureProcessor = null;
        }
        if (this.captureContext) {
            this.captureContext.close();
            this.captureContext = null;
        }
    }

    setVolume(level) {
        if (this.audioDownlinkWs && this.audioDownlinkWs.readyState === WebSocket.OPEN) {
            this.sendJson(this.audioDownlinkWs, { type: 'volume', level });
        }
    }

    async speak() {
        const text = document.getElementById('tts-text').value.trim();
        if (!text) return;

        try {
            const response = await fetch('/api/tts/speak', {
                method: 'POST',
                headers: this.getAuthHeaders(),
                body: JSON.stringify({ text })
            });

            if (response.status === 401) {
                this.logout();
                return;
            }

            if (response.ok) {
                console.log('TTS message sent');
            }
        } catch (error) {
            console.error('TTS failed:', error);
        }
    }

    async sendMeshtastic() {
        const text = document.getElementById('tts-text').value.trim();
        if (!text) return;

        try {
            const response = await fetch('/api/meshtastic/send', {
                method: 'POST',
                headers: this.getAuthHeaders(),
                body: JSON.stringify({ text })
            });

            if (response.status === 401) {
                this.logout();
                return;
            }

            if (response.ok) {
                console.log('Meshtastic message sent');
            }
        } catch (error) {
            console.error('Meshtastic send failed:', error);
        }
    }

    // Video streaming methods
    toggleVideoStream() {
        const btn = document.getElementById('video-subscribe');

        if (this.videoWs && this.videoWs.readyState === WebSocket.OPEN) {
            this.sendJson(this.videoWs, { type: 'unsubscribe' });
            this.videoWs.close();
            this.videoWs = null;
            btn.textContent = 'Start Stream';
        } else {
            this.connectVideoStream();
            btn.textContent = 'Stop Stream';
        }
    }

    connectVideoStream() {
        const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
        this.videoWs = new WebSocket(
            `${protocol}//${window.location.host}/ws/video?access_token=${encodeURIComponent(this.token)}`
        );

        this.videoWs.onopen = () => {
            console.log('Video stream connected');
            this.sendJson(this.videoWs, { type: 'subscribe' });
        };

        this.videoWs.onclose = () => {
            console.log('Video stream disconnected');
            document.getElementById('video-subscribe').textContent = 'Start Stream';
        };

        this.videoWs.onmessage = async (event) => {
            if (event.data instanceof Blob) {
                // JPEG frame - display on canvas
                const img = new Image();
                img.onload = () => {
                    const canvas = document.getElementById('video-canvas');
                    const ctx = canvas.getContext('2d');
                    canvas.width = img.width;
                    canvas.height = img.height;
                    ctx.drawImage(img, 0, 0);
                };
                img.src = URL.createObjectURL(event.data);
            }
        };
    }

    async takeSnapshot() {
        try {
            const response = await fetch('/api/camera/snapshot', {
                headers: this.getAuthHeaders()
            });

            if (response.status === 401) {
                this.logout();
                return;
            }

            if (response.ok) {
                const blob = await response.blob();
                const url = URL.createObjectURL(blob);

                // Display on canvas
                const img = new Image();
                img.onload = () => {
                    const canvas = document.getElementById('video-canvas');
                    const ctx = canvas.getContext('2d');
                    canvas.width = img.width;
                    canvas.height = img.height;
                    ctx.drawImage(img, 0, 0);
                };
                img.src = url;
            }
        } catch (error) {
            console.error('Snapshot failed:', error);
        }
    }

    async toggleRecording() {
        const btn = document.getElementById('video-record');

        if (this.isRecording) {
            try {
                const response = await fetch('/api/camera/recording/stop', {
                    method: 'POST',
                    headers: this.getAuthHeaders()
                });

                if (response.status === 401) {
                    this.logout();
                    return;
                }

                btn.textContent = 'Record';
                btn.classList.remove('recording');
                this.isRecording = false;
            } catch (error) {
                console.error('Stop recording failed:', error);
            }
        } else {
            try {
                const filename = `recording_${Date.now()}.mp4`;
                const response = await fetch('/api/camera/recording/start', {
                    method: 'POST',
                    headers: this.getAuthHeaders(),
                    body: JSON.stringify({ filename })
                });

                if (response.status === 401) {
                    this.logout();
                    return;
                }

                if (response.ok) {
                    btn.textContent = 'Stop';
                    btn.classList.add('recording');
                    this.isRecording = true;
                }
            } catch (error) {
                console.error('Start recording failed:', error);
            }
        }
    }

    sendJson(ws, data) {
        if (ws && ws.readyState === WebSocket.OPEN) {
            ws.send(JSON.stringify(data));
        }
    }
}

// Initialize on load
document.addEventListener('DOMContentLoaded', () => {
    window.familiarClient = new FamiliarClient();
});
