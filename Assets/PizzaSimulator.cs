using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;

// things to work on:
// add a better background

public class PizzaSimulator : MonoBehaviour
{
    [Header("Pizza Components")]
    [SerializeField] private GameObject pizzaDoughObject; // the actual pizza object in the scene
    [SerializeField] private Transform ovenTransform; // where the pizza sits (empty gameobject for positioning)
    
    [Header("Ingredient Sprites (Drag PNG files here)")]
    [SerializeField] private Sprite doughSprite; // image for the pizza base
    [SerializeField] private Sprite tomatoSauceSprite; // red sauce blob image
    [SerializeField] private Sprite cheeseSprite; // cheese layer image
    [SerializeField] private Sprite pepperoniSprite; // pepperoni slice image
    [SerializeField] private Sprite cornSprite; // corn kernel image
    [SerializeField] private Sprite olivesSprite; // olive slice image
    
    [Header("UI Buttons")]
    [SerializeField] private Button sauceButton; // button to activate sauce spreading mode
    [SerializeField] private Button cheeseButton; 
    [SerializeField] private Button pepperoniButton; // button to select pepperoni placement
    [SerializeField] private Button cornButton; // button to select corn placement
    [SerializeField] private Button olivesButton; 
    [SerializeField] private Button newPizzaButton; // button to clear pizza and start over
    [SerializeField] private Button cookPizzaButton; // button to cook the pizza (turns golden)
    [SerializeField] private Button servePizzaButton; 
    
    [Header("Settings")]
    [SerializeField] private float sauceBlobSize = 0.3f; // how big each sauce blob appears (bigger for paint effect)
    [SerializeField] private float cheeseSize = 0.08f; // how big cheese pieces appear (small for sprinkle effect)
    [SerializeField] private float pepperoniSize = 0.2f; // how big pepperoni slices appear
    [SerializeField] private float cornSize = 0.05f; // how big corn kernels appear
    [SerializeField] private float olivesSize = 0.15f; // how big olive slices appear
    
    [Header("Sauce Paint Settings")]
    [SerializeField] private float saucePaintDistance = 0.2f; // minimum distance between sauce blobs for smooth painting
    [SerializeField] private float sauceAlpha = 0.8f; // transparency level for sauce blobs
    [SerializeField] private int maxSauceBlobs = 500; 
    [SerializeField] private float pizzaRadius = 2.0f; // radius of your pizza for circular sauce boundary
    [SerializeField] private float sauceBoundaryPercent = 0.85f; // how much of pizza radius sauce covers (85% = stays away from crust)
    
    [Header("Animation Settings")]
    [SerializeField] private float slideTime = 1.5f; // how long pizza slide animation takes
    [SerializeField] private float slideDistance = 10f; // how far pizza slides off screen
    
    [Header("Visual Feedback")]
    [SerializeField] private Color selectedButtonColor = Color.yellow; // color when button is selected
    [SerializeField] private Color normalButtonColor = Color.white; // normal button color
    
    // core game state variables
    private bool sauceModeActive = false; // tracks if we're in sauce spreading mode
    private Camera mainCamera; 
    private List<GameObject> currentIngredients = new List<GameObject>(); // list of all ingredients on current pizza
    private Vector3 lastSaucePosition; 
    private Vector3 originalPizzaPosition; // stores pizza's starting position for slide animation
    private int currentSauceBlobCount = 0; // tracks sauce blob count for performance
    
    // tracks which ingredient is currently selected (pepperoni, corn, etc.)
    private IngredientType? selectedIngredient = null;
    
    // tracks what ingredients are currently on the pizza (for customer orders)
    private List<IngredientType> pizzaIngredients = new List<IngredientType>();
    
    // button management for visual feedback
    private Dictionary<IngredientType, Button> ingredientButtons;
    private Dictionary<Button, Color> originalButtonColors;
    
    // defines the different types of ingredients we can place
    public enum IngredientType
    {
        TomatoSauce, // red sauce
        Cheese,      // cheese layer
        Pepperoni,   // pepperoni slices
        Corn,        // corn kernels
        Olives       // olive slices
    }
    
