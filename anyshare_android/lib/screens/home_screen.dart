import 'package:flutter/material.dart';
import '../widgets/action_button.dart';
import '../widgets/status_card.dart';
import '../services/network_speed_service.dart';
import 'clipboard_screen.dart';
import 'internet_share_screen.dart';
import 'history_screen.dart';

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key});

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  final NetworkSpeedService _speedService = NetworkSpeedService();
  bool speedEnabled = false;

  void _open(BuildContext context, Widget screen) {
    Navigator.push(context, MaterialPageRoute(builder: (_) => screen));
  }

  Future<void> _toggleSpeed() async {
    if (speedEnabled) {
      await _speedService.stopNotification();
    } else {
      await _speedService.startNotification();
    }

    setState(() {
      speedEnabled = !speedEnabled;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Text('AnyShare',
                  style: TextStyle(fontSize: 32, fontWeight: FontWeight.w900)),
              const SizedBox(height: 6),
              const Text('USB Clipboard + Android Internet Sharing',
                  style: TextStyle(color: Color(0xFF6B7280))),
              const SizedBox(height: 24),

              StatusCard(
                title: 'Notification Speed Meter',
                value: speedEnabled ? 'Enabled' : 'Disabled',
              ),

              SwitchListTile(
                value: speedEnabled,
                onChanged: (_) => _toggleSpeed(),
                title: const Text('Show network speed notification'),
                subtitle: const Text('Swipe-proof foreground notification'),
              ),

              const SizedBox(height: 20),

              ActionButton(
                title: 'Clipboard Sharing',
                onPressed: () => _open(context, const ClipboardScreen()),
              ),
              ActionButton(
                title: 'Android → Windows Internet Sharing',
                onPressed: () => _open(context, const InternetShareScreen()),
              ),
              ActionButton(
                title: '7-Day Usage History',
                onPressed: () => _open(context, const HistoryScreen()),
              ),
            ],
          ),
        ),
      ),
    );
  }
}