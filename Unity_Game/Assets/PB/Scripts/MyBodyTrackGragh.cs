using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Mediapipe.Tasks.Components.Containers;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using Mediapipe.Unity.Sample.PoseLandmarkDetection;
using static PB.Scripts.MediaPipePoseDriver;


namespace PB.Scripts
{
    
    public class MyBodyTrackGragh : MonoBehaviour
    {
        public PoseLandmarkerRunner runner;
        [HideInInspector]
        public bool isTracking;
        [HideInInspector]
        public bool isReceivingData;

        [HideInInspector]
        public Landmarks worldLandmarks;
        [HideInInspector]
        public NormalizedLandmarks normalizedLandmarks;

        void OnEnable()
        {
            runner.OnResult += HandleResult;
        }

        void OnDisable()
        {
            runner.OnResult -= HandleResult;
        }

        // 新增公共属性暴露筛选后的landmarks
        public List<Vector3> FilteredWorldLandmarks { get; private set; } = new List<Vector3>();
        
        public string WorldLandmarksString { get; private set; } = "";
        
        
        [System.Serializable]
        public struct FilteredLandmark
        {
            public Vector3 position;

            public FilteredLandmark(Landmark landmark)
            {
                this.position = new Vector3(landmark.x, landmark.y, landmark.z);
            }
        }
        
        // 添加一个事件来通知数据已准备好
        public delegate void LandmarksProcessedEventHandler(List<Vector3> landmarks);
        public event LandmarksProcessedEventHandler OnLandmarksProcessed;
        
        // 添加一个事件来通知数据已经以字符串形式已准备好
        public delegate void LandmarksProcessedStringEventHandler(string landmarksString);
        public event LandmarksProcessedStringEventHandler OnLandmarksProcessedString;
        
        private void HandleResult(PoseLandmarkerResult result)
        {
            isReceivingData = true;

            if (result.poseLandmarks.Count > 0)
            {
                // 将得到的result转换成unity的vector3坐标
                ProcessWorldLandmarks(result);
                
                isTracking = true;
                
                // 处理完数据后立即触发事件
                OnLandmarksProcessed?.Invoke(FilteredWorldLandmarks);
                OnLandmarksProcessedString?.Invoke(WorldLandmarksString);
                
                //normalizedLandmarks = result.poseLandmarks[0];
                //worldLandmarks = result.poseWorldLandmarks[0];
                //Debug.Log($"First normalized landmark: X={normalizedLandmarks.landmarks[0].x}, Y={normalizedLandmarks.landmarks[0].y}, Z={normalizedLandmarks.landmarks[0].z},visibility={normalizedLandmarks.landmarks[0].visibility}");
                //Debug.Log($"First worldLandmarks : X={worldLandmarks.landmarks[0].x}, Y={worldLandmarks.landmarks[0].y}, Z={worldLandmarks.landmarks[0].z},visibility={worldLandmarks.landmarks[0].visibility}");
            }
        }
        
        
        private void ProcessWorldLandmarks(PoseLandmarkerResult result)
        {
            // 清除之前的筛选结果
            FilteredWorldLandmarks.Clear();
            WorldLandmarksString = ""; // 重置字符串
    
            // 检查是否有世界坐标的关键点
            if (result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0)
                return;

            // 使用StringBuilder提高字符串拼接效率
            StringBuilder sb = new StringBuilder();
    
            // 遍历所有landmarks
            for (int i = 0; i < result.poseWorldLandmarks[0].landmarks.Count; i++)
            {
                Landmark landmark = result.poseWorldLandmarks[0].landmarks[i];
        
                // 将Landmark转换为Vector3并添加到列表
                FilteredWorldLandmarks.Add(new Vector3(
                    landmark.x, 
                    landmark.y, 
                    landmark.z
                ));
        
                // 将坐标添加到字符串，格式为x,y,z
                sb.Append(landmark.x.ToString(CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(landmark.y.ToString(CultureInfo.InvariantCulture));
                sb.Append(",");
                sb.Append(landmark.z.ToString(CultureInfo.InvariantCulture));
        
                // 如果不是最后一个点，添加逗号分隔符
                if (i < result.poseWorldLandmarks[0].landmarks.Count - 1)
                {
                    sb.Append(",");
                }
            }
    
            WorldLandmarksString = sb.ToString(); // 存储最终结果
        }

        
        void Update()
        {
            if (isReceivingData)
            {
                Invoke("ResetDataReceivingStatus", 1f);
            }
        }
    
        void ResetDataReceivingStatus()
        {
            isReceivingData = false;
        }
    }
}