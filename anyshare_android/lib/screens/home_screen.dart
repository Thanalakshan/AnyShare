import 'dart:async';

import 'package:flutter/material.dart';

import '../services/clipboard_bridge_service.dart';
import '../services/network_proxy_service.dart';
import '../services/network_speed_service.dart';
import '../services/settings_service.dart';
import '../services/usb_debug_service.dart';
import 'history_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen>
    with WidgetsBindingObserver {
  final NetworkSpeedService _speedService = NetworkSpeedService();
  final UsbDebugService _usbDebugService = UsbDebugService();
  final SettingsService _settingsService = SettingsService();
  final ClipboardBridgeService _clipboardBridge = ClipboardBridgeService();
  final NetworkProxyService _networkProxy = NetworkProxyService();

  bool speedEnabled = false;
  bool clipboardEnabled = false;
  bool networkSharingEnabled = false;
  bool loading = true;
  Timer? _usbMonitor;
  bool _checkingUsbDebugging = false;

  @override
  void initState() {
    super.initState();
    WidgetsBinding.instance.addObserver(this);
    _loadSettings();
    _usbMonitor = Timer.periodic(
      const Duration(seconds: 2),
      (_) => _monitorUsbDebugging(),
    );
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _usbMonitor?.cancel();
    super.dispose();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.resumed) {
      _syncNetworkSharingState();
      _monitorUsbDebugging();
    }
  }

  Future<void> _syncNetworkSharingState() async {
    final enabled = await _networkProxy.isSharingEnabled();
    await _settingsService.setNetworkEnabled(enabled);

    if (!mounted || networkSharingEnabled == enabled) return;

    setState(() {
      networkSharingEnabled = enabled;
    });
  }

  Future<void> _monitorUsbDebugging() async {
    if (!networkSharingEnabled || _checkingUsbDebugging) return;

    _checkingUsbDebugging = true;

    try {
      final enabled = await _usbDebugService.isUsbDebuggingEnabled();

      if (enabled) return;

      await _networkProxy.stopProxy();
      await _settingsService.setNetworkEnabled(false);

      if (!mounted) return;

      setState(() {
        networkSharingEnabled = false;
      });
    } finally {
      _checkingUsbDebugging = false;
    }
  }

  Future<void> _loadSettings() async {
    final serviceRunning = await _speedService.isNotificationRunning();
    final clipboardRunning = await _clipboardBridge.isBridgeRunning();
    final networkEnabled = await _networkProxy.isSharingEnabled();
    final usbDebuggingEnabled =
        await _usbDebugService.isUsbDebuggingEnabled();

    final shouldRunProxy = networkEnabled && usbDebuggingEnabled;

    if (shouldRunProxy) {
      await _networkProxy.startProxy();
    } else {
      await _networkProxy.stopProxy();
    }

    await _settingsService.setSpeedEnabled(serviceRunning);
    await _settingsService.setClipboardEnabled(clipboardRunning);

    if (networkEnabled && !usbDebuggingEnabled) {
      await _settingsService.setNetworkEnabled(false);
    }

    if (!mounted) return;

    setState(() {
      speedEnabled = serviceRunning;
      clipboardEnabled = clipboardRunning;
      networkSharingEnabled = shouldRunProxy;
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

      if (!ok) {
        await _networkProxy.stopProxy();
        await _settingsService.setNetworkEnabled(false);

        if (!mounted) return;

        setState(() {
          networkSharingEnabled = false;
        });

        return;
      }

      await _networkProxy.startProxy();
    } else {
      await _networkProxy.stopProxy();
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

  Widget _infoText(String text) {
    return Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 12),
      decoration: BoxDecoration(
        color: const Color(0xFF0D1B2A),
        borderRadius: BorderRadius.circular(12),
        border: Border.all(color: const Color(0xFF1B3A5A)),
      ),
      child: Text(
        text,
        style: const TextStyle(
          fontSize: 13,
          color: Color(0xFFBFD7FF),
        ),
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

              if (networkSharingEnabled)
                _infoText(
                  'Network proxy is running. Connect the phone with USB debugging, then turn on Network Sharing in the Windows app.',
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
