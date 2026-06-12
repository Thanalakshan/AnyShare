import 'package:flutter/services.dart';

class ClipboardBridgeService {
  static const MethodChannel _channel = MethodChannel('anyshare/clipboard');

  Future<void> start() async {
    await _channel.invokeMethod('startClipboardBridge');
  }

  Future<void> stop() async {
    await _channel.invokeMethod('stopClipboardBridge');
  }

  Future<void> sendAndroidClipboard() async {
    await _channel.invokeMethod('sendAndroidClipboard');
  }

  Future<void> receiveWindowsClipboard() async {
    await _channel.invokeMethod('receiveWindowsClipboard');
  }

  Future<bool> isBridgeRunning() async {
    final result = await _channel.invokeMethod<bool>('isClipboardBridgeRunning');
    return result ?? false;
  }
}