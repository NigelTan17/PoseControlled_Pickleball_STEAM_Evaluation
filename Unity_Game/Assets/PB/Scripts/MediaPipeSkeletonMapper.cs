using System.Collections.Generic;
using UnityEngine;

public static class MediaPipeSkeletonMapper
{
    // 核心骨骼索引定义
    public enum SkeletonBones
    {
        Hips,
        Spine,
        Chest,
        UpperChest,
        LeftShoulder,
        LeftUpperArm,
        LeftLowerArm,
        LeftHand,
        RightShoulder,
        RightUpperArm,
        RightLowerArm,
        RightHand,
        LeftUpperLeg,
        LeftLowerLeg,
        LeftFoot,
        LeftToes,
        RightUpperLeg,
        RightLowerLeg,
        RightFoot,
        RightToes
    }
    

    // 存储骨骼位置的数据结构
    public static Vector3[] BonePositions = new Vector3[System.Enum.GetNames(typeof(SkeletonBones)).Length];

    /// <summary>
    /// 应用MediaPipe识别结果到骨骼系统
    /// </summary>
    /// <param name="mediapipeLandmarks">转换后的Unity世界坐标点数组(33个点)</param>
    public static void ApplyMediaPipeResults(Vector3[] mediapipeLandmarks)
    {
        if (mediapipeLandmarks == null || mediapipeLandmarks.Length < 29) return;
        
        // 1. 直接对应的关键点
        BonePositions[(int)SkeletonBones.LeftShoulder] = mediapipeLandmarks[11];
        BonePositions[(int)SkeletonBones.LeftUpperArm] = mediapipeLandmarks[11]; // 使用相同点作为起点
        BonePositions[(int)SkeletonBones.LeftLowerArm] = mediapipeLandmarks[13];
        BonePositions[(int)SkeletonBones.LeftHand] = mediapipeLandmarks[15];
        
        BonePositions[(int)SkeletonBones.RightShoulder] = mediapipeLandmarks[12];
        BonePositions[(int)SkeletonBones.RightUpperArm] = mediapipeLandmarks[12]; // 使用相同点作为起点
        BonePositions[(int)SkeletonBones.RightLowerArm] = mediapipeLandmarks[14];
        BonePositions[(int)SkeletonBones.RightHand] = mediapipeLandmarks[16];
        
        BonePositions[(int)SkeletonBones.LeftUpperLeg] = mediapipeLandmarks[23];
        BonePositions[(int)SkeletonBones.LeftLowerLeg] = mediapipeLandmarks[25];
        BonePositions[(int)SkeletonBones.LeftFoot] = mediapipeLandmarks[27];
        BonePositions[(int)SkeletonBones.LeftToes] = mediapipeLandmarks[31];
        
        BonePositions[(int)SkeletonBones.RightUpperLeg] = mediapipeLandmarks[24];
        BonePositions[(int)SkeletonBones.RightLowerLeg] = mediapipeLandmarks[26];
        BonePositions[(int)SkeletonBones.RightFoot] = mediapipeLandmarks[28];
        BonePositions[(int)SkeletonBones.RightToes] = mediapipeLandmarks[32];
        
        // 2. 计算中心点和脊柱位置
        CalculateCorePositions(mediapipeLandmarks);
    }

    /// <summary>
    /// 计算核心骨骼位置（髋部、脊柱、胸部）
    /// </summary>
    private static void CalculateCorePositions(Vector3[] landmarks)
    {
        // 髋部中心点（左右髋部平均）
        Vector3 hipCenter = (landmarks[23] + landmarks[24]) * 0.5f;
        BonePositions[(int)SkeletonBones.Hips] = hipCenter;
        
        // 肩部中心点（左右肩部平均）
        Vector3 shoulderCenter = (landmarks[11] + landmarks[12]) * 0.5f;
        
        // 脊柱位置（髋部和肩部的25%处）
        BonePositions[(int)SkeletonBones.Spine] = Vector3.Lerp(hipCenter, shoulderCenter, 0.25f);
        
        // 胸部位置（髋部和肩部的50%处）
        BonePositions[(int)SkeletonBones.Chest] = Vector3.Lerp(hipCenter, shoulderCenter, 0.5f);
        
        // 上胸部位置（髋部和肩部的75%处）
        BonePositions[(int)SkeletonBones.UpperChest] = Vector3.Lerp(hipCenter, shoulderCenter, 0.75f);
    }

    /// <summary>
    /// 应用计算好的骨骼位置到模型
    /// </summary>
    /// <param name="boneTransforms">骨骼Transform数组（需与SkeletonBones枚举顺序一致）</param>
    public static void ApplyBonePositions(Transform[] boneTransforms)
    {
        Debug.Log("ApplyBonePositions called");
        if (boneTransforms == null || boneTransforms.Length != BonePositions.Length) return;
        
        for (int i = 0; i < boneTransforms.Length; i++)
        {
            Debug.Log($"Bone index {i}: {boneTransforms[i]?.name}");
        }
        
        for (int i = 0; i < BonePositions.Length; i++)
        {
            if (boneTransforms[i] != null)
            {
                boneTransforms[i].position = BonePositions[i];
            }
        }
    }

    /// <summary>
    /// 获取特定骨骼位置
    /// </summary>
    public static Vector3 GetBonePosition(SkeletonBones bone)
    {
        return BonePositions[(int)bone];
    }

    /// <summary>
    /// 创建IK目标点
    /// </summary>
    public static Transform CreateIKTarget(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = Vector3.zero;
        return go.transform;
    }

    /// <summary>
    /// 获取四肢关键点索引（用于IK系统）
    /// </summary>
    public static int[] GetLimbIndicesForIK()
    {
        return new int[] {
            (int)SkeletonBones.LeftHand,
            (int)SkeletonBones.RightHand,
            (int)SkeletonBones.LeftFoot,
            (int)SkeletonBones.RightFoot
        };
    }
    
        
        

}