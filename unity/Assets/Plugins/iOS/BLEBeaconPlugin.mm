//
//  BLEBeaconPlugin.mm
//  Unity iOS BLE Beacon Plugin Implementation
//

#import "BLEBeaconPlugin.h"

// Unity C# コールバック用
typedef void (*BeaconDetectedCallback)(const char* uuid, int major, int minor, float distance, float rssi);
extern BeaconDetectedCallback g_BeaconDetectedCallback;

@implementation BLEBeaconPlugin

+ (instancetype)sharedInstance {
    static BLEBeaconPlugin *sharedInstance = nil;
    static dispatch_once_t onceToken;
    dispatch_once(&onceToken, ^{
        sharedInstance = [[BLEBeaconPlugin alloc] init];
    });
    return sharedInstance;
}

- (instancetype)init {
    self = [super init];
    if (self) {
        self.detectedBeacons = [[NSMutableDictionary alloc] init];
        [self initializeServices];
    }
    return self;
}

- (void)initializeServices {
    // Core Location Manager
    self.locationManager = [[CLLocationManager alloc] init];
    self.locationManager.delegate = self;

    // Bluetooth Peripheral Manager
    self.peripheralManager = [[CBPeripheralManager alloc] initWithDelegate:self queue:nil];

    NSLog(@"[BLE] BLEBeaconPlugin initialized");
}

#pragma mark - Advertising (送信)

- (void)startAdvertising:(NSString *)uuid major:(int)major minor:(int)minor {
    NSUUID *beaconUUID = [[NSUUID alloc] initWithUUIDString:uuid];

    self.beaconRegion = [[CLBeaconRegion alloc] initWithUUID:beaconUUID
                                                       major:major
                                                       minor:minor
                                                  identifier:@"OnigokkoBeacon"];

    // iBeacon データ生成
    self.beaconPeripheralData = [self.beaconRegion peripheralDataWithMeasuredPower:@(-59)];

    // アドバタイズ開始
    if (self.peripheralManager.state == CBManagerStatePoweredOn) {
        [self.peripheralManager startAdvertising:self.beaconPeripheralData];
        NSLog(@"[BLE] Started advertising as iBeacon: %@, Major: %d, Minor: %d", uuid, major, minor);
    } else {
        NSLog(@"[BLE] Bluetooth not powered on, cannot start advertising");
    }
}

- (void)stopAdvertising {
    [self.peripheralManager stopAdvertising];
    NSLog(@"[BLE] Stopped advertising");
}

#pragma mark - Scanning (受信)

- (void)startScanning:(NSString *)uuid {
    // 位置情報権限チェック
    if ([CLLocationManager authorizationStatus] != kCLAuthorizationStatusAuthorizedAlways &&
        [CLLocationManager authorizationStatus] != kCLAuthorizationStatusAuthorizedWhenInUse) {
        [self.locationManager requestWhenInUseAuthorization];
        return;
    }

    NSUUID *beaconUUID = [[NSUUID alloc] initWithUUIDString:uuid];
    self.beaconRegion = [[CLBeaconRegion alloc] initWithUUID:beaconUUID
                                                  identifier:@"OnigokkoScanRegion"];

    // iOS 13+ の新しいAPI使用
    if (@available(iOS 13.0, *)) {
        CLBeaconIdentityConstraint *constraint = [[CLBeaconIdentityConstraint alloc] initWithUUID:beaconUUID];
        [self.locationManager startRangingBeaconsWithConstraint:constraint];
    } else {
        [self.locationManager startRangingBeaconsInRegion:self.beaconRegion];
    }

    NSLog(@"[BLE] Started scanning for beacons: %@", uuid);
}

- (void)stopScanning {
    if (@available(iOS 13.0, *)) {
        CLBeaconIdentityConstraint *constraint = [[CLBeaconIdentityConstraint alloc] initWithUUID:self.beaconRegion.UUID];
        [self.locationManager stopRangingBeaconsWithConstraint:constraint];
    } else {
        [self.locationManager stopRangingBeaconsInRegion:self.beaconRegion];
    }

    NSLog(@"[BLE] Stopped scanning");
}

#pragma mark - CLLocationManagerDelegate

- (void)locationManager:(CLLocationManager *)manager didChangeAuthorizationStatus:(CLAuthorizationStatus)status {
    NSLog(@"[BLE] Location authorization status changed: %d", (int)status);

    if (status == kCLAuthorizationStatusAuthorizedWhenInUse ||
        status == kCLAuthorizationStatusAuthorizedAlways) {
        NSLog(@"[BLE] Location permission granted, can start beacon operations");
    }
}