    // events for better integration with other systems
    public System.Action<IngredientType> OnIngredientPlaced;
    public System.Action OnPizzaCooked;
    public System.Action OnPizzaServed;
    public System.Action OnNewPizzaCreated;
    
    // runs once when the game starts
    void Start()
    {
        InitializeComponents(); // setup all components and references
        SetupUI(); // connect button clicks to functions
        StoreOriginalValues(); // store original values for resetting
        CreateNewPizza(); // start with a fresh pizza
    }
    
    // runs every frame while the game is playing
    void Update()
    {
        HandleSauceInput(); // check if player is spreading sauce
        HandleIngredientPlacement(); // check if player is placing ingredients
    }
    
    // setup all components and initialize the game
    private void InitializeComponents()
    {
        mainCamera = Camera.main; // get reference to the main camera
        
        if (mainCamera == null)
        {
            Debug.LogError("No main camera found! Please tag your camera as 'MainCamera'");
        }
        
        // store original pizza position for slide animation
        if (pizzaDoughObject != null)
        {
            originalPizzaPosition = pizzaDoughObject.transform.position;
        }
        
        // initialize button mappings for easy management
        ingredientButtons = new Dictionary<IngredientType, Button>
        {
            { IngredientType.Cheese, cheeseButton },
            { IngredientType.Pepperoni, pepperoniButton },
            { IngredientType.Corn, cornButton },
            { IngredientType.Olives, olivesButton }
        };
        
        originalButtonColors = new Dictionary<Button, Color>();
    }
    
    // connects button clicks to the functions they should run
    void SetupUI()
    {
        // store original button colors for visual feedback
        StoreOriginalButtonColors();
        
        // when sauce button is clicked, runs ToggleSauceMode function
        if (sauceButton != null)
        {
            sauceButton.onClick.AddListener(() => {
                PlayButtonClickSound(); // play click sound effect
                ToggleSauceMode(); // switch sauce mode on/off
            });
        }
        
        // when ingredient buttons are clicked, select that ingredient type
        foreach (var kvp in ingredientButtons)
        {
            if (kvp.Value != null)
            {
                var ingredientType = kvp.Key; // capture for closure
                kvp.Value.onClick.AddListener(() => {
                    PlayButtonClickSound(); // play click sound
                    SelectIngredient(ingredientType); // select this ingredient for placement
                });
            }
        }
        
        // utility buttons
        if (newPizzaButton != null)
        {
            newPizzaButton.onClick.AddListener(() => {
                PlayButtonClickSound(); // click sound
                AudioManager.Instance?.PlayNewPizzaSound(); // special new pizza sound
                CreateNewPizza(); // clear everything and start fresh
            });
        }
        
        if (cookPizzaButton != null)
        {
            cookPizzaButton.onClick.AddListener(() => {
                PlayButtonClickSound(); // click sound
                CookPizza(); // start cooking the pizza
            });
        }
        
        // serve pizza button for customer orders
        if (servePizzaButton != null)
        {
            servePizzaButton.onClick.AddListener(() => {
                PlayButtonClickSound(); // click sound
                ServePizzaToCustomer(); // give pizza to waiting customer
            });
        }
    }
    
    // store original button colors for visual feedback system
    private void StoreOriginalButtonColors()
    {
        var allButtons = new List<Button> { 
            sauceButton, cheeseButton, pepperoniButton, cornButton, olivesButton, 
            newPizzaButton, cookPizzaButton, servePizzaButton 
        };
        
        foreach (var button in allButtons)
        {
            if (button != null)
            {
                originalButtonColors[button] = button.colors.normalColor; // remember original color
            }
        }
    }
    
    // store original values for proper resetting
    private void StoreOriginalValues()
    {
        if (pizzaDoughObject != null)
        {
            originalPizzaPosition = pizzaDoughObject.transform.position; // remember starting position
        }
    }
    
