using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using LightItUp.UI;

namespace LightItUp.Editor
{
    public class SeekingMissileButtonGenerator
    {
        [MenuItem("Tools/LightItUp/Generate Seeking Missile Button")]
        public static void GenerateSeekingMissileButton()
        {
            // Create the main button GameObject
            GameObject buttonObject = new GameObject("SeekingMissileButton");
            
            // Add RectTransform
            RectTransform rectTransform = buttonObject.AddComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(120f, 120f);
            
            // Add Image component for the button background
            Image buttonImage = buttonObject.AddComponent<Image>();
            buttonImage.color = Color.red;
            buttonImage.sprite = CreateSquareSprite();
            
            // Add Button component
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            
            // Add SeekingMissileButton component
            SeekingMissileButton seekingMissileButton = buttonObject.AddComponent<SeekingMissileButton>();
            
            // Create text child
            GameObject textObject = new GameObject("Text");
            textObject.transform.SetParent(buttonObject.transform, false);
            
            RectTransform textRectTransform = textObject.AddComponent<RectTransform>();
            textRectTransform.anchorMin = Vector2.zero;
            textRectTransform.anchorMax = Vector2.one;
            textRectTransform.offsetMin = Vector2.zero;
            textRectTransform.offsetMax = Vector2.zero;
            
            TextMeshProUGUI textComponent = textObject.AddComponent<TextMeshProUGUI>();
            textComponent.text = "MISSILES";
            textComponent.fontSize = 12f;
            textComponent.color = Color.white;
            textComponent.alignment = TextAlignmentOptions.Center;
            textComponent.fontStyle = FontStyles.Bold;
            
            // Create missile icon child
            GameObject iconObject = new GameObject("MissileIcon");
            iconObject.transform.SetParent(buttonObject.transform, false);
            
            RectTransform iconRectTransform = iconObject.AddComponent<RectTransform>();
            iconRectTransform.anchorMin = new Vector2(0.2f, 0.6f);
            iconRectTransform.anchorMax = new Vector2(0.8f, 0.9f);
            iconRectTransform.offsetMin = Vector2.zero;
            iconRectTransform.offsetMax = Vector2.zero;
            
            Image iconImage = iconObject.AddComponent<Image>();
            iconImage.sprite = CreateMissileIconSprite();
            iconImage.color = Color.white;
            
            // Set up the prefab path
            string prefabPath = "Assets/_Game/Prefabs/UI/SeekingMissileButton.prefab";
            
            // Ensure the directory exists
            string directory = System.IO.Path.GetDirectoryName(prefabPath);
            if (!System.IO.Directory.Exists(directory))
            {
                System.IO.Directory.CreateDirectory(directory);
            }
            
            // Create the prefab
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(buttonObject, prefabPath);
            
            // Clean up the scene object
            Object.DestroyImmediate(buttonObject);
            
            // Select the created prefab
            Selection.activeObject = prefab;
            
            Debug.Log($"Seeking Missile Button prefab created at: {prefabPath}");
        }
        
        private static Sprite CreateSquareSprite()
        {
            // Create a simple square texture
            int size = 64;
            Texture2D texture = new Texture2D(size, size);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    // Create a square with rounded corners
                    float edgeDistance = Mathf.Min(x, y, size - 1 - x, size - 1 - y);
                    float alpha = edgeDistance >= 4f ? 1f : edgeDistance / 4f;
                    
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            
            texture.Apply();
            
            // Create sprite from texture
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
            sprite.name = "ButtonSquare";
            
            return sprite;
        }
        
        private static Sprite CreateMissileIconSprite()
        {
            // Create a simple missile icon texture
            int size = 32;
            Texture2D texture = new Texture2D(size, size);
            
            Vector2 center = new Vector2(size / 2f, size / 2f);
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    Vector2 pos = new Vector2(x, y);
                    float distance = Vector2.Distance(pos, center);
                    
                    // Create a missile shape (elongated circle)
                    float normalizedX = (x - center.x) / (size * 0.4f);
                    float normalizedY = (y - center.y) / (size * 0.2f);
                    float ellipse = (normalizedX * normalizedX) + (normalizedY * normalizedY);
                    
                    float alpha = ellipse <= 1f ? 1f : 0f;
                    
                    // Add some gradient
                    if (ellipse <= 1f)
                    {
                        float gradient = 1f - ellipse * 0.5f;
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
            sprite.name = "MissileIcon";
            
            return sprite;
        }
    }
} 