// using Mediapipe;
// using Mediapipe.Unity;
// using Mediapipe.Unity.Sample;
// using System.Collections.Generic;
// using System.IO;
// using UnityEngine;
//
// namespace MediaPipeUnity.Tests
// {
//     public class MyHandTackingGragh : GraphRunner
//     {
//         // 1. 定义输入输出流名称
//         private const string _InputStreamName = "input_video";
//         private const string _LandmarksOutputStream = "hand_landmarks";
//         private const string _WorldLandmarksOutputStream = "world_hand_landmarks";
//         
//         // 2. 声明输出流处理器
//         private OutputStream<NormalizedLandmarkList> _landmarksStream;
//         private OutputStream<LandmarkList> _worldLandmarksStream;
//         
//         // 3. 实现抽象方法
//         public override void StartRun(ImageSource imageSource)
//         {
//             // 加载Graph配置文件
//             var configPath = Path.Combine(Application.streamingAssetsPath, "hand_tracking_desktop_live.pbtxt");
//             var configText = File.ReadAllText(configPath);
//             
//             // 初始化计算图
//             Initialize(configText);
//             
//             // 设置输入流
//             SetInputStream(_InputStreamName, imageSource);
//             
//             // 启动输出流
//             _landmarksStream = AddOutputStream<NormalizedLandmarkList>(
//                 _LandmarksOutputStream, OnLandmarksOutput, true);
//             
//             _worldLandmarksStream = AddOutputStream<LandmarkList>(
//                 _WorldLandmarksOutputStream, OnWorldLandmarksOutput, true);
//             
//             // 启动计算图
//             StartRun(BuildSidePacket(imageSource));
//         }
//         
//         // 4. 实现依赖资源加载
//         public override IList<WaitForResult> RequestDependentAssets()
//         {
//             return new List<WaitForResult> {
//                 WaitForAsset("hand_landmark_full.bytes"),
//                 WaitForAsset("hand_recrop.bytes"),
//                 WaitForAsset("handedness.txt")
//             };
//         }
//         
//         // 5. 处理输出回调
//         private void OnLandmarksOutput(NormalizedLandmarkList landmarks, long timestamp)
//         {
//             // 处理手部关键点（0-1标准化坐标）
//             Debug.Log($"Received {landmarks.Landmark.Count} hand landmarks");
//         }
//         
//         private void OnWorldLandmarksOutput(LandmarkList worldLandmarks, long timestamp)
//         {
//             // 处理3D世界坐标关键点
//             Debug.Log($"Received {worldLandmarks.Landmark.Count} 3D landmarks");
//         }
//         
//         // 6. 添加自定义方法
//         public void ProcessFrame(Texture2D frameTexture)
//         {
//             using (var imageFrame = BuildImageFrameFromTexture(frameTexture))
//             {
//                 AddPacketToInputStream(
//                     _InputStreamName,
//                     new ImageFramePacket(imageFrame, GetCurrentTimestampMicrosec()));
//             }
//         }
//     }
// }