    // handles sauce spreading when in sauce mode
    void HandleSauceInput()
    {
        if (!sauceModeActive) return; // early exit if sauce mode not active
        
        // if sauce mode is on AND left mouse button is held down
        if (Input.GetMouseButton(0))
        {
            Vector3 worldPos = GetMouseWorldPosition(); // get mouse position in world space
            if (worldPos != Vector3.zero && ShouldPlaceSauceBlob(worldPos)) // check if we should place sauce here
            {
                SpreadSauce(worldPos); // create sauce at that position
                lastSaucePosition = worldPos; // remember this position for smooth painting
            }
        }
        
        // reset last position when mouse is released (for next paint stroke)
        if (Input.GetMouseButtonUp(0))
        {
            lastSaucePosition = Vector3.zero;
        }
    }
    
    // handles placing ingredients when one is selected
    void HandleIngredientPlacement()
    {
        // if an ingredient is selected AND left mouse button is clicked (not held)
        if (selectedIngredient.HasValue && Input.GetMouseButtonDown(0))
        {
            Vector3 worldPos = GetMouseWorldPosition(); // get mouse position in world space
            if (worldPos != Vector3.zero)
            {
                PlaceIngredient(selectedIngredient.Value, worldPos); // place the ingredient at mouse position
                // note: we don't clear selectedIngredient so player can place multiple pieces
            }
        }
    }
    
    // helper method to get mouse world position with null checking
    private Vector3 GetMouseWorldPosition()
    {
        if (mainCamera == null) return Vector3.zero; // safety check
        
        Vector3 mousePos = Input.mousePosition; // get mouse position on screen
        // convert screen position to world position (where objects actually are)
        return mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
    }
    
    // check if we should place a sauce blob at this position (for smooth painting)
    private bool ShouldPlaceSauceBlob(Vector3 worldPos)
    {
        // only check distance, removed the blob count limit that was causing sauce to stop
        return Vector3.Distance(worldPos, lastSaucePosition) >= saucePaintDistance || lastSaucePosition == Vector3.zero;
    }
    
    // helper method to check if position is on pizza
    private bool IsPositionOnPizza(Vector3 position)
    {
        if (pizzaDoughObject == null) return false; // safety check
        
        // check if the position is over the pizza using collider bounds
        Collider2D pizzaCollider = pizzaDoughObject.GetComponent<Collider2D>();
        return pizzaCollider != null && pizzaCollider.bounds.Contains(position);
    }
    
    // clears the pizza and starts fresh
    public void CreateNewPizza()
    {
        // stop any ongoing effects
        ParticleEffectsManager.Instance?.StopAllEffects();
        
        ClearCurrentIngredients(); // remove all ingredients from pizza
        ResetPizzaState(); // reset pizza to starting state
        ResetUI(); // reset UI visual states
        
        OnNewPizzaCreated?.Invoke(); // notify other systems that we made a new pizza
        Debug.Log("new pizza created!"); // log message for debugging in the terminal
    }
    
    // remove all ingredients from the current pizza
    private void ClearCurrentIngredients()
    {
        // destroy all ingredients currently on the pizza
        foreach (GameObject ingredient in currentIngredients)
        {
            if (ingredient != null) // make sure the ingredient still exists
                DestroyImmediate(ingredient); // remove it from the scene immediately
        }
        
        currentIngredients.Clear(); // empty our ingredient list
        pizzaIngredients.Clear(); 
        currentSauceBlobCount = 0; // reset sauce blob count for performance tracking
    }
    
    // reset pizza back to its starting state
    private void ResetPizzaState()
    {
        // reset sauce painting position
        lastSaucePosition = Vector3.zero;
        
        // reset states but keep button colors
        sauceModeActive = false;
        selectedIngredient = null;
        
        // reset the pizza dough to its original state
        if (pizzaDoughObject != null)
        {
            // make sure pizza is at original position
            pizzaDoughObject.transform.position = originalPizzaPosition;
            
            SpriteRenderer doughRenderer = pizzaDoughObject.GetComponent<SpriteRenderer>();
            if (doughRenderer != null && doughSprite != null)
            {
                doughRenderer.sprite = doughSprite; // reset to original dough image
                doughRenderer.color = Color.white; // reset to uncooked color
            }
            
            // reset cooking state
            PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
            if (doughComponent != null)
            {
                doughComponent.ResetPizza(); // tell pizza dough to reset itself
            }
        }
    }
    
    // reset UI visual states
    private void ResetUI()
    {
        UpdateButtonVisualStates(); // update button colors to show current state
    }
    
