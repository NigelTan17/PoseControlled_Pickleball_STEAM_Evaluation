using System;
using System.Collections.Generic;
using UnityEngine;

namespace PB.Scripts
{
    public class BodyDriver : MonoBehaviour
    {
        /// Joints doc omitted for brevity (same as your file)

        private Vector3 initPos;
        Animator animator;
        private Transform root, spine, neck, head, leye, reye,
            lshoulder, lelbow, lhand, lthumb2, lmid1,
            rshoulder, relbow, rhand, rthumb2, rmid1,
            lhip, lknee, lfoot, ltoe, rhip, rknee, rfoot, rtoe;

        private Quaternion midRoot, midSpine, midNeck, /*midHead,*/
            midLshoulder, midLelbow, midLhand, midRshoulder, midRelbow, midRhand,
            midLhip, midLknee, midLfoot, midRhip, midRknee, midRfoot;

        public Transform nose;

        [Header("Input")]
        public UDPReceive udpReceive;
        public MyBodyTrackGragh bodyTrackGragh; // optional, if you still pipe local graph

        [Header("Side / Court")]
        [Tooltip("Tick for Qin L (mirror X and Z). Unticked for Qin R.")]
        public bool isLeftSide = false;

        [Header("Root Position Settings")]
        public Vector3 RootPositionVector3 = new Vector3(0f, 0f, -70f);

        [Header("位移参数")]
        public float shoulderThreshold = 0.1f;
        public float movementSpeed = 5.0f;
        public float smoothTime = 0.1f;
        private Vector3 currentVelocity = Vector3.zero;
        private float lastShoulderCenterX = 0f;
        private bool isFirstFrame = true;

        private Dictionary<int, float> _previousZValues = new Dictionary<int, float>();

        // mediapipe -> posenet index map
        Dictionary<int, int> media2pose_index = new Dictionary<int, int> {
            {0, 11}, {1, 13}, {2, 15}, {3, 21}, {4, 17},
            {5, 12}, {6, 14}, {7, 16}, {8, 22}, {9, 18},
            {10, 7}, {11, 2}, {12, 8}, {13, 5}, {14, 0},
            {15, 23}, {16, 25}, {17, 27}, {18, 31},
            {19, 24}, {20, 26}, {21, 28}, {22, 32}
        };

        [Header("Ground Planting")]
        public float groundY = 0f;
        public float footClearance = 0.02f;
        private float restHipToFootY;

        [Header("Head Safety (glitch guard)")]
        public bool driveHeadRotation = false;
        public bool limitHeadAngles = true;
        public Vector2 headPitchRange = new Vector2(-30f, 30f);
        public float headYawMax = 80f;
        [Range(0f, 1f)] public float headSlerp = 0.2f;

        [Header("Renderer Safety")]
        public bool forceUpdateWhenOffscreen = true;

