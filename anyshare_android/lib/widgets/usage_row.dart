import 'package:flutter/material.dart';
import '../models/daily_usage.dart';

class UsageRow extends StatelessWidget {
  final DailyUsage item;

  const UsageRow({super.key, required this.item});

  @override
  Widget build(BuildContext context) {
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      padding: const EdgeInsets.all(14),
      decoration: BoxDecoration(
        color: const Color(0xFF111115),
        borderRadius: BorderRadius.circular(10),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(item.date, style: const TextStyle(fontWeight: FontWeight.bold)),
          const SizedBox(height: 6),
          Text('WiFi: ${item.wifiMB.toStringAsFixed(2)} MB'),
          Text('Cellular: ${item.cellularMB.toStringAsFixed(2)} MB'),
          Text(
            'Total: ${item.totalMB.toStringAsFixed(2)} MB',
            style: const TextStyle(
              color: Color(0xFF3A86FF),
              fontWeight: FontWeight.bold,
            ),
          ),
        ],
      ),
    );
  }
}