    // toggles sauce spreading mode on/off with visual feedback
    void ToggleSauceMode()
    {
        sauceModeActive = !sauceModeActive; // flip between true and false
        selectedIngredient = null; // clear any selected ingredient
        
        // reset sauce painting when switching modes
        lastSaucePosition = Vector3.zero;
        
        UpdateButtonVisualStates(); // update button colors to show current mode
        
        // log current state for debugging
        Debug.Log("sauce mode: " + (sauceModeActive ? "ON - click and drag on pizza to paint sauce!" : "OFF"));
    }
    
    // selects an ingredient type for placement with visual feedback
    void SelectIngredient(IngredientType ingredientType)
    {
        sauceModeActive = false; // turn off sauce mode
        selectedIngredient = ingredientType; // remember which ingredient was selected
        
        // reset sauce painting when switching to ingredients
        lastSaucePosition = Vector3.zero;
        
        UpdateButtonVisualStates(); // update button colors to show selected ingredient
        
        // log what was selected for debugging
        Debug.Log($"selected {ingredientType} - now click on pizza to place!");
    }
    
    // update button colors to show current state
    private void UpdateButtonVisualStates()
    {
        // reset all buttons to normal color first
        foreach (var kvp in originalButtonColors)
        {
            SetButtonColor(kvp.Key, kvp.Value); // restore original color
        }
        
        // highlight the currently active button
        if (sauceModeActive && sauceButton != null)
        {
            SetButtonColor(sauceButton, selectedButtonColor); // highlight sauce button
        }
        else if (selectedIngredient.HasValue && ingredientButtons.ContainsKey(selectedIngredient.Value))
        {
            SetButtonColor(ingredientButtons[selectedIngredient.Value], selectedButtonColor); // highlight selected ingredient button
        }
    }
    
    // helper method to set button color
    private void SetButtonColor(Button button, Color color)
    {
        if (button == null) return; // safety check
        
        var colors = button.colors; // get button color settings
        colors.normalColor = color; // change the normal color
        button.colors = colors; // apply the new color settings
    }
    
    // creates sauce blobs when mouse is dragged over pizza
    void SpreadSauce(Vector3 worldPosition)
    {
        // make sure we have a pizza and sauce image
        if (!IsPositionOnPizza(worldPosition) || tomatoSauceSprite == null) return;
        
        // create a sauce blob at this position with paint-style settings
        CreateSaucePaintBlob(tomatoSauceSprite, worldPosition, sauceBlobSize, 1);
        currentSauceBlobCount++; // track how many sauce blobs we've created
        
        // track that we added sauce to this pizza (only add once to ingredients list)
        if (!pizzaIngredients.Contains(IngredientType.TomatoSauce))
        {
            pizzaIngredients.Add(IngredientType.TomatoSauce); // add sauce to ingredient list for customer orders
        }
        
        // play audio and visual effects (less frequently than before for smoother painting)
        if (Random.Range(0f, 1f) < 0.3f) // only 30% chance to play effects so it's not too noisy
        {
            AudioManager.Instance?.PlayIngredientSound(IngredientType.TomatoSauce); // play sauce sound
            ParticleEffectsManager.Instance?.PlayIngredientEffect(worldPosition, IngredientType.TomatoSauce); // play sauce particles
        }
    }
    
    // creates a sauce blob optimized for paint-style spreading
    void CreateSaucePaintBlob(Sprite sprite, Vector3 position, float size, int sortingOrder)
    {
        // create a new gameobject for this sauce blob
        GameObject newSauceBlob = new GameObject("SauceBlob");
        newSauceBlob.transform.position = position; // place it at the mouse position
        newSauceBlob.transform.parent = pizzaDoughObject.transform; // make it a child of the pizza
        
        // add a sprite renderer to display the sauce image
        SpriteRenderer renderer = newSauceBlob.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; // set the sauce image
        renderer.sortingOrder = sortingOrder; 
        
        // make sauce semi-transparent so blobs blend together for smooth painting effect
        Color sauceColor = renderer.color;
        sauceColor.a = sauceAlpha; // set transparency level
        renderer.color = sauceColor;
        
        // set the size of the sauce blob
        newSauceBlob.transform.localScale = Vector3.one * size;
        
        // slight random rotation for natural look
        newSauceBlob.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        
        // add to our list so we can clear it later
        currentIngredients.Add(newSauceBlob);
    }
    
