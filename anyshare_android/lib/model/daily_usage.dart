class DailyUsage {
  final String date;
  final double downloadMB;
  final double uploadMB;
  final double totalMB;

  DailyUsage({
    required this.date,
    required this.downloadMB,
    required this.uploadMB,
    required this.totalMB,
  });

  Map<String, dynamic> toJson() {
    return {
      'date': date,
      'downloadMB': downloadMB,
      'uploadMB': uploadMB,
      'totalMB': totalMB,
    };
  }

  factory DailyUsage.fromJson(Map<String, dynamic> json) {
    return DailyUsage(
      date: json['date'],
      downloadMB: (json['downloadMB'] as num).toDouble(),
      uploadMB: (json['uploadMB'] as num).toDouble(),
      totalMB: (json['totalMB'] as num).toDouble(),
    );
  }
}