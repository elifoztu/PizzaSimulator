using UnityEngine;
using UnityEngine.Audio;
using System.Collections.Generic;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixers")]
    [SerializeField] private AudioMixerGroup masterMixerGroup; // master audio control
    [SerializeField] private AudioMixerGroup sfxMixerGroup; // sound effects control
    [SerializeField] private AudioMixerGroup musicMixerGroup; // music control
    
    [Header("Ingredient Sounds")]
    [SerializeField] private AudioClip[] saucePlacementSounds; // multiple sounds for variety
    [SerializeField] private AudioClip[] cheesePlacementSounds; // cheese sounds
    [SerializeField] private AudioClip[] pepperoniPlacementSounds; // sound when placing pepperoni
    [SerializeField] private AudioClip[] cornPlacementSounds; // sound when placing corn
    [SerializeField] private AudioClip[] olivesPlacementSounds; // sound when placing olives
    
    [Header("Cooking Sounds")]
    [SerializeField] private AudioClip cookingStartSound; // sound when cooking begins
    [SerializeField] private AudioClip cookingCompleteSound; // sound when pizza is done
    [SerializeField] private AudioClip ovenOpenSound; // oven interaction sound
    [SerializeField] private AudioClip pizzaSlideSound; // pizza sliding sound
    
    [Header("UI Sounds")]
    [SerializeField] private AudioClip buttonClickSound; // sound for button presses
    [SerializeField] private AudioClip buttonHoverSound; // button hover sound
    [SerializeField] private AudioClip newPizzaSound; // sound when starting new pizza
    [SerializeField] private AudioClip errorSound; // error sound for wrong actions
    
    [Header("Customer Sounds")]
    [SerializeField] private AudioClip customerArrivalSound; // when customer enters
    [SerializeField] private AudioClip customerHappySound; // when customer is satisfied
    [SerializeField] private AudioClip customerAngrySound; // when customer is upset
    [SerializeField] private AudioClip cashRegisterSound; // when earning money
    
    [Header("Background Music")]
    [SerializeField] private AudioClip backgroundMusic; // main game music
    [SerializeField] private bool playMusicOnStart = true; // auto-play music on start
    
    [Header("Audio Settings")]
    [SerializeField] private float masterVolume = 1.0f; // overall volume control
    [SerializeField] private float sfxVolume = 1.0f; // separate SFX volume
    [SerializeField] private float musicVolume = 0.7f; // separate music volume
    [SerializeField] private bool soundEnabled = true; // enable/disable all sounds
    [SerializeField] private int maxSimultaneousSounds = 10; // performance limit
    
    [Header("Audio Source Pool")]
    [SerializeField] private int audioSourcePoolSize = 15; // number of audio sources to pool
    
    // singleton pattern for easy access from other scripts
    public static AudioManager Instance { get; private set; }
    
    // professional audio management system
    private AudioSource musicAudioSource; // dedicated music player
    private Queue<AudioSource> audioSourcePool; // pool of audio sources for SFX
    private List<AudioSource> activeSources; // currently playing sources
    private Dictionary<PizzaSimulator.IngredientType, AudioClip[]> ingredientSoundMap; // organized sound mapping
    
    // settings persistence keys
    private const string MASTER_VOLUME_KEY = "MasterVolume";
    private const string SFX_VOLUME_KEY = "SFXVolume";
    private const string MUSIC_VOLUME_KEY = "MusicVolume";
    private const string SOUND_ENABLED_KEY = "SoundEnabled";
    
    #region Unity Lifecycle
    
    // initialize the audio manager and set up singleton pattern
    void Awake()
    {
        InitializeSingleton();
        LoadAudioSettings();
        InitializeAudioSources();
        SetupIngredientSoundMap();
    }
    
    void Start()
    {
        if (playMusicOnStart && backgroundMusic != null)
        {
            PlayBackgroundMusic();
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
            DontDestroyOnLoad(gameObject); // persist between scenes
        }
        else
        {
            Debug.LogWarning("Multiple AudioManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    // load audio settings from PlayerPrefs for persistence
    private void LoadAudioSettings()
    {
        masterVolume = PlayerPrefs.GetFloat(MASTER_VOLUME_KEY, 1.0f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 1.0f);
        musicVolume = PlayerPrefs.GetFloat(MUSIC_VOLUME_KEY, 0.7f);
        soundEnabled = PlayerPrefs.GetInt(SOUND_ENABLED_KEY, 1) == 1;
    }
    
    // professional audio source pooling system for better performance
    private void InitializeAudioSources()
    {
        audioSourcePool = new Queue<AudioSource>();
        activeSources = new List<AudioSource>();
        
        // create pool of audio sources for SFX
        for (int i = 0; i < audioSourcePoolSize; i++)
        {
            var audioSource = CreateAudioSource($"PooledAudioSource_{i}");
            audioSource.outputAudioMixerGroup = sfxMixerGroup;
            audioSourcePool.Enqueue(audioSource);
        }
        
        // create dedicated music audio source
        musicAudioSource = CreateAudioSource("MusicAudioSource");
        musicAudioSource.outputAudioMixerGroup = musicMixerGroup;
        musicAudioSource.loop = true; // music should loop
    }
    
    // helper method to create properly configured audio sources
    private AudioSource CreateAudioSource(string name)
    {
        var audioObject = new GameObject(name);
        audioObject.transform.SetParent(transform);
        
        var audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D sound (not 3D positional)
        
        return audioSource;
    }
    
    // organize ingredient sounds into a dictionary for efficient access
    private void SetupIngredientSoundMap()
    {
        ingredientSoundMap = new Dictionary<PizzaSimulator.IngredientType, AudioClip[]>
        {
            { PizzaSimulator.IngredientType.TomatoSauce, saucePlacementSounds },
            { PizzaSimulator.IngredientType.Cheese, cheesePlacementSounds },
            { PizzaSimulator.IngredientType.Pepperoni, pepperoniPlacementSounds },
            { PizzaSimulator.IngredientType.Corn, cornPlacementSounds },
            { PizzaSimulator.IngredientType.Olives, olivesPlacementSounds }
        };
    }
    
    #endregion
    
    #region Public Sound Methods
    
    // plays sound effect for ingredient placement based on type
    public void PlayIngredientSound(PizzaSimulator.IngredientType ingredientType)
    {
        if (!soundEnabled) return;
        
        // use sound map with multiple variants for variety
        if (ingredientSoundMap.TryGetValue(ingredientType, out AudioClip[] clips) && clips.Length > 0)
        {
            var randomClip = clips[Random.Range(0, clips.Length)]; // pick random variant
            PlaySFX(randomClip);
        }
    }
    
    // cooking sounds
    public void PlayCookingStartSound() => PlaySFX(cookingStartSound);
    public void PlayCookingCompleteSound() => PlaySFX(cookingCompleteSound);
    public void PlayOvenOpenSound() => PlaySFX(ovenOpenSound);
    public void PlayPizzaSlideSound() => PlaySFX(pizzaSlideSound);
    
    // UI sounds
    public void PlayButtonClickSound() => PlaySFX(buttonClickSound);
    public void PlayButtonHoverSound() => PlaySFX(buttonHoverSound);
    public void PlayNewPizzaSound() => PlaySFX(newPizzaSound);
    public void PlayErrorSound() => PlaySFX(errorSound);
    
    // customer interaction sounds
    public void PlayCustomerArrivalSound() => PlaySFX(customerArrivalSound);
    public void PlayCustomerHappySound() => PlaySFX(customerHappySound);
    public void PlayCustomerAngrySound() => PlaySFX(customerAngrySound);
    public void PlayCashRegisterSound() => PlaySFX(cashRegisterSound);
    
    #endregion
    
    #region Core Audio System
    
    // core SFX playing method with pooling and performance optimization
    private void PlaySFX(AudioClip clip, float volumeScale = 1.0f)
    {
        if (!IsAudioValid(clip)) return;
        
        var audioSource = GetPooledAudioSource();
        if (audioSource != null)
        {
            audioSource.clip = clip;
            audioSource.volume = sfxVolume * masterVolume * volumeScale;
            audioSource.pitch = Random.Range(0.95f, 1.05f); // slight pitch variation for naturalness
            audioSource.Play();
            
            // return to pool when finished
            StartCoroutine(ReturnToPoolWhenFinished(audioSource));
        }
    }
    
    // plays background music
    private void PlayBackgroundMusic()
    {
        if (!IsAudioValid(backgroundMusic) || musicAudioSource == null) return;
        
        musicAudioSource.clip = backgroundMusic;
        musicAudioSource.volume = musicVolume * masterVolume;
        musicAudioSource.Play();
    }
    
    // helper method to validate audio before playing
    private bool IsAudioValid(AudioClip clip)
    {
        return clip != null && soundEnabled;
    }
    
    // get audio source from pool with performance limiting
    private AudioSource GetPooledAudioSource()
    {
        // clean up finished sources
        activeSources.RemoveAll(source => source == null || !source.isPlaying);
        
        // limit simultaneous sounds for performance
        if (activeSources.Count >= maxSimultaneousSounds)
        {
            var oldestSource = activeSources[0];
            oldestSource.Stop();
            activeSources.RemoveAt(0);
            audioSourcePool.Enqueue(oldestSource);
        }
        
        if (audioSourcePool.Count > 0)
        {
            var audioSource = audioSourcePool.Dequeue();
            activeSources.Add(audioSource);
            return audioSource;
        }
        
        Debug.LogWarning("No available audio sources in pool!");
        return null;
    }
    
    // coroutine to return audio source to pool when finished
    private System.Collections.IEnumerator ReturnToPoolWhenFinished(AudioSource audioSource)
    {
        yield return new WaitWhile(() => audioSource.isPlaying);
        
        if (activeSources.Contains(audioSource))
        {
            activeSources.Remove(audioSource);
            audioSourcePool.Enqueue(audioSource);
        }
    }
    
    #endregion
    
    #region Volume Control
    
    // professional volume control with persistence
    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        SaveAudioSettings();
    }
    
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        UpdateAllVolumes();
        SaveAudioSettings();
    }
    
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume * masterVolume;
        }
        SaveAudioSettings();
    }
    
    // toggles sound on/off
    public void ToggleSound()
    {
        soundEnabled = !soundEnabled;
        
        if (!soundEnabled)
        {
            StopAllSounds();
        }
        
        SaveAudioSettings();
    }
    
    // update all active audio volumes
    private void UpdateAllVolumes()
    {
        // update music volume
        if (musicAudioSource != null)
        {
            musicAudioSource.volume = musicVolume * masterVolume;
        }
        
        // update active SFX volumes
        foreach (var source in activeSources)
        {
            if (source != null && source.isPlaying)
            {
                // preserve the original volume scale
                var originalVolume = source.volume / (sfxVolume * masterVolume);
                source.volume = sfxVolume * masterVolume * originalVolume;
            }
        }
    }
    
    // save audio settings to PlayerPrefs
    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat(MASTER_VOLUME_KEY, masterVolume);
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, sfxVolume);
        PlayerPrefs.SetFloat(MUSIC_VOLUME_KEY, musicVolume);
        PlayerPrefs.SetInt(SOUND_ENABLED_KEY, soundEnabled ? 1 : 0);
        PlayerPrefs.Save();
    }
    
    #endregion
    
    #region Audio Control
    
    // stop all sounds
    public void StopAllSounds()
    {
        foreach (var source in activeSources)
        {
            if (source != null && source.isPlaying)
            {
                source.Stop();
            }
        }
        
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Stop();
        }
    }
    
    // music control methods
    public void PauseMusic()
    {
        if (musicAudioSource != null && musicAudioSource.isPlaying)
        {
            musicAudioSource.Pause();
        }
    }
    
    public void ResumeMusic()
    {
        if (musicAudioSource != null && !musicAudioSource.isPlaying && backgroundMusic != null)
        {
            musicAudioSource.UnPause();
        }
    }
    
    #endregion
    
    #region Public API
    
    // getters for external systems
    public float GetMasterVolume() => masterVolume;
    public float GetSFXVolume() => sfxVolume;
    public float GetMusicVolume() => musicVolume;
    public bool IsSoundEnabled() => soundEnabled;
    public bool IsMusicPlaying() => musicAudioSource != null && musicAudioSource.isPlaying;
    public int GetActiveSoundCount() => activeSources.Count;
    
    #endregion
}