        void Start()
        {
            if (bodyTrackGragh != null)
                bodyTrackGragh.OnLandmarksProcessedString += updateData;

            animator = GetComponent<Animator>();

            // bones
            root = animator.GetBoneTransform(HumanBodyBones.Hips);
            spine = animator.GetBoneTransform(HumanBodyBones.Spine);
            neck  = animator.GetBoneTransform(HumanBodyBones.Neck);
            head  = animator.GetBoneTransform(HumanBodyBones.Head);
            leye  = animator.GetBoneTransform(HumanBodyBones.LeftEye);
            reye  = animator.GetBoneTransform(HumanBodyBones.RightEye);

            lshoulder = animator.GetBoneTransform(HumanBodyBones.LeftUpperArm);
            lelbow    = animator.GetBoneTransform(HumanBodyBones.LeftLowerArm);
            lhand     = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            lthumb2   = animator.GetBoneTransform(HumanBodyBones.LeftThumbIntermediate);
            lmid1     = animator.GetBoneTransform(HumanBodyBones.LeftMiddleProximal);

            rshoulder = animator.GetBoneTransform(HumanBodyBones.RightUpperArm);
            relbow    = animator.GetBoneTransform(HumanBodyBones.RightLowerArm);
            rhand     = animator.GetBoneTransform(HumanBodyBones.RightHand);
            rthumb2   = animator.GetBoneTransform(HumanBodyBones.RightThumbIntermediate);
            rmid1     = animator.GetBoneTransform(HumanBodyBones.RightMiddleDistal);

            lhip  = animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
            lknee = animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
            lfoot = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            ltoe  = animator.GetBoneTransform(HumanBodyBones.LeftToes);
            rhip  = animator.GetBoneTransform(HumanBodyBones.RightUpperLeg);
            rknee = animator.GetBoneTransform(HumanBodyBones.RightLowerLeg);
            rfoot = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            rtoe  = animator.GetBoneTransform(HumanBodyBones.RightToes);

            initPos = root.position;

            // mid/bind calibration
            Vector3 fwd = TriangleNormal(root.position, lhip.position, rhip.position);
            midRoot = Quaternion.Inverse(root.rotation) * Quaternion.LookRotation(fwd);
            midSpine = Quaternion.Inverse(spine.rotation) * Quaternion.LookRotation(spine.position - neck.position, fwd);
            midNeck  = Quaternion.Inverse(neck.rotation)  * Quaternion.LookRotation(neck.position - head.position, fwd);

            midLshoulder = Quaternion.Inverse(lshoulder.rotation) * Quaternion.LookRotation(lshoulder.position - lelbow.position, fwd);
            midLelbow    = Quaternion.Inverse(lelbow.rotation)    * Quaternion.LookRotation(lelbow.position - lhand.position, fwd);
            midLhand     = Quaternion.Inverse(lhand.rotation)     * Quaternion.LookRotation(
                                lthumb2.position - lmid1.position,
                                TriangleNormal(lhand.position, lthumb2.position, lmid1.position));

            midRshoulder = Quaternion.Inverse(rshoulder.rotation) * Quaternion.LookRotation(rshoulder.position - relbow.position, fwd);
            midRelbow    = Quaternion.Inverse(relbow.rotation)    * Quaternion.LookRotation(relbow.position - rhand.position, fwd);
            midRhand     = Quaternion.Inverse(rhand.rotation)     * Quaternion.LookRotation(
                                rthumb2.position - rmid1.position,
                                TriangleNormal(rhand.position, rthumb2.position, rmid1.position));

            midLhip  = Quaternion.Inverse(lhip.rotation)  * Quaternion.LookRotation(lhip.position  - lknee.position, fwd);
            midLknee = Quaternion.Inverse(lknee.rotation) * Quaternion.LookRotation(lknee.position - lfoot.position, fwd);
            midLfoot = Quaternion.Inverse(lfoot.rotation) * Quaternion.LookRotation(lfoot.position - ltoe.position, lknee.position - lfoot.position);

            midRhip  = Quaternion.Inverse(rhip.rotation)  * Quaternion.LookRotation(rhip.position  - rknee.position, fwd);
            midRknee = Quaternion.Inverse(rknee.rotation) * Quaternion.LookRotation(rknee.position - rfoot.position, fwd);
            midRfoot = Quaternion.Inverse(rfoot.rotation) * Quaternion.LookRotation(rfoot.position - rtoe.position, rknee.position - rfoot.position);

            restHipToFootY = root.position.y - Mathf.Min(lfoot.position.y, rfoot.position.y);

            if (forceUpdateWhenOffscreen)
                foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                    smr.updateWhenOffscreen = true;
        }

        private void OnDestroy()
        {
            if (bodyTrackGragh != null)
                bodyTrackGragh.OnLandmarksProcessedString -= updateData;
        }

        private void updateData(string landmark)
        {
            if (udpReceive != null) udpReceive.data = landmark;  // optional bridge
        }

        Vector3 TriangleNormal(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 d1 = a - b;
            Vector3 d2 = a - c;
            Vector3 dd = Vector3.Cross(d1, d2);
            dd.Normalize();
            return dd;
        }

