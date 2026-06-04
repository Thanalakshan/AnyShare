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

  Future<List<DailyUsage>> addTodaySample({
    required double downloadMB,
    required double uploadMB,
  }) async {
    final prefs = await SharedPreferences.getInstance();
    final history = await getHistory();

    final today = DateTime.now().toIso8601String().split('T').first;

    final index = history.indexWhere((item) => item.date == today);

    if (index >= 0) {
      final old = history[index];
      history[index] = DailyUsage(
        date: old.date,
        downloadMB: old.downloadMB + downloadMB,
        uploadMB: old.uploadMB + uploadMB,
        totalMB: old.totalMB + downloadMB + uploadMB,
      );
    } else {
      history.add(
        DailyUsage(
          date: today,
          downloadMB: downloadMB,
          uploadMB: uploadMB,
          totalMB: downloadMB + uploadMB,
        ),
      );
    }

    final last7 = history.length > 7
        ? history.sublist(history.length - 7)
        : history;

    await prefs.setString(
      _key,
      jsonEncode(last7.map((e) => e.toJson()).toList()),
    );

    return last7;
  }
}