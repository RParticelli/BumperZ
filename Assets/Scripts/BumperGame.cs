using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class BumperGame : MonoBehaviour
{
    public static BumperGame Instance { get; private set; }
    readonly List<BumperCarMotor> cars = new();
    BumperCarMotor player;
    string result = "";
    string itemMessage = "";
    float itemMessageUntil;
    float matchStartedAt;
    bool matchRecorded;
    GUIStyle titleStyle;
    GUIStyle infoStyle;
    GUIStyle rivalNameStyle;
    GUIStyle rivalShadowStyle;
    GUIStyle hudCaptionStyle;
    GUIStyle hudValueStyle;
    Texture2D hudLeftPanel;
    Texture2D hudRightPanel;
    Texture2D endPixel;
    GUIStyle endTitleStyle;
    GUIStyle endSubtitleStyle;
    GUIStyle endStatStyle;
    GUIStyle endButtonStyle;
    int playerKills;
    int finalPlacement;
    int finalDuration;
    int earnedCoins;
    int earnedTrophies;
    bool endActionQueued;

    public bool IsMatchFinished => !string.IsNullOrEmpty(result);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartGame()
    {
        if (PlayerPrefs.GetInt("BumperZ.InMatch", 0) == 0) return;
        if (!FindAnyObjectByType<BumperGame>())
            new GameObject("BumperGame").AddComponent<BumperGame>();
    }

    void Awake()
    {
        Instance = this;
        BuildWorld();
        StartMatch();
    }

    void BuildWorld()
    {
        if (!FindAnyObjectByType<BumperMobileInput>())
            new GameObject("MobileInput").AddComponent<BumperMobileInput>();

        RenderSettings.ambientLight = new Color(0.48f, 0.52f, 0.58f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.012f, 0.025f, 0.055f);
        RenderSettings.fogDensity = 0.018f;

        var lightObject = new GameObject("Sun");
        var light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.color = new Color(1f, 0.91f, 0.78f);
        light.shadows = LightShadows.Soft;
        lightObject.transform.rotation = Quaternion.Euler(48f, -32f, 0f);

        BuildAbyssFog();

        var arenaSource = Resources.Load<GameObject>("Models/Arena");
        if (arenaSource)
        {
            var arena = Instantiate(arenaSource);
            arena.name = "Arena_Root";
            arena.transform.position = Vector3.zero;
            Bounds? playableFloorBounds = null;
            foreach (var renderer in arena.GetComponentsInChildren<Renderer>())
            {
                Material[] sourceMaterials = renderer.sharedMaterials;
                var runtimeMaterials = new Material[Mathf.Max(1, sourceMaterials.Length)];
                for (int i = 0; i < runtimeMaterials.Length; i++)
                {
                    string sourceName = sourceMaterials.Length > i && sourceMaterials[i]
                        ? sourceMaterials[i].name : renderer.name;
                    runtimeMaterials[i] = CreateArenaMaterial(renderer.name, sourceName);
                }
                renderer.materials = runtimeMaterials;

                string rendererKey = renderer.name.ToLowerInvariant();
                if (rendererKey.Contains("inferno_floor") ||
                    rendererKey.Contains("plataforma") ||
                    rendererKey.Contains("floorplate"))
                {
                    playableFloorBounds = playableFloorBounds.HasValue
                        ? Encapsulate(playableFloorBounds.Value, renderer.bounds)
                        : renderer.bounds;
                }
            }
            BuildArenaFloorCollider(playableFloorBounds);
            BuildInfernoEffects(arena.transform);
        }
        else
        {
            var arena = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            arena.name = "Arena_Fallback";
            arena.transform.position = new Vector3(0f, -0.25f, 0f);
            arena.transform.localScale = new Vector3(13f, 0.5f, 13f);
        }

        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        // Tight diagonal mobile framing: the complete playable ring remains
        // visible with a small knockout safety margin.
        camera.orthographicSize = 7.8f;
        camera.nearClipPlane = 0.08f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = RenderSettings.fogColor;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<BumperCamera>();
    }

    static Bounds Encapsulate(Bounds current, Bounds other)
    {
        current.Encapsulate(other);
        return current;
    }

    static void BuildArenaFloorCollider(Bounds? detectedBounds)
    {
        Bounds bounds = detectedBounds ?? new Bounds(Vector3.zero, new Vector3(14.5f, 0.2f, 14.5f));
        float radius = Mathf.Max(6.8f, Mathf.Min(bounds.extents.x, bounds.extents.z));
        float height = detectedBounds.HasValue ? bounds.max.y : 0f;
        Vector3 center = new Vector3(bounds.center.x, height, bounds.center.z);

        const int segments = 64;
        var vertices = new Vector3[segments + 1];
        var triangles = new int[segments * 3];
        vertices[0] = Vector3.zero;
        for (int i = 0; i < segments; i++)
        {
            float angle = i * Mathf.PI * 2f / segments;
            vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
            int next = (i + 1) % segments;
            int triangle = i * 3;
            // Reverse the ring order so the collision surface faces upward.
            triangles[triangle] = 0;
            triangles[triangle + 1] = next + 1;
            triangles[triangle + 2] = i + 1;
        }

        var mesh = new Mesh { name = "Arena_Floor_Collision_Mesh" };
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();

        var collisionObject = new GameObject("Arena_Floor_Collision");
        collisionObject.transform.position = center;
        var collider = collisionObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
        Debug.Log($"[BumperZ] Created arena floor collider with radius {radius:F2} at height {height:F2}.");
    }

    static void BuildInfernoEffects(Transform arenaRoot)
    {
        Shader shader = Shader.Find("BumperZ/InfernoFire");
        if (!shader) shader = Shader.Find("Particles/Standard Unlit");
        var fireMaterial = new Material(shader)
        {
            name = "Inferno_Fire_Shared"
        };
        if (fireMaterial.HasProperty("_Intensity")) fireMaterial.SetFloat("_Intensity", 1.4f);

        var effectsRoot = new GameObject("Inferno_Fire_Effects").transform;
        effectsRoot.SetParent(arenaRoot, false);
        for (int i = 0; i < 8; i++)
        {
            float angle = i * Mathf.PI * 0.25f;
            var go = new GameObject($"Inferno_Flame_{i}");
            go.transform.SetParent(effectsRoot, false);
            // Each emitter sits inside one of the eight outer anchor housings,
            // rather than floating over the playable floor.
            go.transform.localPosition = new Vector3(Mathf.Cos(angle) * 8.82f, 0.06f, Mathf.Sin(angle) * 8.82f);

            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.duration = 1.2f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.82f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.65f, 1.35f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.34f, 0.62f);
            main.startRotation = new ParticleSystem.MinMaxCurve(-0.16f, 0.16f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(1f, 0.50f, 0.08f, 0.88f),
                new Color(1f, 0.82f, 0.32f, 0.96f));
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 24;
            main.gravityModifier = -0.12f;

            var emission = ps.emission;
            emission.rateOverTime = 16f;
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 11f;
            shape.radius = 0.18f;

            var noise = ps.noise;
            noise.enabled = true;
            noise.strength = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            noise.frequency = 0.75f;
            noise.scrollSpeed = 0.45f;
            noise.damping = true;

            var color = ps.colorOverLifetime;
            color.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(new Color(1f, .7f, .1f), 0f), new GradientColorKey(new Color(1f, .05f, 0f), 1f) },
                new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(.9f, .12f), new GradientAlphaKey(0f, 1f) });
            color.color = gradient;

            var size = ps.sizeOverLifetime;
            size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, .25f), new Keyframe(.25f, 1f), new Keyframe(1f, .12f)));

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            renderer.material = fireMaterial;
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 1.25f;
            renderer.velocityScale = 0.28f;
            renderer.sortingOrder = 2;
            ps.Play();
        }
    }

    static void BuildAbyssFog()
    {
        Shader shader = Shader.Find("BumperZ/AbyssFog");
        if (!shader) return;

        CreateFogLayer("AbyssFog_Upper", -3.2f, 72f,
            new Color(0.10f, 0.22f, 0.38f, 0.72f), 7.2f, 0.075f, 0.82f, shader, 12f);
        CreateFogLayer("AbyssFog_Lower", -6.8f, 86f,
            new Color(0.035f, 0.09f, 0.19f, 0.86f), 10.5f, -0.045f, 0.90f, shader, -18f);
    }

    static void CreateFogLayer(string name, float height, float size, Color color,
        float scale, float speed, float density, Shader shader, float rotation)
    {
        var layer = GameObject.CreatePrimitive(PrimitiveType.Quad);
        layer.name = name;
        Destroy(layer.GetComponent<MeshCollider>());
        layer.transform.position = new Vector3(0f, height, 0f);
        layer.transform.rotation = Quaternion.Euler(90f, rotation, 0f);
        layer.transform.localScale = Vector3.one * size;
        var material = new Material(shader);
        material.SetColor("_Color", color);
        material.SetFloat("_Scale", scale);
        material.SetFloat("_Speed", speed);
        material.SetFloat("_Density", density);
        var renderer = layer.GetComponent<MeshRenderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    static Material CreateArenaMaterial(string objectName, string sourceName)
    {
        string key = (objectName + " " + sourceName).ToLowerInvariant();
        var material = new Material(Shader.Find("Standard"));
        Color color = new Color(0.075f, 0.085f, 0.10f);
        float metallic = 0.58f;
        float smoothness = 0.30f;
        Color emission = Color.black;

        if (key.Contains("inferno_floor"))
        {
            color = new Color(0.075f, 0.058f, 0.047f);
            metallic = 0.68f;
            smoothness = 0.25f;
        }
        else if (key.Contains("inferno_burntplate"))
        {
            color = new Color(0.13f, 0.082f, 0.052f);
            metallic = 0.72f;
            smoothness = 0.30f;
        }
        else if (key.Contains("inferno_iron"))
        {
            color = new Color(0.17f, 0.105f, 0.065f);
            metallic = 0.86f;
            smoothness = 0.34f;
        }
        else if (key.Contains("inferno_grate"))
        {
            color = new Color(0.022f, 0.018f, 0.016f);
            metallic = 0.58f;
            smoothness = 0.16f;
        }
        else if (key.Contains("inferno_gold"))
        {
            color = new Color(0.62f, 0.27f, 0.035f);
            metallic = 0.88f;
            smoothness = 0.48f;
        }
        else if (key.Contains("plate_b")) color = new Color(0.115f, 0.125f, 0.14f);
        else if (key.Contains("plate_a") || key.Contains("floorplate")) color = new Color(0.065f, 0.075f, 0.09f);
        else if (key.Contains("cyan") || key.Contains("city_light"))
        {
            color = new Color(0.015f, 0.34f, 0.52f);
            emission = new Color(0.01f, 0.65f, 1f) * 2.6f;
            metallic = 0.22f;
            smoothness = 0.72f;
        }
        else if (key.Contains("orange") || key.Contains("borda"))
        {
            color = new Color(0.78f, 0.17f, 0.012f);
            emission = new Color(1f, 0.20f, 0.006f) * 2.5f;
            metallic = 0.38f;
            smoothness = 0.58f;
        }
        else if (key.Contains("cloud_shadow"))
        {
            color = new Color(0.075f, 0.11f, 0.19f);
            metallic = 0f;
            smoothness = 0.04f;
        }
        else if (key.Contains("cloud"))
        {
            color = new Color(0.34f, 0.42f, 0.54f);
            metallic = 0f;
            smoothness = 0.06f;
        }
        else if (key.Contains("city"))
        {
            color = new Color(0.009f, 0.014f, 0.026f);
            metallic = 0.15f;
            smoothness = 0.12f;
        }
        else if (key.Contains("dark") || key.Contains("underside"))
        {
            color = new Color(0.025f, 0.032f, 0.045f);
            metallic = 0.68f;
            smoothness = 0.34f;
        }

        material.color = color;
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Glossiness", smoothness);
        if (emission.maxColorComponent > 0f)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }
        return material;
    }

    void StartMatch()
    {
        Time.timeScale = 1f;
        StopAllCoroutines();
        foreach (var pickup in FindObjectsByType<BumperPickup>(FindObjectsSortMode.None))
            Destroy(pickup.gameObject);
        foreach (var oil in FindObjectsByType<BumperOilSlick>(FindObjectsSortMode.None))
            Destroy(oil.gameObject);
        foreach (var projectile in FindObjectsByType<BumperSpinProjectile>(FindObjectsSortMode.None))
            Destroy(projectile.gameObject);
        foreach (var hurricane in FindObjectsByType<BumperHurricaneProjectile>(FindObjectsSortMode.None))
            Destroy(hurricane.gameObject);
        foreach (var car in cars)
            if (car) Destroy(car.gameObject);
        cars.Clear();
        result = "";
        itemMessage = "";
        matchRecorded = false;
        playerKills = 0;
        finalPlacement = 0;
        finalDuration = 0;
        earnedCoins = 0;
        earnedTrophies = 0;
        endActionQueued = false;
        matchStartedAt = Time.time;

        Color[] colors =
        {
            new(0.85f, 0.08f, 0.04f), new(0.08f, 0.35f, 0.95f), new(0.12f, 0.78f, 0.28f),
            new(0.95f, 0.58f, 0.04f), new(0.62f, 0.12f, 0.88f), new(0.05f, 0.75f, 0.78f)
        };

        const int count = 6;
        for (int i = 0; i < count; i++)
        {
            float angle = i * Mathf.PI * 2f / count;
            Vector3 spawn = new(Mathf.Sin(angle) * 5.25f, 0.42f, Mathf.Cos(angle) * 5.25f);
            var root = new GameObject();
            root.transform.SetPositionAndRotation(spawn, Quaternion.LookRotation(-new Vector3(spawn.x, 0f, spawn.z)));
            var motor = root.AddComponent<BumperCarMotor>();
            motor.Initialize(i == 0, i, colors[i]);
            cars.Add(motor);
            if (i == 0) player = motor;
            else root.AddComponent<BumperBot>();
        }

        StartCoroutine(SpawnPickupAfterDelay(Random.Range(2.5f, 4.5f)));

        var follow = Camera.main.GetComponent<BumperCamera>();
        follow.Target = player.transform;
        Camera.main.transform.position = new Vector3(0f, 12f, -12f);
        Camera.main.transform.rotation = Quaternion.Euler(45f, 0f, 0f);
    }

    public BumperCarMotor ClosestOpponent(BumperCarMotor seeker)
    {
        BumperCarMotor closest = null;
        float best = float.MaxValue;
        foreach (var car in cars)
        {
            if (!car || car == seeker || !car.IsAlive) continue;
            float distance = (car.transform.position - seeker.transform.position).sqrMagnitude;
            if (distance < best) { best = distance; closest = car; }
        }
        return closest;
    }

    public void Eliminate(BumperCarMotor car)
    {
        if (!car || !car.IsAlive) return;
        BumperCarMotor attacker = car.RecentAttacker;
        if (car != player && attacker == player)
            playerKills++;
        car.KnockOut();
        int alive = 0;
        BumperCarMotor survivor = null;
        foreach (var candidate in cars)
            if (candidate && candidate.IsAlive) { alive++; survivor = candidate; }

        if (car == player)
            FinishMatch(false, alive + 1);
        else if (alive == 1 && survivor == player)
            FinishMatch(true, 1);
        else if (alive <= 1)
            FinishMatch(false, Mathf.Max(2, alive + 1));
    }

    void FinishMatch(bool won, int placement)
    {
        if (matchRecorded) return;
        matchRecorded = true;
        result = won ? "VITORIA" : "DERROTA";
        finalPlacement = placement;
        finalDuration = Mathf.Max(0, Mathf.FloorToInt(Time.time - matchStartedAt));
        earnedCoins = won ? 350 : 90;
        earnedTrophies = won ? 30 : -8;
        BumperProfile.RecordMatch(won);
        Time.timeScale = 0f;
    }

    void SpawnPickup()
    {
        if (!Application.isPlaying) return;
        // sqrt keeps the distribution uniform across the whole circular arena.
        float angle = Random.Range(0f, Mathf.PI * 2f);
        float radius = Mathf.Sqrt(Random.value) * 6.5f;
        Vector3 position = new Vector3(Mathf.Cos(angle) * radius, 0.45f, Mathf.Sin(angle) * radius);
        BumperPickup.Create(position);
    }

    public void PickupCollected(BumperPickup pickup, BumperCarMotor collector)
    {
        string item = collector.GiveRandomPowerup();
        if (collector == player)
        {
            itemMessage = item;
            itemMessageUntil = Time.time + 2.2f;
        }
        Destroy(pickup.gameObject);
        StartCoroutine(SpawnPickupAfterDelay(Random.Range(12f, 22f)));
    }

    IEnumerator SpawnPickupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (string.IsNullOrEmpty(result) && !FindAnyObjectByType<BumperPickup>())
            SpawnPickup();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) StartMatch();
    }

    void OnGUI()
    {
        titleStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.67f, 0.12f) }
        };
        infoStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 20, alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        rivalNameStyle ??= new GUIStyle(GUI.skin.label)
        {
            fontSize = 15,
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = new Color(1f, 0.16f, 0.12f, 0.88f) }
        };
        rivalShadowStyle ??= new GUIStyle(rivalNameStyle)
        {
            normal = { textColor = new Color(0f, 0f, 0f, 0.72f) }
        };

        EnsureMatchHud();

        int alive = 0;
        foreach (var car in cars) if (car && car.IsAlive) alive++;

        float hudScale = Mathf.Clamp(Screen.height / 540f, 0.72f, 1.55f);
        float panelWidth = 124f * hudScale;
        float panelHeight = 68f * hudScale;
        float edge = Mathf.Max(6f, Screen.safeArea.xMin + 6f);
        float rightEdge = Mathf.Max(6f, Screen.width - Screen.safeArea.xMax + 6f);
        Rect leftPanel = new Rect(edge, 0f, panelWidth, panelHeight);
        Rect rightPanel = new Rect(Screen.width - panelWidth - rightEdge, 0f, panelWidth, panelHeight);
        GUI.DrawTexture(leftPanel, hudLeftPanel, ScaleMode.StretchToFill, true);
        GUI.DrawTexture(rightPanel, hudRightPanel, ScaleMode.StretchToFill, true);

        hudCaptionStyle.fontSize = Mathf.RoundToInt(11f * hudScale);
        hudValueStyle.fontSize = Mathf.RoundToInt(24f * hudScale);
        GUI.Label(new Rect(leftPanel.x + 9f * hudScale, 5f * hudScale, panelWidth - 22f * hudScale, 18f * hudScale), "TEMPO", hudCaptionStyle);
        int elapsedSeconds = Mathf.Max(0, Mathf.FloorToInt(Time.time - matchStartedAt));
        GUI.Label(new Rect(leftPanel.x + 8f * hudScale, 22f * hudScale, panelWidth - 23f * hudScale, 35f * hudScale),
            $"{elapsedSeconds / 60:00}:{elapsedSeconds % 60:00}", hudValueStyle);

        GUI.Label(new Rect(rightPanel.x + 15f * hudScale, 5f * hudScale, panelWidth - 24f * hudScale, 18f * hudScale), "VIVOS", hudCaptionStyle);
        GUI.Label(new Rect(rightPanel.x + 17f * hudScale, 22f * hudScale, panelWidth - 25f * hudScale, 35f * hudScale),
            $"{alive}/{cars.Count}", hudValueStyle);
        if (Time.time < itemMessageUntil)
            GUI.Label(new Rect(0, 100, Screen.width, 40), "ITEM: " + itemMessage, titleStyle);
        if (player && player.HasPower)
            GUI.Label(new Rect(Screen.width - 305, Screen.height - 210, 270, 34), player.HeldPowerName + " DISPONIVEL", infoStyle);
        if (!string.IsNullOrEmpty(result))
        {
            DrawEndScreen();
            return;
        }

        // Rival labels use lightweight world-space billboards.
    }

    void DrawEndScreen()
    {
        EnsureEndScreenStyles();
        float scale = Mathf.Min(Screen.width / 960f, Screen.height / 540f);
        float originX = (Screen.width - 960f * scale) * 0.5f;
        float originY = (Screen.height - 540f * scale) * 0.5f;
        Rect R(float x, float y, float w, float h) =>
            new(originX + x * scale, originY + y * scale, w * scale, h * scale);

        GUI.color = new Color(0f, 0.015f, 0.035f, 0.84f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), endPixel);
        GUI.color = Color.white;

        Color accent = result == "VITORIA"
            ? new Color(1f, 0.67f, 0.08f)
            : new Color(1f, 0.18f, 0.10f);
        DrawEndBox(R(205, 58, 550, 424), new Color(0.018f, 0.035f, 0.06f, 0.98f), accent, 4f * scale);
        DrawEndBox(R(225, 78, 510, 74), new Color(0.025f, 0.07f, 0.12f, 0.98f), accent, 2f * scale);

        endTitleStyle.fontSize = Mathf.RoundToInt(42f * scale);
        endTitleStyle.normal.textColor = accent;
        GUI.Label(R(235, 82, 490, 52), result, endTitleStyle);

        endSubtitleStyle.fontSize = Mathf.RoundToInt(15f * scale);
        GUI.Label(R(235, 130, 490, 22), result == "VITORIA" ? "ULTIMO PILOTO NA ARENA" : $"VOCE TERMINOU EM #{finalPlacement}", endSubtitleStyle);

        DrawEndStat(R, 230, "COLOCACAO", $"#{finalPlacement}", accent);
        DrawEndStat(R, 365, "ELIMINACOES", playerKills.ToString(), new Color(0.05f, 0.78f, 1f));
        DrawEndStat(R, 500, "TEMPO", $"{finalDuration / 60:00}:{finalDuration % 60:00}", Color.white);
        DrawEndStat(R, 635, "MOEDAS", $"+{earnedCoins}", new Color(1f, 0.70f, 0.08f));

        endSubtitleStyle.fontSize = Mathf.RoundToInt(13f * scale);
        endSubtitleStyle.normal.textColor = earnedTrophies >= 0 ? new Color(0.25f, 1f, 0.55f) : new Color(1f, 0.35f, 0.25f);
        GUI.Label(R(255, 300, 450, 30), $"TROFEUS  {(earnedTrophies >= 0 ? "+" : "")}{earnedTrophies}", endSubtitleStyle);

        endButtonStyle.fontSize = Mathf.RoundToInt(17f * scale);
        endButtonStyle.normal.textColor = Color.white;
        DrawEndBox(R(245, 352, 220, 62), new Color(0.02f, 0.30f, 0.52f, 0.98f), new Color(0.05f, 0.80f, 1f), 2f * scale);
        if (!endActionQueued && GUI.Button(R(245, 352, 220, 62), "JOGAR NOVAMENTE", endButtonStyle))
        {
            endActionQueued = true;
            StartCoroutine(RestartMatch());
        }

        DrawEndBox(R(495, 352, 220, 62), new Color(0.12f, 0.13f, 0.16f, 0.98f), new Color(0.65f, 0.68f, 0.74f), 2f * scale);
        if (!endActionQueued && GUI.Button(R(495, 352, 220, 62), "VOLTAR AO MENU", endButtonStyle))
        {
            endActionQueued = true;
            ReturnToMenu();
        }

        endSubtitleStyle.normal.textColor = new Color(0.64f, 0.69f, 0.76f);
        endSubtitleStyle.fontSize = Mathf.RoundToInt(11f * scale);
        GUI.Label(R(260, 435, 440, 22), "RESULTADO SALVO NO SEU PERFIL", endSubtitleStyle);
    }

    void DrawEndStat(System.Func<float, float, float, float, Rect> R, float x, string caption, string value, Color color)
    {
        DrawEndBox(R(x, 178, 125, 104), new Color(0.025f, 0.055f, 0.09f, 0.96f), new Color(color.r, color.g, color.b, 0.70f), 1.5f);
        endSubtitleStyle.fontSize = Mathf.RoundToInt(11f * Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        endSubtitleStyle.normal.textColor = new Color(0.68f, 0.73f, 0.80f);
        GUI.Label(R(x + 5, 186, 115, 25), caption, endSubtitleStyle);
        endStatStyle.fontSize = Mathf.RoundToInt(29f * Mathf.Min(Screen.width / 960f, Screen.height / 540f));
        endStatStyle.normal.textColor = color;
        GUI.Label(R(x + 5, 211, 115, 55), value, endStatStyle);
    }

    void EnsureEndScreenStyles()
    {
        if (!endPixel)
        {
            endPixel = new Texture2D(1, 1, TextureFormat.RGBA32, false) { name = "EndScreenPixel" };
            endPixel.SetPixel(0, 0, Color.white);
            endPixel.Apply(false, true);
        }
        endTitleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        endSubtitleStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        endStatStyle ??= new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        endButtonStyle ??= new GUIStyle(GUI.skin.button) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
    }

    void DrawEndBox(Rect rect, Color fill, Color edge, float border)
    {
        GUI.color = edge;
        GUI.DrawTexture(rect, endPixel);
        GUI.color = fill;
        GUI.DrawTexture(new Rect(rect.x + border, rect.y + border, rect.width - border * 2f, rect.height - border * 2f), endPixel);
        GUI.color = Color.white;
    }

    IEnumerator RestartMatch()
    {
        Time.timeScale = 1f;
        yield return null;
        StartMatch();
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("BumperZ.InMatch", 0);
        PlayerPrefs.Save();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    void EnsureMatchHud()
    {
        hudCaptionStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.78f, 0.80f, 0.82f) }
        };
        hudValueStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        if (!hudLeftPanel) hudLeftPanel = CreateHudPanelTexture(false);
        if (!hudRightPanel) hudRightPanel = CreateHudPanelTexture(true);
    }

    static Texture2D CreateHudPanelTexture(bool mirror)
    {
        const int width = 248;
        const int height = 136;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = mirror ? "RuntimeHudPanelRight" : "RuntimeHudPanelLeft",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        var pixels = new Color32[width * height];
        Color32 clear = new(0, 0, 0, 0);
        Color32 shadow = new(0, 0, 0, 118);
        Color32 panel = new(7, 10, 13, 232);
        Color32 inner = new(18, 21, 24, 246);
        Color32 accent = new(255, 181, 0, 255);

        for (int y = 0; y < height; y++)
        for (int x = 0; x < width; x++)
        {
            int px = mirror ? width - 1 - x : x;
            // Texture pixels use a bottom-left origin: taper the lower outside
            // corner so the panel keeps the same downward diagonal as the reference.
            int diagonal = Mathf.Max(0, 58 - y) * 2;
            bool inside = px < width - diagonal;
            Color32 color = clear;
            if (inside && y >= 6) color = panel;
            if (inside && px >= 8 && y >= 13 && y < height - 7) color = inner;
            if (inside && y >= 8 && y < 15 && px > 15 && px < width - diagonal - 5) color = accent;
            if (!inside && px < width - diagonal + 8 && y < 62) color = shadow;
            pixels[y * width + x] = color;
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, true);
        return texture;
    }

    void DrawRivalNames()
    {
        Camera camera = Camera.main;
        if (!camera) return;
        foreach (var car in cars)
        {
            if (!car || car == player || !car.IsAlive) continue;
            float height = car.IsGiant ? 1.28f : 0.72f;
            Vector3 point = camera.WorldToScreenPoint(car.transform.position + Vector3.up * height);
            if (point.z <= 0f || point.x < -60f || point.x > Screen.width + 60f ||
                point.y < -20f || point.y > Screen.height + 20f) continue;
            float guiY = Screen.height - point.y;
            Rect rect = new Rect(point.x - 55f, guiY - 11f, 110f, 22f);
            GUI.Label(new Rect(rect.x + 1f, rect.y + 1f, rect.width, rect.height), car.DriverName, rivalShadowStyle);
            GUI.Label(rect, car.DriverName, rivalNameStyle);
        }
    }
}
