import 'package:flutter/material.dart';
import '../services/internet_share_service.dart';
import '../widgets/action_button.dart';
import '../widgets/status_card.dart';

class InternetShareScreen extends StatefulWidget {
  const InternetShareScreen({super.key});

  @override
  State<InternetShareScreen> createState() => _InternetShareScreenState();
}

class _InternetShareScreenState extends State<InternetShareScreen> {
  final InternetShareService _service = InternetShareService();
  bool _enabled = false;

  Future<void> _toggle() async {
    final value = await _service.toggleSharing();
    setState(() {
      _enabled = value;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Internet Sharing'),
        backgroundColor: const Color(0xFF0A0A0C),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            const StatusCard(
              title: 'Mode',
              value: 'Android → Windows via USB proxy',
            ),
            StatusCard(
              title: 'Status',
              value: _enabled ? 'Enabled' : 'Disabled',
            ),
            ActionButton(
              title: _enabled ? 'Stop Sharing' : 'Start Sharing',
              onPressed: _toggle,
            ),
          ],
        ),
      ),
    );
  }
}