using UnityEngine;
using System.Collections;

public class PizzaDough : MonoBehaviour
{
    [Header("Cooking Settings")]
    [SerializeField] private Color cookedColor = new Color(0.9f, 0.7f, 0.4f); // what color the dough turns when cooked (golden brown)
    [SerializeField] private float cookingTime = 3f; // how many seconds it takes to cook
    [SerializeField] private AudioClip cookingSound; // optional sound effect when cooking (drag audio file here)
    
    // private variables used internally by this script
    private SpriteRenderer spriteRenderer; // component that displays the pizza image
    private Color originalColor; // stores the original dough color so we can reset it
    private bool isCooked = false; // tracks whether this pizza has been cooked already
    private AudioSource audioSource; // component for playing sounds
    
    // runs once when this object is created
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>(); // get the component that displays our image
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color; // remember the original color for later
        }
        
        audioSource = GetComponent<AudioSource>(); // get audio component (if it exists)
    }
    
    // public function that other scripts can call to start cooking this pizza
    public void CookPizza()
    {
        // only cook if we haven't already cooked this pizza and we have a sprite renderer
        if (!isCooked && spriteRenderer != null)
        {
            StartCoroutine(CookingAnimation()); // start the gradual color change animation
        }
    }
    
    // coroutine that gradually changes the pizza color over time
    System.Collections.IEnumerator CookingAnimation()
    {
        Debug.Log("Pizza cooking started!"); // log message for debugging
        
        // play cooking sound if we have one
        if (cookingSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(cookingSound); // play the sound once
        }
        
        float elapsed = 0f; // tracks how much time has passed
        
        // gradually change color from original to cooked over the cooking time
        while (elapsed < cookingTime)
        {
            elapsed += Time.deltaTime; // add the time since last frame
            float progress = elapsed / cookingTime; // calculate how far through cooking we are (0 to 1)
            
            // smoothly blend between original color and cooked color based on progress
            spriteRenderer.color = Color.Lerp(originalColor, cookedColor, progress);
            
            yield return null; // wait until next frame before continuing
        }
        
        // make sure final color is exactly the cooked color
        spriteRenderer.color = cookedColor;
        isCooked = true; // mark this pizza as fully cooked
        
        // play completion effects
        AudioManager.Instance?.PlayCookingCompleteSound();
        ParticleEffectsManager.Instance?.PlayCookingCompleteEffect(transform.position);
        Debug.Log("Pizza is ready to serve!"); // log completion message
    }
    
    // public function that returns whether this pizza has been cooked
    public bool IsCooked()
    {
        return isCooked;
    }
    
    // public function to reset pizza back to uncooked state
    public void ResetPizza()
    {
        isCooked = false; // mark as not cooked
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor; // change back to original color
        }
    }
}