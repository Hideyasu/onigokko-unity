using UnityEngine;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace Onigokko.BLE.Editor
{
    /// <summary>
    /// iOS BLE Beacon用のポストプロセスビルドスクリプト
    /// 必要なフレームワークとInfo.plistエントリを自動追加
    /// </summary>
    public class BLEPostProcessBuild
    {
        [PostProcessBuild]
        public static void OnPostProcessBuild(BuildTarget buildTarget, string pathToBuiltProject)
        {
            if (buildTarget == BuildTarget.iOS)
            {
                Debug.Log("[BLE] iOS ポストプロセスビルド開始");

                // Xcodeプロジェクトファイルのパス
                string projPath = pathToBuiltProject + "/Unity-iPhone.xcodeproj/project.pbxproj";

                // プロジェクトを読み込み
                PBXProject proj = new PBXProject();
                proj.ReadFromFile(projPath);

#if UNITY_2019_3_OR_NEWER
                string targetGuid = proj.GetUnityFrameworkTargetGuid();
#else
                string targetGuid = proj.TargetGuidByName("Unity-iPhone");
#endif

                // BLE Beacon に必要なフレームワークを追加
                AddRequiredFrameworks(proj, targetGuid);

                // ビルド設定を更新
                UpdateBuildSettings(proj, targetGuid);

                // プロジェクトファイルを保存
                proj.WriteToFile(projPath);

                // Info.plistを更新
                UpdateInfoPlist(pathToBuiltProject);

                Debug.Log("[BLE] iOS ポストプロセスビルド完了");
            }
        }

        private static void AddRequiredFrameworks(PBXProject proj, string targetGuid)
        {
            Debug.Log("[BLE] 必要なフレームワークを追加中...");

            // Core Location (位置情報・iBeacon)
            proj.AddFrameworkToProject(targetGuid, "CoreLocation.framework", false);

            // Core Bluetooth (BLE機能)
            proj.AddFrameworkToProject(targetGuid, "CoreBluetooth.framework", false);

            // Foundation (基本機能)
            proj.AddFrameworkToProject(targetGuid, "Foundation.framework", false);

            // UIKit (バックグラウンド処理)
            proj.AddFrameworkToProject(targetGuid, "UIKit.framework", false);

            Debug.Log("[BLE] フレームワーク追加完了");
        }

        private static void UpdateBuildSettings(PBXProject proj, string targetGuid)
        {
            Debug.Log("[BLE] ビルド設定を更新中...");

            // Objective-C++を有効化
            proj.SetBuildProperty(targetGuid, "CLANG_ENABLE_OBJC_ARC", "YES");

            // デバッグ情報を含める
            proj.SetBuildProperty(targetGuid, "GCC_GENERATE_DEBUGGING_SYMBOLS", "YES");

            Debug.Log("[BLE] ビルド設定更新完了");
        }

        private static void UpdateInfoPlist(string pathToBuiltProject)
        {
            Debug.Log("[BLE] Info.plist を更新中...");

            string plistPath = pathToBuiltProject + "/Info.plist";
            PlistDocument plist = new PlistDocument();
            plist.ReadFromString(File.ReadAllText(plistPath));

            PlistElementDict rootDict = plist.root;

            // 位置情報権限の説明
            rootDict.SetString("NSLocationWhenInUseUsageDescription",
                "ゲーム中の位置追跡とプレイヤー間の距離測定に使用します");
            rootDict.SetString("NSLocationAlwaysAndWhenInUseUsageDescription",
                "リアル鬼ごっこゲームでプレイヤーとの距離を正確に測定するために必要です");

            // Bluetooth権限の説明
            rootDict.SetString("NSBluetoothAlwaysUsageDescription",
                "BLE Beacon機能を使用してプレイヤー間の近接検出を行います");
            rootDict.SetString("NSBluetoothPeripheralUsageDescription",
                "他のプレイヤーにビーコン信号を送信するために必要です");

            // バックグラウンド実行モード
            PlistElementArray backgroundModes = rootDict.CreateArray("UIBackgroundModes");
            backgroundModes.AddString("bluetooth-central");
            backgroundModes.AddString("bluetooth-peripheral");
            backgroundModes.AddString("location");

            // 必要なデバイス機能
            PlistElementArray requiredCapabilities = rootDict.CreateArray("UIRequiredDeviceCapabilities");
            requiredCapabilities.AddString("location-services");
            requiredCapabilities.AddString("bluetooth-le");

            // iOS 13+ 位置情報精度設定
            rootDict.SetString("NSLocationDefaultAccuracyReduced", "false");

            // Info.plistを保存
            File.WriteAllText(plistPath, plist.WriteToString());

            Debug.Log("[BLE] Info.plist 更新完了");
        }
    }
}