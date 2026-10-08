using System.Linq;
using Unity.Netcode;
using Unity.Netcode.Components;
using Unity.Netcode.Transports.UTP;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

internal static class MultiplayerPrototypeSetup
{
    private const string ScenePath = "Assets/_Game/Scenes/Gameplay/Prototype_Battle.unity";
    private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Network/ZoroNetworkPlayer.prefab";
    private const string LootPickupPrefabPath = "Assets/_Game/Resources/NetworkLootPickup.prefab";
    private const string NetworkPrefabsPath = "Assets/DefaultNetworkPrefabs.asset";

    [InitializeOnLoadMethod]
    private static void EnsureLootPickupSetupAfterReload()
    {
        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            GameObject pickupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LootPickupPrefabPath);
            NetworkLootPickup pickup = pickupPrefab != null
                ? pickupPrefab.GetComponent<NetworkLootPickup>()
                : null;
            NetworkPrefabsList prefabList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
            EnemyHealth sceneEnemy = Object.FindFirstObjectByType<EnemyHealth>();
            if (pickup == null || !Mathf.Approximately(pickup.PickupRadius, 0.55f) ||
                prefabList == null || !prefabList.Contains(pickupPrefab) ||
                sceneEnemy == null || !HasExpectedLoot(sceneEnemy))
                Setup();
        };
    }

    [MenuItem("Tools/One Piece 2D/Setup Multiplayer Prototype %#m")]
    public static void Setup()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        EnsureFolder("Assets/_Game/Prefabs");
        EnsureFolder("Assets/_Game/Prefabs/Network");
        EnsureFolder("Assets/_Game/Resources");

        GameObject lootPickupPrefab = CreateOrUpdateLootPickupPrefab();
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject scenePlayer = GameObject.Find("Zoro_Player");

        if (playerPrefab == null)
        {
            if (scenePlayer == null)
            {
                Debug.LogError("Multiplayer setup could not find Zoro_Player in Prototype_Battle.");
                return;
            }

            AddPlayerNetworkComponents(scenePlayer);
            playerPrefab = PrefabUtility.SaveAsPrefabAsset(scenePlayer, PlayerPrefabPath);
        }

        if (scenePlayer != null)
            Object.DestroyImmediate(scenePlayer);

        GameObject managerObject = GameObject.Find("NetworkManager");
        if (managerObject == null)
            managerObject = new GameObject("NetworkManager");

        NetworkManager networkManager = GetOrAdd<NetworkManager>(managerObject);
        UnityTransport transport = GetOrAdd<UnityTransport>(managerObject);
        GetOrAdd<NetworkBootstrapUI>(managerObject);

        if (networkManager.NetworkConfig == null)
            networkManager.NetworkConfig = new NetworkConfig();

        RegisterNetworkPrefab(networkManager, lootPickupPrefab);
        networkManager.NetworkConfig.NetworkTransport = transport;
        networkManager.NetworkConfig.PlayerPrefab = playerPrefab;
        networkManager.NetworkConfig.EnableSceneManagement = true;
        networkManager.NetworkConfig.ConnectionApproval = false;
        EditorUtility.SetDirty(networkManager);

        EnemyHealth sceneEnemy = Object.FindFirstObjectByType<EnemyHealth>();
        if (sceneEnemy != null)
        {
            ConfigureEnemyLoot(sceneEnemy);
            GetOrAdd<NetworkObject>(sceneEnemy.gameObject);
            GetOrAdd<NetworkEnemyController>(sceneEnemy.gameObject);
            EditorUtility.SetDirty(sceneEnemy.gameObject);
        }

        AddSceneToBuildSettings();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log("Multiplayer prototype setup complete. Enter Play Mode and choose Start Host or Start Client.");
    }

    private static bool HasExpectedLoot(EnemyHealth enemy)
    {
        SerializedObject serializedEnemy = new SerializedObject(enemy);
        SerializedProperty lootTable = serializedEnemy.FindProperty("lootTable");
        if (lootTable == null || lootTable.arraySize != 2)
            return false;

        SerializedProperty sword = lootTable.GetArrayElementAtIndex(0);
        SerializedProperty potion = lootTable.GetArrayElementAtIndex(1);
        return sword.FindPropertyRelative("itemId").intValue == (int)ItemId.KizaruLightSword &&
               sword.FindPropertyRelative("minQuantity").intValue == 1 &&
               sword.FindPropertyRelative("maxQuantity").intValue == 1 &&
               Mathf.Approximately(sword.FindPropertyRelative("dropChance").floatValue, 1f) &&
               potion.FindPropertyRelative("itemId").intValue == (int)ItemId.HealthPotion &&
               potion.FindPropertyRelative("minQuantity").intValue == 2 &&
               potion.FindPropertyRelative("maxQuantity").intValue == 3 &&
               Mathf.Approximately(potion.FindPropertyRelative("dropChance").floatValue, 1f);
    }

    private static void ConfigureEnemyLoot(EnemyHealth enemy)
    {
        SerializedObject serializedEnemy = new SerializedObject(enemy);
        SerializedProperty lootTable = serializedEnemy.FindProperty("lootTable");
        lootTable.arraySize = 2;
        ConfigureLootEntry(lootTable.GetArrayElementAtIndex(0), ItemId.KizaruLightSword, 1, 1, 1f);
        ConfigureLootEntry(lootTable.GetArrayElementAtIndex(1), ItemId.HealthPotion, 2, 3, 1f);
        serializedEnemy.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(enemy);
    }

    private static void ConfigureLootEntry(
        SerializedProperty entry,
        ItemId itemId,
        int minimum,
        int maximum,
        float chance)
    {
        entry.FindPropertyRelative("itemId").intValue = (int)itemId;
        entry.FindPropertyRelative("minQuantity").intValue = minimum;
        entry.FindPropertyRelative("maxQuantity").intValue = maximum;
        entry.FindPropertyRelative("dropChance").floatValue = chance;
    }

    private static GameObject CreateOrUpdateLootPickupPrefab()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LootPickupPrefabPath);
        bool loadedPrefabContents = prefab != null;
        GameObject contents = loadedPrefabContents
            ? PrefabUtility.LoadPrefabContents(LootPickupPrefabPath)
            : new GameObject("NetworkLootPickup");

        GetOrAdd<NetworkObject>(contents);
        CircleCollider2D pickupCollider = GetOrAdd<CircleCollider2D>(contents);
        pickupCollider.isTrigger = true;
        pickupCollider.radius = 1.15f;
        NetworkLootPickup pickup = GetOrAdd<NetworkLootPickup>(contents);
        pickup.ConfigurePickupRadius(0.55f);

        prefab = PrefabUtility.SaveAsPrefabAsset(contents, LootPickupPrefabPath);
        if (loadedPrefabContents)
            PrefabUtility.UnloadPrefabContents(contents);
        else
            Object.DestroyImmediate(contents);

        return prefab;
    }

    private static void RegisterNetworkPrefab(NetworkManager networkManager, GameObject prefab)
    {
        if (prefab == null)
            return;

        NetworkPrefabsList prefabList = AssetDatabase.LoadAssetAtPath<NetworkPrefabsList>(NetworkPrefabsPath);
        if (prefabList == null)
        {
            prefabList = ScriptableObject.CreateInstance<NetworkPrefabsList>();
            AssetDatabase.CreateAsset(prefabList, NetworkPrefabsPath);
        }

        if (!prefabList.Contains(prefab))
        {
            prefabList.Add(new NetworkPrefab
            {
                Override = NetworkPrefabOverride.None,
                Prefab = prefab
            });
            EditorUtility.SetDirty(prefabList);
        }

        if (!networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Contains(prefabList))
            networkManager.NetworkConfig.Prefabs.NetworkPrefabsLists.Add(prefabList);
    }

    private static void AddPlayerNetworkComponents(GameObject player)
    {
        GetOrAdd<NetworkObject>(player);

        NetworkTransform networkTransform = GetOrAdd<NetworkTransform>(player);
        networkTransform.AuthorityMode = NetworkTransform.AuthorityModes.Server;

        NetworkAnimator networkAnimator = GetOrAdd<NetworkAnimator>(player);
        networkAnimator.Animator = player.GetComponent<Animator>();

        GetOrAdd<NetworkPlayerController>(player);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        int separatorIndex = path.LastIndexOf('/');
        string parent = path.Substring(0, separatorIndex);
        string folderName = path.Substring(separatorIndex + 1);
        AssetDatabase.CreateFolder(parent, folderName);
    }

    private static void AddSceneToBuildSettings()
    {
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
        if (scenes.Any(entry => entry.path == ScenePath))
            return;

        EditorBuildSettings.scenes = scenes
            .Concat(new[] { new EditorBuildSettingsScene(ScenePath, true) })
            .ToArray();
    }
}