// iOS 13+
- (void)locationManager:(CLLocationManager *)manager didRangeBeacons:(NSArray<CLBeacon *> *)beacons
       satisfyingConstraint:(CLBeaconIdentityConstraint *)beaconConstraint API_AVAILABLE(ios(13.0)) {
    [self processRangedBeacons:beacons];
}

// iOS 12以下用
- (void)locationManager:(CLLocationManager *)manager didRangeBeacons:(NSArray<CLBeacon *> *)beacons
               inRegion:(CLBeaconRegion *)region {
    [self processRangedBeacons:beacons];
}

- (void)processRangedBeacons:(NSArray<CLBeacon *> *)beacons {
    for (CLBeacon *beacon in beacons) {
        if (beacon.proximity != CLProximityUnknown) {
            NSString *beaconKey = [NSString stringWithFormat:@"%@_%@",
                                  beacon.major, beacon.minor];

            // ビーコン情報を保存
            NSDictionary *beaconInfo = @{
                @"uuid": beacon.UUID.UUIDString,
                @"major": beacon.major,
                @"minor": beacon.minor,
                @"distance": @(beacon.accuracy),
                @"rssi": @(beacon.rssi),
                @"timestamp": @([[NSDate date] timeIntervalSince1970])
            };

            self.detectedBeacons[beaconKey] = beaconInfo;

            // Unity にコールバック
            if (g_BeaconDetectedCallback != NULL) {
                const char* uuidStr = [beacon.UUID.UUIDString UTF8String];
                int major = [beacon.major intValue];
                int minor = [beacon.minor intValue];
                float distance = beacon.accuracy;
                float rssi = beacon.rssi;

                g_BeaconDetectedCallback(uuidStr, major, minor, distance, rssi);
            }

            NSLog(@"[BLE] Detected beacon - Major: %@, Minor: %@, Distance: %.1fm, RSSI: %ld",
                  beacon.major, beacon.minor, beacon.accuracy, (long)beacon.rssi);
        }
    }
}

#pragma mark - CBPeripheralManagerDelegate

- (void)peripheralManagerDidUpdateState:(CBPeripheralManager *)peripheral {
    switch (peripheral.state) {
        case CBManagerStatePoweredOn:
            NSLog(@"[BLE] Bluetooth powered on, ready for advertising");
            break;
        case CBManagerStatePoweredOff:
            NSLog(@"[BLE] Bluetooth powered off");
            break;
        case CBManagerStateUnsupported:
            NSLog(@"[BLE] Bluetooth LE not supported");
            break;
        default:
            NSLog(@"[BLE] Bluetooth state: %ld", (long)peripheral.state);
            break;
    }
}

#pragma mark - Distance & Data Access

- (float)getDistanceToBeacon:(int)major minor:(int)minor {
    NSString *beaconKey = [NSString stringWithFormat:@"%d_%d", major, minor];
    NSDictionary *beaconInfo = self.detectedBeacons[beaconKey];

    if (beaconInfo) {
        NSNumber *distance = beaconInfo[@"distance"];
        return [distance floatValue];
    }

    return -1.0f; // 検出されていない
}

- (NSArray *)getNearbyBeacons {
    NSMutableArray *result = [[NSMutableArray alloc] init];

    for (NSString *key in self.detectedBeacons) {
        [result addObject:self.detectedBeacons[key]];
    }

    // 距離でソート
    [result sortUsingComparator:^NSComparisonResult(NSDictionary *obj1, NSDictionary *obj2) {
        NSNumber *distance1 = obj1[@"distance"];
        NSNumber *distance2 = obj2[@"distance"];
        return [distance1 compare:distance2];
    }];

    return result;
}

@end

#pragma mark - Unity C Interface

extern "C" {
    BeaconDetectedCallback g_BeaconDetectedCallback = NULL;

    void _SetBeaconDetectedCallback(BeaconDetectedCallback callback) {
        g_BeaconDetectedCallback = callback;
    }

    void _StartAdvertising(const char* uuid, int major, int minor) {
        NSString *uuidString = [NSString stringWithUTF8String:uuid];
        [[BLEBeaconPlugin sharedInstance] startAdvertising:uuidString major:major minor:minor];
    }

    void _StopAdvertising() {
        [[BLEBeaconPlugin sharedInstance] stopAdvertising];
    }

    void _StartScanning(const char* uuid) {
        NSString *uuidString = [NSString stringWithUTF8String:uuid];
        [[BLEBeaconPlugin sharedInstance] startScanning:uuidString];
    }

    void _StopScanning() {
        [[BLEBeaconPlugin sharedInstance] stopScanning];
    }

    float _GetDistanceToBeacon(int major, int minor) {
        return [[BLEBeaconPlugin sharedInstance] getDistanceToBeacon:major minor:minor];
    }
}