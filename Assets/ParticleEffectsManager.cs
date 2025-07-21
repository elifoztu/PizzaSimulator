using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ParticleEffectsManager : MonoBehaviour
{
    [Header("Cooking Effects")]
    
    // these two are not working
    [SerializeField] private ParticleSystem cookingSmoke; // smoke effect when pizza cooks
    [SerializeField] private ParticleSystem steamEffect; // steam when pizza is done
    
    [Header("Ingredient Effects")]
    [SerializeField] private ParticleSystem sauceSparkle; // effect when placing sauce
    [SerializeField] private ParticleSystem ingredientPop; // effect when placing toppings
    
    [Header("UI Effects")]
    [SerializeField] private ParticleSystem buttonClickEffect; // effect for button presses
    
    [Header("Effect Settings")]
    [SerializeField] private bool effectsEnabled = true; // enable/disable all effects
    [SerializeField] private float effectDuration = 2.0f; // how long effects last
    [SerializeField] private int maxActiveEffects = 20; // performance limit for active effects
    [SerializeField] private LayerMask effectLayer = 10; // layer for particle effects
    
    [Header("Performance Settings")]
    [SerializeField] private bool useObjectPooling = true; // enable object pooling for better performance
    [SerializeField] private int poolSize = 10; // number of each effect to pool
    [SerializeField] private float cleanupInterval = 5.0f; // how often to clean up finished effects

    public static ParticleEffectsManager Instance { get; private set; }
    
    // professional effect management system
    private Dictionary<ParticleSystem, Queue<ParticleSystem>> effectPools; // object pools for each effect type
    private List<ParticleSystem> activeEffects; // currently playing effects
    private Dictionary<PizzaSimulator.IngredientType, ParticleSystemData> ingredientEffectMap; // organized effect mapping
    private Coroutine cleanupCoroutine; // automatic cleanup coroutine
    
    // effect configuration data for customization
    [System.Serializable]
    private struct ParticleSystemData
    {
        public ParticleSystem prefab;
        public Color primaryColor;
        public Color secondaryColor;
        public float duration;
        public float intensity;
    }
    
    #region Unity Lifecycle
    
    // initialize the particle effects manager
    void Awake()
    {
        InitializeSingleton();
        InitializeEffectSystems();
        SetupIngredientEffectMap();
        
        if (useObjectPooling)
        {
            InitializeObjectPools();
        }
    }
    
    void Start()
    {
        cleanupCoroutine = StartCoroutine(PeriodicCleanup());
    }
    
    void OnDestroy()
    {
        if (cleanupCoroutine != null)
        {
            StopCoroutine(cleanupCoroutine);
        }
    }
    
    #endregion
    
    #region Initialization
    
    // organized singleton initialization with proper error handling
    private void InitializeSingleton()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Multiple ParticleEffectsManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    // initialize effect management systems
    private void InitializeEffectSystems()
    {
        activeEffects = new List<ParticleSystem>();
        effectPools = new Dictionary<ParticleSystem, Queue<ParticleSystem>>();
        
        // create fallback effects if prefabs are missing
        CreateFallbackEffects();
    }
    
    // sets up all particle systems with default settings
    private void CreateFallbackEffects()
    {
        // create basic particle effects if they don't exist
        if (cookingSmoke == null)
        {
            cookingSmoke = CreateBasicParticleSystem("CookingSmoke", Color.gray);
        }
        
        if (steamEffect == null)
        {
            steamEffect = CreateBasicParticleSystem("SteamEffect", Color.white);
        }
        
        if (sauceSparkle == null)
        {
            sauceSparkle = CreateBasicParticleSystem("SauceSparkle", Color.red);
        }
        
        if (ingredientPop == null)
        {
            ingredientPop = CreateBasicParticleSystem("IngredientPop", Color.yellow);
        }
        
        if (buttonClickEffect == null)
        {
            buttonClickEffect = CreateBasicParticleSystem("ButtonClickEffect", Color.cyan);
        }
    }
    
    // creates a basic particle system with common settings
    private ParticleSystem CreateBasicParticleSystem(string name, Color color)
    {
        GameObject particleObject = new GameObject(name);
        particleObject.transform.SetParent(transform);
        particleObject.layer = Mathf.RoundToInt(Mathf.Log(effectLayer.value, 2)); // set proper layer
        
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
        ConfigureBasicParticleSystem(particles, color);
        
        return particles;
    }
    
    // organized particle system configuration
    private void ConfigureBasicParticleSystem(ParticleSystem particles, Color color)
    {
        // make particles appear on top of everything
        var renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sortingOrder = 10; // higher number = appears on top
        
        // configure main module
        var main = particles.main;
        main.startLifetime = 1.0f;
        main.startSpeed = 2.0f;
        main.startSize = 0.1f;
        main.startColor = color;
        main.maxParticles = 50;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World; // proper simulation space
        
        // configure emission
        var emission = particles.emission;
        emission.rateOverTime = 10;
        emission.SetBursts(new ParticleSystem.Burst[] { // burst emission for better effects
            new ParticleSystem.Burst(0.0f, 15)
        });
        
        // configure shape
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        // additional modules for better effects
        var velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        
        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        
        var colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
    }
    
    // setup ingredient effect mapping for organized management
    private void SetupIngredientEffectMap()
    {
        ingredientEffectMap = new Dictionary<PizzaSimulator.IngredientType, ParticleSystemData>
        {
            {
                PizzaSimulator.IngredientType.TomatoSauce,
                new ParticleSystemData
                {
                    prefab = sauceSparkle,
                    primaryColor = Color.red,
                    secondaryColor = new Color(1f, 0.5f, 0f),
                    duration = 0.8f,
                    intensity = 1.0f
                }
            },
            {
                PizzaSimulator.IngredientType.Cheese,
                new ParticleSystemData
                {
                    prefab = ingredientPop,
                    primaryColor = Color.yellow,
                    secondaryColor = Color.white,
                    duration = 0.6f,
                    intensity = 0.8f
                }
            },
            {
                PizzaSimulator.IngredientType.Pepperoni,
                new ParticleSystemData
                {
                    prefab = ingredientPop,
                    primaryColor = new Color(0.8f, 0.2f, 0.1f),
                    secondaryColor = new Color(0.6f, 0.1f, 0.05f),
                    duration = 0.5f,
                    intensity = 0.7f
                }
            },
            {
                PizzaSimulator.IngredientType.Corn,
                new ParticleSystemData
                {
                    prefab = ingredientPop,
                    primaryColor = Color.yellow,
                    secondaryColor = new Color(1f, 0.8f, 0.2f),
                    duration = 0.4f,
                    intensity = 0.6f
                }
            },
            {
                PizzaSimulator.IngredientType.Olives,
                new ParticleSystemData
                {
                    prefab = ingredientPop,
                    primaryColor = new Color(0.2f, 0.4f, 0.1f),
                    secondaryColor = new Color(0.1f, 0.2f, 0.05f),
                    duration = 0.5f,
                    intensity = 0.7f
                }
            }
        };
    }
    
    // initialize object pools for better performance
    private void InitializeObjectPools()
    {
        var prefabsToPool = new List<ParticleSystem>
        {
            cookingSmoke, steamEffect, sauceSparkle, ingredientPop, buttonClickEffect
        };
        
        foreach (var prefab in prefabsToPool)
        {
            if (prefab != null)
            {
                var pool = new Queue<ParticleSystem>();
                
                for (int i = 0; i < poolSize; i++)
                {
                    var instance = Instantiate(prefab, transform);
                    instance.gameObject.SetActive(false);
                    pool.Enqueue(instance);
                }
                
                effectPools[prefab] = pool;
            }
        }
    }
    
    #endregion
    
    #region Public Effect Methods
    
    // starts cooking smoke effect at pizza position
    public void StartCookingEffect(Vector3 pizzaPosition)
    {
        if (!effectsEnabled || cookingSmoke == null) return;
        
        var effect = GetPooledEffect(cookingSmoke);
        if (effect != null)
        {
            effect.transform.position = pizzaPosition + Vector3.up * 0.5f;
            effect.Play();
            
            activeEffects.Add(effect);
            StartCoroutine(ManageEffect(effect, effectDuration, true));
        }
    }
    
    // stops cooking smoke and plays completion steam effect
    public void PlayCookingCompleteEffect(Vector3 pizzaPosition)
    {
        if (!effectsEnabled) return;
        
        // stop cooking smoke
        StopEffectsOfType(cookingSmoke);
        
        // play steam effect
        if (steamEffect != null)
        {
            var effect = GetPooledEffect(steamEffect);
            if (effect != null)
            {
                effect.transform.position = pizzaPosition + Vector3.up * 0.3f;
                effect.Play();
                
                activeEffects.Add(effect);
                StartCoroutine(ManageEffect(effect, 1.5f, false));
            }
        }
        
        // play success effect
        PlaySuccessEffect(pizzaPosition);
    }
    
    // plays ingredient placement effect
    public void PlayIngredientEffect(Vector3 position, PizzaSimulator.IngredientType ingredientType)
    {
        if (!effectsEnabled || !ingredientEffectMap.ContainsKey(ingredientType)) return;
        
        var effectData = ingredientEffectMap[ingredientType];
        var effect = GetPooledEffect(effectData.prefab);
        
        if (effect != null)
        {
            effect.transform.position = position;
            
            // customize effect based on ingredient
            CustomizeEffect(effect, effectData);
            
            effect.Play();
            activeEffects.Add(effect);
            StartCoroutine(ManageEffect(effect, effectData.duration, false));
        }
    }
    
    // plays button click visual effect
    public void PlayButtonClickEffect(Vector3 buttonPosition)
    {
        if (!effectsEnabled || buttonClickEffect == null) return;
        
        var effect = GetPooledEffect(buttonClickEffect);
        if (effect != null)
        {
            effect.transform.position = buttonPosition;
            effect.Play();
            
            activeEffects.Add(effect);
            StartCoroutine(ManageEffect(effect, 0.3f, false));
        }
    }
    
    // success effect for positive feedback
    public void PlaySuccessEffect(Vector3 position)
    {
        if (!effectsEnabled) return;
        
        var effect = GetPooledEffect(ingredientPop); // use ingredient pop as fallback
        if (effect != null)
        {
            effect.transform.position = position;
            
            // make it green for success
            var main = effect.main;
            main.startColor = Color.green;
            
            effect.Play();
            activeEffects.Add(effect);
            StartCoroutine(ManageEffect(effect, 1.0f, false));
        }
    }
    
    // error effect for negative feedback
    public void PlayErrorEffect(Vector3 position)
    {
        if (!effectsEnabled) return;
        
        var effect = GetPooledEffect(ingredientPop);
        if (effect != null)
        {
            effect.transform.position = position;
            
            // make it red for error
            var main = effect.main;
            main.startColor = Color.red;
            
            effect.Play();
            activeEffects.Add(effect);
            StartCoroutine(ManageEffect(effect, 0.8f, false));
        }
    }
    
    #endregion
    
    #region Effect Management
    
    // get pooled effect with fallback to instantiation
    private ParticleSystem GetPooledEffect(ParticleSystem prefab)
    {
        if (prefab == null) return null;
        
        if (useObjectPooling && effectPools.ContainsKey(prefab) && effectPools[prefab].Count > 0)
        {
            var pooledEffect = effectPools[prefab].Dequeue();
            pooledEffect.gameObject.SetActive(true);
            return pooledEffect;
        }
        
        // create new instance if pool is empty or pooling is disabled
        var newEffect = Instantiate(prefab, transform);
        return newEffect;
    }
    
    // return effect to pool when finished
    private void ReturnToPool(ParticleSystem effect, ParticleSystem prefab)
    {
        if (effect == null) return;
        
        effect.Stop();
        effect.Clear();
        effect.gameObject.SetActive(false);
        
        if (useObjectPooling && effectPools.ContainsKey(prefab))
        {
            effectPools[prefab].Enqueue(effect);
        }
        else
        {
            Destroy(effect.gameObject);
        }
    }
    
    // customize effect based on ingredient data
    private void CustomizeEffect(ParticleSystem effect, ParticleSystemData data)
    {
        var main = effect.main;
        main.startColor = data.primaryColor;
        
        // randomize some properties for variety
        main.startSize = main.startSize.constant * Random.Range(0.8f, 1.2f) * data.intensity;
        main.startSpeed = main.startSpeed.constant * Random.Range(0.9f, 1.1f);
        
        // apply secondary color to color over lifetime if available
        var colorOverLifetime = effect.colorOverLifetime;
        if (colorOverLifetime.enabled)
        {
            var gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(data.primaryColor, 0.0f), new GradientColorKey(data.secondaryColor, 1.0f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.0f, 1.0f) }
            );
            colorOverLifetime.color = gradient;
        }
    }
    
    // organized effect management coroutine
    private IEnumerator ManageEffect(ParticleSystem effect, float duration, bool isLooping)
    {
        if (isLooping)
        {
            // for looping effects, just manage the reference
            yield return new WaitForSeconds(duration);
        }
        else
        {
            // for one-shot effects, wait for completion
            yield return new WaitForSeconds(duration);
            
            // wait for particles to finish
            yield return new WaitWhile(() => effect.IsAlive());
        }
        
        // clean up
        activeEffects.Remove(effect);
        
        // find the original prefab for pool return
        ParticleSystem originalPrefab = FindOriginalPrefab(effect);
        ReturnToPool(effect, originalPrefab);
    }
    
    // find original prefab for pool return
    private ParticleSystem FindOriginalPrefab(ParticleSystem effect)
    {
        // try to match by name or component similarity
        string effectName = effect.name.Replace("(Clone)", "");
        
        var allPrefabs = new List<ParticleSystem>
        {
            cookingSmoke, steamEffect, sauceSparkle, ingredientPop, buttonClickEffect
        };
        
        foreach (var prefab in allPrefabs)
        {
            if (prefab != null && prefab.name == effectName)
            {
                return prefab;
            }
        }
        
        // default fallback
        return ingredientPop;
    }
    
    // stop specific effect types
    private void StopEffectsOfType(ParticleSystem prefabType)
    {
        var effectsToStop = new List<ParticleSystem>();
        
        foreach (var effect in activeEffects)
        {
            if (effect != null && FindOriginalPrefab(effect) == prefabType)
            {
                effectsToStop.Add(effect);
            }
        }
        
        foreach (var effect in effectsToStop)
        {
            effect.Stop();
        }
    }
    
    // automatic cleanup coroutine for performance
    private IEnumerator PeriodicCleanup()
    {
        while (true)
        {
            yield return new WaitForSeconds(cleanupInterval);
            
            // remove null or finished effects from active list
            activeEffects.RemoveAll(effect => effect == null || (!effect.isPlaying && !effect.IsAlive()));
            
            // limit active effects for performance
            if (activeEffects.Count > maxActiveEffects)
            {
                int excessCount = activeEffects.Count - maxActiveEffects;
                for (int i = 0; i < excessCount; i++)
                {
                    if (activeEffects.Count > 0)
                    {
                        var oldestEffect = activeEffects[0];
                        if (oldestEffect != null)
                        {
                            oldestEffect.Stop();
                        }
                        activeEffects.RemoveAt(0);
                    }
                }
            }
        }
    }
    
    #endregion
    
    #region Public Control
    
    // toggles all particle effects on/off
    public void ToggleEffects()
    {
        effectsEnabled = !effectsEnabled;
        
        if (!effectsEnabled)
        {
            StopAllEffects();
        }
    }
    
    // stops all currently playing effects
    public void StopAllEffects()
    {
        foreach (var effect in activeEffects)
        {
            if (effect != null && effect.isPlaying)
            {
                effect.Stop();
            }
        }
        
        activeEffects.Clear();
    }
    
    // performance control methods
    public void SetEffectQuality(float quality)
    {
        quality = Mathf.Clamp01(quality);
        
        // adjust max particles based on quality
        foreach (var effect in activeEffects)
        {
            if (effect != null)
            {
                var main = effect.main;
                main.maxParticles = Mathf.RoundToInt(main.maxParticles * quality);
            }
        }
    }
    
    #endregion
    
    #region Public API
    
    // getters for external systems
    public bool AreEffectsEnabled() => effectsEnabled;
    public int GetActiveEffectCount() => activeEffects.Count;
    public float GetEffectDuration() => effectDuration;
    
    #endregion
}