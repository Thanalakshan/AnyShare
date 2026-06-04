import 'package:flutter/material.dart';
import '../services/clipboard_service.dart';
import '../widgets/action_button.dart';

class ClipboardScreen extends StatefulWidget {
  const ClipboardScreen({super.key});

  @override
  State<ClipboardScreen> createState() => _ClipboardScreenState();
}

class _ClipboardScreenState extends State<ClipboardScreen> {
  final ClipboardService _service = ClipboardService();
  final TextEditingController _controller = TextEditingController();

  Future<void> _pasteFromAndroid() async {
    final text = await _service.getClipboardText();
    setState(() {
      _controller.text = text;
    });
  }

  Future<void> _copyToAndroid() async {
    await _service.setClipboardText(_controller.text);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Copied to Android clipboard')),
    );
  }

  Future<void> _sendToWindows() async {
    await _service.sendToWindows(_controller.text);

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('USB bridge will be added next')),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Clipboard Sharing'),
        backgroundColor: const Color(0xFF0A0A0C),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            TextField(
              controller: _controller,
              minLines: 5,
              maxLines: 8,
              decoration: InputDecoration(
                hintText: 'Clipboard text',
                filled: true,
                fillColor: const Color(0xFF111115),
                border: OutlineInputBorder(
                  borderRadius: BorderRadius.circular(12),
                ),
              ),
            ),
            const SizedBox(height: 20),
            ActionButton(
              title: 'Paste from Android Clipboard',
              onPressed: _pasteFromAndroid,
            ),
            ActionButton(
              title: 'Copy to Android Clipboard',
              onPressed: _copyToAndroid,
            ),
            ActionButton(
              title: 'Send to Windows',
              onPressed: _sendToWindows,
            ),
          ],
        ),
      ),
    );
  }
}