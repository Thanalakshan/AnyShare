import 'package:flutter/material.dart';
import '../models/daily_usage.dart';
import '../services/usage_history_service.dart';
import '../widgets/action_button.dart';
import '../widgets/usage_row.dart';

class HistoryScreen extends StatefulWidget {
  const HistoryScreen({super.key});

  @override
  State<HistoryScreen> createState() => _HistoryScreenState();
}

class _HistoryScreenState extends State<HistoryScreen> {
  final UsageHistoryService _service = UsageHistoryService();
  List<DailyUsage> history = [];

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    final data = await _service.getHistory();
    setState(() {
      history = data;
    });
  }

  Future<void> _addSample() async {
    final data = await _service.addTodaySample(
      downloadMB: 12.5,
      uploadMB: 3.2,
    );

    setState(() {
      history = data;
    });
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('7-Day Usage History'),
        backgroundColor: const Color(0xFF0A0A0C),
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: Column(
          children: [
            ActionButton(
              title: 'Add Test Usage Sample',
              onPressed: _addSample,
            ),
            Expanded(
              child: history.isEmpty
                  ? const Center(
                      child: Text(
                        'No usage history yet',
                        style: TextStyle(color: Color(0xFF6B7280)),
                      ),
                    )
                  : ListView(
                      children: history
                          .map((item) => UsageRow(item: item))
                          .toList(),
                    ),
            ),
          ],
        ),
      ),
    );
  }
}