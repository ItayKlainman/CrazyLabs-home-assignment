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
            if (missileController != null)
            {
                missileController.OnMissilesSpawned += OnMissilesSpawned;
                missileController.OnMissilesCompleted += OnMissilesCompleted;
            }
            
            // Check if we need to reset the button state for a new level
            if (missileController != null && !missileController.HasBeenUsedThisLevel)
            {
                Debug.Log("[SeekingMissileButton] New level detected, resetting button state");
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
                if (buttonImage == null)
                {
                    Debug.LogError($"[{gameObject.name}] SeekingMissileButton: Missing Image component!");
                }
            }

            if (missileController == null)
            {
                missileController = FindObjectOfType<SeekingMissileController>();
                if (missileController == null)
                {
                    Debug.LogError($"[{gameObject.name}] SeekingMissileButton: No SeekingMissileController found in scene!");
                }
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
            Debug.Log($"[SeekingMissileButton] Button clicked! Ready: {isReady}, Used: {isUsed}, Controller: {missileController != null}");
            Debug.Log($"[SeekingMissileButton] Controller state - CanUseMissiles: {missileController?.CanUseMissiles()}, HasBeenUsedThisLevel: {missileController?.HasBeenUsedThisLevel}");
            
            if (!isReady || isUsed) 
            {
                Debug.Log("[SeekingMissileButton] Button not ready or already used, returning");
                return;
            }

            if (missileController != null)
            {
                Debug.Log("[SeekingMissileButton] Calling SpawnMissiles on controller");
                missileController.SpawnMissiles();
                OnButtonPressed?.Invoke();
            }
            else
            {
                Debug.LogError("[SeekingMissileButton] No missile controller assigned!");
            }
        }

        private void UpdateButtonState()
        {
            Debug.Log($"[SeekingMissileButton] UpdateButtonState called - Controller: {missileController != null}, isResetting: {isResetting}");
            
            if (isResetting)
            {
                Debug.Log("[SeekingMissileButton] Skipping UpdateButtonState because button is being reset");
                return;
            }
            
            if (missileController == null) 
            {
                Debug.Log("[SeekingMissileButton] No missile controller, cannot update state");
                return;
            }

            bool wasReady = isReady;
            bool wasUsed = isUsed;
            
            isReady = missileController.CanUseMissiles();
            isUsed = missileController.HasBeenUsedThisLevel;

            Debug.Log($"[SeekingMissileButton] Button state updated - Ready: {isReady} (was: {wasReady}), Used: {isUsed} (was: {wasUsed})");
            Debug.Log($"[SeekingMissileButton] Controller state - CanUseMissiles: {missileController.CanUseMissiles()}, HasBeenUsedThisLevel: {missileController.HasBeenUsedThisLevel}");

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
            Debug.Log($"[SeekingMissileButton] ResetButton called - Current state: Ready={isReady}, Used={isUsed}");
            isResetting = true;
            isUsed = false;
            isReady = true; // Force ready state
            Debug.Log("[SeekingMissileButton] Forced button state - Ready: true, Used: false");
            
            ButtonState currentState = GetCurrentState();
            Debug.Log($"[SeekingMissileButton] Button state after reset: {currentState}");
            ApplyVisualState(currentState);
            OnButtonStateChanged?.Invoke();
            
            // Reset the flag after a short delay to allow the new level to start
            StartCoroutine(ResetFlagAfterDelay());
        }
        
        private System.Collections.IEnumerator ResetFlagAfterDelay()
        {
            yield return new WaitForSeconds(0.1f);
            isResetting = false;
            Debug.Log("[SeekingMissileButton] Reset flag cleared");
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