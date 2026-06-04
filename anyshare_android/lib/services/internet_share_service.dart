class InternetShareService {
  bool _enabled = false;

  bool get enabled => _enabled;

  Future<bool> toggleSharing() async {
    _enabled = !_enabled;

    // Later: call native Kotlin service to start Android → Windows proxy.
    return _enabled;
  }
}