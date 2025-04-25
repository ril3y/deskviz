import 'package:flutter/material.dart';

/// Mixin that provides orientation awareness to widgets
mixin OrientationAware {
  /// Provides the current orientation from MediaQuery
  Orientation getOrientation(BuildContext context) {
    return MediaQuery.of(context).orientation;
  }
  
  /// Checks if the current orientation is landscape
  bool isLandscape(BuildContext context) {
    return getOrientation(context) == Orientation.landscape;
  }
  
  /// Checks if the current orientation is portrait
  bool isPortrait(BuildContext context) {
    return getOrientation(context) == Orientation.portrait;
  }
  
  /// Returns the value based on current orientation
  /// Takes two values: one for portrait and one for landscape
  T orientationValue<T>(BuildContext context, T portraitValue, T landscapeValue) {
    return isLandscape(context) ? landscapeValue : portraitValue;
  }
  
  /// Returns a child widget wrapped in appropriate layout constraints
  /// based on the current orientation
  Widget orientationBuilder(
    BuildContext context, 
    Widget Function(BuildContext context) portraitBuilder,
    Widget Function(BuildContext context) landscapeBuilder,
  ) {
    return isLandscape(context) 
        ? landscapeBuilder(context)
        : portraitBuilder(context);
  }
}

/// Extension to make OrientationAware functionality available to all widgets
extension OrientationAwareContext on BuildContext {
  /// Returns the current orientation
  Orientation get orientation => MediaQuery.of(this).orientation;
  
  /// Returns true if the device is in landscape orientation
  bool get isLandscape => orientation == Orientation.landscape;
  
  /// Returns true if the device is in portrait orientation
  bool get isPortrait => orientation == Orientation.portrait;
  
  /// Switches between two values based on orientation
  T orientationValue<T>(T portraitValue, T landscapeValue) {
    return isLandscape ? landscapeValue : portraitValue;
  }
}
