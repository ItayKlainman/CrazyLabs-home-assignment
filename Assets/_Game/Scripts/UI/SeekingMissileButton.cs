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
        [SerializeField] private TextMeshProUGUI buttonText;
        [SerializeField] private Image missileIcon;

        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;
        [SerializeField] private SeekingMissileController missileController;

        [Header("Visual States")]
        [SerializeField] private Color readyColor = Color.red;
        [SerializeField] private Color usedColor = Color.gray;
        [SerializeField] private Color disabledColor = Color.darkGray;

        // Private fields
        private bool isReady = true;
        private bool isUsed = false;

        // Events
        public System.Action OnButtonPressed;
        public System.Action OnButtonStateChanged;

        private void Awake()
        {
            // Get components if not assigned
            if (buttonImage == null) buttonImage = GetComponent<Image>();
            if (buttonText == null) buttonText = GetComponentInChildren<TextMeshProUGUI>();
            if (missileIcon == null) missileIcon = transform.Find("MissileIcon")?.GetComponent<Image>();

            // Find references if not assigned
            if (missileController == null)
            {
                missileController = FindObjectOfType<SeekingMissileController>();
            }

            if (config == null)
            {
                // Try to find config in resources
                config = Resources.Load<SeekingMissileConfig>("SeekingMissileConfig");
            }
        }

        private void Start()
        {
            UpdateButtonState();
        }

        private void Update()
        {
            // Check if we can use missiles
            bool canUse = missileController != null && missileController.CanUseMissiles();
            
            if (isReady != canUse || isUsed != missileController?.HasBeenUsedThisLevel)
            {
                UpdateButtonState();
            }
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!isReady || isUsed) return;

            // Trigger missile spawning
            if (missileController != null)
            {
                missileController.SpawnMissiles();
                OnButtonPressed?.Invoke();
            }

            UpdateButtonState();
        }

        private void UpdateButtonState()
        {
            if (missileController == null) return;

            isReady = missileController.CanUseMissiles();
            isUsed = missileController.HasBeenUsedThisLevel;

            // Update visual appearance
            if (buttonImage != null)
            {
                if (isUsed)
                {
                    buttonImage.color = usedColor;
                }
                else if (isReady)
                {
                    buttonImage.color = readyColor;
                }
                else
                {
                    buttonImage.color = disabledColor;
                }
            }

            // Update text
            if (buttonText != null)
            {
                if (isUsed)
                {
                    buttonText.text = "USED";
                }
                else if (isReady)
                {
                    buttonText.text = "MISSILES";
                }
                else
                {
                    buttonText.text = "DISABLED";
                }
            }

            // Update icon
            if (missileIcon != null)
            {
                missileIcon.enabled = isReady && !isUsed;
            }

            OnButtonStateChanged?.Invoke();
        }

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }

        public void SetMissileController(SeekingMissileController controller)
        {
            missileController = controller;
            UpdateButtonState();
        }

        public void ResetButton()
        {
            isUsed = false;
            UpdateButtonState();
        }

        public bool IsReady => isReady;
        public bool IsUsed => isUsed;
    }
} 