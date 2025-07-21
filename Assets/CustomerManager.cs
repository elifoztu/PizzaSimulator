using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class CustomerManager : MonoBehaviour
{
    [Header("Order Display UI")]
    [SerializeField] private TextMeshProUGUI currentOrderText; // shows current customer order
    [SerializeField] private TextMeshProUGUI customerNameText; // shows customer name
    [SerializeField] private Slider patienceSlider; // shows how patient customer is
    [SerializeField] private Image patienceSliderFill; // to change patience bar color
    
    [Header("Game Stats UI")]
    [SerializeField] private TextMeshProUGUI moneyText; // displays current money
    [SerializeField] private TextMeshProUGUI ordersCompletedText; // displays successful orders
    [SerializeField] private TextMeshProUGUI dailyGoalText; // displays daily goal progress
    
    [Header("Game Settings")]
    [SerializeField] private float customerInterval = 20f; // seconds between new customers
    [SerializeField] private float customerPatience = 45f; 
    [SerializeField] private int dailyGoalMoney = 50; // money needed to complete the day
    [SerializeField] private int startingMoney = 0; // money player starts with
    
    [Header("Visual Feedback")]
    [SerializeField] private Color happyTextColor = Color.green;
    [SerializeField] private Color angryTextColor = Color.red; // color for angry customer messages
    [SerializeField] private Color normalTextColor = Color.white; // normal text color
    [SerializeField] private float messageDisplayTime = 2f; // how long to show customer reaction messages
    
    // singleton pattern for easy access
    public static CustomerManager Instance { get; private set; }
    
    // game state tracking
    private int currentMoney; // how much money player has earned
    private int ordersCompleted; // successful orders served
    private int ordersFailed; // failed orders (for stats)
    private bool gameActive = true; // whether the game is running
    
    // current customer state
    private CustomerOrder currentOrder; // what the current customer wants
    private string currentCustomerName; // current customer's name
    private float currentPatience; // how much patience current customer has left
    private bool hasActiveCustomer = false; // whether there's currently a customer waiting
    
    // customer names for variety
    private readonly string[] customerNames = {
        "Alice", "Bob", "Charlie", "Diana", "Emma", "Frank", "Grace", "Henry",
        "Ivy", "Jack", "Kate", "Leo", "Mia", "Noah", "Olivia", "Paul",
        "Quinn", "Rachel", "Sam", "Tina", "Victor", "Wendy", "Xavier", "Yara", "Zoe"
    };
    
    // events for better decoupling between systems
    public System.Action<int> OnMoneyChanged;
    public System.Action<int> OnOrderCompleted;
    public System.Action OnDayCompleted;
    
    #region Customer Order System
    
    // represents what pizza a customer wants
    [System.Serializable]
    public class CustomerOrder
    {
        // using proper flags enum with bitwise operations for cleaner code
        [System.Flags]
        public enum ToppingsFlags
        {
            None = 0,
            Sauce = 1,
            Cheese = 2,
            Pepperoni = 4,
            Corn = 8,
            Olives = 16
        }
        
        public ToppingsFlags requiredToppings; // what toppings the customer wants (using flags)
        public int tipAmount; // bonus money if order is perfect
        public int basePayment = 10; // configurable base payment
        
        // properties for easy access to individual toppings
        public bool WantsSauce => (requiredToppings & ToppingsFlags.Sauce) != 0;
        public bool WantsCheese => (requiredToppings & ToppingsFlags.Cheese) != 0;
        public bool WantsPepperoni => (requiredToppings & ToppingsFlags.Pepperoni) != 0;
        public bool WantsCorn => (requiredToppings & ToppingsFlags.Corn) != 0;
        public bool WantsOlives => (requiredToppings & ToppingsFlags.Olives) != 0;
        public bool WantsCooked => true; // customers always want their pizza cooked
        
        // generates a random pizza order
        public static CustomerOrder GenerateRandomOrder()
        {
            var order = new CustomerOrder();
            
            // using bitwise operations for cleaner flag setting
            // sauce is very common (90% chance)
            if (Random.value < 0.9f) order.requiredToppings |= ToppingsFlags.Sauce;
            
            // cheese is very common too (85% chance)
            if (Random.value < 0.85f) order.requiredToppings |= ToppingsFlags.Cheese;
            
            // each topping has a 50% chance
            if (Random.value < 0.5f) order.requiredToppings |= ToppingsFlags.Pepperoni;
            if (Random.value < 0.5f) order.requiredToppings |= ToppingsFlags.Corn;
            if (Random.value < 0.5f) order.requiredToppings |= ToppingsFlags.Olives;
            
            // random tip amount
            order.tipAmount = Random.Range(0, 6);
            
            return order;
        }
        
        // converts order to readable text
        public string ToOrderString()
        {
            var ingredients = new List<string>();
            
            if (WantsSauce) ingredients.Add("sauce");
            if (WantsCheese) ingredients.Add("cheese");
            if (WantsPepperoni) ingredients.Add("pepperoni");
            if (WantsCorn) ingredients.Add("corn");
            if (WantsOlives) ingredients.Add("olives");
            
            if (ingredients.Count == 0)
                return "I want just plain dough, cooked please!";
            
            if (ingredients.Count == 1)
                return $"I want {ingredients[0]} pizza, cooked!";
            
            string ingredientList = string.Join(", ", ingredients.ToArray(), 0, ingredients.Count - 1);
            return $"I want {ingredientList} and {ingredients[ingredients.Count - 1]} pizza, cooked!";
        }
        
        // calculate payment with time bonus for better game mechanics
        public int CalculatePayment(float remainingPatience, float maxPatience)
        {
            int timeBonus = Mathf.RoundToInt(remainingPatience / maxPatience * 5f); // max 5 bonus for speed
            return basePayment + tipAmount + timeBonus;
        }
    }
    
    #endregion
    
    #region Unity Lifecycle
    
    void Awake()
    {
        InitializeSingleton();
        currentMoney = startingMoney;
    }
    
    void Start()
    {
        HideOrderUI();
        UpdateUI();
        
        if (gameActive)
        {
            StartCoroutine(CustomerCycle());
        }
        
        Debug.Log($"restaurant opened! daily goal: ${dailyGoalMoney}");
    }
    
    void Update()
    {
        if (hasActiveCustomer)
        {
            UpdateCustomerPatience();
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
        }
        else
        {
            Debug.LogWarning("Multiple CustomerManager instances detected. Destroying duplicate.");
            Destroy(gameObject);
        }
    }
    
    #endregion
    
    #region Customer Lifecycle
    
    // main customer cycle - spawns customers at intervals
    IEnumerator CustomerCycle()
    {
        while (gameActive)
        {
            yield return new WaitForSeconds(customerInterval);
            
            if (!hasActiveCustomer)
            {
                SpawnNewCustomer();
            }
        }
    }
    
    // creates a new customer with random order, this one is important
    void SpawnNewCustomer()
    {
        currentOrder = CustomerOrder.GenerateRandomOrder();
        currentCustomerName = customerNames[Random.Range(0, customerNames.Length)];
        currentPatience = customerPatience;
        hasActiveCustomer = true;
        
        ShowOrderUI();
        PlayCustomerArrivalSound();
        
        Debug.Log($"{currentCustomerName} wants: {currentOrder.ToOrderString()}");
    }
    
    private void PlayCustomerArrivalSound()
    {
        AudioManager.Instance?.PlayButtonClickSound(); // could be replaced with customer arrival sound
    }
    
    #endregion
    
    #region UI Management
    
    // shows the order UI at top of screen
    void ShowOrderUI()
    {
        SetUIElementActive(currentOrderText, currentOrder.ToOrderString(), normalTextColor);
        SetUIElementActive(customerNameText, $"{currentCustomerName} says:", normalTextColor);
        
        if (patienceSlider != null)
        {
            patienceSlider.maxValue = customerPatience;
            patienceSlider.value = currentPatience;
            patienceSlider.gameObject.SetActive(true);
        }
    }
    
    // hides the order UI
    void HideOrderUI()
    {
        if (currentOrderText != null)
            currentOrderText.gameObject.SetActive(false);
        
        if (customerNameText != null)
            customerNameText.gameObject.SetActive(false);
        
        if (patienceSlider != null)
            patienceSlider.gameObject.SetActive(false);
    }
    
    // helper method for text UI element management with color support
    private void SetUIElementActive(TextMeshProUGUI textElement, string text, Color? color = null, bool active = true)
    {
        if (textElement != null)
        {
            textElement.text = text;
            if (color.HasValue) textElement.color = color.Value;
            textElement.gameObject.SetActive(active);
        }
    }
    
    // updates the UI with current game stats
    void UpdateUI()
    {
        if (moneyText != null)
            moneyText.text = $"Money: ${currentMoney}";
        
        if (ordersCompletedText != null)
            ordersCompletedText.text = $"Orders: {ordersCompleted}";
        
        if (dailyGoalText != null)
            dailyGoalText.text = $"Goal: ${currentMoney}/{dailyGoalMoney}";
    }
    
    #endregion
    
    #region Patience System
    
    // updates customer patience and patience bar
    void UpdateCustomerPatience()
    {
        currentPatience -= Time.deltaTime;
        
        if (patienceSlider != null)
        {
            patienceSlider.value = currentPatience;
            
            if (patienceSliderFill != null)
            {
                patienceSliderFill.color = GetPatienceColor(currentPatience / customerPatience);
            }
        }
        
        if (currentPatience <= 0f)
        {
            CustomerLeavesAngry("Ugh! This is taking forever! I'm leaving!", true);
        }
    }
    
    // helper method to get patience color based on ratio
    private Color GetPatienceColor(float patienceRatio)
    {
        if (patienceRatio > 0.6f) return Color.green; // calm
        if (patienceRatio > 0.3f) return Color.yellow; // getting impatient
        return Color.red; // very impatient
    }
    
    #endregion
    
    #region Order Processing
    
    // public function for pizza simulator to serve pizza to current customer
    public bool ServePizzaToCustomer(PizzaSimulator.IngredientType[] ingredients, bool isCooked)
    {
        if (!hasActiveCustomer)
        {
            Debug.Log("no customer is waiting!");
            return false;
        }
        
        bool orderCorrect = CheckOrderCorrectness(ingredients, isCooked);
        
        if (orderCorrect)
        {
            CustomerLeavesHappy();
            return true;
        }
        else
        {
            CustomerLeavesAngry("This isn't what I ordered! I'm not paying for this!", false);
            return false;
        }
    }
    
    // checks if the served pizza matches the customer's order
    bool CheckOrderCorrectness(PizzaSimulator.IngredientType[] ingredients, bool isCooked)
    {
        if (isCooked != currentOrder.WantsCooked) return false;
        
        // create efficient lookup for better performance
        var ingredientSet = new System.Collections.Generic.HashSet<PizzaSimulator.IngredientType>(ingredients);
        
        // check if ingredients match order exactly using properties
        return (ingredientSet.Contains(PizzaSimulator.IngredientType.TomatoSauce) == currentOrder.WantsSauce &&
                ingredientSet.Contains(PizzaSimulator.IngredientType.Cheese) == currentOrder.WantsCheese &&
                ingredientSet.Contains(PizzaSimulator.IngredientType.Pepperoni) == currentOrder.WantsPepperoni &&
                ingredientSet.Contains(PizzaSimulator.IngredientType.Corn) == currentOrder.WantsCorn &&
                ingredientSet.Contains(PizzaSimulator.IngredientType.Olives) == currentOrder.WantsOlives);
    }
    
    #endregion
    
    #region Customer Reactions
    
    // customer leaves happy when order is correct
    void CustomerLeavesHappy()
    {
        SetUIElementActive(currentOrderText, "Yum! Thank you so much! This is perfect!", happyTextColor);
        
        Debug.Log($"{currentCustomerName} is happy! correct order!");
        
        // calculate payment using method with time bonus
        int payment = currentOrder.CalculatePayment(currentPatience, customerPatience);
        AddMoney(payment);
        ordersCompleted++;
        
        OnOrderCompleted?.Invoke(ordersCompleted);
        UpdateUI();
        CheckDailyGoal();
        
        Debug.Log($"earned ${payment}! (includes time bonus for fast service)");
        
        AudioManager.Instance?.PlayCookingCompleteSound();
        StartCoroutine(HideOrderUIAfterDelay(messageDisplayTime));
    }
    
    // unified method for customer leaving angry with different reasons
    private void CustomerLeavesAngry(string message, bool isTimeout)
    {
        SetUIElementActive(currentOrderText, message, angryTextColor);
        
        Debug.Log($"{currentCustomerName} left angry! {(isTimeout ? "Too slow!" : "Wrong order!")}");
        
        AddMoney(-5);
        ordersFailed++;
        
        UpdateUI();
        
        AudioManager.Instance?.PlayButtonClickSound(); // placeholder for angry customer sound
        StartCoroutine(HideOrderUIAfterDelay(messageDisplayTime));
    }
    
    // hides order UI after a delay and sets up for next customer
    IEnumerator HideOrderUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hasActiveCustomer = false;
        HideOrderUI();
    }
    
    #endregion
    
    #region Money Management
    
    // organized money handling with events
    private void AddMoney(int amount)
    {
        currentMoney = Mathf.Max(0, currentMoney + amount);
        OnMoneyChanged?.Invoke(currentMoney);
    }
    
    #endregion
    
    #region Game Progression
    
    // checks if player has reached the daily goal
    void CheckDailyGoal()
    {
        if (currentMoney >= dailyGoalMoney && gameActive)
        {
            CompleteDay();
        }
    }
    
    // called when player reaches the daily money goal
    void CompleteDay()
    {
        gameActive = false;
        OnDayCompleted?.Invoke();
        Debug.Log($"congratulations! you reached the daily goal of ${dailyGoalMoney}!");
        
        // could show victory screen here
    }
    
    #endregion
    
    #region Public API
    
    // public getters for game stats
    public int GetMoney() => currentMoney;
    public int GetOrdersCompleted() => ordersCompleted;
    public int GetOrdersFailed() => ordersFailed;
    public bool IsGameActive() => gameActive;
    public bool HasActiveCustomer() => hasActiveCustomer;
    public CustomerOrder GetCurrentOrder() => currentOrder;
    public string GetCurrentCustomerName() => currentCustomerName;
    public float GetCurrentPatience() => currentPatience;
    
    #endregion
}