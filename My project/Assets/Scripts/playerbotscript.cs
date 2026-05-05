using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class Fighter : MonoBehaviour
{
    [Header("Настройки")]
    public bool isPlayer;
    public int maxHealth = 100;
    public float attackCooldown = 1.2f;
    public float strongAttackCooldown = 3f;
    public int attackdamage;
    public int Strongattackdamage;

    [Header("Противник")]
    public Fighter opponent;

    [Header("UI")]
    public Text healthText;
    public Text actionText;

    private int currentHealth;
    private float lastAttackTime;
    private float lastStrongAttackTime;
    private bool isBlocking = false;
    private float blockEndTime;
    private bool isDead = false;  //Флаг смерти

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();

        //Проверяем, назначен ли противник
        if (opponent == null)
        {
            Debug.LogError($"У {gameObject.name} не назначен opponent! Перетащи противника в инспекторе.");
            return;
        }

        //Запускаем ИИ для бота
        if (!isPlayer)
        {
            Debug.Log($"{gameObject.name}: Бот запущен!");
            StartCoroutine(BotAI());
        }
        else
        {
            Debug.Log($"{gameObject.name}: Игрок готов!");
        }
    }

    void Update()
    {
        if (isDead) return;  // Если мёртв — ничего не делаем

        // Обновляем блок
        if (isBlocking && Time.time >= blockEndTime)
        {
            isBlocking = false;
            ShowAction("Блок закончен", Color.gray);
        }

        // Управление игроком (только если жив)
        if (isPlayer)
        {
            if (Input.GetKeyDown(KeyCode.E) && Time.time >= lastAttackTime + attackCooldown)
            {
                PerformAttack(false);
            }

            if (Input.GetKeyDown(KeyCode.R) && Time.time >= lastStrongAttackTime + strongAttackCooldown)
            {
                PerformAttack(true);
            }

            if (Input.GetKeyDown(KeyCode.Q) && !isBlocking)
            {
                StartBlock();
            }
        }
    }

    void PerformAttack(bool isStrong)
    {
        if (isDead) return;  //Мёртвый не атакует
        if (opponent.isDead) return;  // Противник мёртв — не атакуем

        int damage = isStrong ? Strongattackdamage : attackdamage;

        if (isStrong)
            lastStrongAttackTime = Time.time;
        else
            lastAttackTime = Time.time;

        // Проверяем блок противника
        bool opponentBlocking = opponent.isBlocking;
        int finalDamage = opponentBlocking ? Mathf.RoundToInt(damage * 0.3f) : damage;

        // Наносим урон
        opponent.TakeDamage(finalDamage);

        // Показываем текст
        string attackName = isStrong ? "💥  СИЛЬНЫЙ УДАР" : "👊  УДАР";

        if (opponentBlocking)
        {
            ShowAction($"{attackName} (ЗАБЛОКИРОВАН! -{finalDamage})", Color.yellow);
            opponent.ShowAction($"🛡️  БЛОК! -{finalDamage}", Color.cyan);
        }
        else
        {
            ShowAction($"{attackName}! -{finalDamage}", Color.red);
            opponent.ShowAction($"💔  ПОПАДАНИЕ! -{finalDamage}", Color.magenta);
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        if (currentHealth < 0) currentHealth = 0;
        UpdateUI();

        ShowAction($"-{damage} ❤️ ", Color.red);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void StartBlock()
    {
        if (isDead) return;

        isBlocking = true;
        blockEndTime = Time.time + 1f;
        ShowAction("🛡️  БЛОК!", Color.green);
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;
        ShowAction("💀  ПОБЕЖДЁН!", Color.black);

        // Останавливаем все корутины
        StopAllCoroutines();


// Показываем победителя
        if (isPlayer)
        {
            Debug.Log("ТЫ ПРОИГРАЛ!");
            if (actionText) actionText.text = "GAME OVER - ТЫ ПРОИГРАЛ!";
        }
        else
        {
            Debug.Log("ТЫ ПОБЕДИЛ!");
            if (actionText) actionText.text = "ПОБЕДА!!!";
        }

        // Меняем цвет спрайта на серый (если есть SpriteRenderer)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = Color.gray;
        }
    }

    void UpdateUI()
    {
        if (healthText != null)
        {
            healthText.text = $"{(isPlayer ? "❤️  ИГРОК" : "💀  БОТ")}\n{currentHealth}/{maxHealth}";
            
        }
        if (currentHealth > 66)
        {
            healthText.color = Color.white;
        }
        else if (currentHealth<66 && currentHealth>33)
        { 
            healthText.color = Color.yellow; 
        }
        else
        {  
            healthText.color = Color.red; 
        }
    }

    void ShowAction(string message, Color color, float duration = 3f)
    {
        if (actionText != null)
        {
            actionText.text = message;
            actionText.color = color;
            CancelInvoke("ClearActionText");
            Invoke("ClearActionText", duration);
        }
    }

    void ClearActionText()
    {
        if (actionText != null)
            actionText.text = "";
    }

    // БОТ (теперь точно работает) 
    IEnumerator BotAI()
    {
        Debug.Log($"{gameObject.name}: Бот AI запущен!");

        // Ждём 0.5 секунды перед первой атакой
        yield return new WaitForSeconds(0.5f);

        while (!isDead && opponent != null && !opponent.isDead)
        {
            // Пауза между действиями (1-2 секунды)
            float waitTime = Random.Range(1f, 2f);
            yield return new WaitForSeconds(waitTime);

            if (isDead || opponent.isDead) break;

            // Решение: что делать боту?
            int action = Random.Range(0, 100);

            // 30% шанс на блок (особенно если здоровье низкое)
            if (currentHealth < 40 && action < 40 && !isBlocking)
            {
                Debug.Log($"{gameObject.name}: Бот решил заблокировать");
                ShowAction("🤖  БЛОК!", Color.magenta);
                StartBlock();
                yield return new WaitForSeconds(1f);
            }
            // 25% шанс на сильный удар (если перезарядка прошла)
            else if (action < 25 && Time.time >= lastStrongAttackTime + strongAttackCooldown)
            {
                Debug.Log($"{gameObject.name}: Бот наносит СИЛЬНЫЙ удар");
                ShowAction("🤖  СИЛЬНО АТАКУЕТ!", Color.yellow);
                yield return new WaitForSeconds(1f);
                PerformAttack(true);
            }
            // Обычный удар
            else if (Time.time >= lastAttackTime + attackCooldown)
            {
                Debug.Log($"{gameObject.name}: Бот атакует");
                ShowAction("🤖  АТАКУЕТ!", Color.white);
                yield return new WaitForSeconds(1f);
                PerformAttack(false);
            }
            else
            {
                Debug.Log($"{gameObject.name}: Бот ждёт...");
                ShowAction("🤖  ЖДЁТ...", Color.gray, 1f);
            }
        }

        Debug.Log($"{gameObject.name}: Бот AI остановлен (мёртв или побеждён)");
    }
}