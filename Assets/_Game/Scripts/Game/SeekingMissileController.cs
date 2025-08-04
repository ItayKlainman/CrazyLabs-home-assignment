using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightItUp.Data;
using LightItUp.Game;

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

        public bool CanUseMissiles() => config != null && config.isEnabled && !hasBeenUsedThisLevel && !isSpawning;

        public void SpawnMissiles()
        {
            if (!CanUseMissiles()) return;

            FindMissingReferences();
            if (playerController == null)
            {
                Debug.LogWarning("SeekingMissileController: No player found, cannot spawn missiles");
                return;
            }

            StartCoroutine(SpawnMissilesCoroutine());
        }

        private IEnumerator SpawnMissilesCoroutine()
        {
            if (config == null || playerController == null) yield break;

            isSpawning = true;
            hasBeenUsedThisLevel = true;

            ClearActiveMissiles();
            ObjectPool.PrewarmSeekingMissiles(config.missileCount);

            for (int i = 0; i < config.missileCount; i++)
            {
                SpawnMissile();
                
                if (i < config.missileCount - 1)
                {
                    yield return new WaitForSeconds(config.spawnDelay);
                }
            }

            isSpawning = false;
            OnMissilesSpawned?.Invoke();
        }

        private void SpawnMissile()
        {
            Vector3 spawnPosition = playerController.transform.position;
            SeekingMissile missile = ObjectPool.GetSeekingMissile();
            
            if (missile != null)
            {
                missile.transform.position = spawnPosition;
                missile.transform.rotation = Quaternion.identity;
                missile.Initialize(config);
                missile.OnMissileDestroyed += OnMissileDestroyed;
                missile.OnMissileHitBlock += HandleMissileHitBlock;
                
                activeMissiles.Add(missile);

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
            if (activeMissiles.Remove(missile) && activeMissiles.Count == 0)
            {
                OnMissilesCompleted?.Invoke();
            }
        }

        private void HandleMissileHitBlock(SeekingMissile missile, BlockController block)
        {
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
            hasBeenUsedThisLevel = false;
            ClearActiveMissiles();
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