    // places an ingredient at the specified position
    void PlaceIngredient(IngredientType type, Vector3 position)
    {
        if (!IsPositionOnPizza(position)) return; // make sure click is on pizza
        
        // get the sprite, size, and layering info for this ingredient type
        Sprite ingredientSprite = GetIngredientSprite(type);
        float ingredientSize = GetIngredientSize(type);
        int sortingOrder = GetSortingOrder(type);
        
        // if we have a valid sprite, create the ingredient
        if (ingredientSprite != null)
        {
            CreateIngredientImage(ingredientSprite, position, ingredientSize, sortingOrder);
            
            // track this ingredient for customer orders
            pizzaIngredients.Add(type);
            
            // play audio and visual effects
            AudioManager.Instance?.PlayIngredientSound(type); // play ingredient-specific sound
            ParticleEffectsManager.Instance?.PlayIngredientEffect(position, type); // play ingredient particles
            OnIngredientPlaced?.Invoke(type); // notify other systems that ingredient was placed
            
            Debug.Log($"placed {type} on pizza!"); // log for debugging
        }
    }
    
    // creates a visual ingredient image at the specified position
    void CreateIngredientImage(Sprite sprite, Vector3 position, float size, int sortingOrder)
    {
        // create a new gameobject for this ingredient
        GameObject newIngredient = new GameObject("Ingredient");
        newIngredient.transform.position = position; // place it at the click position
        newIngredient.transform.parent = pizzaDoughObject.transform; // make it a child of the pizza
        
        // add a sprite renderer to display the ingredient image
        SpriteRenderer renderer = newIngredient.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite; // set the ingredient image
        renderer.sortingOrder = sortingOrder; // control what appears on top (sauce bottom, toppings top)
        
        // set the size of the ingredient
        newIngredient.transform.localScale = Vector3.one * size;
        
        // rotate randomly for a natural look
        newIngredient.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        
        // add to our list so we can clear it later
        currentIngredients.Add(newIngredient);
    }
    
    // returns the correct sprite image for each ingredient type
    Sprite GetIngredientSprite(IngredientType type)
    {
        // using modern switch expression for cleaner code
        return type switch
        {
            IngredientType.TomatoSauce => tomatoSauceSprite,
            IngredientType.Cheese => cheeseSprite,
            IngredientType.Pepperoni => pepperoniSprite,
            IngredientType.Corn => cornSprite,
            IngredientType.Olives => olivesSprite,
            _ => null // fallback if type not recognized
        };
    }
    
    // returns the correct size for each ingredient type
    float GetIngredientSize(IngredientType type)
    {
        // using modern switch expression for cleaner code
        return type switch
        {
            IngredientType.TomatoSauce => sauceBlobSize,
            IngredientType.Cheese => cheeseSize, // small size for sprinkle effect
            IngredientType.Pepperoni => pepperoniSize,
            IngredientType.Corn => cornSize, // very small for corn kernels
            IngredientType.Olives => olivesSize,
            _ => 0.3f // default size if type not recognized
        };
    }
    
    // returns the correct layering order for each ingredient type (cheese goes on sauce, under toppings)
    int GetSortingOrder(IngredientType type)
    {
        // using modern switch expression for cleaner code
        return type switch
        {
            IngredientType.TomatoSauce => 1, // sauce goes on bottom layer
            IngredientType.Cheese => 2,      // cheese goes on sauce layer
            IngredientType.Pepperoni => 4,   // pepperoni on top layer
            IngredientType.Corn => 3,        // corn above cheese layer
            IngredientType.Olives => 3,      // olives above cheese layer
            _ => 1 // default layer if type not recognized
        };
    }
    
