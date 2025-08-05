using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightItUp.Data;
using LightItUp.Game;
using LightItUp.Sound;
using LightItUp.UI;

namespace LightItUp.Game
{
    public class SeekingMissileController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config; 

        private List<SeekingMissile> activeMissiles = new List<SeekingMissile>();
        private bool hasBeenUsedThisLevel = false;
        private bool isSpawning = false;
        private PlayerController playerController;
        private CameraFocus cameraFocus;

        public System.Action OnMissilesSpawned { get; set; }
        public System.Action OnMissilesCompleted { get; set; }
        public System.Action OnMissileHitBlock { get; set; }

        private void Awake()
        {
            FindMissingReferences();
        }

        private void FindMissingReferences()
        {
            if (playerController == null)
            {
                var gameLevel = GameManager.Instance?.currentLevel;
                
                if (gameLevel != null && gameLevel.player != null)
                {
                    playerController = gameLevel.player;
                }
                else
                {
                    playerController = FindObjectOfType<PlayerController>();
                }
            }

            if (cameraFocus == null && playerController != null)
            {
                cameraFocus = playerController.camFocus;
            }
        }

        private void Start()
        {
            ResetLevelUsage();
        }

        public bool CanUseMissiles()
        {
            return config != null && config.isEnabled && !hasBeenUsedThisLevel && !isSpawning;
        }

        public void SpawnMissiles()
        {
            if (!CanUseMissiles()) return;

            FindMissingReferences();
            if (playerController == null) return;

            // Ensure target pool is initialized
            if (SeekingMissileTargetManager.Instance != null)
            {
                SeekingMissileTargetManager.Instance.InitializeTargetPool();
            }

            StartCoroutine(SpawnMissilesCoroutine());
        }

        private IEnumerator SpawnMissilesCoroutine()
        {
            Debug.Log("[1] SpawnMissilesCoroutine started");
            if (config == null || playerController == null) 
            {
                Debug.Log("[2] Config or player is null, breaking");
                yield break;
            }

            isSpawning = true;
            hasBeenUsedThisLevel = true;

            ClearActiveMissiles();

            Debug.Log($"[3] Starting to spawn {config.missileCount} missiles");
            Debug.Log($"[35] Pool status before spawning: {ObjectPool.Instance.seekingMissiles.UnusedCount} unused, {ObjectPool.Instance.seekingMissiles.UsedCount} used");

            for (int i = 0; i < config.missileCount; i++)
            {
                Debug.Log($"[4] Spawning missile {i + 1}/{config.missileCount}");
                SpawnMissile();
                
                if (i < config.missileCount - 1)
                {
                    Debug.Log($"[5] Waiting {config.spawnDelay}s before next missile");
                    yield return new WaitForSeconds(config.spawnDelay);
                }
            }

            Debug.Log($"[6] Finished spawning. Active missiles: {activeMissiles.Count}");
            isSpawning = false;
            OnMissilesSpawned?.Invoke();
        }

        private void SpawnMissile()
        {
            Debug.Log("[7] SpawnMissile() called");
            Vector3 spawnPosition = playerController.transform.position;
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            spawnPosition += randomOffset;
            
            SeekingMissile missile = ObjectPool.GetSeekingMissile();
            Debug.Log($"[8] Got missile from pool: {missile != null}");
            
            if (missile != null)
            {
                missile.transform.position = spawnPosition;
                missile.transform.rotation = Quaternion.identity;
                missile.Initialize(config);
                missile.OnMissileDestroyed += OnMissileDestroyed;
                missile.OnMissileHitBlock += HandleMissileHitBlock;
                
                activeMissiles.Add(missile);
                Debug.Log($"[9] Missile spawned successfully. Total active: {activeMissiles.Count}");

                SoundManager.PlaySound(SoundNames.MissileLaunch);

                if (config.includeInCameraTracking && cameraFocus != null)
                {
                    var missileCollider = missile.GetComponent<Collider2D>();
                    if (missileCollider != null)
                    {
                        cameraFocus.AddTempTarget(missileCollider, config.cameraTrackingDuration);
                    }
                }
            }
            else
            {
                Debug.LogError("[10] Failed to get missile from pool!");
            }
        }

        private void OnMissileDestroyed(SeekingMissile missile)
        {
            Debug.Log($"[11] Missile destroyed. Active missiles before: {activeMissiles.Count}");
            if (activeMissiles.Remove(missile) && activeMissiles.Count == 0)
            {
                Debug.Log("[12] All missiles completed");
                OnMissilesCompleted?.Invoke();
            }
            Debug.Log($"[13] Active missiles after: {activeMissiles.Count}");
        }

        private void HandleMissileHitBlock(SeekingMissile missile, BlockController block)
        {
            SoundManager.PlaySound(SoundNames.MissileHit);
            OnMissileHitBlock?.Invoke();
        }

        public void ClearActiveMissiles()
        {
            var missilesToDestroy = new List<SeekingMissile>(activeMissiles);
            activeMissiles.Clear();
            
            foreach (var missile in missilesToDestroy)
            {
                missile?.DestroyMissile();
            }
            
            // Release all reserved targets back to pool
            if (SeekingMissileTargetManager.Instance != null)
            {
                SeekingMissileTargetManager.Instance.ReleaseAllTargets();
            }
        }

        public void ResetLevelUsage()
        {
            hasBeenUsedThisLevel = false;
            ClearActiveMissiles();
            
            // Initialize target pool for new level
            if (SeekingMissileTargetManager.Instance != null)
            {
                SeekingMissileTargetManager.Instance.InitializeTargetPool();
            }
            
            // Prewarm the missile pool for this level
            if (config != null)
            {
                ObjectPool.PrewarmSeekingMissiles(config.missileCount);
                Debug.Log($"[30] Prewarmed missile pool with {config.missileCount} missiles");
            }
            
            TryResetButton();
            StartCoroutine(TryResetButtonDelayed());
        }
        
        private void TryResetButton()
        {
            var missileButton = FindObjectOfType<SeekingMissileButton>();
            if (missileButton != null)
            {
                missileButton.ResetButton();
            }
        }
        
        private System.Collections.IEnumerator TryResetButtonDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            TryResetButton();
        }

        public bool HasBeenUsedThisLevel => hasBeenUsedThisLevel;
        public int ActiveMissileCount => activeMissiles.Count;
        public bool IsSpawning => isSpawning;

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }
    }
} 