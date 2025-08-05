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
        private bool isResetting = false;

        public System.Action OnButtonPressed { get; private set; }
        public System.Action OnButtonStateChanged { get; private set; }

        private void Awake()
        {
            SetupComponents();
        }

        private void Start()
        {
            SetupComponents();
            
            if (missileController != null)
            {
                missileController.OnMissilesSpawned += OnMissilesSpawned;
                missileController.OnMissilesCompleted += OnMissilesCompleted;
            }
            
            if (missileController != null && !missileController.HasBeenUsedThisLevel)
            {
                ResetButton();
            }
            else
            {
                UpdateButtonState();
            }
        }

        private void OnDestroy()
        {
            if (missileController != null)
            {
                missileController.OnMissilesSpawned -= OnMissilesSpawned;
                missileController.OnMissilesCompleted -= OnMissilesCompleted;
            }
        }

        private void SetupComponents()
        {
            if (buttonImage == null)
            {
                buttonImage = GetComponent<Image>();
            }

            if (missileController == null)
            {
                missileController = FindObjectOfType<SeekingMissileController>();
            }

            if (config == null)
            {
                config = Resources.Load<SeekingMissileConfig>("SeekingMissileConfig");
            }
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
            
            if (config != null && !config.isEnabled) return;

            if (missileController != null)
            {
                missileController.SpawnMissiles();
                OnButtonPressed?.Invoke();
            }
        }

        private void UpdateButtonState()
        {
            if (isResetting) return;
            
            if (missileController == null) return;

            if (config == null || !config.isEnabled)
            {
                isReady = false;
                isUsed = false;
            }
            else
            {
                isReady = missileController.CanUseMissiles();
                isUsed = missileController.HasBeenUsedThisLevel;
            }

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
            isResetting = true;
            isUsed = false;
            
            if (config == null || !config.isEnabled)
            {
                isReady = false;
            }
            else
            {
                isReady = true;
            }
            
            ButtonState currentState = GetCurrentState();
            ApplyVisualState(currentState);
            OnButtonStateChanged?.Invoke();
            
            StartCoroutine(ResetFlagAfterDelay());
        }
        
        private System.Collections.IEnumerator ResetFlagAfterDelay()
        {
            yield return new WaitForSeconds(0.1f);
            isResetting = false;
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