#import <MapboxCommon/MapboxLocation.h>
#import <MapboxCommon/MBXLocationService_Internal.h>
#import <MapboxCommon/MBXLocationServiceFactory_Internal.h>

extern "C" {
    // Callback type for location updates
    typedef void (*LocationUpdateCallback)(
        double latitude, double longitude,
        float accuracy, double timestamp,
        double altitude, float speed, float bearing);

    // Callback types for service observer
    typedef void (*AuthorizationStatusCallback)(int status);
    typedef void (*AccuracyAuthorizationCallback)(int accuracy);
    typedef void (*AvailabilityCallback)(bool available);

    // Request location permissions (call this before starting location updates)
    void requestLocationAuthorization();

    // Create location service and start updates
    void* startLocationUpdatesWithSettings(
        long long minimumInterval, long long maximumInterval, long long interval,
        int accuracyLevel, float displacement,
        LocationUpdateCallback callback);

    // Stop location updates
    void stopLocationUpdates(void* providerPtr);

    // Add service observer for permission and availability changes
    void addLocationServiceObserver(
        AuthorizationStatusCallback authCallback,
        AccuracyAuthorizationCallback accuracyCallback,
        AvailabilityCallback availabilityCallback);

    // Remove service observer
    void removeLocationServiceObserver();
}
