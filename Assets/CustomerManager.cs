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
    [SerializeField] private float customerPatience = 45f; // how long customers wait
    [SerializeField] private int dailyGoalMoney = 50; // money needed to complete the day
    [SerializeField] private int startingMoney = 0; // money player starts with
    
    // singleton pattern for easy access
    public static CustomerManager Instance { get; private set; }
    
    // game state tracking
    private int currentMoney; // how much money player has earned
    private int ordersCompleted; // successful orders served
    private int ordersFailed; // failed orders (for stats)
    private bool gameActive = true; // whether the game is running
    
    // current customer state
    private SimpleCustomerOrder currentOrder; // what the current customer wants
    private string currentCustomerName; // current customer's name
    private float currentPatience; // how much patience current customer has left
    private bool hasActiveCustomer = false; // whether there's currently a customer waiting
    
    // customer names for variety
    private string[] customerNames = {
        "Alice", "Bob", "Charlie", "Diana", "Emma", "Frank", "Grace", "Henry",
        "Ivy", "Jack", "Kate", "Leo", "Mia", "Noah", "Olivia", "Paul"
    };
    
    // represents what pizza a customer wants (now with cheese!)
    [System.Serializable]
    public class SimpleCustomerOrder
    {
        public bool wantsSauce; // does customer want tomato sauce
        public bool wantsCheese; // does customer want cheese - NEW!
        public bool wantsPepperoni; // does customer want pepperoni
        public bool wantsCorn; // does customer want corn
        public bool wantsOlives; // does customer want olives
        public bool wantsCooked; // does customer want pizza cooked
        public int tipAmount; // bonus money if order is perfect
        
        // generates a random pizza order
        public static SimpleCustomerOrder GenerateRandomOrder()
        {
            SimpleCustomerOrder order = new SimpleCustomerOrder();
            
            // sauce is very common (90% chance)
            order.wantsSauce = Random.Range(0f, 1f) < 0.9f;
            
            // cheese is very common too (85% chance) - NEW!
            order.wantsCheese = Random.Range(0f, 1f) < 0.85f;
            
            // each topping has a 50% chance
            order.wantsPepperoni = Random.Range(0f, 1f) < 0.5f;
            order.wantsCorn = Random.Range(0f, 1f) < 0.5f;
            order.wantsOlives = Random.Range(0f, 1f) < 0.5f;
            
            // customers always want their pizza cooked
            order.wantsCooked = true;
            
            // random tip amount (0-5 extra coins)
            order.tipAmount = Random.Range(0, 6);
            
            return order;
        }
        
        // converts order to readable text
        public string ToOrderString()
        {
            List<string> ingredients = new List<string>();
            
            if (wantsSauce) ingredients.Add("sauce");
            if (wantsCheese) ingredients.Add("cheese"); // NEW!
            if (wantsPepperoni) ingredients.Add("pepperoni");
            if (wantsCorn) ingredients.Add("corn");
            if (wantsOlives) ingredients.Add("olives");
            
            if (ingredients.Count == 0)
            {
                return "I want just plain dough, cooked please!";
            }
            else if (ingredients.Count == 1)
            {
                return $"I want {ingredients[0]} pizza, cooked!";
            }
            else
            {
                string ingredientList = string.Join(", ", ingredients.ToArray(), 0, ingredients.Count - 1);
                return $"I want {ingredientList} and {ingredients[ingredients.Count - 1]} pizza, cooked!";
            }
        }
    }
    
    void Awake()
    {
        // singleton setup
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        currentMoney = startingMoney;
        UpdateUI();
    }
    
    void Start()
    {
        // hide order UI initially
        HideOrderUI();
        
        // start the customer cycle
        if (gameActive)
        {
            StartCoroutine(CustomerCycle());
        }
        
        Debug.Log($"restaurant opened! daily goal: ${dailyGoalMoney}");
    }
    
    void Update()
    {
        // update patience if we have an active customer
        if (hasActiveCustomer)
        {
            UpdateCustomerPatience();
        }
    }
    
    // main customer cycle - spawns customers at intervals
    IEnumerator CustomerCycle()
    {
        while (gameActive)
        {
            // wait for customer interval
            yield return new WaitForSeconds(customerInterval);
            
            // only spawn new customer if no one is currently waiting
            if (!hasActiveCustomer)
            {
                SpawnNewCustomer();
            }
        }
    }
    
    // creates a new customer with random order, this one is important
    void SpawnNewCustomer()
    {
        currentOrder = SimpleCustomerOrder.GenerateRandomOrder();
        currentCustomerName = customerNames[Random.Range(0, customerNames.Length)];
        currentPatience = customerPatience;
        hasActiveCustomer = true;
        
        ShowOrderUI();
        
        Debug.Log($"{currentCustomerName} wants: {currentOrder.ToOrderString()}");
    }
    
    // shows the order UI at top of screen
    void ShowOrderUI()
    {
        if (currentOrderText != null)
        {
            currentOrderText.text = currentOrder.ToOrderString();
            currentOrderText.color = Color.white; // reset to normal color
            currentOrderText.gameObject.SetActive(true);
        }
        
        if (customerNameText != null)
        {
            customerNameText.text = currentCustomerName + " says:";
            customerNameText.gameObject.SetActive(true);
        }
        
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
    
    // updates customer patience and patience bar
    void UpdateCustomerPatience()
    {
        currentPatience -= Time.deltaTime;
        
        // update patience slider
        if (patienceSlider != null)
        {
            patienceSlider.value = currentPatience;
            
            // change patience bar color based on remaining time
            if (patienceSliderFill != null)
            {
                if (currentPatience > customerPatience * 0.6f)
                    patienceSliderFill.color = Color.green; // calm
                else if (currentPatience > customerPatience * 0.3f)
                    patienceSliderFill.color = Color.yellow; // getting impatient
                else
                    patienceSliderFill.color = Color.red; // very impatient
            }
        }
        
        // customer leaves if patience runs out
        if (currentPatience <= 0f)
        {
            CustomerLeavesAngryTimeout();
        }
    }
    
    // customer leaves angry (patience ran out)
    void CustomerLeavesAngryTimeout()
    {
        // show angry timeout message
        if (currentOrderText != null)
        {
            currentOrderText.text = "Ugh! This is taking forever! I'm leaving!";
            currentOrderText.color = Color.red;
        }
        
        Debug.Log($"{currentCustomerName} left angry! too slow!");
        
        // lose money when customers leave angry
        currentMoney -= 5;
        if (currentMoney < 0) currentMoney = 0;
        
        ordersFailed++;
        
        UpdateUI();
        
        // play angry sound effect
        AudioManager.Instance?.PlayButtonClickSound(); // placeholder
        
        // hide UI after showing message for a moment
        StartCoroutine(HideOrderUIAfterDelay(2f));
    }
    
    // public function for pizza simulator to serve pizza to current customer
    public bool ServePizzaToCustomer(PizzaSimulator.IngredientType[] ingredients, bool isCooked)
    {
        if (!hasActiveCustomer) 
        {
            Debug.Log("no customer is waiting!");
            return false;
        }
        
        // check if the pizza matches what customer ordered
        bool orderCorrect = CheckOrderCorrectness(ingredients, isCooked);
        
        if (orderCorrect)
        {
            CustomerLeavesHappy();
            return true;
        }
        else
        {
            CustomerLeavesAngryWrongOrder();
            return false;
        }
    }
    
    // checks if the served pizza matches the customer's order
    bool CheckOrderCorrectness(PizzaSimulator.IngredientType[] ingredients, bool isCooked)
    {
        // check if cooking state matches
        if (isCooked != currentOrder.wantsCooked) return false;
        
        // count what ingredients are on the pizza
        bool hasSauce = System.Array.Exists(ingredients, ing => ing == PizzaSimulator.IngredientType.TomatoSauce);
        bool hasCheese = System.Array.Exists(ingredients, ing => ing == PizzaSimulator.IngredientType.Cheese);
        bool hasPepperoni = System.Array.Exists(ingredients, ing => ing == PizzaSimulator.IngredientType.Pepperoni);
        bool hasCorn = System.Array.Exists(ingredients, ing => ing == PizzaSimulator.IngredientType.Corn);
        bool hasOlives = System.Array.Exists(ingredients, ing => ing == PizzaSimulator.IngredientType.Olives);
        
        // check if ingredients match order exactly
        return (hasSauce == currentOrder.wantsSauce &&
                hasCheese == currentOrder.wantsCheese &&
                hasPepperoni == currentOrder.wantsPepperoni &&
                hasCorn == currentOrder.wantsCorn &&
                hasOlives == currentOrder.wantsOlives);
    }
    
    // customer leaves happy when order is correct
    void CustomerLeavesHappy()
    {
        // show happy message
        if (currentOrderText != null)
        {
            currentOrderText.text = "Yum! Thank you so much! This is perfect!";
            currentOrderText.color = Color.green;
        }
        
        Debug.Log($"{currentCustomerName} is happy! correct order!");
        
        // calculate payment (base + tip + time bonus)
        int basePayment = 10;
        int timeBonus = Mathf.RoundToInt(currentPatience / 10f); // bonus for being fast
        int payment = basePayment + currentOrder.tipAmount + timeBonus;
        
        currentMoney += payment;
        ordersCompleted++;
        
        UpdateUI();
        CheckDailyGoal();
        
        Debug.Log($"earned ${payment}! (base: $10, tip: ${currentOrder.tipAmount}, time bonus: ${timeBonus})");
        
        // play happy sound effect
        AudioManager.Instance?.PlayCookingCompleteSound();
        
        // hide UI after showing message for a moment
        StartCoroutine(HideOrderUIAfterDelay(2f));
    }
    
    // customer leaves angry when order is wrong
    void CustomerLeavesAngryWrongOrder()
    {
        // show angry message
        if (currentOrderText != null)
        {
            currentOrderText.text = "This isn't what I ordered! I'm not paying for this!";
            currentOrderText.color = Color.red;
        }
        
        Debug.Log($"{currentCustomerName} left angry! wrong order!");
        
        // lose money when customers leave angry
        currentMoney -= 5;
        if (currentMoney < 0) currentMoney = 0;
        
        ordersFailed++;
        
        UpdateUI();
        
        // play angry sound effect
        AudioManager.Instance?.PlayButtonClickSound(); // placeholder
        
        // hide UI after showing message for a moment
        StartCoroutine(HideOrderUIAfterDelay(2f));
    }
    
    // hides order UI after a delay and sets up for next customer
    IEnumerator HideOrderUIAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        hasActiveCustomer = false;
        HideOrderUI();
        
        // reset text color for next customer
        if (currentOrderText != null)
        {
            currentOrderText.color = Color.white; // or whatever your normal text color is
        }
    }
    
    // updates the UI with current game stats
    void UpdateUI()
    {
        if (moneyText != null)
        {
            moneyText.text = $"Money: ${currentMoney}";
        }
        
        if (ordersCompletedText != null)
        {
            ordersCompletedText.text = $"Orders: {ordersCompleted}";
        }
        
        if (dailyGoalText != null)
        {
            dailyGoalText.text = $"Goal: ${currentMoney}/{dailyGoalMoney}";
        }
    }
    
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
        Debug.Log($"congratulations! you reached the daily goal of ${dailyGoalMoney}!");
        
        // could show victory screen here
    }
    
     // public getters for game stats
	public int GetMoney() 
	{ 
    	return currentMoney; 
	}

	public int GetOrdersCompleted() 
	{ 
    	return ordersCompleted; 
	}

	public int GetOrdersFailed() 
	{	 
    	return ordersFailed; 
	}

	public bool IsGameActive() 
	{ 
    	return gameActive; 
	}

	public bool HasActiveCustomer() 
	{ 
    	return hasActiveCustomer; 
	}
}