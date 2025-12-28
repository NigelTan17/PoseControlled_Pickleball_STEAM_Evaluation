using UnityEngine;

namespace PB.Scripts
{
    public class QinMove : MonoBehaviour
    {
        public Animator animator;
        public float acceleration = 3f;
        public float deceleration = 5f;
        public float walkSpeed = 0.9f;
        public float runSpeed = 3f;
        
        public KeyCode forwardKey = KeyCode.W;
        public KeyCode backwardKey = KeyCode.S;
        public KeyCode leftKey = KeyCode.A;
        public KeyCode rightKey = KeyCode.D;
        public KeyCode runKey = KeyCode.LeftShift;

        private Vector3 _movement;
        private float _horizontalInput;
        private float _verticalInput;
        private bool _isRunning;
        private float _currentMoveSpeed;
        private float _targetMoveSpeed;
        private float _currentVelocity;

        private void Start()
        {
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
            _targetMoveSpeed = walkSpeed;
            _currentMoveSpeed = 0f;
        }

        private void Update()
        {
            // 检测奔跑键
            _isRunning = Input.GetKey(runKey);
            _targetMoveSpeed = _isRunning ? runSpeed : walkSpeed;

            // 获取输入
            _horizontalInput = (Input.GetKey(rightKey) ? 1 : 0) - (Input.GetKey(leftKey) ? 1 : 0);
            _verticalInput = (Input.GetKey(forwardKey) ? 1 : 0) - (Input.GetKey(backwardKey) ? 1 : 0);

            // 创建移动向量
            _movement = new Vector3(_horizontalInput, 0f, _verticalInput).normalized;

            // 计算目标速度
            float targetSpeed = _movement.magnitude > 0.1f ? _targetMoveSpeed : 0f;

            // 平滑过渡当前速度到目标速度
            _currentMoveSpeed = Mathf.SmoothDamp(_currentMoveSpeed, targetSpeed, ref _currentVelocity, 
                                                _movement.magnitude > 0.1f ? 1f/acceleration : 1f/deceleration);

            // 移动角色
            if (_currentMoveSpeed > 0.01f)
            {
                transform.Translate(_movement * _currentMoveSpeed * Time.deltaTime, Space.World);
            }

            // 控制动画
            UpdateAnimation(_movement);
        }

        private void UpdateAnimation(Vector3 movement)
        {
            // 计算动画速度参数
            float animationSpeed = _currentMoveSpeed / runSpeed;
            
            // 设置Animator参数
            animator.SetFloat("Speed", animationSpeed);

            if (movement != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(movement);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.1f);
            }
        }
    }
}