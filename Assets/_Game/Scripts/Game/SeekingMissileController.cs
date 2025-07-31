using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightItUp.BlockComponents;
using LightItUp.Data;

namespace LightItUp.Game
{
    public class SeekingMissileController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;
        
        [Header("References")]
        [SerializeField] private GameObject missilePrefab;
        [SerializeField] private CameraFocus cameraFocus;
        [SerializeField] private PlayerController playerController;

        // Private fields
        private List<SeekingMissile> activeMissiles = new List<SeekingMissile>();
        private bool hasBeenUsedThisLevel = false;
        private bool isSpawning = false;

        // Events
        public System.Action OnMissilesSpawned;
        public System.Action OnMissilesCompleted;
        public System.Action OnMissileHitBlock;

        private void Awake()
        {
            // Find references if not assigned
            if (cameraFocus == null)
            {
                var player = FindObjectOfType<PlayerController>();
                if (player != null)
                {
                    cameraFocus = player.camFocus;
                }
            }

            if (playerController == null)
            {
                playerController = FindObjectOfType<PlayerController>();
            }
        }

        private void Start()
        {
            // Reset usage when level starts
            ResetLevelUsage();
        }

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }

        public void SetMissilePrefab(GameObject prefab)
        {
            missilePrefab = prefab;
        }

        public void SetCameraFocus(CameraFocus focus)
        {
            cameraFocus = focus;
        }

        public void SetPlayerController(PlayerController player)
        {
            playerController = player;
        }

        public bool CanUseMissiles()
        {
            return config != null && config.isEnabled && !hasBeenUsedThisLevel && !isSpawning;
        }

        public void SpawnMissiles()
        {
            if (!CanUseMissiles()) return;

            StartCoroutine(SpawnMissilesCoroutine());
        }

        private IEnumerator SpawnMissilesCoroutine()
        {
            if (config == null || missilePrefab == null || playerController == null) yield break;

            isSpawning = true;
            hasBeenUsedThisLevel = true;

            // Clear any existing missiles
            ClearActiveMissiles();

            // Spawn missiles with delay
            for (int i = 0; i < config.missileCount; i++)
            {
                SpawnSingleMissile();
                
                // Wait before spawning next missile
                if (i < config.missileCount - 1)
                {
                    yield return new WaitForSeconds(config.spawnDelay);
                }
            }

            isSpawning = false;
            OnMissilesSpawned?.Invoke();
        }

        private void SpawnSingleMissile()
        {
            if (missilePrefab == null || playerController == null) return;

            // Spawn missile at player position
            Vector3 spawnPosition = playerController.transform.position;
            GameObject missileObject = Instantiate(missilePrefab, spawnPosition, Quaternion.identity);
            
            SeekingMissile missile = missileObject.GetComponent<SeekingMissile>();
            if (missile != null)
            {
                missile.Initialize(config);
                missile.OnMissileDestroyed += OnMissileDestroyed;
                missile.OnMissileHitBlock += OnMissileHitBlockHandler;
                
                activeMissiles.Add(missile);

                // Add to camera tracking if enabled
                if (config.includeInCameraTracking && cameraFocus != null)
                {
                    var missileCollider = missile.GetComponent<Collider2D>();
                    if (missileCollider != null)
                    {
                        cameraFocus.AddTempTarget(missileCollider, config.cameraTrackingDuration);
                    }
                }
            }
        }

        private void OnMissileDestroyed(SeekingMissile missile)
        {
            if (activeMissiles.Contains(missile))
            {
                activeMissiles.Remove(missile);
            }

            // Check if all missiles are destroyed
            if (activeMissiles.Count == 0)
            {
                OnMissilesCompleted?.Invoke();
            }
        }

        private void OnMissileHitBlockHandler(SeekingMissile missile, BlockController block)
        {
            OnMissileHitBlock?.Invoke();
        }

        public void ClearActiveMissiles()
        {
            foreach (var missile in activeMissiles)
            {
                if (missile != null)
                {
                    missile.DestroyMissile();
                }
            }
            activeMissiles.Clear();
        }

        public void ResetLevelUsage()
        {
            hasBeenUsedThisLevel = false;
            ClearActiveMissiles();
        }

        public bool HasBeenUsedThisLevel => hasBeenUsedThisLevel;
        public int ActiveMissileCount => activeMissiles.Count;
        public bool IsSpawning => isSpawning;
    }
} 