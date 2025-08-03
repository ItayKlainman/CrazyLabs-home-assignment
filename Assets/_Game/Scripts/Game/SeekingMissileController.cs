using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LightItUp.Data;

namespace LightItUp.Game
{
    public class SeekingMissileController : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;
        
        [Header("References")]
        [SerializeField] private CameraFocus cameraFocus;
        [SerializeField] private PlayerController playerController;

        private List<SeekingMissile> activeMissiles = new List<SeekingMissile>();
        private bool hasBeenUsedThisLevel = false;
        private bool isSpawning = false;

        public System.Action OnMissilesSpawned { get; private set; }
        public System.Action OnMissilesCompleted { get; private set; }
        public System.Action OnMissileHitBlock { get; private set; }

        private void Awake()
        {
            FindMissingReferences();
        }

        private void FindMissingReferences()
        {
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
            ResetLevelUsage();
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
            if (config == null || playerController == null) yield break;

            isSpawning = true;
            hasBeenUsedThisLevel = true;

            ClearActiveMissiles();
            ObjectPool.PrewarmSeekingMissiles(config.missileCount);

            for (int i = 0; i < config.missileCount; i++)
            {
                SpawnSingleMissile();
                
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
            if (playerController == null) return;

            Vector3 spawnPosition = playerController.transform.position;
            SeekingMissile missile = ObjectPool.GetSeekingMissile();
            
            if (missile != null)
            {
                missile.transform.position = spawnPosition;
                missile.transform.rotation = Quaternion.identity;
                missile.Initialize(config);
                missile.OnMissileDestroyed += OnMissileDestroyed;
                missile.OnMissileHitBlock += OnMissileHitBlockHandler;
                
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
            if (activeMissiles.Contains(missile))
            {
                activeMissiles.Remove(missile);
            }

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