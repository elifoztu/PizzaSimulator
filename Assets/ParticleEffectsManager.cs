using UnityEngine;
using System.Collections;

public class ParticleEffectsManager : MonoBehaviour
{
    [Header("Cooking Effects")]
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
    
    // singleton pattern for easy access
    public static ParticleEffectsManager Instance { get; private set; }
    
    // initialize the particle effects manager
    void Awake()
    {
        // ensure only one ParticleEffectsManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeParticleEffects();
    }
    
    // sets up all particle systems with default settings
    private void InitializeParticleEffects()
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
        
        ParticleSystem particles = particleObject.AddComponent<ParticleSystem>();
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
        
        // configure emission
        var emission = particles.emission;
        emission.rateOverTime = 10;
        
        // configure shape
        var shape = particles.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.radius = 0.5f;
        
        return particles;
    }
    
    // starts cooking smoke effect at pizza position
    public void StartCookingEffect(Vector3 pizzaPosition)
    {
        if (!effectsEnabled || cookingSmoke == null) return;
        
        cookingSmoke.transform.position = pizzaPosition + Vector3.up * 0.5f;
        cookingSmoke.Play();
    }
    
    // stops cooking smoke and plays completion steam effect
    public void PlayCookingCompleteEffect(Vector3 pizzaPosition)
    {
        if (!effectsEnabled) return;
        
        // stop cooking smoke
        if (cookingSmoke != null && cookingSmoke.isPlaying)
        {
            cookingSmoke.Stop();
        }
        
        // play steam effect
        if (steamEffect != null)
        {
            steamEffect.transform.position = pizzaPosition + Vector3.up * 0.3f;
            steamEffect.Play();
            
            // stop steam after duration
            StartCoroutine(StopEffectAfterDelay(steamEffect, effectDuration));
        }
    }
    
    // plays ingredient placement effect
    public void PlayIngredientEffect(Vector3 position, PizzaSimulator.IngredientType ingredientType)
    {
        if (!effectsEnabled) return;
        
        ParticleSystem effectToPlay = null;
        
        switch (ingredientType)
        {
            case PizzaSimulator.IngredientType.TomatoSauce:
                effectToPlay = sauceSparkle;
                break;
            default:
                effectToPlay = ingredientPop;
                break;
        }
        
        if (effectToPlay != null)
        {
            effectToPlay.transform.position = position;
            effectToPlay.Play();
            
            // brief effect
            StartCoroutine(StopEffectAfterDelay(effectToPlay, 0.5f));
        }
    }
    
    // plays button click visual effect
    public void PlayButtonClickEffect(Vector3 buttonPosition)
    {
        if (!effectsEnabled || buttonClickEffect == null) return;
        
        buttonClickEffect.transform.position = buttonPosition;
        buttonClickEffect.Play();
        
        StartCoroutine(StopEffectAfterDelay(buttonClickEffect, 0.3f));
    }
    
    // stops a particle effect after a specified delay
    private IEnumerator StopEffectAfterDelay(ParticleSystem effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (effect != null && effect.isPlaying)
        {
            effect.Stop();
        }
    }
    
    // toggles all particle effects on/off
    public void ToggleEffects()
    {
        effectsEnabled = !effectsEnabled;
    }
    
    // stops all currently playing effects
    public void StopAllEffects()
    {
        ParticleSystem[] allEffects = { cookingSmoke, steamEffect, sauceSparkle, ingredientPop, buttonClickEffect };
        
        foreach (var effect in allEffects)
        {
            if (effect != null && effect.isPlaying)
            {
                effect.Stop();
            }
        }
    }
}