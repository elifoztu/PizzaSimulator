using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


// things to work on:
// center pizza and buttons on the screen
// fix the steam and smoke effect 
// add a better background
// next steps will be adding customer interaction, pizza moving on an oven, more toppings, and so on

public class PizzaSimulator : MonoBehaviour
{
    [Header("Pizza Components")]
    [SerializeField] private GameObject pizzaDoughObject; // the actual pizza object in the scene
    [SerializeField] private Transform ovenTransform; // where the pizza sits (empty gameobject for positioning)
    
    [Header("Ingredient Sprites (Drag PNG files here)")]
    [SerializeField] private Sprite doughSprite; // image for the pizza base
    [SerializeField] private Sprite tomatoSauceSprite; // red sauce blob image
    [SerializeField] private Sprite pepperoniSprite; // pepperoni slice image
    [SerializeField] private Sprite cornSprite; // corn kernel image
    [SerializeField] private Sprite olivesSprite; // olive slice image
    
    [Header("UI Buttons")]
    [SerializeField] private Button sauceButton; // button to activate sauce spreading mode
    [SerializeField] private Button pepperoniButton; // button to select pepperoni placement
    [SerializeField] private Button cornButton; // button to select corn placement
    [SerializeField] private Button olivesButton; // button to select olive placement
    [SerializeField] private Button newPizzaButton; // button to clear pizza and start over
    [SerializeField] private Button cookPizzaButton; // button to cook the pizza (turns golden)
    
    [Header("Settings")]
    [SerializeField] private float sauceBlobSize = 0.3f; // how big each sauce blob appears (bigger for paint effect)
    [SerializeField] private float pepperoniSize = 0.2f; // how big pepperoni slices appear
    [SerializeField] private float cornSize = 0.05f; // how big corn kernels appear
    [SerializeField] private float olivesSize = 0.15f; // how big olive slices appear
    
    [Header("Sauce Paint Settings")]
    [SerializeField] private float saucePaintDistance = 0.2f; // minimum distance between sauce blobs for smooth painting
    [SerializeField] private float sauceAlpha = 0.8f;
    
    private bool sauceModeActive = false; // tracks if we're in sauce spreading mode
    private Camera mainCamera; // reference to the main camera for mouse position calculations
    private List<GameObject> currentIngredients = new List<GameObject>(); // list of all ingredients on current pizza
    private Vector3 lastSaucePosition; // tracks last sauce position to ensure smooth painting
    
    // tracks which ingredient is currently selected (pepperoni, corn, etc.)
    private IngredientType? selectedIngredient = null;
    
    // defines the different types of ingredients we can place
    public enum IngredientType
    {
        TomatoSauce, // red sauce
        Pepperoni,   // pepperoni slices
        Corn,        // corn kernels
        Olives       // olive slices
    }
    
    // runs once when the game starts
    void Start()
    {
        mainCamera = Camera.main; // get reference to the main camera
        SetupUI(); // connect button clicks to functions
        CreateNewPizza(); // start with a fresh pizza
    }
    
