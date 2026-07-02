using System.Collections;
using UnityEngine;

namespace Player
{
    [RequireComponent(typeof(Animator))]
    public class PlayerAnimator : MonoBehaviour
    {
        [Header("Animation Parameters")]
        [SerializeField] private float idleTimerDuration = 3f;
    
        private Animator _animator;
        private PlayerController _playerController;
        private float _lastInputTime;
        private bool _isIdle = false;
        private Coroutine _idleCheckCoroutine;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            _playerController = GetComponent<PlayerController>();
            if (_animator == null) Debug.LogError("PlayerAnimator: Animator component is missing!");
            if (_playerController == null) Debug.LogError("PlayerAnimator: PlayerController component is missing!");    
        }

        private void OnEnable()
        {
            if (_playerController != null)
            {
                _playerController.OnInputDetected += ResetIdleTimer;
                _playerController.OnMovementStarted += OnMovementStarted;
                _playerController.OnMovementStopped += OnMovementStopped;
            }
        
            // Empieza la rotuina de chequear el estado idle
            _idleCheckCoroutine = StartCoroutine(CheckIdleState());
        }

        private void OnDisable()
        {
            if (_playerController != null)
            {
                _playerController.OnInputDetected -= ResetIdleTimer;
                _playerController.OnMovementStarted -= OnMovementStarted;
                _playerController.OnMovementStopped -= OnMovementStopped;
            }
        
            if (_idleCheckCoroutine != null)
            {
                StopCoroutine(_idleCheckCoroutine);
                _idleCheckCoroutine = null;
            }
        }

        private void Start()
        {
            ResetIdleTimer();
        }

        public void ResetIdleTimer()
        {
            _lastInputTime = Time.time;
        
            if (_isIdle)
            {
                _isIdle = false;
                SetIdleAnimation(false);
            }
        }

        private void OnMovementStarted()
        {
            ResetIdleTimer();
            SetWalkingAnimation(true);
            SetIdleAnimation(false);
        }

        private void OnMovementStopped()
        {
            SetWalkingAnimation(false);
            ResetIdleTimer();
        }

        private IEnumerator CheckIdleState()
        {
            while (true)
            {
                // Lo pone en Idle Animation sino nos estamos moviendo
                if (!_playerController.IsMoving && !_isIdle && Time.time - _lastInputTime >= idleTimerDuration)
                {
                    _isIdle = true;
                    SetIdleAnimation(true);
                }
                yield return new WaitForSeconds(0.5f); // Chequea cada medio segundo
            }
        }

        private void SetIdleAnimation(bool value)
        {
            if (_animator != null) _animator.SetBool("isIdle", value);
        }

        private void SetWalkingAnimation(bool value)
        {
            if (_animator != null) _animator.SetBool("isWalking", value);
        }

        public void TriggerAbilityAnimation()
        {
            if (_animator != null) _animator.SetTrigger("useAbility");
            ResetIdleTimer();
        }
    }
}