    // starts the pizza cooking process
    void CookPizza()
    {
        if (pizzaDoughObject == null) return; // make sure pizza exists
        
        // play cooking start effects
        AudioManager.Instance?.PlayCookingStartSound(); // play cooking sound
        ParticleEffectsManager.Instance?.StartCookingEffect(pizzaDoughObject.transform.position); // start cooking particles
        
        // get the PizzaDough script component and tell it to start cooking
        PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
        if (doughComponent != null)
        {
            doughComponent.CookPizza(); // this will turn the dough golden brown over time
            OnPizzaCooked?.Invoke(); // notify other systems that cooking started
        }
        
        Debug.Log("pizza is cooking!"); // log for terminal debugging
    }
    
    // serves the current pizza to a waiting customer with slide animation
    void ServePizzaToCustomer()
    {
        if (pizzaDoughObject == null) return; // safety check
        
        // check if pizza is cooked
        PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
        bool isCooked = doughComponent != null && doughComponent.IsCooked(); // check cooking state
        
        // convert our ingredient list to array for customer system
        IngredientType[] ingredientArray = pizzaIngredients.ToArray();
        
        // try to serve to customer manager
        if (CustomerManager.Instance != null)
        {
            bool orderAccepted = CustomerManager.Instance.ServePizzaToCustomer(ingredientArray, isCooked);
            
            if (orderAccepted)
            {
                Debug.Log("pizza served to customer!"); // customer was happy
            }
            else
            {
                Debug.Log("customer rejected the pizza!"); // customer didn't like it
            }
            
            // always slide pizza away regardless of whether customer liked it
            StartCoroutine(SlidePizzaOffScreen());
            OnPizzaServed?.Invoke(); // notify other systems that pizza was served
        }
        else
        {
            Debug.LogError("CustomerManager not found! Make sure CustomerManager is in the scene.");
        }
    }
    
    // slides pizza off screen with smooth animation
    IEnumerator SlidePizzaOffScreen()
    {
        if (pizzaDoughObject == null) yield break; // safety check
        
        Vector3 startPosition = pizzaDoughObject.transform.position; // where pizza starts
        Vector3 endPosition = startPosition + Vector3.right * slideDistance; // slide to the right off screen
        float elapsed = 0f; // time tracking for animation
        
        // play slide sound effect
        AudioManager.Instance?.PlayButtonClickSound(); // placeholder sound for now
        
        // smoothly move pizza from current position to off-screen
        while (elapsed < slideTime)
        {
            elapsed += Time.deltaTime; // add time since last frame
            float progress = elapsed / slideTime; // calculate progress (0 to 1)
            
            // use smooth curve for nice animation
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);
            
            pizzaDoughObject.transform.position = Vector3.Lerp(startPosition, endPosition, smoothProgress); // move pizza
            
            yield return null; // wait for next frame
        }
        
        // make sure pizza ends up exactly at end position
        pizzaDoughObject.transform.position = endPosition;
        
        // wait a moment then create new pizza
        yield return new WaitForSeconds(0.5f);
        
        // create new pizza (this will reset position automatically)
        CreateNewPizza();
    }
    
    // helper method for playing button click sounds
    private void PlayButtonClickSound()
    {
        AudioManager.Instance?.PlayButtonClickSound(); // play click sound through audio manager
    }
    
    // public function to get current pizza ingredients (for debugging or UI)
    public List<IngredientType> GetCurrentPizzaIngredients()
    {
        return new List<IngredientType>(pizzaIngredients); // return copy of ingredient list
    }
    
    // public function to check if pizza is cooked
    public bool IsPizzaCooked()
    {
        if (pizzaDoughObject == null) return false; // safety check
        
        PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
        return doughComponent != null && doughComponent.IsCooked(); // check if pizza is cooked
    }
    
    // additional public API methods for external systems
    public bool IsSauceModeActive() => sauceModeActive; // returns true if sauce painting mode is active
    public IngredientType? GetSelectedIngredient() => selectedIngredient; // returns currently selected ingredient (or null)
    public int GetIngredientCount(IngredientType type) => pizzaIngredients.FindAll(x => x == type).Count; // count specific ingredient on pizza
    public int GetTotalIngredientCount() => currentIngredients.Count; // total number of ingredient objects on pizza
    public float GetSauceCoverage() => (float)currentSauceBlobCount / maxSauceBlobs; 
}