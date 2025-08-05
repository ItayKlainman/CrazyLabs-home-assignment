using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LightItUp.Data;
using LightItUp.Game;

namespace LightItUp.Game
{
    public class SeekingMissile : PooledObject
    {
        [Header("Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;

        private BlockController currentTarget;
        private float lifetime;
        private bool isActive = true;
        private Vector2 velocity;

        public System.Action<SeekingMissile> OnMissileDestroyed { get; set; }
        public System.Action<SeekingMissile, BlockController> OnMissileHitBlock { get; set; }

        private void Awake()
        {
            GetComponents();
            InitializeRigidbody();
        }

        private void GetComponents()
        {
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();
        }

        private void InitializeRigidbody()
        {
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.drag = 0f;
                rb.angularDrag = 0f;
            }
        }

        public override void OnInitPoolObj()
        {
            base.OnInitPoolObj();
            isActive = true;
            lifetime = 0f;
            currentTarget = null;
            velocity = Vector2.zero;
        }

        public override void OnReturnedPoolObj()
        {
            base.OnReturnedPoolObj();
            isActive = false;
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }
        }

        public void Initialize(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
            
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.missileColor;
                transform.localScale = Vector3.one * config.missileSize;
            }
            
            if (trailRenderer != null)
            {
                trailRenderer.startColor = config.missileColor;
                trailRenderer.endColor = new Color(config.missileColor.r, config.missileColor.g, config.missileColor.b, 0f);
                trailRenderer.enabled = true;
            }

            FindTarget();
        }

        private void Update()
        {
            if (!isActive || config == null) return;

            UpdateLifetime();
            
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget();
                
                // If still no target after trying, self-destruct after a short delay
                if (currentTarget == null)
                {
                    if (lifetime > 2f) // Give 2 seconds to find a target
                    {
                        DestroyMissile();
                        return;
                    }
                }
            }
            
            if (currentTarget != null)
            {
                MoveTowardsTarget();
            }
        }

        private void FindTarget()
        {
            if (config == null) return;

            if (SeekingMissileTargetManager.Instance != null)
            {
                currentTarget = SeekingMissileTargetManager.Instance.GetNextTarget(transform.position, config);
            }
            else
            {
                currentTarget = FindBestTarget();
            }
        }

        private void MoveTowardsTarget()
        {
            Vector2 direction = (currentTarget.transform.position - transform.position).normalized;
            Vector2 desiredVelocity = direction * config.missileSpeed;
            
            UpdateRotation(direction);
            UpdateVelocity(desiredVelocity);
            ApplyMovement();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            var blockController = other.GetComponent<BlockController>();
            if (IsValidBlockTarget(blockController))
            {
                HitBlock(blockController);
            }
        }

        private void HitBlock(BlockController block)
        {
            ApplyImpactForce(block);
            block.Collide();
            OnMissileHitBlock?.Invoke(this, block);
            DestroyMissile(true); // true = hit target
        }

        public void DestroyMissile(bool hitTarget = false)
        {
            if (!isActive) return;

            // Only release target back to pool if it wasn't hit
            if (currentTarget != null && SeekingMissileTargetManager.Instance != null && !hitTarget)
            {
                SeekingMissileTargetManager.Instance.ReleaseTarget(currentTarget);
            }

            OnMissileDestroyed?.Invoke(this);
            ObjectPool.ReturnSeekingMissile(this);
        }



        public bool IsActive => isActive;
        public BlockController CurrentTarget => currentTarget;

        private bool IsValidBlockTarget(BlockController block) => block != null && !block.IsLit;

        private void UpdateLifetime()
        {
            lifetime += Time.deltaTime;
            if (lifetime >= config.maxLifetime)
            {
                DestroyMissile();
            }
        }

        private BlockController FindBestTarget()
        {
            var currentLevel = GameManager.Instance?.currentLevel;
            if (currentLevel?.blocks == null) return null;

            var validTargets = new List<BlockController>();
            var targetedBlocks = GetTargetedBlocks();

            foreach (var block in currentLevel.blocks)
            {
                if (!IsValidTargetBlock(block)) continue;

                float distance = Vector2.Distance(transform.position, block.transform.position);
                if (distance > config.detectionRadius) continue;

                if (config.prioritizeRegularBlocks && (block.useExplode || block.useMove))
                {
                    continue;
                }

                if (!targetedBlocks.Contains(block))
                {
                    validTargets.Add(block);
                }
            }

            if (validTargets.Count == 0)
            {
                validTargets = GetValidBlocksInRange();
            }

            if (validTargets.Count == 0) return null;

            return validTargets[Random.Range(0, validTargets.Count)];
        }

        private List<BlockController> GetTargetedBlocks()
        {
            var targetedBlocks = new List<BlockController>();
            var missiles = FindObjectsOfType<SeekingMissile>();
            
            foreach (var missile in missiles)
            {
                if (missile != this && missile.IsActive && missile.CurrentTarget != null)
                {
                    targetedBlocks.Add(missile.CurrentTarget);
                }
            }
            
            return targetedBlocks;
        }

        private List<BlockController> GetValidBlocksInRange()
        {
            var validBlocks = new List<BlockController>();
            var currentLevel = GameManager.Instance?.currentLevel;
            
            if (currentLevel?.blocks == null) return validBlocks;

            foreach (var block in currentLevel.blocks)
            {
                if (!IsValidTargetBlock(block)) continue;

                float distance = Vector2.Distance(transform.position, block.transform.position);
                if (distance > config.detectionRadius) continue;

                if (config.prioritizeRegularBlocks && (block.useExplode || block.useMove))
                {
                    continue;
                }

                validBlocks.Add(block);
            }
            
            return validBlocks;
        }

        private bool IsValidTargetBlock(BlockController block)
        {
            return block != null && block.gameObject.activeInHierarchy && !block.IsLit;
        }

        private void UpdateRotation(Vector2 direction)
        {
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            float rotationStep = config.rotationSpeed * Time.deltaTime;
            float newAngle = currentAngle + Mathf.Clamp(angleDifference, -rotationStep, rotationStep);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);
        }

        private void UpdateVelocity(Vector2 desiredVelocity)
        {
            velocity = Vector2.Lerp(velocity, desiredVelocity, Time.deltaTime * 5f);
        }

        private void ApplyMovement()
        {
            if (rb != null)
            {
                rb.velocity = velocity;
            }
            else
            {
                transform.position += (Vector3)velocity * Time.deltaTime;
            }
        }

        private void ApplyImpactForce(BlockController block)
        {
            if (config.forceOnImpact <= 0f) return;

            var blockRb = block.GetComponent<Rigidbody2D>();
            if (blockRb != null)
            {
                Vector2 forceDirection = (block.transform.position - transform.position).normalized;
                blockRb.AddForce(forceDirection * config.forceOnImpact, ForceMode2D.Impulse);
            }
        }


    }
} 