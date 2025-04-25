import 'package:flutter/material.dart';
import 'screens/home_screen.dart'; // Import the HomeScreen

class MyApp extends StatelessWidget {
  const MyApp({Key? key}) : super(key: key);

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'DeskViz',
      debugShowCheckedModeBanner: false, // Hide debug banner
      theme: ThemeData(
        brightness: Brightness.dark, // Use a dark theme
        primarySwatch: Colors.blueGrey, // Example primary color
        visualDensity: VisualDensity.adaptivePlatformDensity,
        textTheme: TextTheme( // Define default text styles
           bodyMedium: TextStyle(color: Colors.white70),
           bodySmall: TextStyle(color: Colors.white54),
           headlineMedium: TextStyle(color: Colors.white),
           headlineSmall: TextStyle(color: Colors.white),
        ),
        // You can further customize colors, fonts etc.
      ),
      home: const HomeScreen(), // Set HomeScreen as the starting point
    );
  }
}