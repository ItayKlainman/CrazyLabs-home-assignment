using UnityEngine;

namespace LightItUp.Game
{
    [CreateAssetMenu(fileName = "SeekingMissileConfig", menuName = "LightItUp/SeekingMissileConfig")]
    public class SeekingMissileConfig : ScriptableObject
    {
        [Header("Feature Toggle")]
        [Tooltip("Master on/off switch for the seeking missiles feature")]
        public bool isEnabled = true;

        [Header("Missile Behavior")]
        [Tooltip("Number of missiles to fire")]
        [Range(1, 10)]
        public int missileCount = 3;

        [Tooltip("Movement speed of missiles")]
        [Range(1f, 20f)]
        public float missileSpeed = 10f;

        [Tooltip("Turn rate of missiles (degrees per second)")]
        [Range(50f, 500f)]
        public float rotationSpeed = 200f;

        [Tooltip("Auto-destroy time for missiles")]
        [Range(5f, 30f)]
        public float maxLifetime = 10f;

        [Tooltip("Physics force applied to blocks on impact")]
        [Range(1f, 20f)]
        public float forceOnImpact = 5f;

        [Tooltip("Time between missile spawns")]
        [Range(0.1f, 1f)]
        public float spawnDelay = 0.2f;

        [Header("Targeting")]
        [Tooltip("Target regular blocks first before special blocks")]
        public bool prioritizeRegularBlocks = true;

        [Header("Visual")]
        [Tooltip("Missile appearance color")]
        public Color missileColor = Color.red;

        [Tooltip("Scale factor for missile size")]
        [Range(0.1f, 2f)]
        public float missileSize = 0.5f;

        [Header("Camera")]
        [Tooltip("Include missiles in camera tracking")]
        public bool includeInCameraTracking = true;

        [Tooltip("Duration for camera to track each missile")]
        [Range(1f, 10f)]
        public float cameraTrackingDuration = 3f;
    }
} 