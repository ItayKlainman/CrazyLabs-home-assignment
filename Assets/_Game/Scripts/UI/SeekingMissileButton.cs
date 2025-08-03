using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using LightItUp.Game;

namespace LightItUp.UI
{
    public class SeekingMissileButton : MonoBehaviour, IPointerClickHandler
    {
        [Header("UI Components")]
        [SerializeField] private Image buttonImage;

        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;
        [SerializeField] private SeekingMissileController missileController;

        [Header("Visual States")]
        [SerializeField] private Color readyColor = Color.red;
        [SerializeField] private Color usedColor = Color.gray;
        [SerializeField] private Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 1f);

        private bool isReady = true;
        private bool isUsed = false;

        public System.Action OnButtonPressed { get; private set; }
        public System.Action OnButtonStateChanged { get; private set; }

        private void Awake()
        {
            ValidateCriticalComponents();
            FindMissingReferences();
        }

        private void Start()
        {
            if (missileController != null)
            {
                missileController.OnMissilesSpawned += OnMissilesSpawned;
                missileController.OnMissilesCompleted += OnMissilesCompleted;
            }
            UpdateButtonState();
        }

        private void OnDestroy()
        {
            if (missileController != null)
            {
                missileController.OnMissilesSpawned -= OnMissilesSpawned;
                missileController.OnMissilesCompleted -= OnMissilesCompleted;
            }
        }

        private void ValidateCriticalComponents()
        {
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
                if (buttonImage == null)
                {
                    LogCriticalError("Missing Image component! This is critical for button functionality.");
                }
            }

            if (missileController == null)
            {
                missileController = FindObjectOfType<SeekingMissileController>();
                if (missileController == null)
                {
                    LogCriticalError("No SeekingMissileController found in scene! This is critical for button functionality.");
                }
            }
        }

        private void LogCriticalError(string message)
        {
            Debug.LogError($"[{gameObject.name}] SeekingMissileButton: {message}");
        }

        private void FindMissingReferences()
        {
            if (config == null)
                config = Resources.Load<SeekingMissileConfig>("SeekingMissileConfig");
        }

        private void OnMissilesSpawned()
        {
            isUsed = true;
            UpdateButtonState();
        }

        private void OnMissilesCompleted()
        {
            UpdateButtonState();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isReady || isUsed) return;

            if (missileController != null)
            {
                missileController.SpawnMissiles();
                OnButtonPressed?.Invoke();
            }
        }

        private void UpdateButtonState()
        {
            if (missileController == null) return;

            isReady = missileController.CanUseMissiles();
            isUsed = missileController.HasBeenUsedThisLevel;

            ButtonState currentState = GetCurrentState();
            ApplyVisualState(currentState);
            OnButtonStateChanged?.Invoke();
        }

        private ButtonState GetCurrentState()
        {
            if (isUsed) return ButtonState.Used;
            if (isReady) return ButtonState.Ready;
            return ButtonState.Disabled;
        }

        private void ApplyVisualState(ButtonState state)
        {
            if (buttonImage == null) return;
            
            switch (state)
            {
                case ButtonState.Ready:
                    buttonImage.color = readyColor;
                    break;

                case ButtonState.Used:
                    buttonImage.color = usedColor;
                    break;

                case ButtonState.Disabled:
                    buttonImage.color = disabledColor;
                    break;
            }
        }

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }

        public void SetMissileController(SeekingMissileController controller)
        {
            if (missileController != null)
            {
                missileController.OnMissilesSpawned -= OnMissilesSpawned;
                missileController.OnMissilesCompleted -= OnMissilesCompleted;
            }

            missileController = controller;
            
            if (missileController != null)
            {
                missileController.OnMissilesSpawned += OnMissilesSpawned;
                missileController.OnMissilesCompleted += OnMissilesCompleted;
            }
            
            UpdateButtonState();
        }

        public void ResetButton()
        {
            isUsed = false;
            UpdateButtonState();
        }

        public bool IsReady => isReady;
        public bool IsUsed => isUsed;

        private enum ButtonState
        {
            Ready,
            Used,
            Disabled
        }
    }
} 