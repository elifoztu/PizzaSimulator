using UnityEngine;

public class AudioManager : MonoBehaviour
{
    [Header("Ingredient Sounds")]
    [SerializeField] private AudioClip saucePlacementSound;  // sound when placing sauce
    [SerializeField] private AudioClip pepperoniPlacementSound; // sound when placing pepperoni
    [SerializeField] private AudioClip cornPlacementSound; // sound when placing corn
    [SerializeField] private AudioClip olivesPlacementSound; // sound when placing olives
    
    [Header("Cooking Sounds")]
    [SerializeField] private AudioClip cookingStartSound; // sound when cooking begins
    [SerializeField] private AudioClip cookingCompleteSound; // sound when pizza is done
    
    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound; // sound for button presses
    [SerializeField] private AudioClip newPizzaSound; // sound when starting new pizza
    
    [Header("Audio Settings")]
    [SerializeField] private float masterVolume = 1.0f; // overall volume control
    [SerializeField] private bool soundEnabled = true; // enable/disable all sounds
    
    // singleton pattern for easy access from other scripts
    public static AudioManager Instance { get; private set; }
    
    private AudioSource audioSource; // component that actually plays the sounds
    
    // initialize the audio manager and set up singleton pattern
    void Awake()
    {
        // ensure only one AudioManager exists
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // persist between scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        SetupAudioSource();
    }
    
    // sets up the audio source component with proper settings
    private void SetupAudioSource()
    {
        audioSource = GetComponent<AudioSource>();
        
        // add audio source if it doesn't exist
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // configure audio source settings
        audioSource.playOnAwake = false;
        audioSource.volume = masterVolume;
        audioSource.spatialBlend = 0f; // 2D sound (not 3D positional)
    }
    
    // plays sound effect for ingredient placement based on type
    public void PlayIngredientSound(PizzaSimulator.IngredientType ingredientType)
    {
        if (!soundEnabled || audioSource == null) return;
        
        AudioClip clipToPlay = null;
        
        switch (ingredientType)
        {
            case PizzaSimulator.IngredientType.TomatoSauce:
                clipToPlay = saucePlacementSound;
                break;
            case PizzaSimulator.IngredientType.Pepperoni:
                clipToPlay = pepperoniPlacementSound;
                break;
            case PizzaSimulator.IngredientType.Corn:
                clipToPlay = cornPlacementSound;
                break;
            case PizzaSimulator.IngredientType.Olives:
                clipToPlay = olivesPlacementSound;
                break;
        }
        
        PlaySound(clipToPlay);
    }
    
    // plays cooking start sound effect
    public void PlayCookingStartSound()
    {
        PlaySound(cookingStartSound);
    }
    
    // plays cooking complete sound effect
    public void PlayCookingCompleteSound()
    {
        PlaySound(cookingCompleteSound);
    }
    
    // plays button click sound effect
    public void PlayButtonClickSound()
    {
        PlaySound(buttonClickSound);
    }
    
    // plays new pizza sound effect
    public void PlayNewPizzaSound()
    {
        PlaySound(newPizzaSound);
    }
    
    // plays a specific audio clip if it exists
    private void PlaySound(AudioClip clip)
    {
        if (clip != null && soundEnabled && audioSource != null)
        {
            audioSource.PlayOneShot(clip, masterVolume);
        }
    }
    
    // toggles sound on/off
    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
    }
    
    // sets the master volume level
    public void SetVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        if (audioSource != null)
        {
            audioSource.volume = masterVolume;
        }
    }
}