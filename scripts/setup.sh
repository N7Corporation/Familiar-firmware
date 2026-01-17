#!/bin/bash
# Familiar Firmware Setup Script
# Run as root on Raspberry Pi

set -e

echo "=========================================="
echo "Familiar Firmware Setup"
echo "=========================================="

# Check if running as root
if [ "$EUID" -ne 0 ]; then
    echo "Please run as root (sudo ./setup.sh)"
    exit 1
fi

# Detect Pi model
PI_MODEL=$(cat /proc/device-tree/model 2>/dev/null || echo "Unknown")
echo "Detected: $PI_MODEL"

# Update system
echo ""
echo "Updating system packages..."
apt-get update
apt-get upgrade -y

# Install .NET 8 runtime
echo ""
echo "Installing .NET 8 runtime..."
if ! command -v dotnet &> /dev/null; then
    curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin --channel 8.0 --runtime aspnetcore --install-dir /usr/share/dotnet
    ln -sf /usr/share/dotnet/dotnet /usr/bin/dotnet
fi
dotnet --info

# Install audio dependencies
echo ""
echo "Installing audio dependencies..."
apt-get install -y alsa-utils espeak-ng

# Install WiFi AP dependencies
echo ""
echo "Installing WiFi AP dependencies..."
apt-get install -y hostapd dnsmasq

# Stop services during configuration
systemctl stop hostapd 2>/dev/null || true
systemctl stop dnsmasq 2>/dev/null || true

# Configure hostapd
echo ""
echo "Configuring WiFi Access Point..."
cat > /etc/hostapd/hostapd.conf << 'EOF'
interface=wlan0
driver=nl80211
ssid=Familiar
hw_mode=g
channel=7
wmm_enabled=0
macaddr_acl=0
auth_algs=1
ignore_broadcast_ssid=0
wpa=2
wpa_passphrase=familiar123
wpa_key_mgmt=WPA-PSK
wpa_pairwise=TKIP
rsn_pairwise=CCMP
EOF

# Point hostapd to config
sed -i 's|#DAEMON_CONF=""|DAEMON_CONF="/etc/hostapd/hostapd.conf"|' /etc/default/hostapd

# Configure dnsmasq
echo ""
echo "Configuring DHCP server..."
mv /etc/dnsmasq.conf /etc/dnsmasq.conf.orig 2>/dev/null || true
cat > /etc/dnsmasq.conf << 'EOF'
interface=wlan0
dhcp-range=192.168.4.2,192.168.4.20,255.255.255.0,24h
domain=local
address=/familiar.local/192.168.4.1
EOF

# Configure static IP for wlan0
echo ""
echo "Configuring static IP..."
cat >> /etc/dhcpcd.conf << 'EOF'

# Familiar WiFi AP configuration
interface wlan0
    static ip_address=192.168.4.1/24
    nohook wpa_supplicant
EOF

# Check if Pi Camera is available (Pi 5)
if [[ "$PI_MODEL" == *"Pi 5"* ]]; then
    echo ""
    echo "Pi 5 detected - Installing camera dependencies..."
    apt-get install -y libcamera-apps
fi

# Create application directory
echo ""
echo "Creating application directory..."
mkdir -p /opt/familiar
mkdir -p /opt/familiar/recordings
mkdir -p /var/log/familiar

# Copy systemd service
echo ""
echo "Installing systemd service..."
cp /home/pi/Familiar-firmware/scripts/familiar.service /etc/systemd/system/
systemctl daemon-reload

# Create familiar user if not exists
if ! id "familiar" &>/dev/null; then
    useradd -r -s /bin/false familiar
fi

# Set permissions
chown -R familiar:familiar /opt/familiar
chown -R familiar:familiar /var/log/familiar

# Add familiar user to audio and video groups
usermod -a -G audio,video familiar

# Enable services
echo ""
echo "Enabling services..."
systemctl unmask hostapd
systemctl enable hostapd
systemctl enable dnsmasq
systemctl enable familiar

echo ""
echo "=========================================="
echo "Setup complete!"
echo ""
echo "Next steps:"
echo "1. Build and publish the application:"
echo "   dotnet publish -c Release -o /opt/familiar"
echo ""
echo "2. Edit configuration:"
echo "   nano /opt/familiar/appsettings.json"
echo ""
echo "3. Start the service:"
echo "   sudo systemctl start familiar"
echo ""
echo "4. Reboot to activate WiFi AP:"
echo "   sudo reboot"
echo ""
echo "After reboot, connect to WiFi 'Familiar'"
echo "Password: familiar123"
echo "Access: http://192.168.4.1:8080"
echo "=========================================="
