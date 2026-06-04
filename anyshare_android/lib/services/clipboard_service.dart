import 'package:flutter/services.dart';

class ClipboardService {
  Future<String> getClipboardText() async {
    final data = await Clipboard.getData(Clipboard.kTextPlain);
    return data?.text ?? '';
  }

  Future<void> setClipboardText(String text) async {
    await Clipboard.setData(ClipboardData(text: text));
  }

  Future<void> sendToWindows(String text) async {
    // Later: send through USB/WebSocket bridge.
  }
}