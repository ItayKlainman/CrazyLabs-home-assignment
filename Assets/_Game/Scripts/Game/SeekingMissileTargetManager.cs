using System.Collections.Generic;
using UnityEngine;
using LightItUp.Game;
using LightItUp.Singletons;
using LightItUp.Data;

namespace LightItUp.Game
{
    public class SeekingMissileTargetManager : SingletonLoad<SeekingMissileTargetManager>
    {
        private HashSet<BlockController> availableTargets = new HashSet<BlockController>();
        private HashSet<BlockController> reservedTargets = new HashSet<BlockController>();

        public void InitializeTargetPool()
        {
            availableTargets.Clear();
            reservedTargets.Clear();

            var currentLevel = GameManager.Instance?.currentLevel;
            if (currentLevel?.blocks == null) 
            {
                Debug.LogWarning("[TargetManager] No current level or blocks found");
                return;
            }

            int validTargets = 0;
            foreach (var block in currentLevel.blocks)
            {
                if (IsValidTarget(block))
                {
                    availableTargets.Add(block);
                    validTargets++;
                }
            }
            
            Debug.Log($"[TargetManager] Initialized with {validTargets} valid targets out of {currentLevel.blocks.Count} total blocks");
        }

        public BlockController GetNextTarget(Vector3 missilePosition, SeekingMissileConfig config)
        {
            if (availableTargets.Count == 0) 
            {
                Debug.LogWarning("[TargetManager] No available targets in pool");
                return null;
            }

            var validTargets = new List<BlockController>();

            foreach (var block in availableTargets)
            {
                if (!IsValidTarget(block)) continue;

                float distance = Vector2.Distance(missilePosition, block.transform.position);
                if (distance > config.detectionRadius) continue;

                if (config.prioritizeRegularBlocks && (block.useExplode || block.useMove))
                {
                    continue;
                }

                validTargets.Add(block);
            }

            if (validTargets.Count == 0) 
            {
                Debug.LogWarning($"[TargetManager] No valid targets in range. Available: {availableTargets.Count}, Detection radius: {config.detectionRadius}");
                return null;
            }

            // Choose a random target from valid targets
            var selectedTarget = validTargets[Random.Range(0, validTargets.Count)];
            
            // Reserve the target
            ReserveTarget(selectedTarget);
            
            Debug.Log($"[TargetManager] Assigned target {selectedTarget.name} to missile. Available: {availableTargets.Count}, Reserved: {reservedTargets.Count}");
            
            return selectedTarget;
        }

        public void ReserveTarget(BlockController target)
        {
            if (availableTargets.Contains(target))
            {
                availableTargets.Remove(target);
                reservedTargets.Add(target);
            }
        }

        public void ReleaseTarget(BlockController target)
        {
            if (reservedTargets.Contains(target))
            {
                reservedTargets.Remove(target);
                if (IsValidTarget(target))
                {
                    availableTargets.Add(target);
                    Debug.Log($"[TargetManager] Released target {target.name} back to pool. Available: {availableTargets.Count}, Reserved: {reservedTargets.Count}");
                }
            }
        }

        public void ReleaseAllTargets()
        {
            foreach (var target in reservedTargets)
            {
                if (IsValidTarget(target))
                {
                    availableTargets.Add(target);
                }
            }
            reservedTargets.Clear();
        }

        private bool IsValidTarget(BlockController block)
        {
            return block != null && !block.IsLit;
        }

        public int AvailableTargetCount => availableTargets.Count;
        public int ReservedTargetCount => reservedTargets.Count;
    }
} 