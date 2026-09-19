#import "tokenSetter.h"
#import <MapboxCommon/MBXMapboxOptions.h>
#import <MapboxCommon/MBXTelemetryService_Internal.h>
#import <MapboxCommon/MBXEventsService_Internal.h>
#import <MapboxCommon/MBXBillingService_Internal.h>
#import <MapboxCommon/MBXBillingServiceFactory_Internal.h>
#import <MapboxCommon/MBXTurnstileEvent_Internal.h>
#import <MapboxCommon/MBXUserSKUIdentifier_Internal.h>
#import <MapboxCommon/MBXTelemetryUtils_Internal.h>
#import <MapboxCommon/MBXSdkInformation.h>
#import <MapboxCommon/MBXEventsServerOptions_Internal.h>
#import <MapboxCommon/MBXSdkInfORegistryFactory_Internal.h>
#import <MapboxCommon/MBXSdkInfORegistry_Internal.h>

NSString* CreateNSString (const char* string)
{
  if (string)
    return [NSString stringWithUTF8String: string];
  else
        return [NSString stringWithUTF8String: ""];
}

char* convertNSStringToCString(const NSString* nsString)
{
    if (nsString == NULL)
        return NULL;

    const char* nsStringUtf8 = [nsString UTF8String];
    //create a null terminated C string on the heap so that our string's memory isn't wiped out right after method's return
    char* cString = (char*)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);

    return cString;
}

void setAccessTokenForToken(const char* token) {
  [MBXMapboxOptions setAccessTokenForToken: [[NSString alloc] initWithCString: token encoding:NSUTF8StringEncoding]];
}

char* getAccessToken() {
    
    const char *nsStringUtf8 = [[MBXMapboxOptions getAccessToken] UTF8String];
    char* cString = (char*)malloc(strlen(nsStringUtf8) + 1);
    strcpy(cString, nsStringUtf8);
    return cString;
}

MBXTelemetryService* getOrCreateTelemetryService() {
    MBXTelemetryService* telemService = [MBXTelemetryService getOrCreate];
    return telemService;
}

void setEventsCollectionStateForEnableCollection(bool state)
{
    [MBXTelemetryUtils setEventsCollectionStateForEnableCollection:state callback:NULL];
}


void registerSdkInfo(const char* sdkIdentifier, const char* sdkversion, const char* packageName)
{
    MBXSdkInformation *information = [[MBXSdkInformation alloc] initWithName:CreateNSString(sdkIdentifier) version:CreateNSString(sdkversion) packageName:CreateNSString(packageName)];
    
    MBXSdkInfoRegistry *registry = [MBXSdkInfoRegistryFactory getInstance];

    [registry registerSdkInformationForInfo: information];
}

void sendTurnstileEvent(const char* sdkIdentifier, const char* sdkversion, const char* packageName)
{
    MBXSdkInformation *information = [[MBXSdkInformation alloc] initWithName:CreateNSString(sdkIdentifier) version:CreateNSString(sdkversion) packageName:CreateNSString(packageName)];
    MBXEventsServerOptions *options = [[MBXEventsServerOptions alloc] initWithSdkInformation:information
                                                                  deferredDeliveryServiceOptions:nil];
    MBXEventsService *service = [MBXEventsService getOrCreateForOptions:options];
    
    MBXTurnstileEvent *turnstile = [[MBXTurnstileEvent alloc] initWithSkuId:MBXUserSKUIdentifierUnityMAUS];
    [service sendTurnstileEventForTurnstileEvent:turnstile callback:^(MBXExpected<NSNull *, MBXEventsServiceError *> * _Nonnull result) {
                // place to check and log result if needed
            }];
}

void sendSdkEvent(const char* sdkIdentifier, const char* sdkversion, const char* packageName)
{
    MBXSdkInformation *information = [[MBXSdkInformation alloc] initWithName:CreateNSString(sdkIdentifier) version:CreateNSString(sdkversion) packageName:CreateNSString(packageName)];
    MBXBillingService *service = [MBXBillingServiceFactory getInstance];
    
    [service triggerUserBillingEventForSdkInformation:information
                                         skuIdentifier:MBXUserSKUIdentifierUnityMAUS
                                               callback:^(MBXBillingServiceError * _Nonnull error) {
        // No action needed in this block
    }];
}

char* getUserSKUToken() 
{
    MBXBillingService *service = [MBXBillingServiceFactory getInstance];
    const NSString *nsStringUtf8 = [service getUserSKUTokenForSkuIdentifier:MBXUserSKUIdentifierUnityMAUS];
    return convertNSStringToCString(nsStringUtf8);
}


