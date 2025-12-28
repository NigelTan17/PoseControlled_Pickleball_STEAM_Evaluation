using UnityEngine;

namespace PB.Scripts
{
    public class SplitScreenManager : MonoBehaviour
    {
        [Header("Cameras")]
        public Camera leftCamera;
        public Camera rightCamera;

        [Header("Players (optional)")]
        public QinMove leftPlayer;
        public QinMove rightPlayer;

        [Header("Layout")]
        public bool swapSides = false;                   // check to flip left/right halves
        public Rect leftRect  = new Rect(0f, 0f, 0.5f, 1f);
        public Rect rightRect = new Rect(0.5f, 0f, 0.5f, 1f);

        void Start()
        {
            if (!leftCamera || !rightCamera)
            {
                Debug.LogError("Assign Left/Right Camera on SplitScreenManager.");
                return;
            }

            ApplyRects();
            EnsureSingleAudioListener();

            // Optional: default key bindings (kept from your original)
            if (leftPlayer)
            {
                leftPlayer.forwardKey  = KeyCode.W;
                leftPlayer.backwardKey = KeyCode.S;
                leftPlayer.leftKey     = KeyCode.A;
                leftPlayer.rightKey    = KeyCode.D;
                leftPlayer.runKey      = KeyCode.LeftShift;
            }
            if (rightPlayer)
            {
                rightPlayer.forwardKey  = KeyCode.DownArrow;
                rightPlayer.backwardKey = KeyCode.UpArrow;
                rightPlayer.leftKey     = KeyCode.RightArrow;
                rightPlayer.rightKey    = KeyCode.LeftArrow;
                rightPlayer.runKey      = KeyCode.RightShift;
            }
        }

        // Also update in-editor when you tweak the checkbox/rects.
        void OnValidate()
        {
            if (leftCamera && rightCamera) ApplyRects();
        }

        private void ApplyRects()
        {
            var L = swapSides ? rightRect : leftRect;
            var R = swapSides ? leftRect  : rightRect;

            leftCamera.rect  = L;
            rightCamera.rect = R;

            // Keep both at the same depth so one doesn't overwrite the other
            leftCamera.depth  = 0;
            rightCamera.depth = 0;
        }

        private void EnsureSingleAudioListener()
        {
            var la = leftCamera.GetComponent<AudioListener>();
            var ra = rightCamera.GetComponent<AudioListener>();
            if (la && ra) ra.enabled = false; // keep only one to avoid Unity warning
        }
    }
}
