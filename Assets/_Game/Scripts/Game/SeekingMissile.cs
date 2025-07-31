using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using LightItUp.BlockComponents;
using LightItUp.Data;

namespace LightItUp.Game
{
    public class SeekingMissile : MonoBehaviour
    {
        [Header("Components")]
        [SerializeField] private Rigidbody2D rb;
        [SerializeField] private Collider2D col;
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField] private TrailRenderer trailRenderer;

        [Header("Configuration")]
        [SerializeField] private SeekingMissileConfig config;

        // Private fields
        private BlockController currentTarget;
        private float lifetime;
        private bool isActive = true;
        private Vector2 velocity;

        // Events
        public System.Action<SeekingMissile> OnMissileDestroyed;
        public System.Action<SeekingMissile, BlockController> OnMissileHitBlock;

        private void Awake()
        {
            // Get components if not assigned
            if (rb == null) rb = GetComponent<Rigidbody2D>();
            if (col == null) col = GetComponent<Collider2D>();
            if (spriteRenderer == null) spriteRenderer = GetComponent<SpriteRenderer>();
            if (trailRenderer == null) trailRenderer = GetComponent<TrailRenderer>();

            // Initialize components
            if (rb != null)
            {
                rb.gravityScale = 0f;
                rb.drag = 0f;
                rb.angularDrag = 0f;
            }

            // Set up visual appearance
            if (spriteRenderer != null && config != null)
            {
                spriteRenderer.color = config.missileColor;
                transform.localScale = Vector3.one * config.missileSize;
            }

            // Set up trail renderer
            if (trailRenderer != null && config != null)
            {
                trailRenderer.startColor = config.missileColor;
                trailRenderer.endColor = new Color(config.missileColor.r, config.missileColor.g, config.missileColor.b, 0f);
            }
        }

        public void Initialize(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
            lifetime = 0f;
            isActive = true;
            currentTarget = null;
            velocity = Vector2.zero;

            // Update visual appearance
            if (spriteRenderer != null)
            {
                spriteRenderer.color = config.missileColor;
                transform.localScale = Vector3.one * config.missileSize;
            }

            if (trailRenderer != null)
            {
                trailRenderer.startColor = config.missileColor;
                trailRenderer.endColor = new Color(config.missileColor.r, config.missileColor.g, config.missileColor.b, 0f);
            }

            // Find initial target
            FindTarget();
        }

        private void Update()
        {
            if (!isActive || config == null) return;

            // Update lifetime
            lifetime += Time.deltaTime;
            if (lifetime >= config.maxLifetime)
            {
                DestroyMissile();
                return;
            }

            // Find target if we don't have one
            if (currentTarget == null || !currentTarget.gameObject.activeInHierarchy)
            {
                FindTarget();
            }

            // Move towards target
            if (currentTarget != null)
            {
                MoveTowardsTarget();
            }
        }

        private void FindTarget()
        {
            if (config == null) return;

            // Get all blocks from current level
            var currentLevel = GameManager.Instance?.currentLevel;
            if (currentLevel == null || currentLevel.blocks == null) return;

            // Filter unlit blocks
            var unlitBlocks = currentLevel.blocks.Where(b => b != null && b.gameObject.activeInHierarchy && !b.IsLit).ToList();
            if (unlitBlocks.Count == 0) return;

            // Prioritize regular blocks if configured
            if (config.prioritizeRegularBlocks)
            {
                var regularBlocks = unlitBlocks.Where(b => !b.useExplode && !b.useMove).ToList();
                if (regularBlocks.Count > 0)
                {
                    unlitBlocks = regularBlocks;
                }
            }

            // Find nearest block within detection radius
            BlockController nearestBlock = null;
            float nearestDistance = float.MaxValue;

            foreach (var block in unlitBlocks)
            {
                float distance = Vector2.Distance(transform.position, block.transform.position);
                if (distance <= config.detectionRadius && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestBlock = block;
                }
            }

            currentTarget = nearestBlock;
        }

        private void MoveTowardsTarget()
        {
            if (currentTarget == null || config == null) return;

            Vector2 direction = (currentTarget.transform.position - transform.position).normalized;
            
            // Calculate desired velocity
            Vector2 desiredVelocity = direction * config.missileSpeed;
            
            // Smoothly rotate towards target
            float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            float currentAngle = transform.eulerAngles.z;
            float angleDifference = Mathf.DeltaAngle(currentAngle, targetAngle);
            
            // Apply rotation
            float rotationStep = config.rotationSpeed * Time.deltaTime;
            float newAngle = currentAngle + Mathf.Clamp(angleDifference, -rotationStep, rotationStep);
            transform.rotation = Quaternion.Euler(0, 0, newAngle);

            // Update velocity
            velocity = Vector2.Lerp(velocity, desiredVelocity, Time.deltaTime * 5f);
            
            // Apply movement
            if (rb != null)
            {
                rb.velocity = velocity;
            }
            else
            {
                transform.position += (Vector3)velocity * Time.deltaTime;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!isActive) return;

            // Check if we hit a block
            var blockController = other.GetComponent<BlockController>();
            if (blockController != null && !blockController.IsLit)
            {
                HitBlock(blockController);
            }
        }

        private void HitBlock(BlockController block)
        {
            if (!isActive || config == null) return;

            // Apply force to the block
            if (config.forceOnImpact > 0f)
            {
                var blockRb = block.GetComponent<Rigidbody2D>();
                if (blockRb != null)
                {
                    Vector2 forceDirection = (block.transform.position - transform.position).normalized;
                    blockRb.AddForce(forceDirection * config.forceOnImpact, ForceMode2D.Impulse);
                }
            }

            // Trigger collision with the block
            block.Collide();

            // Notify listeners
            OnMissileHitBlock?.Invoke(this, block);

            // Destroy the missile
            DestroyMissile();
        }

        public void DestroyMissile()
        {
            if (!isActive) return;

            isActive = false;
            OnMissileDestroyed?.Invoke(this);
            
            // Disable trail renderer to prevent visual artifacts
            if (trailRenderer != null)
            {
                trailRenderer.enabled = false;
            }

            // Destroy the game object
            Destroy(gameObject);
        }

        public void SetConfig(SeekingMissileConfig missileConfig)
        {
            config = missileConfig;
        }

        public bool IsActive => isActive;
        public BlockController CurrentTarget => currentTarget;
    }
} 