#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import <MapboxCommon/MBXExpected.h>
#import <MapboxCommon/MapboxLocation.h>
#import <MapboxCommon/MBXLocationService_Internal.h>
#import <MapboxCommon/MBXLocationServiceFactory_Internal.h>
#import "locationBridge.h"

// Objective-C++ observer class
@interface LocationObserverBridge : NSObject<MBXLocationObserver>
@property (nonatomic, assign) LocationUpdateCallback callback;
- (instancetype)initWithCallback:(LocationUpdateCallback)callback;
- (void)onLocationUpdateReceivedForLocations:(NSArray<MBXLocation *> *)locations;
@end

@implementation LocationObserverBridge

- (instancetype)initWithCallback:(LocationUpdateCallback)callback {
    self = [super init];
    if (self) {
        _callback = callback;
    }
    return self;
}

- (void)onLocationUpdateReceivedForLocations:(NSArray<MBXLocation *> *)locations {
    if (_callback && locations.count > 0) {
        MBXLocation* loc = locations.firstObject;

        // Extract location data, handling optional NSNumber properties
        double latitude = loc.latitude;
        double longitude = loc.longitude;
        float accuracy = loc.horizontalAccuracy ? [loc.horizontalAccuracy floatValue] : 0.0f;
        // Convert timestamp from milliseconds to seconds
        double timestamp = loc.timestamp / 1000.0;
        double altitude = loc.altitude ? [loc.altitude doubleValue] : 0.0;
        float speed = loc.speed ? [loc.speed floatValue] : 0.0f;
        float bearing = loc.bearing ? [loc.bearing floatValue] : 0.0f;

        // Call the C# callback
        _callback(latitude, longitude, accuracy, timestamp, altitude, speed, bearing);
    }
}

@end

// Objective-C++ service observer class
@interface LocationServiceObserverBridge : NSObject<MBXLocationServiceObserver>
@property (nonatomic, assign) AuthorizationStatusCallback authCallback;
@property (nonatomic, assign) AccuracyAuthorizationCallback accuracyCallback;
@property (nonatomic, assign) AvailabilityCallback availabilityCallback;
- (instancetype)initWithCallbacks:(AuthorizationStatusCallback)authCallback
                  accuracyCallback:(AccuracyAuthorizationCallback)accuracyCallback
               availabilityCallback:(AvailabilityCallback)availabilityCallback;
@end

@implementation LocationServiceObserverBridge

- (instancetype)initWithCallbacks:(AuthorizationStatusCallback)authCallback
                  accuracyCallback:(AccuracyAuthorizationCallback)accuracyCallback
               availabilityCallback:(AvailabilityCallback)availabilityCallback {
    self = [super init];
    if (self) {
        _authCallback = authCallback;
        _accuracyCallback = accuracyCallback;
        _availabilityCallback = availabilityCallback;
    }
    return self;
}

- (void)onPermissionStatusChangedForPermission:(MBXPermissionStatus)permission {
    if (_authCallback) {
        _authCallback((int)permission);
    }
}

- (void)onAccuracyAuthorizationChangedForAccuracyAuthorization:(MBXAccuracyAuthorization)accuracyAuthorization {
    if (_accuracyCallback) {
        _accuracyCallback((int)accuracyAuthorization);
    }
}

- (void)onAvailabilityChangedForIsAvailable:(BOOL)isAvailable {
    if (_availabilityCallback) {
        _availabilityCallback(isAvailable);
    }
}

@end

// Global storage for observers and providers (must persist)
static NSMutableDictionary<NSValue*, LocationObserverBridge*>* observerMap = nil;
static NSMutableDictionary<NSValue*, id<MBXDeviceLocationProvider>>* providerMap = nil;
static CLLocationManager* authLocationManager = nil;
static LocationServiceObserverBridge* serviceObserver = nil;

