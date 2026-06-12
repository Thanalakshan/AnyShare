import 'package:flutter/services.dart';

class NetworkProxyService {
  static const MethodChannel _channel = MethodChannel('anyshare/device');

  Future<void> startProxy() async {
    await _channel.invokeMethod('startNetworkProxy');
  }

  Future<void> stopProxy() async {
    await _channel.invokeMethod('stopNetworkProxy');
  }

  Future<bool> isProxyRunning() async {
    final result = await _channel.invokeMethod<bool>('isNetworkProxyRunning');
    return result ?? false;
  }

  Future<bool> isSharingEnabled() async {
    final result =
        await _channel.invokeMethod<bool>('isNetworkSharingEnabled');
    return result ?? false;
  }
}
