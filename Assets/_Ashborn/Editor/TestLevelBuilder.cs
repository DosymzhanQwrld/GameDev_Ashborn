using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Ashborn.Core;
using Ashborn.Player;

namespace Ashborn.EditorTools
{
    /// <summary>
    /// Step 3 helper: builds the grey-box test level in Level_01.
    /// Menu: Ashborn → Build Test Level. Safe to run again: it rebuilds the level.
    /// Editor-only (lives in an Editor folder, not included in builds).
    /// </summary>
    public static class TestLevelBuilder
    {
        const string ScenePath = "Assets/_Ashborn/Scenes/Level_01.unity";
        const string TilesFolder = "Assets/_Ashborn/Art/Tiles";
        const string TexturePath = TilesFolder + "/Placeholder_Tile.png";
        const string TilePath = TilesFolder + "/Placeholder_Tile.asset";
        const string PhysicsFolder = "Assets/_Ashborn/Physics";
        const string NoFrictionPath = PhysicsFolder + "/NoFriction.physicsMaterial2D";
        const string GridName = "Level Grid";

        [MenuItem("Ashborn/Build Test Level")]
        public static void Build()
        {
            if (EditorApplication.isPlaying) { Debug.LogWarning("Exit Play Mode first."); return; }

            if (SceneManager.GetActiveScene().path != ScenePath)
            {
                if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
                EditorSceneManager.OpenScene(ScenePath);
            }

            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer < 0) { Debug.LogError("Layer 'Ground' is missing (Project Settings → Tags and Layers)."); return; }

            TileBase tile = GetOrCreateTile();
            PhysicsMaterial2D noFriction = GetOrCreateNoFriction();

            RemoveOldGround();
            Tilemap map = CreateTilemap(groundLayer);
            PaintLevel(map, tile);

            var player = Object.FindAnyObjectByType<PlayerMovement>();
            if (player == null) { Debug.LogError("No object with PlayerMovement in the scene (step 2)."); return; }
            SetupPlayer(player, noFriction);
            SetupCamera(player.transform);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            Selection.activeGameObject = map.gameObject;
            Debug.Log("Ashborn: test level built and Level_01 saved. Press Play.");
        }

        // ---------- assets ----------

        static TileBase GetOrCreateTile()
        {
            var existing = AssetDatabase.LoadAssetAtPath<Tile>(TilePath);
            if (existing != null) return existing;

            // 32×32 grey square with a darker border, so single tiles are visible.
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var fill = new Color32(120, 116, 110, 255);
            var edge = new Color32(70, 67, 62, 255);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    tex.SetPixel(x, y, (x == 0 || y == 0 || x == size - 1 || y == size - 1) ? edge : fill);
            File.WriteAllBytes(TexturePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(TexturePath);

            // Pixel-art import settings: 32 px = 1 unit = 1 grid cell.
            var importer = (TextureImporter)AssetImporter.GetAtPath(TexturePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = size;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();

            var tile = ScriptableObject.CreateInstance<Tile>();
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(TexturePath);
            tile.colliderType = Tile.ColliderType.Grid;
            AssetDatabase.CreateAsset(tile, TilePath);
            AssetDatabase.SaveAssets();
            return tile;
        }

        static PhysicsMaterial2D GetOrCreateNoFriction()
        {
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);
            if (mat != null) return mat;
            if (!AssetDatabase.IsValidFolder(PhysicsFolder)) AssetDatabase.CreateFolder("Assets/_Ashborn", "Physics");
            mat = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(mat, NoFrictionPath);
            AssetDatabase.SaveAssets();
            return mat;
        }

        // ---------- scene ----------

        static void RemoveOldGround()
        {
            // The temporary stretched square from step 2.
            foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
                if (go.name == "Ground" && go.GetComponent<SpriteRenderer>() != null && go.GetComponent<Tilemap>() == null)
                    Undo.DestroyObjectImmediate(go);

            var oldGrid = GameObject.Find(GridName);
            if (oldGrid != null) Undo.DestroyObjectImmediate(oldGrid);
        }

        static Tilemap CreateTilemap(int groundLayer)
        {
            var gridGo = new GameObject(GridName, typeof(Grid));
            Undo.RegisterCreatedObjectUndo(gridGo, "Build Test Level");

            var mapGo = new GameObject("Ground", typeof(Tilemap), typeof(TilemapRenderer));
            mapGo.transform.SetParent(gridGo.transform, false);
            mapGo.layer = groundLayer;

            // One merged collider instead of one box per tile: no snagging on tile seams.
            var body = mapGo.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Static;
            var tileCollider = mapGo.AddComponent<TilemapCollider2D>();
            tileCollider.compositeOperation = Collider2D.CompositeOperation.Merge;
            var composite = mapGo.AddComponent<CompositeCollider2D>();
            composite.geometryType = CompositeCollider2D.GeometryType.Polygons;

            return mapGo.GetComponent<Tilemap>();
        }

        // Level layout in grid cells (1 cell = 1 unit). Floor surface is at y = -2.
        // Max jump height is about 2.9 units, so every step up is 2 cells.
        static void PaintLevel(Tilemap map, TileBase t)
        {
            Fill(map, t, -14, -13, -5, 8);   // left wall
            Fill(map, t, -12, 15, -5, -3);   // floor, start area
            Fill(map, t, 16, 19, -5, -5);    // shallow pit (2 cells deep, you can jump out)
            Fill(map, t, 20, 50, -5, -3);    // floor after the pit
            Fill(map, t, 51, 52, -5, 8);     // right wall

            Fill(map, t, -2, 1, -1, -1);     // platform 1 (+2)
            Fill(map, t, 4, 7, 1, 1);        // platform 2 (+4)
            Fill(map, t, 10, 13, 3, 3);      // platform 3 (+6)
            Fill(map, t, 26, 29, -1, -1);    // platform after the pit
            Fill(map, t, 36, 37, -2, -1);    // small wall to jump over
            Fill(map, t, 42, 45, 0, 0);      // platform near the end

            map.CompressBounds();
            map.GetComponent<CompositeCollider2D>().GenerateGeometry();
        }

        static void Fill(Tilemap map, TileBase t, int x0, int x1, int y0, int y1)
        {
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    map.SetTile(new Vector3Int(x, y, 0), t);
        }

        static void SetupPlayer(PlayerMovement player, PhysicsMaterial2D noFriction)
        {
            Undo.RecordObject(player.transform, "Move Player");
            player.transform.position = new Vector3(-10f, 0f, 0f);

            var body = player.GetComponent<Rigidbody2D>();
            Undo.RecordObject(body, "No friction");
            body.sharedMaterial = noFriction;

            // Make sure ground check uses the Ground layer.
            var so = new SerializedObject(player);
            var mask = so.FindProperty("groundLayer");
            if (mask != null && mask.intValue == 0)
            {
                mask.intValue = LayerMask.GetMask("Ground");
                so.ApplyModifiedProperties();
            }
        }

        static void SetupCamera(Transform target)
        {
            var cam = Camera.main;
            if (cam == null) { Debug.LogWarning("No Main Camera in the scene."); return; }

            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) follow = Undo.AddComponent<CameraFollow>(cam.gameObject);

            var so = new SerializedObject(follow);
            so.FindProperty("target").objectReferenceValue = target;
            so.ApplyModifiedProperties();

            Undo.RecordObject(cam.transform, "Move Camera");
            cam.transform.position = new Vector3(target.position.x, target.position.y + 1f, cam.transform.position.z);
        }
    }
}