// Implementation of extern "C" functions
void requestLocationAuthorization()
{
    if (!authLocationManager) {
        authLocationManager = [[CLLocationManager alloc] init];
    }

    CLAuthorizationStatus status = [CLLocationManager authorizationStatus];

    if (status == kCLAuthorizationStatusNotDetermined) {
        [authLocationManager requestWhenInUseAuthorization];
    }
}
void* startLocationUpdatesWithSettings(
    long long minimumInterval, long long maximumInterval, long long interval,
    int accuracyLevel, float displacement,
    LocationUpdateCallback callback)
{
    // Check location authorization status
    CLAuthorizationStatus status = [CLLocationManager authorizationStatus];

    if (!observerMap) {
        observerMap = [NSMutableDictionary dictionary];
    }
    if (!providerMap) {
        providerMap = [NSMutableDictionary dictionary];
    }

    // Get location service
    id<MBXLocationService> service = [MBXLocationServiceFactory getOrCreate];

    // Create interval settings
    MBXIntervalSettings* intervalSettings = [[MBXIntervalSettings alloc]
        initWithMinimumInterval:@(minimumInterval)
        maximumInterval:@(maximumInterval)
        interval:@(interval)];

    // Create location provider request
    MBXLocationProviderRequest* request = [[MBXLocationProviderRequest alloc]
        initWithAccuracy:@(accuracyLevel)
        displacement:@(displacement)
        interval:intervalSettings];

    // Get device location provider
    auto result = [service getDeviceLocationProviderForRequest:request];

    if (![result isValue]) {
        NSLog(@"Failed to get device location provider: %@", result.error);
        return NULL;
    }

    id<MBXDeviceLocationProvider> provider = result.value;

    // Create and register observer
    LocationObserverBridge* observer = [[LocationObserverBridge alloc] initWithCallback:callback];

    // Store both provider and observer
    NSValue* providerKey = [NSValue valueWithPointer:(__bridge void*)provider];
    observerMap[providerKey] = observer;
    providerMap[providerKey] = provider;

    // Request authorization if needed before starting location updates
    if (status == kCLAuthorizationStatusNotDetermined) {
        if (!authLocationManager) {
            authLocationManager = [[CLLocationManager alloc] init];
        }
        [authLocationManager requestWhenInUseAuthorization];
    }

    // Add observer to provider (this starts location updates)
    [provider addLocationObserverForObserver:observer];

    // Return provider pointer for later cleanup
    return (__bridge_retained void*)provider;
}

void stopLocationUpdates(void* providerPtr) {
    if (providerPtr == NULL) {
        return;
    }

    id<MBXDeviceLocationProvider> provider = (__bridge_transfer id<MBXDeviceLocationProvider>)providerPtr;
    NSValue* providerKey = [NSValue valueWithPointer:(__bridge void*)provider];
    LocationObserverBridge* observer = observerMap[providerKey];

    if (observer) {
        [provider removeLocationObserverForObserver:observer];
        [observerMap removeObjectForKey:providerKey];
        [providerMap removeObjectForKey:providerKey];
    }
}

void addLocationServiceObserver(
    AuthorizationStatusCallback authCallback,
    AccuracyAuthorizationCallback accuracyCallback,
    AvailabilityCallback availabilityCallback)
{
    // Remove existing observer if any
    if (serviceObserver) {
        removeLocationServiceObserver();
    }

    // Create new service observer
    serviceObserver = [[LocationServiceObserverBridge alloc]
        initWithCallbacks:authCallback
         accuracyCallback:accuracyCallback
      availabilityCallback:availabilityCallback];

    // Get location service and add observer
    id<MBXLocationService> service = [MBXLocationServiceFactory getOrCreate];
    [service registerObserverForObserver:serviceObserver];
}

void removeLocationServiceObserver()
{
    if (serviceObserver) {
        id<MBXLocationService> service = [MBXLocationServiceFactory getOrCreate];
        [service unregisterObserverForObserver:serviceObserver];
        serviceObserver = nil;
    }
}
