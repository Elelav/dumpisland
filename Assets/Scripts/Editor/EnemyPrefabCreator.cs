#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class EnemyPrefabCreator : EditorWindow
{
    [SerializeField] private Sprite enemySprite;
    [SerializeField] private string enemyName = "NewEnemy";

    private enum EnemyType { Small, Medium, Large, Elite, Boss }
    private EnemyType selectedType = EnemyType.Medium;

    private enum CreationMode { Template, Manual }
    private CreationMode creationMode = CreationMode.Template;

    private GameObject templatePrefab = null;

    // Настройки для Manual Mode
    private static readonly EnemyConfig[] configs = new EnemyConfig[]
    {
        new EnemyConfig {
            type = "Small",
            scale = 0.8f,
            hp = 50,
            damage = 10,
            speed = 3.0f,
            attackCooldown = 1f,
            lootSize = 1,
            expReward = 10f
        },
        new EnemyConfig {
            type = "Medium",
            scale = 2.0f,
            hp = 100,
            damage = 15,
            speed = 2.5f,
            attackCooldown = 1f,
            lootSize = 2,
            expReward = 20f
        },
        new EnemyConfig {
            type = "Large",
            scale = 3.0f,
            hp = 200,
            damage = 25,
            speed = 2.0f,
            attackCooldown = 1.2f,
            lootSize = 3,
            expReward = 40f
        },
        new EnemyConfig {
            type = "Elite",
            scale = 1.5f,
            hp = 300,
            damage = 30,
            speed = 2.8f,
            attackCooldown = 1f,
            lootSize = 3,
            expReward = 60f
        },
        new EnemyConfig {
            type = "Boss",
            scale = 5.0f,
            hp = 1000,
            damage = 50,
            speed = 1.5f,
            attackCooldown = 1.5f,
            lootSize = 5,
            expReward = 200f
        }
    };

    [MenuItem("Tools/Enemy Prefab Creator")]
    public static void ShowWindow()
    {
        GetWindow<EnemyPrefabCreator>("Enemy Creator");
    }

    void OnGUI()
    {
        GUILayout.Label("🗑️ Enemy Prefab Creator", EditorStyles.boldLabel);
        GUILayout.Space(10);

        // РЕЖИМ РАБОТЫ
        EditorGUILayout.LabelField("--- Creation Mode ---", EditorStyles.boldLabel);
        creationMode = (CreationMode)EditorGUILayout.EnumPopup("Mode", creationMode);

        EditorGUILayout.HelpBox(
            creationMode == CreationMode.Template
                ? "📋 Template Mode: Copy all settings from existing prefab, only change sprite & name"
                : "✍️ Manual Mode: Create from scratch with predefined settings",
            MessageType.Info
        );

        GUILayout.Space(10);

        // ОБЩИЕ ПОЛЯ
        enemySprite = (Sprite)EditorGUILayout.ObjectField("Enemy Sprite", enemySprite, typeof(Sprite), false);
        enemyName = EditorGUILayout.TextField("Enemy Name", enemyName);

        GUILayout.Space(10);

        // РЕЖИМ: TEMPLATE
        if (creationMode == CreationMode.Template)
        {
            EditorGUILayout.LabelField("--- Template Settings ---", EditorStyles.boldLabel);

            templatePrefab = (GameObject)EditorGUILayout.ObjectField(
                "Template Prefab",
                templatePrefab,
                typeof(GameObject),
                false
            );

            if (GUILayout.Button("🔍 Auto-Find Template"))
            {
                AutoFindTemplate();
            }

            if (templatePrefab != null)
            {
                EditorGUILayout.HelpBox($"✅ Using: {templatePrefab.name}", MessageType.Info);
            }
        }
        // РЕЖИМ: MANUAL
        else
        {
            EditorGUILayout.LabelField("--- Manual Settings ---", EditorStyles.boldLabel);
            selectedType = (EnemyType)EditorGUILayout.EnumPopup("Enemy Type", selectedType);

            GUILayout.Space(5);

            EnemyConfig config = configs[(int)selectedType];
            EditorGUILayout.LabelField("Parameters:", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"  Scale: {config.scale}");
            EditorGUILayout.LabelField($"  HP: {config.hp}");
            EditorGUILayout.LabelField($"  Damage: {config.damage}");
            EditorGUILayout.LabelField($"  Speed: {config.speed}");
            EditorGUILayout.LabelField($"  Attack Cooldown: {config.attackCooldown}s");
            EditorGUILayout.LabelField($"  Loot Size: {config.lootSize}");
            EditorGUILayout.LabelField($"  EXP Reward: {config.expReward}");
        }

        GUILayout.Space(20);

        // КНОПКА СОЗДАНИЯ
        bool canCreate = enemySprite != null && !string.IsNullOrEmpty(enemyName);
        if (creationMode == CreationMode.Template)
        {
            canCreate &= templatePrefab != null;
        }

        GUI.enabled = canCreate;

        Color originalColor = GUI.backgroundColor;
        GUI.backgroundColor = Color.green;

        if (GUILayout.Button("✨ Create Enemy Prefab", GUILayout.Height(50)))
        {
            if (creationMode == CreationMode.Template)
            {
                CreateFromTemplate();
            }
            else
            {
                CreateFromScratch();
            }
        }

        GUI.backgroundColor = originalColor;
        GUI.enabled = true;

        GUILayout.Space(10);

        // КНОПКИ НАВИГАЦИИ
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("📂 Open Enemies Folder"))
        {
            EnsureFolderExists("Assets/Prefabs/Enemies");
            EditorUtility.RevealInFinder("Assets/Prefabs/Enemies/");
        }

        if (GUILayout.Button("🎨 Open Sprites Folder"))
        {
            EnsureFolderExists("Assets/Sprites/Enemies");
            EditorUtility.RevealInFinder("Assets/Sprites/Enemies/");
        }

        EditorGUILayout.EndHorizontal();
    }

    // ========================================
    // TEMPLATE MODE
    // ========================================

    private void AutoFindTemplate()
    {
        string[] prefabPaths = new string[]
        {
            "Assets/Prefabs/Enemy.prefab",
            "Assets/Prefabs/EnemyMedium.prefab",
            "Assets/Prefabs/EnemySmall.prefab",
            "Assets/Prefabs/EnemyLarge.prefab",
            "Assets/Prefabs/EnemyElite.prefab",
            "Assets/Prefabs/Boss.prefab"
        };

        foreach (string path in prefabPaths)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab != null)
            {
                templatePrefab = prefab;
                DebugHelper.Log($"✅ Found template: {path}");
                return;
            }
        }

        DebugHelper.LogWarning("⚠️ No template prefabs found! Create one manually.");
    }

    private void CreateFromTemplate()
    {
        // Создаём копию шаблона
        GameObject enemy = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;

        if (enemy == null)
        {
            DebugHelper.LogError("❌ Failed to instantiate template!");
            return;
        }

        // Меняем имя
        enemy.name = enemyName;

        // Меняем спрайт
        SpriteRenderer sr = enemy.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = enemySprite;
        }

        // Создаём лут и привязываем
        GameObject lootPrefab = CreateLootPrefab(enemyName, enemySprite);

        EnemyHealth health = enemy.GetComponent<EnemyHealth>();
        if (health != null && lootPrefab != null)
        {
            SerializedObject so = new SerializedObject(health);
            so.FindProperty("useAutoScale").boolValue = false;
            so.FindProperty("lootPrefab").objectReferenceValue = lootPrefab;
            so.ApplyModifiedProperties();
        }

        // Сохраняем префаб
        SavePrefab(enemy);
    }

    // ========================================
    // MANUAL MODE
    // ========================================

    private void CreateFromScratch()
    {
        EnemyConfig config = configs[(int)selectedType];

        // Создаём GameObject
        GameObject enemy = new GameObject(enemyName);

        // SpriteRenderer
        SpriteRenderer sr = enemy.AddComponent<SpriteRenderer>();
        sr.sprite = enemySprite;
        sr.sortingLayerName = "Enemies";

        // CircleCollider2D
        CircleCollider2D collider = enemy.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;

        // Rigidbody2D
        Rigidbody2D rb = enemy.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        // EnemyAI
        EnemyAI ai = enemy.AddComponent<EnemyAI>();
        SetField(ai, "moveSpeed", config.speed);
        SetField(ai, "damage", config.damage);
        SetField(ai, "attackCooldown", config.attackCooldown);

        // Создаём лут
        GameObject lootPrefab = CreateLootPrefab(enemyName, enemySprite, config.lootSize);

        // EnemyHealth
        EnemyHealth health = enemy.AddComponent<EnemyHealth>();
        SetField(health, "maxHealth", config.hp);
        SetField(health, "lootPrefab", lootPrefab);
        SetField(health, "lootSize", config.lootSize);
        SetField(health, "useAutoScale", false);

        // Конвертируем EnemyType string в enum
        System.Type enumType = typeof(EnemyHealth).GetNestedType("EnemyType");
        if (enumType != null)
        {
            object enumValue = System.Enum.Parse(enumType, config.type);
            SetField(health, "enemyType", enumValue);
        }

        SetField(health, "expReward", config.expReward);

        // Elite Ability
        if (selectedType == EnemyType.Elite)
        {
            enemy.AddComponent<EliteAbility>();
        }

        // Boss Abilities
        if (selectedType == EnemyType.Boss)
        {
            enemy.AddComponent<BossAbilities>();
        }

        // Scale
        enemy.transform.localScale = new Vector3(config.scale, config.scale, 1f);

        // Tag и Layer
        enemy.tag = "Enemy";
        enemy.layer = LayerMask.NameToLayer("Default");

        // Сохраняем префаб
        SavePrefab(enemy);
    }

    // ========================================
    // СОЗДАНИЕ ЛУТА (МИНИ-ВЕРСИЯ ВРАГА)
    // ========================================

    private GameObject CreateLootPrefab(string enemyName, Sprite enemySprite, int lootSize = 2)
    {
        // Создаём root объект лута
        GameObject loot = new GameObject($"Loot_{enemyName}");

        // === ОБВОДКА (черный спрайт позади) ===
        GameObject outline = new GameObject("Outline");
        outline.transform.SetParent(loot.transform);
        outline.transform.localPosition = Vector3.zero;

        SpriteRenderer outlineSR = outline.AddComponent<SpriteRenderer>();
        outlineSR.sprite = enemySprite;
        outlineSR.color = Color.black;
        outlineSR.sortingLayerName = "Items";
        outlineSR.sortingOrder = -1; // Позади основного спрайта
        outline.transform.localScale = Vector3.one * 1.15f; // На 15% больше

        // === ОСНОВНОЙ СПРАЙТ ===
        GameObject spriteObj = new GameObject("Sprite");
        spriteObj.transform.SetParent(loot.transform);
        spriteObj.transform.localPosition = Vector3.zero;

        SpriteRenderer sr = spriteObj.AddComponent<SpriteRenderer>();
        sr.sprite = enemySprite;
        sr.sortingLayerName = "Items";
        sr.color = Color.white; // Оригинальный цвет врага
        sr.sortingOrder = 0;

        // === COLLIDER ===
        CircleCollider2D col = loot.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.3f;

        // === LOOT SCRIPT ===
        Loot lootScript = loot.AddComponent<Loot>();
        SetField(lootScript, "size", lootSize);
        SetField(lootScript, "bobSpeed", 2f);
        SetField(lootScript, "bobHeight", 0.1f);
        SetField(lootScript, "magnetSpeed", 5f);

        // Размер лута - мини-версия (30% от оригинала)
        loot.transform.localScale = Vector3.one * 0.3f;

        // Сохраняем
        EnsureFolderExists("Assets/Prefabs/Enemies");
        string lootPath = $"Assets/Prefabs/Enemies/Loot_{enemyName}.prefab";

        if (AssetDatabase.LoadAssetAtPath<GameObject>(lootPath) != null)
        {
            AssetDatabase.DeleteAsset(lootPath);
        }

        GameObject savedLoot = PrefabUtility.SaveAsPrefabAsset(loot, lootPath);
        DestroyImmediate(loot);

        DebugHelper.Log($"✅ Loot prefab created: {lootPath}");

        return savedLoot;
    }

    // ========================================
    // УТИЛИТЫ
    // ========================================

    private void SavePrefab(GameObject enemy)
    {
        EnsureFolderExists("Assets/Prefabs/Enemies");
        string path = $"Assets/Prefabs/Enemies/{enemyName}.prefab";

        // Проверяем существование
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
        {
            if (!EditorUtility.DisplayDialog("Prefab Exists",
                $"Prefab '{enemyName}' already exists. Overwrite?", "Yes", "No"))
            {
                DestroyImmediate(enemy);
                return;
            }

            AssetDatabase.DeleteAsset(path);
        }

        // Создаём префаб
        PrefabUtility.SaveAsPrefabAsset(enemy, path);
        DestroyImmediate(enemy);

        // Пингуем в Project
        GameObject createdPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        EditorGUIUtility.PingObject(createdPrefab);

        DebugHelper.Log($"✅ Enemy prefab created: {path}");

        // Очищаем поля
        enemyName = "NewEnemy";
        enemySprite = null;
    }

    private void EnsureFolderExists(string path)
    {
        string[] folders = path.Split('/');
        string currentPath = folders[0];

        for (int i = 1; i < folders.Length; i++)
        {
            string nextPath = currentPath + "/" + folders[i];

            if (!AssetDatabase.IsValidFolder(nextPath))
            {
                AssetDatabase.CreateFolder(currentPath, folders[i]);
                DebugHelper.Log($"📁 Created folder: {nextPath}");
            }

            currentPath = nextPath;
        }

        AssetDatabase.Refresh();
    }

    private void SetField(Component component, string fieldName, object value)
    {
        SerializedObject so = new SerializedObject(component);
        SerializedProperty prop = so.FindProperty(fieldName);

        if (prop != null)
        {
            if (value is float f)
                prop.floatValue = f;
            else if (value is int i)
                prop.intValue = i;
            else if (value is bool b)
                prop.boolValue = b;
            else if (value is string s)
                prop.stringValue = s;
            else if (value is Object obj)
                prop.objectReferenceValue = obj;
            else if (value is System.Enum)
                prop.enumValueIndex = (int)value;

            so.ApplyModifiedProperties();
        }
        else
        {
            DebugHelper.LogWarning($"⚠️ Field '{fieldName}' not found in {component.GetType().Name}");
        }
    }

    // Структура конфигурации
    private class EnemyConfig
    {
        public string type;
        public float scale;
        public float hp;
        public float damage;
        public float speed;
        public float attackCooldown;
        public int lootSize;
        public float expReward;
    }
}
#endif