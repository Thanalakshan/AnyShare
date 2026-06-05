import 'package:flutter/material.dart';
import '../models/daily_usage.dart';
import '../services/usage_history_service.dart';
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

  Future<void> _reset() async {
    await _service.resetHistory();
    await _load();

    if (!mounted) return;

    ScaffoldMessenger.of(context).showSnackBar(
      const SnackBar(content: Text('Usage history reset')),
    );
  }

  @override
  Widget build(BuildContext context) {
    final total = history.fold<double>(
      0,
      (sum, item) => sum + item.totalMB,
    );

    return Scaffold(
      appBar: AppBar(
        title: const Text('7-Day Usage History'),
        backgroundColor: const Color(0xFF0A0A0C),
        actions: [
          IconButton(
            onPressed: _reset,
            icon: const Icon(Icons.delete_outline),
            tooltip: 'Reset history',
          ),
        ],
      ),
      body: Padding(
        padding: const EdgeInsets.all(20),
        child: history.isEmpty
            ? const Center(
                child: Text(
                  'No usage history yet',
                  style: TextStyle(color: Color(0xFF6B7280)),
                ),
              )
            : Column(
                children: [
                  Container(
                    width: double.infinity,
                    margin: const EdgeInsets.only(bottom: 16),
                    padding: const EdgeInsets.all(16),
                    decoration: BoxDecoration(
                      color: const Color(0xFF111115),
                      borderRadius: BorderRadius.circular(14),
                      border: Border.all(color: const Color(0xFF1E1E24)),
                    ),
                    child: Text(
                      'Last 7 days total: ${total.toStringAsFixed(2)} MB',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 18,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ),
                  Expanded(
                    child: ListView(
                      children:
                          history.map((item) => UsageRow(item: item)).toList(),
                    ),
                  ),
                ],
              ),
      ),
    );
  }
}