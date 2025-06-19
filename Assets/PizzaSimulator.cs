using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class PizzaSimulator : MonoBehaviour
{
    [Header("Pizza Components")]
    public GameObject pizzaDoughObject; // the actual pizza object in the scene
    public Transform ovenTransform; // where the pizza sits (empty gameobject for positioning)
    
    [Header("Ingredient Sprites (Drag PNG files here)")]
    public Sprite doughSprite; // image for the pizza base
    public Sprite tomatoSauceSprite; // red sauce blob image
    public Sprite pepperoniSprite; // pepperoni slice image
    public Sprite cornSprite; // corn kernel image
    public Sprite olivesSprite; // olive slice image
    
    [Header("UI Buttons")]
    public Button sauceButton; // button to activate sauce spreading mode
    public Button pepperoniButton; // button to select pepperoni placement
    public Button cornButton; // button to select corn placement
    public Button olivesButton; // button to select olive placement
    public Button newPizzaButton; // button to clear pizza and start over
    public Button cookPizzaButton; // button to cook the pizza (turns golden: code in pizza dough)
    
    [Header("Settings")]
    public float sauceBlobSize = 0.1f; // how big each sauce splat appears
    public float pepperoniSize = 0.2f; // how big pepperoni slices appear
    public float cornSize = 0.05f; // how big corn kernels appear
    public float olivesSize = 0.15f; // how big olive slices appear
    
    // private variables that the script uses internally
    private bool sauceModeActive = false; // tracks if we're in sauce spreading mode
    private Camera mainCamera; // reference to the main camera for mouse position calculations
    private List<GameObject> currentIngredients = new List<GameObject>(); // list of all ingredients on current pizza
    
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
        sauceButton.onClick.AddListener(() => ToggleSauceMode());
        
        // when ingredient buttons are clicked, select that ingredient type
        // button click settings
        pepperoniButton.onClick.AddListener(() => SelectIngredient(IngredientType.Pepperoni));
        cornButton.onClick.AddListener(() => SelectIngredient(IngredientType.Corn));
        olivesButton.onClick.AddListener(() => SelectIngredient(IngredientType.Olives));
        
        // utility buttons
        newPizzaButton.onClick.AddListener(CreateNewPizza); // clear pizza
        cookPizzaButton.onClick.AddListener(CookPizza); // cook the pizza
    }
    
    // runs every frame while the game is playing
    void Update()
    {
        HandleSauceInput(); // check if player is spreading sauce
        HandleIngredientPlacement(); // check if player is placing ingredients
    }
    
    // handles sauce spreading when in sauce mode
    void HandleSauceInput()
    {
        // if sauce mode is on AND left mouse button is held down
        if (sauceModeActive && Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition; // get mouse position on screen
            // convert screen position to world position (where objects actually are)
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10));
            SpreadSauce(worldPos); // create sauce at that position
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
        // destroy all ingredients currently on the pizza
        foreach (GameObject ingredient in currentIngredients)
        {
            if (ingredient != null) // make sure the ingredient still exists
                DestroyImmediate(ingredient); // remove it from the scene
        }
        currentIngredients.Clear(); // empty our ingredient list
        
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
        
        // change button color to show if sauce mode is active
        ColorBlock colors = sauceButton.colors;
        colors.normalColor = sauceModeActive ? Color.red : Color.white; // red when active
        sauceButton.colors = colors;
        
        // log current state for debugging
        Debug.Log("Sauce mode: " + (sauceModeActive ? "ON - Click and drag on pizza!" : "OFF"));
    }
    
    // creates sauce blobs when mouse is dragged over pizza
    void SpreadSauce(Vector3 worldPosition)
{
    if (pizzaDoughObject == null || tomatoSauceSprite == null) return;
    
    // check distance from pizza center instead of bounds
    float distanceFromCenter = Vector3.Distance(worldPosition, pizzaDoughObject.transform.position);
    if (distanceFromCenter <= 7f) 
    {
        CreateIngredientImage(tomatoSauceSprite, worldPosition, sauceBlobSize, 1);
    }
}
    
    // selects an ingredient type for placement
    void SelectIngredient(IngredientType ingredientType)
    {
        sauceModeActive = false; // turn off sauce mode
        selectedIngredient = ingredientType; // remember which ingredient was selected
        
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
        
        // get the PizzaDough script component and tell it to start cooking
        PizzaDough doughComponent = pizzaDoughObject.GetComponent<PizzaDough>();
        if (doughComponent != null)
        {
            doughComponent.CookPizza(); // this will turn the dough golden brown
        }
        
        Debug.Log("Pizza is cooking!"); // log for terminal
    }
}