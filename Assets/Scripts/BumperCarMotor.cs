using UnityEngine;

public sealed class BumperCarMotor : MonoBehaviour
{
    public static BumperCarMotor LocalPlayer { get; private set; }
    public bool IsPlayer;
    public bool IsAlive { get; private set; } = true;
    public int DriverIndex;
    public string DriverName { get; private set; }

    Rigidbody body;
    float throttle;
    float steering;
    Transform visual;
    float impactLockUntil;
    float boostUntil;
    float shieldUntil;
    float powerImpactUntil;
    float oilSlipUntil;
    int heldPower; // 0 none, 1 oil, 2 spin shot, 3 shield, 4 giant, 5 hurricane
    float oilDriftSign = 1f;
    float shotSpinUntil;
    float shotSpinSign = 1f;
    bool shotSpinWasActive;
    float giantUntil;
    float hurricaneUntil;
    Vector3 hurricaneCenter;
    Vector3 hurricaneEjectDirection;
    float hurricaneSign = 1f;
    bool hurricaneWasActive;
    TrailRenderer[] oilTrails;
    LineRenderer[] energyTrails;
    float nextOilMarkAt;

    const float Acceleration = 16f;
    const float ReverseAcceleration = 15f;
    const float BrakingAcceleration = 30f;
    const float MaxSpeed = 8.5f;
    const float TurnSpeed = 105f;

    public Rigidbody Body => body;
    public bool HasPower => heldPower != 0;
    public int HeldPowerId => heldPower;
    public string HeldPowerName => heldPower == 1 ? "OLEO" : heldPower == 2 ? "TIRO DE GIRO" : heldPower == 3 ? "ESCUDO" : heldPower == 4 ? "GIGANTE" : heldPower == 5 ? "FURACAO" : "";
    public bool IsSpinShotActive => Time.time < shotSpinUntil;
    public bool IsShieldActive => Time.time < shieldUntil;
    public bool IsGiant => Time.time < giantUntil;
    public bool IsHurricaneActive => Time.time < hurricaneUntil;

    public void Initialize(bool player, int index, Color color)
    {
        IsPlayer = player;
        if (player) LocalPlayer = this;
        DriverIndex = index;
        string[] botNames = { "PLAYER", "VEX", "NOVA", "REX", "KIRO", "BLAZE" };
        DriverName = index >= 0 && index < botNames.Length ? botNames[index] : $"RIVAL {index}";
        if (!player) BumperRivalNameplate.Create(this, color);
        gameObject.name = player ? "PlayerCar" : $"BotCar_{index}";

        body = gameObject.AddComponent<Rigidbody>();
        body.mass = 110f;
        body.linearDamping = 0.8f;
        body.angularDamping = 4.5f;
        body.centerOfMass = new Vector3(0f, 0.08f, 0f);
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        var collider = gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 0.19f, 0f);
        collider.size = new Vector3(0.86f, 0.38f, 1.02f);
        var bumperMaterial = new PhysicsMaterial("BumperCar_Rubber")
        {
            dynamicFriction = 0.05f,
            staticFriction = 0.05f,
            bounciness = 0.44f,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            bounceCombine = PhysicsMaterialCombine.Maximum
        };
        collider.material = bumperMaterial;
        BuildOilTrails();
        BuildEnergyTrails(color);

