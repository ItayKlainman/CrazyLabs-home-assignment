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
            Debug.Log($"[17] GetNextTarget called. Available: {availableTargets.Count}, Reserved: {reservedTargets.Count}");
            Debug.Log($"[29] Missile position: {missilePosition}");
            
            if (availableTargets.Count == 0) 
            {
                Debug.LogWarning("[18] No available targets in pool");
                return null;
            }

            var validTargets = new List<BlockController>();

            foreach (var block in availableTargets)
            {
                if (!IsValidTarget(block)) 
                {
                    Debug.Log($"[25] Block {block.name} is not valid target");
                    continue;
                }

                // Distance limitation removed - missiles can target any block in the level

                if (config.prioritizeRegularBlocks && (block.useExplode || block.useMove))
                {
                    Debug.Log($"[27] Block {block.name} skipped due to prioritizeRegularBlocks");
                    continue;
                }

                validTargets.Add(block);
                Debug.Log($"[28] Added {block.name} to valid targets");
            }

            if (validTargets.Count == 0) 
            {
                Debug.LogWarning($"[19] No valid targets found. Available: {availableTargets.Count}");
                return null;
            }

            // Choose a random target from valid targets
            var selectedTarget = validTargets[Random.Range(0, validTargets.Count)];
            
            // Reserve the target
            ReserveTarget(selectedTarget);
            
            Debug.Log($"[20] Assigned target {selectedTarget.name} to missile. Available: {availableTargets.Count}, Reserved: {reservedTargets.Count}");
            
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
            Debug.Log($"[21] ReleaseTarget called for {target?.name}. Reserved: {reservedTargets.Count}");
            if (reservedTargets.Contains(target))
            {
                reservedTargets.Remove(target);
                if (IsValidTarget(target))
                {
                    availableTargets.Add(target);
                    Debug.Log($"[22] Released target {target.name} back to pool. Available: {availableTargets.Count}, Reserved: {reservedTargets.Count}");
                }
                else
                {
                    Debug.Log($"[23] Target {target.name} is no longer valid (probably lit)");
                }
            }
            else
            {
                Debug.Log($"[24] Target {target?.name} was not in reserved list");
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