        void updatePose()
        {
            if (udpReceive == null) return;
            string csv = udpReceive.data;
            if (string.IsNullOrEmpty(csv)) return;

            string[] points = csv.Split(',');
            if (points.Length < 3) return;

            float mirror = isLeftSide ? -1f : 1f;

            var pred3d = new List<Vector3>();
            foreach (var kvp in media2pose_index)
            {
                int baseIdx = kvp.Value * 3;
                // Robust parse
                if (baseIdx + 2 >= points.Length) return;

                float px = SafeParse(points[baseIdx + 0]);
                float py = SafeParse(points[baseIdx + 1]);
                float pz = SafeParse(points[baseIdx + 2]);

                // MediaPipe -> Unity: y,z sign, then mirror X & Z for left side
                float x =  px * mirror;
                float y = -py;
                float z = -pz * mirror;

                pred3d.Add(new Vector3(x, y, z));
            }

            // Derived points (same as your file)
            Vector3 necks = new Vector3(
                (pred3d[5].x + pred3d[0].x) / 2f,
                (pred3d[5].y + pred3d[0].y) / 2f,
                (pred3d[0].z + pred3d[5].z) / 2f);

            Vector3 hips = new Vector3(
                (pred3d[15].x + pred3d[19].x) / 2f,
                (pred3d[15].y + pred3d[19].y) / 2f,
                (pred3d[15].z + pred3d[19].z) / 2f);

            pred3d.Add((hips + necks) / 2f);      // 23 abdomenUpper
            pred3d.Add((hips + pred3d[23]) / 2f); // 24 hip

            Vector3 nhv = Vector3.Normalize((pred3d[10] + pred3d[12]) / 2f - necks);
            Vector3 nv  = pred3d[14] - necks;
            Vector3 heads = necks + nhv * Vector3.Dot(nhv, nv);
            pred3d.Add(heads);                    // 25 head
            pred3d.Add(necks);                    // 26 neck
            pred3d.Add(new Vector3(               // 27 spine
                (pred3d[0].x + pred3d[19].x) / 2f,
                (pred3d[0].y + pred3d[19].y) / 2f,
                (pred3d[0].z + pred3d[19].z) / 2f));

            // Root scale/offset (unchanged)
            float tallShin  = (Vector3.Distance(pred3d[16], pred3d[17]) + Vector3.Distance(pred3d[20], pred3d[21])) / 2f;
            float tallThigh = (Vector3.Distance(pred3d[15], pred3d[16]) + Vector3.Distance(pred3d[19], pred3d[20])) / 2f;
            float tallUnity = (Vector3.Distance(lhip.position, lknee.position) + Vector3.Distance(lknee.position, lfoot.position)) / 2f
                            + (Vector3.Distance(rhip.position, rknee.position) + Vector3.Distance(rknee.position, rfoot.position));

            root.position = pred3d[24] * (tallUnity / (tallThigh + tallShin));
            root.position = new Vector3(root.position.x * 1.5f, -root.position.y, root.position.z * 1.2f);
            root.position += new Vector3(RootPositionVector3.x, 0f, RootPositionVector3.z);

            // Feet planting
            float lowestFootY = Mathf.Min(lfoot.position.y, rfoot.position.y);
            float targetLowestY = groundY + footClearance;
            float deltaY = targetLowestY - lowestFootY;
            root.position += new Vector3(0f, deltaY, 0f);

            // Rotations (unchanged)
            Vector3 forward = TriangleNormal(pred3d[24], pred3d[19], pred3d[15]);
            root.rotation   = Quaternion.LookRotation(forward) * Quaternion.Inverse(midRoot);

            spine.rotation  = Quaternion.LookRotation(pred3d[27] - pred3d[26], forward) * Quaternion.Inverse(midSpine);
            neck.rotation   = Quaternion.LookRotation(pred3d[26] - pred3d[25], forward) * Quaternion.Inverse(midNeck);

            lshoulder.rotation = Quaternion.LookRotation(pred3d[5] - pred3d[6], forward)  * Quaternion.Inverse(midLshoulder);
            lelbow.rotation    = Quaternion.LookRotation(pred3d[6] - pred3d[7], forward)  * Quaternion.Inverse(midLelbow);
            rshoulder.rotation = Quaternion.LookRotation(pred3d[0] - pred3d[1], forward)  * Quaternion.Inverse(midRshoulder);
            relbow.rotation    = Quaternion.LookRotation(pred3d[1] - pred3d[2], forward)  * Quaternion.Inverse(midRelbow);

            lhip.rotation  = Quaternion.LookRotation(pred3d[19] - pred3d[20], forward) * Quaternion.Inverse(midLhip);
            lknee.rotation = Quaternion.LookRotation(pred3d[20] - pred3d[21], forward) * Quaternion.Inverse(midLknee);
            rhip.rotation  = Quaternion.LookRotation(pred3d[15] - pred3d[16], forward) * Quaternion.Inverse(midRhip);
            rknee.rotation = Quaternion.LookRotation(pred3d[16] - pred3d[17], forward) * Quaternion.Inverse(midRknee);

            if (driveHeadRotation && neck && head)
            {
                Quaternion desired = Quaternion.LookRotation(
                    pred3d[14] - pred3d[25],
                    TriangleNormal(pred3d[14], pred3d[12], pred3d[10])
                );
                if (limitHeadAngles)
                {
                    Quaternion local = Quaternion.Inverse(neck.rotation) * desired;
                    Vector3 e = local.eulerAngles;
                    e.x = Clamp180(e.x);
                    e.y = Clamp180(e.y);
                    e.x = Mathf.Clamp(e.x, headPitchRange.x, headPitchRange.y);
                    e.y = Mathf.Clamp(e.y, -headYawMax, headYawMax);
                    e.z = 0f;
                    desired = neck.rotation * Quaternion.Euler(e);
                }
                float a = (headSlerp <= 0f) ? 1f : 1f - Mathf.Exp(-headSlerp * 60f * Mathf.Max(Time.unscaledDeltaTime, 1e-3f));
                head.rotation = Quaternion.Slerp(head.rotation, desired, a);
            }

            lhand.rotation = Quaternion.LookRotation(
                                pred3d[8] - pred3d[9],
                                TriangleNormal(pred3d[7], pred3d[8], pred3d[9])) * Quaternion.Inverse(midLhand);

            rhand.rotation = Quaternion.LookRotation(
                                pred3d[3] - pred3d[4],
                                TriangleNormal(pred3d[2], pred3d[3], pred3d[4])) * Quaternion.Inverse(midRhand);

            lfoot.rotation = Quaternion.LookRotation(pred3d[21] - pred3d[22], pred3d[20] - pred3d[21]) * Quaternion.Inverse(midLfoot);
            rfoot.rotation = Quaternion.LookRotation(pred3d[17] - pred3d[18], pred3d[16] - pred3d[17]) * Quaternion.Inverse(midRfoot);

            // Shoulder-driven slight X drifting
            float leftShoulderX  = pred3d[5].x;
            float rightShoulderX = pred3d[0].x;
            float currentShoulderCenterX = (leftShoulderX + rightShoulderX) / 2f;

            float shoulderVelocityX = 0f;
            if (isFirstFrame)
            {
                lastShoulderCenterX = currentShoulderCenterX;
                isFirstFrame = false;
            }
            else
            {
                shoulderVelocityX = (currentShoulderCenterX - lastShoulderCenterX) / Mathf.Max(Time.unscaledDeltaTime, 1e-3f);
                if (Mathf.Abs(shoulderVelocityX) < shoulderThreshold) shoulderVelocityX = 0f;

                float targetX = root.position.x + shoulderVelocityX * movementSpeed * Mathf.Max(Time.unscaledDeltaTime, 1e-3f);
                Vector3 targetPos = new Vector3(targetX, root.position.y, root.position.z);
                root.position = Vector3.SmoothDamp(root.position, targetPos, ref currentVelocity, smoothTime);

                lastShoulderCenterX = currentShoulderCenterX;
            }
        }

        static float SafeParse(string s)
        {
            float v; return float.TryParse(s, out v) ? v : 0f;
        }
        static float Clamp180(float deg)
        {
            deg %= 360f;
            if (deg > 180f) deg -= 360f;
            if (deg < -180f) deg += 360f;
            return deg;
        }

        void Update()
        {
            updatePose();
        }
    }
}