        var source = Resources.Load<GameObject>("Models/BumperCar");
        if (source)
        {
            visual = Instantiate(source, transform).transform;
            visual.name = "Visual";
            visual.localPosition = Vector3.zero;
            visual.localRotation = Quaternion.identity;
            visual.localScale = Vector3.one;
            ApplyAppearance(visual, color);
        }
        else
        {
            var fallback = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(fallback.GetComponent<Collider>());
            fallback.transform.SetParent(transform, false);
            fallback.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            fallback.transform.localScale = new Vector3(0.85f, 0.35f, 1f);
            fallback.GetComponent<Renderer>().material.color = color;
            visual = fallback.transform;
        }
    }

    void ApplyAppearance(Transform root, Color tint)
    {
        Texture texture = Resources.Load<Texture>("Textures/BumperCar_BaseColor");
        foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
        {
            var material = new Material(Shader.Find("Standard"));
            material.name = IsPlayer ? "PlayerPaint" : $"BotPaint_{DriverIndex}";
            material.color = Color.Lerp(tint, Color.white, 0.28f);
            material.mainTexture = texture;
            material.SetFloat("_Metallic", 0.15f);
            material.SetFloat("_Glossiness", 0.42f);
            renderer.material = material;
        }
    }

    public void SetInput(float drive, float turn)
    {
        throttle = Mathf.Clamp(drive, -1f, 1f);
        steering = Mathf.Clamp(turn, -1f, 1f);
    }

    void Update()
    {
        if (!IsAlive) return;
        float desiredScale = IsGiant ? 1.72f : 1f;
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one * desiredScale, Time.deltaTime * 5.5f);
        if (body) body.mass = IsGiant ? 185f : 110f;
        if (IsPlayer)
        {
            if (BumperMobileInput.IsDriving)
                SetInput(BumperMobileInput.Value.y, BumperMobileInput.Value.x);
            else
                SetInput(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal"));

            bool usePower = Input.GetKeyDown(KeyCode.Space) || BumperMobileInput.ConsumePower();
            if (usePower)
                UsePower();
        }
        UpdateEnergyJets(IsAlive && body && body.linearVelocity.sqrMagnitude > 0.75f);
    }

    void FixedUpdate()
    {
        if (!IsAlive || !body) return;

        bool slipping = Time.time < oilSlipUntil;
        bool shotSpinning = Time.time < shotSpinUntil;
        bool hurricaneActive = Time.time < hurricaneUntil;
        if (!shotSpinning && shotSpinWasActive)
        {
            body.angularVelocity = new Vector3(0f, 0f, 0f);
            shotSpinWasActive = false;
        }
        if (!hurricaneActive && hurricaneWasActive)
        {
            hurricaneWasActive = false;
            body.angularVelocity = Vector3.zero;
            body.AddForce(hurricaneEjectDirection * 3.4f + Vector3.up * 0.08f, ForceMode.VelocityChange);
        }
        SetOilTrails(false);
        if (slipping && Time.time >= nextOilMarkAt)
        {
            nextOilMarkAt = Time.time + 0.075f;
            SpawnOilTrackMarks();
        }

        // Let the collision throw the car clear before engine force returns.
        if (Time.time < impactLockUntil && !hurricaneActive)
            return;

        if (hurricaneActive)
        {
            Vector3 radial = transform.position - hurricaneCenter;
            radial.y = 0f;
            if (radial.sqrMagnitude < 0.04f) radial = transform.right * 0.72f;
            radial.Normalize();
            Vector3 tangent = Vector3.Cross(Vector3.up, radial) * hurricaneSign;
            Vector3 desiredPosition = hurricaneCenter + radial * 0.78f;
            Vector3 correction = desiredPosition - transform.position;
            correction.y = 0f;
            body.linearVelocity = tangent * 4.1f + correction * 5.5f + Vector3.up * body.linearVelocity.y;
            body.angularVelocity = new Vector3(0f, hurricaneSign * 6.4f, 0f);
            if (transform.position.y < -2f || new Vector2(transform.position.x, transform.position.z).magnitude > 10.5f)
                BumperGame.Instance.Eliminate(this);
            return;
        }

        if (shotSpinning)
        {
            // A spin-shot is a real stun: throttle, reverse and steering are
            // ignored until the rotation finishes. External collision momentum
            // remains valid, so another car can still knock the victim away.
            body.angularDamping = 0.25f;
            if (transform.position.y < -2f || new Vector2(transform.position.x, transform.position.z).magnitude > 10.5f)
                BumperGame.Instance.Eliminate(this);
            return;
        }

        Vector3 flatVelocity = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up);
        float forwardSpeed = Vector3.Dot(flatVelocity, transform.forward);
        float giantSpeed = IsGiant ? 0.62f : 1f;
        float boost = (Time.time < boostUntil ? 1.45f : 1f) * giantSpeed;
        float power = (throttle >= 0f ? Acceleration : ReverseAcceleration) * boost;
        bool changingDirection = Mathf.Abs(throttle) > 0.05f && Mathf.Abs(forwardSpeed) > 0.15f &&
                                 Mathf.Sign(throttle) != Mathf.Sign(forwardSpeed);
        if (changingDirection)
        {
            // Strong arcade brake before engaging the opposite direction.
            body.AddForce(-transform.forward * Mathf.Sign(forwardSpeed) * BrakingAcceleration,
                ForceMode.Acceleration);
        }
        else if (Mathf.Abs(forwardSpeed) < MaxSpeed)
        {
            body.AddForce(transform.forward * (throttle * power), ForceMode.Acceleration);
        }

        float speedFactor = Mathf.Clamp01(flatVelocity.magnitude / 1.4f);
        float direction = forwardSpeed < -0.1f ? -1f : 1f;
        float steeringGrip = slipping ? 0.28f : shotSpinning ? 0.12f : 1f;
        Quaternion turn = Quaternion.Euler(0f, steering * TurnSpeed * steeringGrip * speedFactor * direction * Time.fixedDeltaTime, 0f);
        body.angularDamping = 4.5f;
        body.MoveRotation(body.rotation * turn);

        if (slipping)
        {
            // Oil preserves the existing momentum but destabilizes yaw. There
            // is no artificial sideways force: the sideways motion now comes
            // naturally from inertia while the car spins.
            float wobble = Mathf.Sin(Time.time * 7.5f + DriverIndex * 1.7f);
            body.AddTorque(Vector3.up * (oilDriftSign * 3.6f + wobble * 1.35f), ForceMode.Acceleration);
        }

        Vector3 local = transform.InverseTransformDirection(flatVelocity);
        local.x *= slipping ? 0.995f : 0.88f;
        Vector3 corrected = transform.TransformDirection(local);
        body.linearVelocity = new Vector3(corrected.x, body.linearVelocity.y, corrected.z);

        float currentMaxSpeed = MaxSpeed * (Time.time < boostUntil ? 1.32f : 1f) * (IsGiant ? 0.58f : 1f);
        if (flatVelocity.magnitude > currentMaxSpeed)
        {
            Vector3 limited = flatVelocity.normalized * currentMaxSpeed;
            body.linearVelocity = new Vector3(limited.x, body.linearVelocity.y, limited.z);
        }

        if (transform.position.y < -2f || new Vector2(transform.position.x, transform.position.z).magnitude > 10.5f)
            BumperGame.Instance.Eliminate(this);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!IsAlive) return;
        var other = collision.rigidbody ? collision.rigidbody.GetComponent<BumperCarMotor>() : null;
        if (!other || !other.IsAlive) return;

        // A car-to-car hit interrupts a spin-shot. Control returns 0.3s
        // after this collision, rather than waiting for the original duration.
        if (Time.time < shotSpinUntil)
            shotSpinUntil = Time.time + 0.3f;
        if (Time.time < other.shotSpinUntil)
            other.shotSpinUntil = Time.time + 0.3f;

        // Unity invokes this callback on both cars. The ID guard resolves the
        // pair once, avoiding a doubled or asymmetric impulse.
        if (DriverIndex > other.DriverIndex) return;

        Vector3 separation = other.transform.position - transform.position;
        separation.y = 0f;
        if (separation.sqrMagnitude < 0.001f)
            separation = transform.right;
        separation.Normalize();

        float closingSpeed = Mathf.Max(0f, Vector3.Dot(body.linearVelocity - other.body.linearVelocity, separation));
        float knockback = Mathf.Clamp(2.9f + closingSpeed * 0.32f, 2.9f, 5.4f);

        float thisAttack = Vector3.Dot(body.linearVelocity, separation);
        float otherAttack = Vector3.Dot(other.body.linearVelocity, -separation);
        bool clearAttacker = Mathf.Max(thisAttack, otherAttack) > 0.75f &&
                             Mathf.Abs(thisAttack - otherAttack) > 0.45f;

        if (clearAttacker)
        {
            BumperCarMotor attacker = thisAttack > otherAttack ? this : other;
            BumperCarMotor victim = attacker == this ? other : this;
            Vector3 pushDirection = attacker == this ? separation : -separation;
            Vector3 victimToAttacker = -pushDirection;

            float rearDot = Vector3.Dot(victim.transform.forward, victimToAttacker);
            float sideDot = Mathf.Abs(Vector3.Dot(victim.transform.right, victimToAttacker));
            float positionalBonus = rearDot < -0.42f ? 1.68f : sideDot > 0.52f ? 1.46f : 1f;
            float attackPower = Time.time < attacker.powerImpactUntil ? 1.55f : 1f;
            if (attacker.IsGiant) attackPower *= 1.18f;
            float shieldProtection = Time.time < victim.shieldUntil ? 0.30f : 1f;
            float victimForce = knockback * positionalBonus * attackPower * shieldProtection;

            victim.body.AddForce(pushDirection * victimForce + Vector3.up * 0.10f, ForceMode.VelocityChange);
            attacker.body.AddForce(-pushDirection * knockback * 0.08f, ForceMode.VelocityChange);
            victim.impactLockUntil = Time.time + 0.25f;
            attacker.impactLockUntil = Time.time + 0.12f;
        }
        else
        {
            // A true head-on clash has no clear attacker, so both cars bounce.
            float forceAgainstThis = (Time.time < other.powerImpactUntil ? 1.55f : 1f) *
                                     (Time.time < shieldUntil ? 0.30f : 1f);
            float forceAgainstOther = (Time.time < powerImpactUntil ? 1.55f : 1f) *
                                      (Time.time < other.shieldUntil ? 0.30f : 1f);
            body.AddForce(-separation * knockback * forceAgainstThis + Vector3.up * 0.10f, ForceMode.VelocityChange);
            other.body.AddForce(separation * knockback * forceAgainstOther + Vector3.up * 0.10f, ForceMode.VelocityChange);
            impactLockUntil = Time.time + 0.25f;
            other.impactLockUntil = Time.time + 0.25f;
        }
    }

    public void KnockOut()
    {
        if (!IsAlive) return;
        IsAlive = false;
        throttle = steering = 0f;
        SetOilTrails(false);
        SetEnergyTrails(false);
        if (body) body.constraints = RigidbodyConstraints.None;
    }

    public string GiveRandomPowerup()
    {
        heldPower = Random.Range(2, 6);
        return heldPower == 2 ? "TIRO DE GIRO PRONTO" : heldPower == 3 ? "ESCUDO PRONTO" :
            heldPower == 4 ? "GIGANTE PRONTO" : "FURACAO PRONTO";
    }

    public void UsePower()
    {
        if (heldPower == 1) DropOil();
        else if (heldPower == 2) FireSpinShot();
        else if (heldPower == 3) ActivateShield();
        else if (heldPower == 4) ActivateGiant();
        else if (heldPower == 5) FireHurricane();
    }

    void DropOil()
    {
        heldPower = 0;
        Vector3 position = transform.position - transform.forward * 0.82f;
        position.y = 0.035f;
        BumperOilSlick.Create(position, this);
    }

    void FireSpinShot()
    {
        heldPower = 0;
        Vector3 position = transform.position + transform.forward * 0.78f + Vector3.up * 0.16f;
        BumperSpinProjectile.Create(position, transform.forward, this);
    }

    void ActivateShield()
    {
        heldPower = 0;
        shieldUntil = Time.time + 6f;
        BumperShieldVisual.Create(this);
    }

    void ActivateGiant()
    {
        heldPower = 0;
        giantUntil = Time.time + 6f;
    }

    void FireHurricane()
    {
        heldPower = 0;
        Vector3 position = transform.position + transform.forward * 0.86f + Vector3.up * 0.12f;
        BumperHurricaneProjectile.Create(position, transform.forward, this);
    }

    public void ApplyHurricane(Vector3 center, Vector3 shotDirection)
    {
        hurricaneCenter = new Vector3(center.x, transform.position.y, center.z);
        hurricaneEjectDirection = Vector3.ProjectOnPlane(shotDirection, Vector3.up).normalized;
        hurricaneSign = Random.value < 0.5f ? -1f : 1f;
        hurricaneUntil = Time.time + 2.1f;
        hurricaneWasActive = true;
        shotSpinUntil = 0f;
        body.linearVelocity = Vector3.zero;
        BumperHurricaneVisual.Create(this, hurricaneCenter);
    }

    public void BlockSpinShot()
    {
        var shield = GetComponentInChildren<BumperShieldVisual>();
        if (shield) shield.Flash();
    }

    public void ApplySpinShot(Vector3 shotDirection)
    {
        shotSpinSign = Vector3.Dot(transform.right, shotDirection) >= 0f ? -1f : 1f;
        if (Mathf.Abs(Vector3.Dot(transform.right, shotDirection)) < 0.15f)
            shotSpinSign = Random.value < 0.5f ? -1f : 1f;
        shotSpinUntil = Time.time + 2.55f;
        shotSpinWasActive = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = new Vector3(0f, shotSpinSign * 7.2f, 0f);
        BumperSpinHitEffect.Create(this);
    }

    public void ApplyOilSlip(Vector3 oilPosition)
    {
        oilSlipUntil = Mathf.Max(oilSlipUntil, Time.time + 4f);
        oilDriftSign = Random.value < 0.5f ? -1f : 1f;
        body.AddTorque(Vector3.up * oilDriftSign * 1.65f, ForceMode.VelocityChange);
    }

    void BuildOilTrails()
    {
        oilTrails = new TrailRenderer[2];
        for (int i = 0; i < oilTrails.Length; i++)
        {
            var trailObject = new GameObject(i == 0 ? "RastroOleo_Esquerdo" : "RastroOleo_Direito");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = new Vector3(i == 0 ? -0.27f : 0.27f, -0.37f, 0.22f);
            var trail = trailObject.AddComponent<TrailRenderer>();
            trail.time = 2.8f;
            trail.minVertexDistance = 0.07f;
            trail.startWidth = 0.13f;
            trail.endWidth = 0.035f;
            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            Shader trackShader = Shader.Find("BumperZ/OilTrack");
            var trailMaterial = new Material(trackShader ? trackShader : Shader.Find("Unlit/Color"));
            trailMaterial.color = new Color(0.015f, 0.018f, 0.025f, 0.86f);
            trail.material = trailMaterial;
            oilTrails[i] = trail;
        }
    }

    void BuildEnergyTrails(Color carColor)
    {
        energyTrails = new LineRenderer[2];
        Color bright = Color.Lerp(carColor, Color.white, 0.18f);
        Shader trailShader = Shader.Find("BumperZ/EnergyTrail");
        var material = new Material(trailShader ? trailShader : Shader.Find("Sprites/Default"))
        {
            name = $"CarFireTrail_{DriverIndex}",
            color = Color.white
        };

        for (int i = 0; i < energyTrails.Length; i++)
        {
            var trailObject = new GameObject(i == 0 ? "ChamaTraseira_Esquerda" : "ChamaTraseira_Direita");
            trailObject.transform.SetParent(transform, false);
            trailObject.transform.localPosition = new Vector3(i == 0 ? -0.22f : 0.22f, -0.06f, -0.43f);
            var jet = trailObject.AddComponent<LineRenderer>();
            jet.useWorldSpace = false;
            jet.positionCount = 2;
            jet.SetPosition(0, Vector3.zero);
            jet.SetPosition(1, new Vector3(0f, 0f, -.72f));
            jet.startWidth = .20f;
            jet.endWidth = 0f;
            jet.widthCurve = new AnimationCurve(
                new Keyframe(0f, .34f), new Keyframe(.12f, 1f), new Keyframe(.55f, .62f), new Keyframe(1f, 0f));
            var gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(bright, .22f), new GradientColorKey(bright, 1f) },
                new[] { new GradientAlphaKey(.95f, 0f), new GradientAlphaKey(.78f, .35f), new GradientAlphaKey(0f, 1f) });
            jet.colorGradient = gradient;
            jet.material = material;
            jet.alignment = LineAlignment.View;
            jet.textureMode = LineTextureMode.Stretch;
            jet.numCapVertices = 2;
            jet.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            jet.receiveShadows = false;
            jet.enabled = false;
            energyTrails[i] = jet;
        }
    }

    void SetEnergyTrails(bool active)
    {
        if (energyTrails == null) return;
        foreach (var trail in energyTrails)
            if (trail) trail.enabled = active;
    }

    void UpdateEnergyJets(bool active)
    {
        SetEnergyTrails(active);
        if (!active || energyTrails == null || !body) return;
        float speed = Vector3.ProjectOnPlane(body.linearVelocity, Vector3.up).magnitude;
        float baseLength = Mathf.Lerp(.42f, 1.02f, Mathf.Clamp01(speed / MaxSpeed));
        for (int i = 0; i < energyTrails.Length; i++)
        {
            var jet = energyTrails[i];
            if (!jet) continue;
            float flicker = 1f + Mathf.Sin(Time.time * 28f + DriverIndex * 1.7f + i * 2.1f) * .10f;
            jet.SetPosition(1, new Vector3(0f, 0f, -baseLength * flicker));
        }
    }

    void SetOilTrails(bool active)
    {
        if (oilTrails == null) return;
        foreach (var trail in oilTrails)
            if (trail) trail.emitting = active;
    }

    void SpawnOilTrackMarks()
    {
        Vector3 left = transform.TransformPoint(new Vector3(-0.27f, 0f, 0.20f));
        Vector3 right = transform.TransformPoint(new Vector3(0.27f, 0f, 0.20f));
        left.y = right.y = 0.028f;
        BumperOilTrackMark.Create(left, transform.eulerAngles.y);
        BumperOilTrackMark.Create(right, transform.eulerAngles.y);
    }
}
