<div align="center">

<img src="anyshare_android/assets/icon/icon.png" width="140" alt="AnyShare Logo"/>

# AnyShare

Cross-platform Android ↔ Windows connectivity over USB.

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?style=for-the-badge\&logo=windows\&logoColor=white)]()
[![Android](https://img.shields.io/badge/Android-12%2B-3DDC84?style=for-the-badge\&logo=android\&logoColor=white)]()
[![.NET](https://img.shields.io/badge/.NET-9-512BD4?style=for-the-badge\&logo=dotnet\&logoColor=white)]()
[![Flutter](https://img.shields.io/badge/Flutter-3.x-02569B?style=for-the-badge\&logo=flutter\&logoColor=white)]()

<br>

<a href="YOUR_GOOGLE_DRIVE_LINK">
  <img src="https://drive.google.com/drive/folders/1CJBPmYUeA5pgckdoQUjfPZvg1rGwCncZ?usp=drive_link"/>
</a>

</div>

---

## Overview

AnyShare is a lightweight utility that connects Android devices and Windows computers through USB. It combines network monitoring, clipboard sharing, and Android internet sharing into a single application without requiring cloud accounts or third-party services.

---

## Features

| Feature               | Description                                        |
| --------------------- | -------------------------------------------------- |
| Network Speed Monitor | Real-time upload and download speed monitoring     |
| Taskbar Widget        | Floating desktop speed widget on Windows           |
| Usage Tracking        | Daily usage statistics with 7-day history          |
| Clipboard Sharing     | Send clipboard content between Android and Windows |
| Network Sharing       | Share Android internet with Windows                |
| VPN Support           | Works with VPN connections running on Android      |
| USB Connectivity      | Uses ADB over USB                                  |
| Startup Support       | Launch automatically with Windows                  |
| System Tray           | Runs quietly in the background                     |

---

## Network Speed Monitor

* Live upload speed
* Live download speed
* Daily usage statistics
* Upload and download breakdown
* 7-day history
* Automatic KB/s, MB/s, and GB/s conversion

---

## Clipboard Sharing

* Android → Windows clipboard transfer
* Windows → Android clipboard transfer
* One-click send and receive actions
* USB-based communication
* No internet connection required

---

## Network Sharing

* Share Android internet with Windows
* Supports:

  * Mobile Data
  * Wi-Fi
  * VPN Connections
* Automatic device detection
* Automatic reconnection when USB is connected

---

## Requirements

### Windows

* Windows 10 or Windows 11
* .NET 9 Runtime
* Android Debug Bridge (ADB)

### Android

* Android 12 or newer
* USB Debugging enabled
* USB cable connection

---

## Technology Stack

### Windows

* Avalonia UI
* .NET 9
* Win32 APIs
* ADB Integration

### Android

* Flutter
* Kotlin
* Android SDK
* Method Channels

---

## Installation

### Enable Developer Options

1. Open **Settings**
2. Navigate to **About Phone**
3. Tap **Build Number** seven times

### Enable USB Debugging

1. Open **Settings**
2. Open **Developer Options**
3. Enable **USB Debugging**

### Connect Device

1. Connect the Android device through USB
2. Accept the debugging authorization prompt
3. Launch AnyShare on both devices
4. Enable the required features