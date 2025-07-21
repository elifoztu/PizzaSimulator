using UnityEngine;
using System.Collections;

public class PizzaDough : MonoBehaviour
{
    [Header("Cooking Settings")]
    [SerializeField] private Color cookedColor = new Color(0.9f, 0.7f, 0.4f); // what color the dough turns when cooked (golden brown)
    [SerializeField] private float cookingTime = 3f; // how many seconds it takes to cook
    [SerializeField] private AudioClip cookingSound; // optional sound effect when cooking (drag audio file here)
    
    [Header("Enhanced Visual Settings")]
    [SerializeField] private Color rawDoughColor = Color.white; // original dough color
    [SerializeField] private Color burntColor = new Color(0.4f, 0.2f, 0.1f); // dark brown for burnt pizza
    [SerializeField] private float perfectCookingTime = 2.5f; // time for perfect cooking bonus
    [SerializeField] private float burntTime = 5f; // time when pizza becomes burnt
    
    [Header("Animation Settings")]
    [SerializeField] private AnimationCurve cookingCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f); // smooth cooking animation
    [SerializeField] private float pulseDuration = 0.5f; // duration of cooking pulse effect
    [SerializeField] private float pulseIntensity = 1.1f; // intensity of pulse effect
    [SerializeField] private bool enableCookingShake = true; // enable subtle cooking shake
    [SerializeField] private float shakeIntensity = 0.02f; // intensity of cooking shake
    
    [Header("Audio Settings")]
    [SerializeField] private AudioClip cookingCompleteSound; // sound when cooking is complete
    [SerializeField] private AudioClip burntSound; // sound when pizza burns
    
    // component references
    private SpriteRenderer spriteRenderer; // component that displays the pizza image
    private AudioSource audioSource; // component for playing sounds
    
    // state tracking
    private Color originalColor; // stores the original dough color so we can reset it
    private Vector3 originalScale; // store original scale for animations
    private Vector3 originalPosition; // store original position for shake effect
    private bool isCooked = false; // tracks whether this pizza has been cooked already
    private bool isBurnt = false; // tracks if pizza is burnt
    private bool isCooking = false; // tracks if currently cooking
    private float cookingProgress = 0f; // cooking progress from 0 to 1
    private Coroutine currentCookingCoroutine; // track cooking coroutine for proper cleanup
    
    // events for better integration with other systems
    public System.Action OnCookingStarted;
    public System.Action OnCookingCompleted;
    public System.Action OnPizzaBurnt;
    public System.Action<float> OnCookingProgress; // progress from 0 to 1
    
    // properties for external access
    public bool IsBurnt => isBurnt;
    public bool IsCooking => isCooking;
    public float CookingProgress => cookingProgress;
    public bool IsPerfectlyCookedTiming => cookingProgress >= (perfectCookingTime / cookingTime) && !isBurnt;
    
    #region Unity Lifecycle
    
    void Awake()
    {
        InitializeComponents();
        StoreOriginalValues();
    }
    
    void Start()
    {
        ValidateSetup();
    }
    
    #endregion
    
    #region Initialization
    
    // organized component initialization
    private void InitializeComponents()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // get the component that displays our image
        audioSource = GetComponent<AudioSource>(); // get audio component (if it exists)
        
        if (spriteRenderer == null)
        {
            Debug.LogError($"PizzaDough on {gameObject.name} requires a SpriteRenderer component!");
        }
        
        // add audio source if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f; // 2D sound
        }
    }
    
    // store original values for proper resetting and animations
    private void StoreOriginalValues()
    {
        if (spriteRenderer != null)
        {
            originalColor = rawDoughColor != Color.clear ? rawDoughColor : spriteRenderer.color;
            spriteRenderer.color = originalColor; // ensure we start with the right color
        }
        
        originalScale = transform.localScale;
        originalPosition = transform.position;
    }
    
    // validate inspector settings to prevent common errors
    private void ValidateSetup()
    {
        if (cookingTime <= 0f)
        {
            Debug.LogWarning($"PizzaDough on {gameObject.name}: Cooking time should be greater than 0!");
            cookingTime = 3f;
        }
        
        if (perfectCookingTime > cookingTime)
        {
            Debug.LogWarning($"PizzaDough on {gameObject.name}: Perfect cooking time should be less than total cooking time!");
            perfectCookingTime = cookingTime * 0.8f;
        }
        
        if (burntTime <= cookingTime)
        {
            Debug.LogWarning($"PizzaDough on {gameObject.name}: Burnt time should be greater than cooking time!");
            burntTime = cookingTime * 1.5f;
        }
    }
    
    #endregion
    
    #region Cooking Process
    
    // public function that other scripts can call to start cooking this pizza
    public void CookPizza()
    {
        // only cook if we haven't already cooked this pizza and we have a sprite renderer
        if (isCooking || isCooked) 
        {
            Debug.Log("Pizza is already cooking or cooked!");
            return;
        }
        
        // stop any existing cooking coroutine
        if (currentCookingCoroutine != null)
        {
            StopCoroutine(currentCookingCoroutine);
        }
        
        currentCookingCoroutine = StartCoroutine(CookingProcess());
    }
    
    // method to stop cooking process
    public void StopCooking()
    {
        if (currentCookingCoroutine != null)
        {
            StopCoroutine(currentCookingCoroutine);
            currentCookingCoroutine = null;
        }
        
        isCooking = false;
        
        // stop effects
        ParticleEffectsManager.Instance?.StopAllEffects();
    }
    
    // enhanced cooking process with multiple states and better feedback
    private IEnumerator CookingProcess()
    {
        isCooking = true;
        cookingProgress = 0f;
        
        // start cooking effects
        PlayCookingStartEffects();
        OnCookingStarted?.Invoke();
        
        Debug.Log("Pizza cooking started!"); // log message for debugging
        
        // cooking animation loop
        float elapsed = 0f;
        
        while (elapsed < burntTime && !isCooked)
        {
            elapsed += Time.deltaTime;
            cookingProgress = elapsed / cookingTime;
            
            // update visual appearance
            UpdateCookingVisuals(elapsed);
            
            // check for cooking completion
            if (!isCooked && elapsed >= cookingTime)
            {
                CompleteCooking();
            }
            
            // check for burning
            if (!isBurnt && elapsed >= burntTime)
            {
                BurnPizza();
                break;
            }
            
            // notify progress for external systems
            OnCookingProgress?.Invoke(Mathf.Clamp01(cookingProgress));
            
            yield return null; // wait until next frame before continuing
        }
        
        isCooking = false;
        currentCookingCoroutine = null;
    }
    
    // enhanced visual updates during cooking
    private void UpdateCookingVisuals(float elapsed)
    {
        if (spriteRenderer == null) return;
        
        Color targetColor;
        float normalizedTime;
        
        if (elapsed < cookingTime)
        {
            // cooking phase: raw to cooked
            normalizedTime = elapsed / cookingTime;
            targetColor = Color.Lerp(originalColor, cookedColor, cookingCurve.Evaluate(normalizedTime));
        }
        else
        {
            // burning phase: cooked to burnt
            normalizedTime = (elapsed - cookingTime) / (burntTime - cookingTime);
            targetColor = Color.Lerp(cookedColor, burntColor, normalizedTime);
        }
        
        spriteRenderer.color = targetColor;
        
        // optional cooking shake effect
        if (enableCookingShake && isCooking)
        {
            Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity;
            transform.position = originalPosition + shakeOffset;
        }
    }
    
    #endregion
    
    #region Cooking States
    
    // handle cooking completion
    private void CompleteCooking()
    {
        if (isCooked) return;
        
        isCooked = true;
        
        // final color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = cookedColor;
        }
        
        // reset position from shake
        transform.position = originalPosition;
        
        // play completion effects
        PlayCookingCompleteEffects();
        OnCookingCompleted?.Invoke();
        
        Debug.Log($"Pizza is ready to serve! {(IsPerfectlyCookedTiming ? "(Perfect timing!)" : "")}");
    }
    
    // handle pizza burning
    private void BurnPizza()
    {
        if (isBurnt) return;
        
        isBurnt = true;
        
        // final burnt color
        if (spriteRenderer != null)
        {
            spriteRenderer.color = burntColor;
        }
        
        // reset position from shake
        transform.position = originalPosition;
        
        // play burnt effects
        PlayBurntEffects();
        OnPizzaBurnt?.Invoke();
        
        Debug.Log("Pizza is burnt! Customer won't be happy...");
    }
    
    #endregion
    
    #region Effects and Audio
    
    // organized cooking start effects
    private void PlayCookingStartEffects()
    {
        // play sound
        if (cookingSound != null && audioSource != null)
        {
            audioSource.clip = cookingSound;
            audioSource.Play();
        }
        
        // use AudioManager if available
        AudioManager.Instance?.PlayCookingStartSound();
        
        // start particle effects
        ParticleEffectsManager.Instance?.StartCookingEffect(transform.position);
        
        // start pulse animation
        if (pulseDuration > 0f)
        {
            StartCoroutine(PulseAnimation());
        }
    }
    
    // organized cooking complete effects
    private void PlayCookingCompleteEffects()
    {
        // play sound
        if (cookingCompleteSound != null && audioSource != null)
        {
            audioSource.clip = cookingCompleteSound;
            audioSource.Play();
        }
        
        // use AudioManager if available
        AudioManager.Instance?.PlayCookingCompleteSound();
        
        // play particle effects
        ParticleEffectsManager.Instance?.PlayCookingCompleteEffect(transform.position);
    }
    
    // organized burnt effects
    private void PlayBurntEffects()
    {
        // play burnt sound
        if (burntSound != null && audioSource != null)
        {
            audioSource.clip = burntSound;
            audioSource.Play();
        }
        
        // use AudioManager for error sound
        AudioManager.Instance?.PlayErrorSound();
        
        // play error particle effect
        ParticleEffectsManager.Instance?.PlayErrorEffect(transform.position);
    }
    
    // pulse animation during cooking
    private IEnumerator PulseAnimation()
    {
        float elapsed = 0f;
        
        while (elapsed < pulseDuration && isCooking)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / pulseDuration;
            
            // create a pulse effect
            float pulse = Mathf.Sin(progress * Mathf.PI * 2f) * (pulseIntensity - 1f) + 1f;
            transform.localScale = originalScale * pulse;
            
            yield return null;
        }
        
        // reset scale
        transform.localScale = originalScale;
    }
    
    #endregion
    
    #region Public Methods
    
    // public function to reset pizza back to uncooked state
    public void ResetPizza()
    {
        // stop any cooking process
        StopCooking();
        
        // reset state
        isCooked = false; // mark as not cooked
        isBurnt = false; // reset burnt state
        isCooking = false; // reset cooking state
        cookingProgress = 0f; // reset progress
        
        // reset visuals
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor; // change back to original color
        }
        
        transform.localScale = originalScale; // reset scale
        transform.position = originalPosition; // reset position
        
        // stop all effects
        ParticleEffectsManager.Instance?.StopAllEffects();
        
        Debug.Log("Pizza reset to raw state");
    }
    
    // public function that returns whether this pizza has been cooked
    public bool IsCooked()
    {
        return isCooked;
    }
    
    #endregion
    
    #region Configuration
    
    // configuration methods for external systems
    public void SetCookingTime(float newCookingTime)
    {
        if (newCookingTime > 0f)
        {
            cookingTime = newCookingTime;
            perfectCookingTime = cookingTime * 0.8f;
            burntTime = cookingTime * 1.5f;
        }
    }
    
    public void SetCookingColors(Color raw, Color cooked, Color burnt)
    {
        rawDoughColor = raw;
        cookedColor = cooked;
        burntColor = burnt;
        
        originalColor = rawDoughColor;
        if (spriteRenderer != null && !isCooked && !isBurnt)
        {
            spriteRenderer.color = originalColor;
        }
    }
    
    #endregion
    
    #region Public API
    
    // getters for external systems
    public float GetCookingTimeRemaining()
    {
        if (!isCooking) return 0f;
        return Mathf.Max(0f, cookingTime - (cookingProgress * cookingTime));
    }
    
    public float GetBurntTimeRemaining()
    {
        if (!isCooking || isBurnt) return 0f;
        float elapsed = cookingProgress * cookingTime;
        return Mathf.Max(0f, burntTime - elapsed);
    }
    
    public string GetCookingStatus()
    {
        if (isBurnt) return "Burnt";
        if (isCooked) return IsPerfectlyCookedTiming ? "Perfect" : "Cooked";
        if (isCooking) return "Cooking";
        return "Raw";
    }
    
    #endregion
}