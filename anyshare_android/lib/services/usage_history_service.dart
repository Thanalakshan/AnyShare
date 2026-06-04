import 'dart:convert';
import 'package:shared_preferences/shared_preferences.dart';
import '../models/daily_usage.dart';

class UsageHistoryService {
  static const String _key = 'anyshare_usage_history';

  Future<List<DailyUsage>> getHistory() async {
    final prefs = await SharedPreferences.getInstance();
    final raw = prefs.getString(_key);

    if (raw == null) return [];

    final List decoded = jsonDecode(raw);
    return decoded.map((item) => DailyUsage.fromJson(item)).toList();
  }

  Future<void> resetHistory() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_key);
  }
}