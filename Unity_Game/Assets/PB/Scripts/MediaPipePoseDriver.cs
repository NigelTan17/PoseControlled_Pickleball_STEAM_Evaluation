using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Animations.Rigging;
using static PB.Scripts.MyBodyTrackGragh;

namespace PB.Scripts
{
    public class MediaPipePoseDriver : MonoBehaviour
    {
        public enum DriveMode
        {
            QinMove,
            MediaPipe,
            IKCoordinateDriven
        }

        [Header("驱动模式")]
        public DriveMode currentDriveMode = DriveMode.QinMove;
        
        public QinMove qinMoveComponent;

        public Animator animator;
        
        
        [Header("配置参数")]
        public float scaleFactor = 1.0f;
        public Transform rootBone;
        public List<BoneMapping> boneMappings = new List<BoneMapping>();

        [Header("平滑设置")]
        public bool enableSmoothing = true;
        [Range(0.1f, 0.9f)] public float smoothFactor = 0.3f;

        private Dictionary<int, BoneData> boneDataMap = new Dictionary<int, BoneData>();

        [System.Serializable]
        public class BoneMapping
        {
            public string boneName;
            public int mediaPipeIndex;
            public Transform boneTransform;
            public Vector3 rotationOffset;
        }

        private class BoneData
        {
            public Vector3 currentPosition;
            public Vector3 targetPosition;
            public Quaternion targetRotation;
        }
        
        public MyBodyTrackGragh bodyTrackGraph;
        void Start()
        {
            InitializeBoneData();
            if (bodyTrackGraph != null)
            {
                bodyTrackGraph.OnLandmarksProcessed += OnLandmarksProcessed;
            }
            else
            {
                Debug.LogError("MyBodyTrackGragh 组件未分配，请在检查器中指定。");
            }
            
            if (animator == null)
            {
                animator = GetComponent<Animator>();
                if (animator == null)
                {
                    Debug.LogWarning("Animator 组件未找到，请手动指定或添加到同一游戏对象上。");
                }
            } 
            
            if (qinMoveComponent == null)
            {
                qinMoveComponent = GetComponent<QinMove>();
                if (qinMoveComponent == null)
                {
                    Debug.LogWarning("QinMove 组件未找到，请手动指定或添加到同一游戏对象上。");
                }
            }
        }
        
        void OnDestroy()
        {
            if (bodyTrackGraph != null)
            {
                bodyTrackGraph.OnLandmarksProcessed -= OnLandmarksProcessed;
            }
        }
        
        

        public MediaPipePoseDriver(MyBodyTrackGragh bodyTrackGraph)
        {
            this.bodyTrackGraph = bodyTrackGraph;
        }
        
        private void OnLandmarksProcessed(List<Vector3> landmarks)
        {
            if (currentDriveMode == DriveMode.MediaPipe)
            {
                UpdatePoseData(landmarks);
            }
        }

        void FixedUpdate()
        {
            switch (currentDriveMode)
            {
                case DriveMode.QinMove:
                    if (qinMoveComponent != null)
                    {
                        qinMoveComponent.enabled = true;
                    }
                    break;
                case DriveMode.MediaPipe:
                    DisableAnimationcontroller();
                    EnableMediaPipeDriving();
                    break;
                case DriveMode.IKCoordinateDriven:
                    DisableAnimationcontroller();
                    break;
            }
        }
        
        private void DisableAnimationcontroller()
        {
            if (animator != null)
            {
                animator.enabled = false;
            }
        }
        
        private void EnableMediaPipeDriving()
        {
            if (bodyTrackGraph != null)
            {
                if (bodyTrackGraph.isReceivingData)
                {
                    if (bodyTrackGraph.FilteredWorldLandmarks.Count > 0)
                    {
                        ProcessNewLandmarks(bodyTrackGraph.FilteredWorldLandmarks.ToArray());
                    }
                }
            }
        }
        
        // 骨骼Transform数组（按SkeletonBones顺序初始化）
        public Transform[] skeletonTransforms;
        
        // IK目标点
        public Transform leftHandIKTarget;
        public Transform rightHandIKTarget;
        public Transform leftFootIKTarget;
        public Transform rightFootIKTarget;
        
        
        public void ProcessNewLandmarks(Vector3[] mediapipeWorldLandmarks)
        {
            //校验数据是否为空
            if (mediapipeWorldLandmarks == null || mediapipeWorldLandmarks.Length == 0)
            {
                Debug.LogWarning("传入的mediapipeWorldLandmarks为空或长度为0。");
                return;
            }
            if (skeletonTransforms == null || skeletonTransforms.Length == 0)
            {
                Debug.LogWarning("骨骼Transform数组为空或长度为0。");
            }
            
            // 1. 应用MediaPipe结果到骨骼系统
            MediaPipeSkeletonMapper.ApplyMediaPipeResults(mediapipeWorldLandmarks);
        
            // 2. 将位置应用到模型骨骼
            MediaPipeSkeletonMapper.ApplyBonePositions(skeletonTransforms);
        
            // 3. 更新IK目标点
            //UpdateIKTargets();
        }
        
        
        void UpdateIKTargets()
        {
            leftHandIKTarget.position = MediaPipeSkeletonMapper.GetBonePosition(
                MediaPipeSkeletonMapper.SkeletonBones.LeftHand);
        
            rightHandIKTarget.position = MediaPipeSkeletonMapper.GetBonePosition(
                MediaPipeSkeletonMapper.SkeletonBones.RightHand);
        
            // 其他目标点同理...
            leftFootIKTarget.position = MediaPipeSkeletonMapper.GetBonePosition(
                MediaPipeSkeletonMapper.SkeletonBones.LeftFoot);

            rightFootIKTarget.position = MediaPipeSkeletonMapper.GetBonePosition(
                MediaPipeSkeletonMapper.SkeletonBones.RightFoot);
        }
        
