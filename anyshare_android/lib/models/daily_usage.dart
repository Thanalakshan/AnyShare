class DailyUsage {
  final String date;
  final double wifiMB;
  final double cellularMB;
  final double downloadMB;
  final double uploadMB;
  final double totalMB;

  DailyUsage({
    required this.date,
    required this.wifiMB,
    required this.cellularMB,
    required this.downloadMB,
    required this.uploadMB,
    required this.totalMB,
  });

  Map<String, dynamic> toJson() {
    return {
      'date': date,
      'wifiMB': wifiMB,
      'cellularMB': cellularMB,
      'downloadMB': downloadMB,
      'uploadMB': uploadMB,
      'totalMB': totalMB,
    };
  }

  factory DailyUsage.fromJson(Map<String, dynamic> json) {
    return DailyUsage(
      date: json['date'] as String,
      wifiMB: (json['wifiMB'] ?? 0).toDouble(),
      cellularMB: (json['cellularMB'] ?? 0).toDouble(),
      downloadMB: (json['downloadMB'] ?? 0).toDouble(),
      uploadMB: (json['uploadMB'] ?? 0).toDouble(),
      totalMB: (json['totalMB'] ?? 0).toDouble(),
    );
  }
}