using UnityEngine;
using UnityEditor;
using LightItUp.Game;

namespace LightItUp.Editor
{
    public class SeekingMissilePrefabGenerator
    {
        [MenuItem("Tools/LightItUp/Generate Seeking Missile Prefab")]
        public static void GenerateSeekingMissilePrefab()
        {
            // Create the main GameObject
            GameObject missileObject = new GameObject("SeekingMissile");
            
            // Add SpriteRenderer with circle sprite
            SpriteRenderer spriteRenderer = missileObject.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = CreateCircleSprite();
            spriteRenderer.sortingOrder = 10; // Ensure it renders above blocks
            
            // Add Rigidbody2D
            Rigidbody2D rb = missileObject.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.drag = 0f;
            rb.angularDrag = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            
            // Add CircleCollider2D
            CircleCollider2D collider = missileObject.AddComponent<CircleCollider2D>();
            collider.isTrigger = true;
            collider.radius = 0.3f;
            
            // Add TrailRenderer
            TrailRenderer trail = missileObject.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = CreateTrailMaterial();
            trail.sortingOrder = 9; // Below the missile sprite
            
            // Add SeekingMissile component
            SeekingMissile seekingMissile = missileObject.AddComponent<SeekingMissile>();
            
            // Set up the prefab path
            string prefabPath = "Assets/_Game/Prefabs/SeekingMissile.prefab";
            
            // Ensure the directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(missileObject, prefabPath);
            
            // Clean up the scene object
            Object.DestroyImmediate(missileObject);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"Seeking Missile prefab created at: {prefabPath}");
        }
        
        private static Sprite CreateCircleSprite()
        {
            // Create a simple circle texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            float radius = size / 2f - 2f;
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    float alpha = distance <= radius ? 1f : 0f;
                    
                    // Add some gradient for better appearance
                    if (distance <= radius)
                    {
                        float gradient = 1f - (distance / radius) * 0.3f;
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * gradient));
                    }
                    else
                    {
                        texture.SetPixel(x, y, new Color(1f, 1f, 1f, 0f));
                    }
                }
            }
            
            texture.Apply();
            
            // Create sprite from texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "MissileCircle";
            
            return sprite;
        }
        
        private static Material CreateTrailMaterial()
        {
            // Create a simple trail material
            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }
            
            Material material = new Material(shader);
            material.name = "MissileTrail";
            
            return material;
        }
    }
} 