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
      child: Row(
        mainAxisAlignment: MainAxisAlignment.spaceBetween,
        children: [
          Text(item.date),
          Text(
            '${item.totalMB.toStringAsFixed(2)} MB',
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