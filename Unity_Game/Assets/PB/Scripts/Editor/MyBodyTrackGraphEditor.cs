// FILEPATH: C:/1development/unityProject/MediaPipeUnityPlugin-all/Assets/PB/Scripts/MyBodyTrackGraphEditor.cs

using UnityEngine;
using UnityEditor;
using PB.Scripts;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using System.Collections.Generic;

namespace PB.Editor
{
    public class MyBodyTrackGraphEditor : EditorWindow
    {
        private MyBodyTrackGragh _target;
        private Vector2 _scrollPosition;

        [MenuItem("Tools/My Body Track Graph")]
        public static void ShowWindow()
        {
            GetWindow<MyBodyTrackGraphEditor>("My Body Track Graph");
        }

        void OnGUI()
        {
            if (_target == null)
            {
                _target = FindObjectOfType<MyBodyTrackGragh>();
                if (_target == null)
                {
                    EditorGUILayout.HelpBox("No MyBodyTrackGragh found in the scene.", MessageType.Warning);
                    return;
                }
            }



            // 显示数据接收状态
            GUI.color = _target.isReceivingData ? Color.green : Color.red;
            EditorGUILayout.LabelField(_target.isReceivingData ? "正在接收数据" : "未接收到数据");
            GUI.color = Color.white;
            
        }
    }
}