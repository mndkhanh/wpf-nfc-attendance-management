import 'package:flutter/material.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_database/firebase_database.dart';
import 'package:nfc_manager/nfc_manager.dart';
import 'package:nfc_manager/nfc_manager_android.dart';
import 'firebase_options.dart';

void main() async {
  WidgetsFlutterBinding.ensureInitialized();
  await Firebase.initializeApp(options: DefaultFirebaseOptions.currentPlatform);
  runApp(const MainApp());
}

class MainApp extends StatefulWidget {
  const MainApp({super.key});

  @override
  State<MainApp> createState() => _MainAppState();
}

class _MainAppState extends State<MainApp> with WidgetsBindingObserver {
  final GlobalKey<ScaffoldMessengerState> _scaffoldKey = GlobalKey<ScaffoldMessengerState>();
  String _statusMessage = 'Initializing...';
  String _lastScannedUid = 'None';
  bool _nfcAvailable = false;

  @override
  void initState() {
    super.initState();
    // Start listening to the app lifecycle (background/foreground)
    WidgetsBinding.instance.addObserver(this);
    _initNfcAndStart();
  }

  Future<void> _initNfcAndStart() async {
    bool isAvailable = await NfcManager.instance.isAvailable();
    setState(() {
      _nfcAvailable = isAvailable;
      if (isAvailable) {
        _statusMessage = 'Ready. Tap an NFC card to scan.';
      } else {
        _statusMessage = 'NFC is not available on this device.';
      }
    });

    if (isAvailable) {
      _startScanning();
    }
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (!_nfcAvailable) return;

    // Automatically manage NFC session when app goes background/foreground
    if (state == AppLifecycleState.resumed) {
      _startScanning();
    } else if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.inactive) {
      NfcManager.instance.stopSession();
    }
  }

  void _startScanning() {
    NfcManager.instance.startSession(
      pollingOptions: {NfcPollingOption.iso14443, NfcPollingOption.iso15693},
      onDiscovered: (NfcTag tag) async {
        try {
          String uid = _extractUid(tag);
          if (uid.isNotEmpty) {
            setState(() {
              _lastScannedUid = uid;
              _statusMessage = 'Processing $uid...';
            });

            // Send to Firebase Realtime Database
            DatabaseReference ref = FirebaseDatabase.instanceFor(
              app: Firebase.app(),
              databaseURL:
                  'https://wpf-nfc-attendance-management-default-rtdb.asia-southeast1.firebasedatabase.app/',
            ).ref('current_uid');

            await ref.set(uid);

            setState(() {
              _statusMessage = 'Sent UID successfully! Ready for next...';
            });

            if (mounted) {
              _scaffoldKey.currentState?.clearSnackBars();
              _scaffoldKey.currentState?.showSnackBar(
                SnackBar(content: Text('Firebase Updated: $uid')),
              );
            }
          } else {
            // Show raw data in UI if UID couldn't be found
            setState(() {
              _lastScannedUid = 'No UID Found';
              _statusMessage = 'Raw Data: ${tag.data}';
            });
          }
        } catch (e) {
          setState(() {
            _statusMessage = 'Parsing error: $e';
          });
        }
      },
    );
  }

  String _extractUid(NfcTag tag) {
    try {
      // First, try to extract Android specific tag ID since this targets Android
      final androidTag = NfcTagAndroid.from(tag);
      if (androidTag != null && androidTag.id.isNotEmpty) {
        return androidTag.id
            .map((byte) => byte.toRadixString(16).padLeft(2, '0'))
            .join(':')
            .toUpperCase();
      }
    } catch (e) {
      debugPrint('Error extracting UID from Tag: $e');
    }

    return '';
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    NfcManager.instance.stopSession();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      scaffoldMessengerKey: _scaffoldKey,
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: Colors.blue),
        useMaterial3: true,
      ),
      home: Scaffold(
        appBar: AppBar(
          title: const Text('NFC Attendance Scanner'),
          backgroundColor: Theme.of(context).colorScheme.inversePrimary,
        ),
        body: Center(
          child: Padding(
            padding: const EdgeInsets.all(24.0),
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                const Icon(Icons.nfc, size: 100, color: Colors.blue),
                const SizedBox(height: 32),
                Text(
                  _statusMessage,
                  textAlign: TextAlign.center,
                  style: const TextStyle(
                    fontSize: 18,
                    fontWeight: FontWeight.w500,
                  ),
                ),
                const SizedBox(height: 48),
                Container(
                  padding: const EdgeInsets.all(20),
                  decoration: BoxDecoration(
                    color: Colors.grey.shade100,
                    borderRadius: BorderRadius.circular(16),
                    border: Border.all(color: Colors.grey.shade300),
                  ),
                  child: Column(
                    children: [
                      const Text(
                        'Last Scanned UID',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          color: Colors.grey,
                        ),
                      ),
                      const SizedBox(height: 12),
                      Text(
                        _lastScannedUid,
                        style: const TextStyle(
                          fontSize: 26,
                          fontWeight: FontWeight.bold,
                          letterSpacing: 1.5,
                        ),
                      ),
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