        private void InitializeBoneData()
        {
            foreach (var mapping in boneMappings)
            {
                if (mapping.boneTransform == null)
                {
                    mapping.boneTransform = FindBoneTransform(mapping.boneName);
                }

                if (mapping.boneTransform != null)
                {
                    boneDataMap[mapping.mediaPipeIndex] = new BoneData();
                }
            }

            if (rootBone == null)
            {
                Debug.LogError("未设置根骨骼！请在检查器中分配根骨骼。");
            }
        }

        private Transform FindBoneTransform(string boneName)
        {
            return FindBoneTransformRecursive(rootBone, boneName);
        }

        private Transform FindBoneTransformRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.Equals(name))
                    return child;

                var result = FindBoneTransformRecursive(child, name);
                if (result != null)
                    return result;
            }
            return null;
        }

        public void UpdatePoseData(List<Vector3> mediaPipeLandmarks)
        {
            if (currentDriveMode != DriveMode.MediaPipe) return;
            if (mediaPipeLandmarks == null || mediaPipeLandmarks.Count == 0) return;

            Vector3 hipCenter = CalculateHipCenter(mediaPipeLandmarks);
            float shoulderWidth = CalculateShoulderWidth(mediaPipeLandmarks);

            foreach (var mapping in boneMappings)
            {
                if (mapping.mediaPipeIndex >= mediaPipeLandmarks.Count) continue;

                Vector3 rawPosition = mediaPipeLandmarks[mapping.mediaPipeIndex];
                Vector3 unityPosition = ConvertToUnitySpace(rawPosition);
                Vector3 relativePosition = unityPosition - hipCenter;
                Vector3 normalizedPosition = NormalizePosition(relativePosition, shoulderWidth);
                Vector3 finalPosition = normalizedPosition * scaleFactor;

                UpdateBoneData(mapping.mediaPipeIndex, finalPosition, mapping);
            }

            ApplyBoneTransforms();
        }

        private Vector3 CalculateHipCenter(List<Vector3> landmarks)
        {
            if (landmarks.Count > 24)
            {
                return (ConvertToUnitySpace(landmarks[23]) + ConvertToUnitySpace(landmarks[24])) / 2f;
            }
            return Vector3.zero;
        }

        private float CalculateShoulderWidth(List<Vector3> landmarks)
        {
            if (landmarks.Count > 12)
            {
                Vector3 leftShoulder = ConvertToUnitySpace(landmarks[11]);
                Vector3 rightShoulder = ConvertToUnitySpace(landmarks[12]);
                return Vector3.Distance(leftShoulder, rightShoulder);
            }
            return 1.0f;
        }

        private Vector3 ConvertToUnitySpace(Vector3 mediaPipePoint)
        {
            return new Vector3(mediaPipePoint.x, mediaPipePoint.y, -mediaPipePoint.z);
        }

        private Vector3 NormalizePosition(Vector3 position, float referenceScale)
        {
            return position / referenceScale;
        }

        private void UpdateBoneData(int index, Vector3 position, BoneMapping mapping)
        {
            if (!boneDataMap.ContainsKey(index)) return;

            BoneData data = boneDataMap[index];

            if (enableSmoothing)
            {
                data.targetPosition = Vector3.Lerp(data.currentPosition, position, smoothFactor);
            }
            else
            {
                data.targetPosition = position;
            }

            if (ShouldCalculateRotation(mapping.boneName))
            {
                data.targetRotation = CalculateBoneRotation(mapping, position);
            }
        }

        private bool ShouldCalculateRotation(string boneName)
        {
            return boneName.Contains("Arm") || boneName.Contains("Leg");
        }

        private Quaternion CalculateBoneRotation(BoneMapping mapping, Vector3 position)
        {
            Vector3 direction = (position - mapping.boneTransform.position).normalized;

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                return lookRotation * Quaternion.Euler(mapping.rotationOffset);
            }

            return mapping.boneTransform.rotation;
        }

        private void ApplyBoneTransforms()
        {
            foreach (var mapping in boneMappings)
            {
                if (!boneDataMap.ContainsKey(mapping.mediaPipeIndex)) continue;

                BoneData data = boneDataMap[mapping.mediaPipeIndex];

                if (mapping.boneTransform != null)
                {
                    mapping.boneTransform.position = data.targetPosition;

                    if (ShouldCalculateRotation(mapping.boneName))
                    {
                        mapping.boneTransform.rotation = data.targetRotation;
                    }

                    data.currentPosition = data.targetPosition;
                }
            }
        }
    }
}