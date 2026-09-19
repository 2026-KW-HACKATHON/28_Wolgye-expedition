#import <MapboxCommon/MapboxTelemetry_Internal.h>

extern "C" {
    void setAccessTokenForToken(const char* token);
    char* getAccessToken();
    MBXTelemetryService* getOrCreateTelemetryService();
    void setEventsCollectionStateForEnableCollection(bool state);
    void sendTurnstileEvent(const char* sdkIdentifier, const char* version, const char* packageName);
    void registerSdkInfo(const char* sdkIdentifier, const char* version, const char* packageName);
    void sendSdkEvent(const char* sdkIdentifier, const char* version, const char* packageName);
    char* getUserSKUToken();
}
