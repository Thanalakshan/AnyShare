import 'package:flutter/services.dart';

class NetworkSpeedService {
  static const MethodChannel _channel =
      MethodChannel('anyshare/network_speed');

  Future<void> startNotification() async {
    await _channel.invokeMethod('startSpeedNotification');
  }

  Future<void> stopNotification() async {
    await _channel.invokeMethod('stopSpeedNotification');
  }
}