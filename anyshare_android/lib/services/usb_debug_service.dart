import 'package:flutter/services.dart';

class UsbDebugService {
  static const MethodChannel _channel = MethodChannel('anyshare/device');

  Future<bool> isUsbDebuggingEnabled() async {
    final result = await _channel.invokeMethod<bool>('isUsbDebuggingEnabled');
    return result ?? false;
  }

  Future<bool> isOverlayPermissionGranted() async {
    final result =
        await _channel.invokeMethod<bool>('isOverlayPermissionGranted');
    return result ?? false;
  }

  Future<void> openOverlayPermissionSettings() async {
    await _channel.invokeMethod('openOverlayPermissionSettings');
  }

  Future<bool> isAccessibilityEnabled() async {
    final result = await _channel.invokeMethod<bool>('isAccessibilityEnabled');
    return result ?? false;
  }

  Future<void> openAccessibilitySettings() async {
    await _channel.invokeMethod('openAccessibilitySettings');
  }
}