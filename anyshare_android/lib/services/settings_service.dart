import 'package:shared_preferences/shared_preferences.dart';

class SettingsService {
  static const String speedKey = 'speed_monitor_enabled';
  static const String clipboardKey = 'clipboard_sharing_enabled';
  static const String networkKey = 'network_sharing_enabled';

  Future<bool> getSpeedEnabled() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(speedKey) ?? false;
  }

  Future<bool> getClipboardEnabled() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(clipboardKey) ?? false;
  }

  Future<bool> getNetworkEnabled() async {
    final prefs = await SharedPreferences.getInstance();
    return prefs.getBool(networkKey) ?? false;
  }

  Future<void> setSpeedEnabled(bool value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(speedKey, value);
  }

  Future<void> setClipboardEnabled(bool value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(clipboardKey, value);
  }

  Future<void> setNetworkEnabled(bool value) async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool(networkKey, value);
  }
}