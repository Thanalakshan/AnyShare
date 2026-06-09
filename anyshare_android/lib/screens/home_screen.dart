import 'package:flutter/material.dart';

import '../services/clipboard_bridge_service.dart';
import '../services/network_speed_service.dart';
import '../services/settings_service.dart';
import '../services/usb_debug_service.dart';
import 'history_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final NetworkSpeedService _speedService = NetworkSpeedService();
  final UsbDebugService _usbDebugService = UsbDebugService();
  final SettingsService _settingsService = SettingsService();
  final ClipboardBridgeService _clipboardBridge = ClipboardBridgeService();

  bool speedEnabled = false;
  bool clipboardEnabled = false;
  bool networkSharingEnabled = false;
  bool loading = true;

  @override
  void initState() {
    super.initState();
    _loadSettings();
  }

  Future<void> _loadSettings() async {
    final serviceRunning = await _speedService.isNotificationRunning();

    await _settingsService.setSpeedEnabled(serviceRunning);
    await _settingsService.setClipboardEnabled(false);
    await _settingsService.setNetworkEnabled(false);

    if (!mounted) return;

    setState(() {
      speedEnabled = serviceRunning;
      clipboardEnabled = false;
      networkSharingEnabled = false;
      loading = false;
    });
  }

  Future<bool> _checkUsbDebugging() async {
    final enabled = await _usbDebugService.isUsbDebuggingEnabled();

    if (!enabled && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(
          content: Text('Turn on USB debugging first, then try again.'),
        ),
      );
    }

    return enabled;
  }

  Future<void> _toggleSpeed(bool value) async {
    if (value) {
      await _speedService.startNotification();
    } else {
      await _speedService.stopNotification();
    }

    await _settingsService.setSpeedEnabled(value);

    if (!mounted) return;

    setState(() {
      speedEnabled = value;
    });
  }

  Future<void> _toggleClipboard(bool value) async {
    if (value) {
      final ok = await _checkUsbDebugging();
      if (!ok) return;

      await _clipboardBridge.start();
    } else {
      await _clipboardBridge.stop();
    }

    await _settingsService.setClipboardEnabled(value);

    if (!mounted) return;

    setState(() {
      clipboardEnabled = value;
    });
  }

  Future<void> _sendClipboardToWindows() async {
    await _clipboardBridge.sendAndroidClipboard();

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Clipboard sent to Windows')),
    );
  }

  Future<void> _receiveClipboardFromWindows() async {
    await _clipboardBridge.receiveWindowsClipboard();

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Clipboard received from Windows')),
    );
  }

  Future<void> _toggleNetworkSharing(bool value) async {
    if (value) {
      final ok = await _checkUsbDebugging();
      if (!ok) return;
    }

    await _settingsService.setNetworkEnabled(value);

    if (!mounted) return;

    setState(() {
      networkSharingEnabled = value;
    });
  }

  Widget _toggleRow({
    required String title,
    required bool value,
    required ValueChanged<bool> onChanged,
  }) {
    return GestureDetector(
      behavior: HitTestBehavior.opaque,
      onTap: () => onChanged(!value),
      child: Container(
        margin: const EdgeInsets.only(bottom: 14),
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 10),
        decoration: BoxDecoration(
          color: const Color(0xFF111115),
          borderRadius: BorderRadius.circular(14),
          border: Border.all(color: const Color(0xFF1E1E24)),
        ),
        child: Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              title,
              style: const TextStyle(
                fontSize: 17,
                fontWeight: FontWeight.bold,
              ),
            ),
            Switch(
              value: value,
              onChanged: onChanged,
            ),
          ],
        ),
      ),
    );
  }

  Widget _smallButton({
    required String title,
    required VoidCallback onPressed,
  }) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      child: OutlinedButton(
        style: OutlinedButton.styleFrom(
          padding: const EdgeInsets.symmetric(vertical: 14),
          side: const BorderSide(color: Color(0xFF3A86FF)),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
          ),
        ),
        onPressed: onPressed,
        child: Text(title),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    if (loading) {
      return const Scaffold(
        body: Center(child: CircularProgressIndicator()),
      );
    }

    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: ListView(
            children: [
              const Text(
                'AnyShare',
                style: TextStyle(
                  fontSize: 32,
                  fontWeight: FontWeight.w900,
                ),
              ),

              const SizedBox(height: 28),

              _toggleRow(
                title: 'Network Speed Monitor',
                value: speedEnabled,
                onChanged: _toggleSpeed,
              ),

              if (speedEnabled)
                _smallButton(
                  title: '7-Day Usage History',
                  onPressed: () {
                    Navigator.push(
                      context,
                      MaterialPageRoute(
                        builder: (_) => const HistoryScreen(),
                      ),
                    );
                  },
                ),

              _toggleRow(
                title: 'Network Sharing',
                value: networkSharingEnabled,
                onChanged: _toggleNetworkSharing,
              ),

              _toggleRow(
                title: 'Clipboard Sharing',
                value: clipboardEnabled,
                onChanged: _toggleClipboard,
              ),

              if (clipboardEnabled) ...[
                _smallButton(
                  title: 'Send Clipboard to Windows',
                  onPressed: _sendClipboardToWindows,
                ),
                _smallButton(
                  title: 'Receive Clipboard from Windows',
                  onPressed: _receiveClipboardFromWindows,
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }
}
