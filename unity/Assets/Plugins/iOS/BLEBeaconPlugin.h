//
//  BLEBeaconPlugin.h
//  Unity iOS BLE Beacon Plugin
//
//  鬼vs陰陽師ゲーム用 iBeacon実装
//

#import <Foundation/Foundation.h>
#import <CoreLocation/CoreLocation.h>
#import <CoreBluetooth/CoreBluetooth.h>

@interface BLEBeaconPlugin : NSObject <CLLocationManagerDelegate, CBPeripheralManagerDelegate>

@property (nonatomic, strong) CLLocationManager *locationManager;
@property (nonatomic, strong) CBPeripheralManager *peripheralManager;
@property (nonatomic, strong) CLBeaconRegion *beaconRegion;
@property (nonatomic, strong) NSDictionary *beaconPeripheralData;
@property (nonatomic, strong) NSMutableDictionary *detectedBeacons;

// Singleton
+ (instancetype)sharedInstance;

// Advertising (送信)
- (void)startAdvertising:(NSString *)uuid major:(int)major minor:(int)minor;
- (void)stopAdvertising;

// Scanning (受信)
- (void)startScanning:(NSString *)uuid;
- (void)stopScanning;

// Distance
- (float)getDistanceToBeacon:(int)major minor:(int)minor;
- (NSArray *)getNearbyBeacons;

@end