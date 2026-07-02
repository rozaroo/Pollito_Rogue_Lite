using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Editor
{
    public class TilemapSpriteSwapper : EditorWindow
    {
        private RuleTile targetRuleTile;
        private Object sourceAsepriteFile;

        [MenuItem("4P Externals/Tiles/Auto Rules Sprite Swapper")]
        public static void ShowWindow()
        {
            GetWindow<TilemapSpriteSwapper>("Auto Rules Sprite Swapper");
        }

        private void OnGUI()
        {
            GUILayout.Label("Tilemap Sprite Swapper", EditorStyles.boldLabel);

            targetRuleTile = (RuleTile)EditorGUILayout.ObjectField("Rule Tile", targetRuleTile, typeof(RuleTile), false);
            sourceAsepriteFile = EditorGUILayout.ObjectField("Aseprite Asset", sourceAsepriteFile, typeof(Object), false);

            if (GUILayout.Button("Swap Sprites"))
            {
                if (targetRuleTile == null || sourceAsepriteFile == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign both the Rule Tile and Aseprite Asset.", "OK");
                    return;
                }

                SwapSprites();
            }
            
            EditorGUILayout.Space();
            GUILayout.Label("Collider Operations", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Set All Rules to Use Default Collider"))
            {
                if (targetRuleTile == null)
                {
                    EditorUtility.DisplayDialog("Error", "Please assign the Rule Tile.", "OK");
                    return;
                }
                
                SetAllRulesToDefaultCollider();
            }
        }

        private void SwapSprites()
        {
            string asepritePath = AssetDatabase.GetAssetPath(sourceAsepriteFile);
            string folderPath = System.IO.Path.GetDirectoryName(asepritePath);
            string newSpriteBaseName = System.IO.Path.GetFileNameWithoutExtension(asepritePath);

            var availableSprites = new List<Sprite>();
            string[] guids = AssetDatabase.FindAssets("t:sprite", new[] { folderPath });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                // Get all assets at this path (could be multiple sprites in a sheet)
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
    
                foreach (Object asset in assets)
                {
                    if (asset is Sprite sprite && sprite.name.Contains(newSpriteBaseName))
                    {
                        availableSprites.Add(sprite);
                        Debug.Log($"Found sprite: {sprite.name}");
                    }
                }
            }

            var indexRegex = new Regex(@"_(\d+)$");
            var serializedTile = new SerializedObject(targetRuleTile);
            serializedTile.Update();

            var tilingRules = serializedTile.FindProperty("m_TilingRules");
            if (tilingRules == null || !tilingRules.isArray)
            {
                Debug.LogError("No tiling rules found in the Rule Tile");
                return;
            }

            int swapCount = 0;

            for (int i = 0; i < tilingRules.arraySize; i++)
            {
                var rule = tilingRules.GetArrayElementAtIndex(i);
                SwapSpritesInArray(rule.FindPropertyRelative("m_Sprites"), availableSprites, newSpriteBaseName, indexRegex, ref swapCount);
                SwapSingleSprite(rule.FindPropertyRelative("m_Output"), availableSprites, newSpriteBaseName, indexRegex, ref swapCount);
            }

            serializedTile.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetRuleTile);
            AssetDatabase.SaveAssets();

            string message = swapCount > 0 ? $"Successfully swapped {swapCount} sprites." : "No sprites were swapped.";
            Debug.Log(message);
            EditorUtility.DisplayDialog("Sprite Swap Result", message, "OK");
        }

        private void SwapSpritesInArray(SerializedProperty spriteArray, List<Sprite> availableSprites, string baseName, Regex indexRegex, ref int swapCount)
        {
            if (spriteArray == null || !spriteArray.isArray) return;

            for (int j = 0; j < spriteArray.arraySize; j++)
            {
                var spriteProperty = spriteArray.GetArrayElementAtIndex(j);
                if (spriteProperty?.propertyType != SerializedPropertyType.ObjectReference) continue;

                Sprite currentSprite = spriteProperty.objectReferenceValue as Sprite;
                if (currentSprite == null) continue;

                Match match = indexRegex.Match(currentSprite.name);
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out int index)) continue;

                string targetName = $"{baseName}_{index}";
                Sprite newSprite = availableSprites.FirstOrDefault(s => s.name == targetName);

                if (newSprite != null && newSprite != currentSprite)
                {
                    spriteProperty.objectReferenceValue = newSprite;
                    swapCount++;
                }
            }
        }

        private void SwapSingleSprite(SerializedProperty spriteProperty, List<Sprite> availableSprites, string baseName, Regex indexRegex, ref int swapCount)
        {
            if (spriteProperty?.propertyType != SerializedPropertyType.ObjectReference) return;

            Sprite currentSprite = spriteProperty.objectReferenceValue as Sprite;
            if (currentSprite == null) return;

            Match match = indexRegex.Match(currentSprite.name);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out int index)) return;

            string targetName = $"{baseName}_{index}";
            Sprite newSprite = availableSprites.FirstOrDefault(s => s.name == targetName);

            if (newSprite != null && newSprite != currentSprite)
            {
                spriteProperty.objectReferenceValue = newSprite;
                swapCount++;
            }
        }
        
        private void SetAllRulesToDefaultCollider()
        {
            var serializedTile = new SerializedObject(targetRuleTile);
            serializedTile.Update();

            var tilingRules = serializedTile.FindProperty("m_TilingRules");
            if (tilingRules == null || !tilingRules.isArray)
            {
                Debug.LogError("No tiling rules found in the Rule Tile");
                return;
            }

            int updatedCount = 0;

            for (int i = 0; i < tilingRules.arraySize; i++)
            {
                var rule = tilingRules.GetArrayElementAtIndex(i);
                var colliderTypeProperty = rule.FindPropertyRelative("m_ColliderType");

                if (colliderTypeProperty != null)
                {
                    // Get the default collider type value from the tile
                    var defaultColliderType = serializedTile.FindProperty("m_DefaultColliderType");
                    if (defaultColliderType != null)
                    {
                        // Set the rule's collider type to the default
                        colliderTypeProperty.intValue = defaultColliderType.intValue;
                        updatedCount++;
                    }
                }
            }

            serializedTile.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetRuleTile);
            AssetDatabase.SaveAssets();

            string message = updatedCount > 0 ? $"Successfully updated {updatedCount} rule colliders to use default collider type." : "No colliders were updated.";
            Debug.Log(message);
            EditorUtility.DisplayDialog("Collider Update Result", message, "OK");
        }
    }
}