    // connects button clicks to the functions they should run
    void SetupUI()
    {
        // when sauce button is clicked, runs ToggleSauceMode function
        sauceButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            ToggleSauceMode();
        });
        
        // when ingredient buttons are clicked, select that ingredient type
        pepperoniButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            SelectIngredient(IngredientType.Pepperoni);
        });
        
        cornButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            SelectIngredient(IngredientType.Corn);
        });
        
        olivesButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            SelectIngredient(IngredientType.Olives);
        });
        
        // utility buttons
        newPizzaButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            AudioManager.Instance?.PlayNewPizzaSound();
            CreateNewPizza();
        });
        
        cookPizzaButton.onClick.AddListener(() => {
            AudioManager.Instance?.PlayButtonClickSound();
            CookPizza();
        });
    }
    
    // runs every frame while the game is playing
    void Update()
    {
        HandleSauceInput(); // check if player is spreading sauce
        HandleIngredientPlacement(); // check if player is placing ingredients
    }
    
    // handles sauce spreading when in sauce mode - now with paint-style smooth spreading
    void HandleSauceInput()
    {
        // if sauce mode is on AND left mouse button is held down
        if (sauceModeActive && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition; // get mouse position on screen
            // convert screen position to world position (where objects actually are)
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            
            // only place sauce if we've moved far enough from last position (creates smooth painting)
            if (Vector3.Distance(worldPos, lastSaucePosition) >= saucePaintDistance || lastSaucePosition == Vector3.zero)
            {
                SpreadSauce(worldPos); // create sauce at that position
                lastSaucePosition = worldPos; // remember this position
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
            Vector3 mousePos = Input.mousePosition; // get mouse position on screen
            // convert screen position to world position
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            PlaceIngredient(selectedIngredient.Value, worldPos); // place the ingredient
            // note: we don't clear selectedIngredient so player can place multiple
        }
    }
    
    // clears the pizza and starts fresh
    void CreateNewPizza()
    {
        // stop any ongoing effects
        ParticleEffectsManager.Instance?.StopAllEffects();
        
        // destroy all ingredients currently on the pizza
        foreach (GameObject ingredient in currentIngredients)
        {
            if (ingredient != null) // make sure the ingredient still exists
                DestroyImmediate(ingredient); // remove it from the scene
        }
        currentIngredients.Clear(); // empty our ingredient list
        
        // reset sauce painting position
        lastSaucePosition = Vector3.zero;
        
        // reset the pizza dough to its original state
        if (pizzaDoughObject != null)
        {
            SpriteRenderer doughRenderer = pizzaDoughObject.GetComponent<SpriteRenderer>();
            if (doughRenderer != null && doughSprite != null)
            {
                doughRenderer.sprite = doughSprite; // reset to original dough image
                doughRenderer.color = Color.white; // reset to uncooked color
            }
        }
        
        Debug.Log("New pizza created!"); // log message for debugging in the terminal
    }
    
    // toggles sauce spreading mode on/off
    void ToggleSauceMode()
    {
        sauceModeActive = !sauceModeActive; // flip between true and false
        selectedIngredient = null; // clear any selected ingredient
        
        // reset sauce painting when switching modes
        lastSaucePosition = Vector3.zero;
        
        // change button color to show if sauce mode is active
        ColorBlock colors = sauceButton.colors;
        colors.normalColor = sauceModeActive ? Color.red : Color.white; // red when active
        sauceButton.colors = colors;
        
        // log current state for debugging
        Debug.Log("Sauce mode: " + (sauceModeActive ? "ON - Click and drag on pizza to paint sauce!" : "OFF"));
    }
    
    // creates sauce blobs when mouse is dragged over pizza - now creates smooth paint-style sauce
    void SpreadSauce(Vector3 worldPosition)
    {
        // make sure we have a pizza and sauce image
        if (pizzaDoughObject == null || tomatoSauceSprite == null) return;
        
        // check if the mouse position is over the pizza
        Collider2D pizzaCollider = pizzaDoughObject.GetComponent<Collider2D>();
        if (pizzaCollider != null && pizzaCollider.bounds.Contains(worldPosition))
        {
            // create a sauce blob at this position with paint-style settings
            CreateSaucePaintBlob(tomatoSauceSprite, worldPosition, sauceBlobSize, 1);
            
            // play audio and visual effects (less frequently than before for smoother painting)
            if (Random.Range(0f, 1f) < 0.3f) // only 30% chance to play effects so it's not too noisy
            {
                AudioManager.Instance?.PlayIngredientSound(IngredientType.TomatoSauce);
                ParticleEffectsManager.Instance?.PlayIngredientEffect(worldPosition, IngredientType.TomatoSauce);
            }
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
        renderer.sprite = sprite; // set the image
        renderer.sortingOrder = sortingOrder; // control what appears on top
        
        // make sauce semi-transparent so blobs blend together for smooth painting effect
        Color sauceColor = renderer.color;
        sauceColor.a = sauceAlpha; // set transparency
        renderer.color = sauceColor;
        
        // set the size of the sauce blob (bigger than before for smoother coverage)
        newSauceBlob.transform.localScale = Vector3.one * size;
        
        // slight random rotation for natural look
        newSauceBlob.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        
        // add to our list so we can clear it later
        currentIngredients.Add(newSauceBlob);
    }
    
    // selects an ingredient type for placement
    void SelectIngredient(IngredientType ingredientType)
    {
        sauceModeActive = false; // turn off sauce mode
        selectedIngredient = ingredientType; // remember which ingredient was selected
        
        // reset sauce painting when switching to ingredients
        lastSaucePosition = Vector3.zero;
        
        // reset sauce button color since we're not in sauce mode anymore
        ColorBlock colors = sauceButton.colors;
        colors.normalColor = Color.white;
        sauceButton.colors = colors;
        
        // log what was selected for debugging
        Debug.Log($"Selected {ingredientType} - now click on pizza to place!");
    }
    
    // places an ingredient at the specified position
    void PlaceIngredient(IngredientType type, Vector3 position)
    {
        if (pizzaDoughObject == null) return; // make sure pizza exists
        
        // check if the click position is over the pizza
        Collider2D pizzaCollider = pizzaDoughObject.GetComponent<Collider2D>();
        if (pizzaCollider != null && pizzaCollider.bounds.Contains(position))
        {
            // get the sprite, size, and layering info for this ingredient type
            Sprite ingredientSprite = GetIngredientSprite(type);
            float ingredientSize = GetIngredientSize(type);
            int sortingOrder = GetSortingOrder(type);
            
            // if we have a valid sprite, create the ingredient
            if (ingredientSprite != null)
            {
                CreateIngredientImage(ingredientSprite, position, ingredientSize, sortingOrder);
                
                // play audio and visual effects
                AudioManager.Instance?.PlayIngredientSound(type);
                ParticleEffectsManager.Instance?.PlayIngredientEffect(position, type);
                
                Debug.Log($"Placed {type} on pizza!"); // log for debugging
            }
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
        renderer.sprite = sprite; // set the image
        renderer.sortingOrder = sortingOrder; // control what appears on top
        
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
        switch (type)
        {
            case IngredientType.TomatoSauce: return tomatoSauceSprite;
            case IngredientType.Pepperoni: return pepperoniSprite;
            case IngredientType.Corn: return cornSprite;
            case IngredientType.Olives: return olivesSprite;
            default: return null; // fallback if type not recognized
        }
    }
    
    // returns the correct size for each ingredient type
    float GetIngredientSize(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.TomatoSauce: return sauceBlobSize;
            case IngredientType.Pepperoni: return pepperoniSize;
            case IngredientType.Corn: return cornSize;
            case IngredientType.Olives: return olivesSize;
            default: return 0.3f; // default size if type not recognized
        }
    }
    
    // returns the correct layering order for each ingredient type
    int GetSortingOrder(IngredientType type)
    {
        switch (type)
        {
            case IngredientType.TomatoSauce: return 1; // sauce goes on bottom
            case IngredientType.Pepperoni: return 3;   // pepperoni on top
            case IngredientType.Corn: return 2;        // corn in middle
            case IngredientType.Olives: return 2;      // olives in middle
            default: return 1; // default layer if type not recognized
        }
    }
    
    // starts the pizza cooking process
    void CookPizza()
    {
        if (pizzaDoughObject == null) return; // make sure pizza exists
        
        // play cooking start effects
        AudioManager.Instance?.PlayCookingStartSound();
        ParticleEffectsManager.Instance?.StartCookingEffect(pizzaDoughObject.transform.position);
        
        // get the PizzaDough script component and tell it to start cooking
        PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
        if (doughComponent != null)
        {
            doughComponent.CookPizza(); // this will turn the dough golden brown
        }
        
        Debug.Log("Pizza is cooking!"); // log for terminal
    }
}