using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance;

    [Header("Upgradeable Stats")]
    public int baseDamage = 3;
    public int finalComboBonus = 2;
    public int maxHealth = 100;

    [Header("Stamina Settings")]
    public float maxStamina = 100f;
    public float staminaRegenRate = 20f;
    public float staminaRegenDelay = 1f;

    [HideInInspector] public float currentStamina;
    private float regenTimer;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            currentStamina = maxStamina;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        regenTimer -= Time.deltaTime;

        if (regenTimer <= 0f && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            currentStamina = Mathf.Clamp(currentStamina, 0f, maxStamina);
        }
    }

    public bool UseStamina(float amount)
    {
        if (currentStamina >= amount)
        {
            currentStamina -= amount;
            regenTimer = staminaRegenDelay;
            return true;
        }
        return false;
    }

    public float GetStaminaPercent()
    {
        return currentStamina / maxStamina;
    }
    public int GetDamage(bool isFinalHit)
    {
        return isFinalHit ? baseDamage + finalComboBonus : baseDamage;
    }

}
