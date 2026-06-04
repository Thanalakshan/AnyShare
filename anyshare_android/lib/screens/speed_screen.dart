import 'package:flutter/material.dart';
import '../services/network_speed_service.dart';
import '../widgets/action_button.dart';
import '../widgets/status_card.dart';

class SpeedScreen extends StatefulWidget {
  const SpeedScreen({super.key});

  @override
  State<SpeedScreen> createState() => _SpeedScreenState();
}

class _SpeedScreenState extends State<SpeedScreen> {
  final NetworkSpeedService _service = NetworkSpeedService();
  bool notificationEnabled = false;

  Future<void> _toggleNotification() async {
    if (notificationEnabled) {
      await _service.stopNotification();
    } else {
      await _service.startNotification();
    }

    setState(() {
      notificationEnabled = !notificationEnabled;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Network Speed'),
        backgroundColor: const Color(0xFF0A0A0C),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            StatusCard(
              title: 'Notification Status',
              value: notificationEnabled ? 'Enabled' : 'Disabled',
            ),
            const StatusCard(
              title: 'Mode',
              value: 'Live Android traffic monitor',
            ),
            const StatusCard(
              title: 'Network Type',
              value: 'Wi-Fi / Cellular / VPN detected natively',
            ),
            ActionButton(
              title: notificationEnabled
                  ? 'Remove Notification Speed Meter'
                  : 'Show Notification Speed Meter',
              onPressed: _toggleNotification,
            ),
          ],
        ),
      ),
    );
  }
}