using UnityEngine;
using LightItUp.Game;
using LightItUp.UI;

namespace LightItUp.Game
{
    /// <summary>
    /// Test script to verify Seeking Missiles implementation
    /// This can be removed after testing is complete
    /// </summary>
    public class SeekingMissilesTest : MonoBehaviour
    {
        [Header("Test Components")]
        [SerializeField] private SeekingMissileController missileController;
        [SerializeField] private SeekingMissileButton missileButton;
        [SerializeField] private SeekingMissileConfig config;

        private void Start()
        {
            // Verify components exist
            if (missileController == null)
            {
                missileController = FindObjectOfType<SeekingMissileController>();
                Debug.Log("SeekingMissilesTest: Found missile controller: " + (missileController != null));
            }

            if (missileButton == null)
            {
                missileButton = FindObjectOfType<SeekingMissileButton>();
                Debug.Log("SeekingMissilesTest: Found missile button: " + (missileButton != null));
            }

            if (config == null)
            {
                config = Resources.Load<SeekingMissileConfig>("SeekingMissileConfig");
                Debug.Log("SeekingMissilesTest: Found config: " + (config != null));
            }

            // Set up connections
            if (missileController != null && config != null)
            {
                missileController.SetConfig(config);
                Debug.Log("SeekingMissilesTest: Connected config to controller");
            }

            if (missileButton != null && missileController != null)
            {
                missileButton.SetMissileController(missileController);
                Debug.Log("SeekingMissilesTest: Connected button to controller");
            }

            // Test functionality
            TestMissileFunctionality();
        }

        private void TestMissileFunctionality()
        {
            if (missileController == null) return;

            // Test if missiles can be used
            bool canUse = missileController.CanUseMissiles();
            Debug.Log("SeekingMissilesTest: Can use missiles: " + canUse);

            // Test if button is ready
            if (missileButton != null)
            {
                bool buttonReady = missileButton.IsReady;
                Debug.Log("SeekingMissilesTest: Button ready: " + buttonReady);
            }
        }

        [ContextMenu("Test Spawn Missiles")]
        public void TestSpawnMissiles()
        {
            if (missileController != null)
            {
                missileController.SpawnMissiles();
                Debug.Log("SeekingMissilesTest: Spawned missiles");
            }
        }

        [ContextMenu("Test Reset Usage")]
        public void TestResetUsage()
        {
            if (missileController != null)
            {
                missileController.ResetLevelUsage();
                Debug.Log("SeekingMissilesTest: Reset missile usage");
            }

            if (missileButton != null)
            {
                missileButton.ResetButton();
                Debug.Log("SeekingMissilesTest: Reset button");
            }
        }
    }
} 