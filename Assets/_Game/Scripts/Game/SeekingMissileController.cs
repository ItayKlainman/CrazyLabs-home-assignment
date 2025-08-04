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
            Debug.Log("[SeekingMissileController] FindMissingReferences called");
            
            if (playerController == null)
            {
                var gameLevel = GameManager.Instance?.currentLevel;
                Debug.Log($"[SeekingMissileController] GameLevel found: {gameLevel != null}");
                
                if (gameLevel != null && gameLevel.player != null)
                {
                    playerController = gameLevel.player;
                    Debug.Log("[SeekingMissileController] Got player from GameLevel");
                }
                else
                {
                    playerController = FindObjectOfType<PlayerController>();
                    Debug.Log($"[SeekingMissileController] Got player via FindObjectOfType: {playerController != null}");
                }
            }

            if (cameraFocus == null && playerController != null)
            {
                cameraFocus = playerController.camFocus;
                Debug.Log($"[SeekingMissileController] Got cameraFocus: {cameraFocus != null}");
            }
            
            Debug.Log($"[SeekingMissileController] Final state - Player: {playerController != null}, Camera: {cameraFocus != null}");
        }

        private void Start()
        {
            ResetLevelUsage();
        }

        public bool CanUseMissiles()
        {
            bool canUse = config != null && config.isEnabled && !hasBeenUsedThisLevel && !isSpawning;
            Debug.Log($"[SeekingMissileController] CanUseMissiles: {canUse} (config:{config != null}, enabled:{config?.isEnabled}, used:{hasBeenUsedThisLevel}, spawning:{isSpawning})");
            return canUse;
        }

        public void SpawnMissiles()
        {
            Debug.Log("[SeekingMissileController] SpawnMissiles called");
            
            if (!CanUseMissiles()) 
            {
                Debug.Log("[SeekingMissileController] Cannot use missiles, returning");
                return;
            }

            FindMissingReferences();
            if (playerController == null)
            {
                Debug.LogWarning("[SeekingMissileController] No player found, cannot spawn missiles");
                return;
            }

            Debug.Log($"[SeekingMissileController] Starting missile spawn coroutine. Player: {playerController != null}, Config: {config != null}");
            StartCoroutine(SpawnMissilesCoroutine());
        }

        private IEnumerator SpawnMissilesCoroutine()
        {
            Debug.Log("[SeekingMissileController] SpawnMissilesCoroutine started");
            
            if (config == null || playerController == null) 
            {
                Debug.LogWarning($"[SeekingMissileController] Coroutine stopped - config:{config != null}, player:{playerController != null}");
                yield break;
            }

            Debug.Log($"[SeekingMissileController] Spawning {config.missileCount} missiles with {config.spawnDelay}s delay between launches");
            
            isSpawning = true;
            hasBeenUsedThisLevel = true;

            ClearActiveMissiles();
            ObjectPool.PrewarmSeekingMissiles(config.missileCount);

            for (int i = 0; i < config.missileCount; i++)
            {
                Debug.Log($"[SeekingMissileController] Spawning missile {i + 1}/{config.missileCount}");
                SpawnMissile();
                
                if (i < config.missileCount - 1)
                {
                    yield return new WaitForSeconds(config.spawnDelay);
                }
            }

            isSpawning = false;
            Debug.Log("[SeekingMissileController] All missiles spawned, invoking OnMissilesSpawned");
            OnMissilesSpawned?.Invoke();
        }

        private void SpawnMissile()
        {
            Debug.Log("[SeekingMissileController] SpawnMissile called");
            
            Vector3 spawnPosition = playerController.transform.position;
            Vector3 randomOffset = new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0f);
            spawnPosition += randomOffset;
            
            Debug.Log($"[SeekingMissileController] Spawn position: {spawnPosition} (with offset: {randomOffset})");
            
            SeekingMissile missile = ObjectPool.GetSeekingMissile();
            Debug.Log($"[SeekingMissileController] Got missile from pool: {missile != null}");
            
            if (missile != null)
            {
                missile.transform.position = spawnPosition;
                missile.transform.rotation = Quaternion.identity;
                missile.Initialize(config);
                missile.OnMissileDestroyed += OnMissileDestroyed;
                missile.OnMissileHitBlock += HandleMissileHitBlock;
                
                activeMissiles.Add(missile);
                Debug.Log($"[SeekingMissileController] Missile added to active list. Total active: {activeMissiles.Count}");

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
                Debug.LogError("[SeekingMissileController] Failed to get missile from pool!");
            }
        }

        private void OnMissileDestroyed(SeekingMissile missile)
        {
            if (activeMissiles.Remove(missile) && activeMissiles.Count == 0)
            {
                OnMissilesCompleted?.Invoke();
            }
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
        }

        public void ResetLevelUsage()
        {
            Debug.Log("[SeekingMissileController] ResetLevelUsage called");
            hasBeenUsedThisLevel = false;
            ClearActiveMissiles();
            
            // Try to reset button immediately
            TryResetButton();
            
            // Also try again after a short delay in case of timing issues
            StartCoroutine(TryResetButtonDelayed());
        }
        
        private void TryResetButton()
        {
            Debug.Log("[SeekingMissileController] Looking for SeekingMissileButton...");
            var missileButton = FindObjectOfType<SeekingMissileButton>();
            Debug.Log($"[SeekingMissileController] Found missile button: {missileButton != null}");
            
            if (missileButton != null)
            {
                Debug.Log("[SeekingMissileController] About to call ResetButton()");
                missileButton.ResetButton();
                Debug.Log("[SeekingMissileController] Reset missile button");
            }
            else
            {
                Debug.LogWarning("[SeekingMissileController] No SeekingMissileButton found to reset!");
            }
        }
        
        private System.Collections.IEnumerator TryResetButtonDelayed()
        {
            yield return new WaitForSeconds(0.2f);
            Debug.Log("[SeekingMissileController] Trying delayed button reset...");
            TryResetButton();
        }

        public bool HasBeenUsedThisLevel => hasBeenUsedThisLevel;
        public int ActiveMissileCount => activeMissiles.Count;
        public bool IsSpawning => isSpawning;

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }

        public void RefreshReferences()
        {
            FindMissingReferences();
        }
    }
} 