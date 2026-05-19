using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using TMPro;

public class GameManager : MonoBehaviour
{
    [Header("Здоздоровье")]
    public int playerMaxHealth = 100;
    public int enemyMaxHealth = 100;
    private int playerCurrentHealth;
    private int enemyCurrentHealth;

    [Header("Параметры действий")]
    public int attackDamage = 25;
    public int healAmount = 20;
    public int enemyAttackDamage = 25;

    [Header("Визуал")]
    public GameObject playerShield;
    public GameObject enemyShield;
    public float shieldDuration = 1f;
    [Header("Модели и спрайты")]
    public SpriteRenderer playerSpriteRenderer;   // компонент спрайта игрока
    public SpriteRenderer enemySpriteRenderer;    // компонент спрайта врага
    public Sprite playerDeadSprite;               // лежачий спрайт игрока
    public Sprite enemyDeadSprite;                // лежачий спрайт врага

    [Header("Настройки боя")]
    public int actionsPerRound = 6;

    [Header("UI")]
    public Text playerHealthText;    // здоровье игрока
    public Text enemyHealthText;     // здоровье босса
    public TextMeshProUGUI actionText;          // лог боя
    public TextMeshProUGUI playerActionsText;   // список выбранных действий игрока
    public TextMeshProUGUI enemyActionsText;    // список действий босса (добавлено!)
    public GameObject planningPanel;            // панель с кнопками и списком – её будем скрывать
    public Button startBattleButton;
    public GameObject gameOverPanel;
    
    [Header("Кнопки действий")]
    public Button attackButton;
    public Button healButton;
    public Button shieldButton; 

    // ----- РУЧНАЯ НАСТРОЙКА ДЕЙСТВИЙ БОССА -----
    // Доступные действия: "Attack", "Heal", "Shield", "Idle"
    private List<List<string>> bossRoundsActions = new List<List<string>>()
    {
        new List<string> { "Attack", "Attack", "Shield", "Heal", "Attack", "Idle" },      // раунд 1
        new List<string> { "Shield", "Attack", "Idle", "Attack", "Heal", "Shield" },      // раунд 2 (после ничьей)
        new List<string> { "Idle", "Heal", "Attack", "Shield", "Attack", "Attack" }       // раунд 3
    };
    private int currentBossRound = 0;
    // -------------------------------------------

    private List<string> playerActions = new List<string>();
    private List<string> enemyActions = new List<string>();
    private bool isChoosing = false;
    private bool isExecuting = false;

    void Start()
    {
        playerCurrentHealth = playerMaxHealth;
        enemyCurrentHealth = enemyMaxHealth;
        UpdateHealthUI();
        StartNewChoice();
        if (attackButton != null)
            attackButton.onClick.AddListener(OnAttackButton);
        if (healButton != null)
            healButton.onClick.AddListener(OnHealButton);
        if (shieldButton != null)
            shieldButton.onClick.AddListener(OnShieldButton);
        if (playerShield != null) playerShield.SetActive(false);
        if (enemyShield != null) enemyShield.SetActive(false);

        if (startBattleButton != null)
            startBattleButton.onClick.AddListener(OnStartBattleButton);

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
    
}   

    void StartNewChoice()
    {
        playerActions.Clear();
        isChoosing = true;
        isExecuting = false;

        // Показываем панель планирования
        if (planningPanel != null) planningPanel.SetActive(true);

        // Генерируем действия босса для этого раунда
        GenerateEnemyActions();

        // Отображаем действия босса
        UpdateEnemyActionsDisplay();

        // Обновляем UI выбора
        UpdatePlayerActionsDisplay();
        if (actionText != null) actionText.text = "Выберите свои действия (6 ходов)";
    }

    void GenerateEnemyActions()
    {
        // Если раунды кончились – повторяем последний
        if (currentBossRound >= bossRoundsActions.Count)
            currentBossRound = bossRoundsActions.Count - 1;

        enemyActions = new List<string>(bossRoundsActions[currentBossRound]);

        // Дополняем или обрезаем до нужной длины
        while (enemyActions.Count < actionsPerRound)
            enemyActions.Add("Idle");
        if (enemyActions.Count > actionsPerRound)
            enemyActions = enemyActions.GetRange(0, actionsPerRound);

        Debug.Log("Действия босса: " + string.Join(", ", enemyActions));
    }

    void UpdateEnemyActionsDisplay()
    {
        if (enemyActionsText == null) return;
        string text = "Действия босса:\n";
        for (int i = 0; i < enemyActions.Count; i++)
        {
            text += $"{i + 1}. {GetRussianActionName(enemyActions[i])}\n";
        }
        enemyActionsText.text = text;
    }

    // Методы для UI-кнопок
    public void OnAttackButton()
    {
        if (!isChoosing) return;
        if (playerActions.Count >= actionsPerRound) return;
        playerActions.Add("Attack");
        UpdatePlayerActionsDisplay();
        CheckChoiceComplete();
    }

    public void OnHealButton()
    {
        if (!isChoosing) return;
        if (playerActions.Count >= actionsPerRound) return;
        playerActions.Add("Heal");
        UpdatePlayerActionsDisplay();
        CheckChoiceComplete();
    }

    public void OnShieldButton()
    {
        if (!isChoosing) return;
        if (playerActions.Count >= actionsPerRound) return;
        playerActions.Add("Shield");
        UpdatePlayerActionsDisplay();
        CheckChoiceComplete();
    }
    public void OnStartBattleButton()
    {
        if (playerActions.Count != actionsPerRound) return;
        if (isExecuting) return;

        // Скрываем панель планирования (если она есть)
        if (planningPanel != null) planningPanel.SetActive(false);

        // Прячем кнопку старта
        if (startBattleButton != null) startBattleButton.gameObject.SetActive(false);

        // Запускаем бой
        StartCoroutine(ExecuteRound());
    }

    void UpdatePlayerActionsDisplay()
    {
        if (playerActionsText == null) return;
        string text = "Твои действия:\n";
        for (int i = 0; i < playerActions.Count; i++)
        {
            text += $"{i + 1}. {GetRussianActionName(playerActions[i])}\n";
        }
        text += $"\nОсталось: {actionsPerRound - playerActions.Count}";
        playerActionsText.text = text;
    }

    void CheckChoiceComplete()
    {
        if (playerActions.Count == actionsPerRound)
        {
            isChoosing = false;  // выбор закончен, больше нельзя добавлять действия

            // Включаем кнопку "Начать бой"
            if (startBattleButton != null)
                startBattleButton.gameObject.SetActive(true);
        }
    }


    IEnumerator ExecuteRound()
    {
        isExecuting = true;
        for (int i = 0; i < actionsPerRound; i++)
        {
            if (playerCurrentHealth <= 0 || enemyCurrentHealth <= 0) break;

            string playerAction = playerActions[i];
            string enemyAction = enemyActions[i];

            yield return StartCoroutine(ExecuteAction(playerAction, enemyAction));
            yield return new WaitForSeconds(0.5f);
        }

        isExecuting = false;

        // Проверяем результат
        if (playerCurrentHealth <= 0 || enemyCurrentHealth <= 0)
        {
            EndGame();
        }
        else
        {
            // Все ходы выполнены, оба живы – ничья
            StartCoroutine(HandleTie());
        }
    }

    IEnumerator ExecuteAction(string playerAction, string enemyAction)
    {
        // Лог действий
        if (actionText != null)
        {
            actionText.text = $"Ты: {GetRussianActionName(playerAction)}\nБосс: {GetRussianActionName(enemyAction)}";
        }

        // Визуализация щитов
        if (playerAction == "Shield") StartCoroutine(ShowShield(playerShield));
        if (enemyAction == "Shield") StartCoroutine(ShowShield(enemyShield));

        int damageToEnemy = 0;
        int damageToPlayer = 0;

        // Действие игрока
        if (playerAction == "Attack")
        {
            damageToEnemy = attackDamage;
        }
        else if (playerAction == "Heal")
        {
            playerCurrentHealth = Mathf.Min(playerCurrentHealth + healAmount, playerMaxHealth);
            if (actionText != null) actionText.text += $"\nТы восстановил {healAmount} HP!";
        }

        // Действие врага
        if (enemyAction == "Attack")
        {
            damageToPlayer = enemyAttackDamage;
        }
        else if (enemyAction == "Heal")
        {
            enemyCurrentHealth = Mathf.Min(enemyCurrentHealth + healAmount, enemyMaxHealth);
            if (actionText != null) actionText.text += $"\nБосс восстановил {healAmount} HP!";
        }

        // Щит полностью блокирует урон в этом ходу
        if (playerAction == "Shield") damageToPlayer = 0;
        if (enemyAction == "Shield") damageToEnemy = 0;

        // Наносим урон
        enemyCurrentHealth -= damageToEnemy;
        playerCurrentHealth -= damageToPlayer;

        enemyCurrentHealth = Mathf.Max(0, enemyCurrentHealth);
        playerCurrentHealth = Mathf.Max(0, playerCurrentHealth);

        UpdateHealthUI();

        if (actionText != null)
        {
            if (damageToEnemy > 0) actionText.text += $"\nТы нанёс {damageToEnemy} урона боссу!";
            if (damageToPlayer > 0) actionText.text += $"\nБосс нанёс {damageToPlayer} урона тебе!";
        }

        yield return new WaitForSeconds(1.5f);
    }

    IEnumerator HandleTie()
    {
        if (actionText != null) actionText.text = "НИЧЬЯ! Раунд продолжается!\nВыбираем новые действия...";
        yield return new WaitForSeconds(2f);

        // Переключаем на следующий раунд действий босса
        currentBossRound++;
        // Начинаем новый выбор
        StartNewChoice();
    }

    IEnumerator ShowShield(GameObject shield)
    {
        if (shield != null)
        {
            shield.SetActive(true);
            yield return new WaitForSeconds(shieldDuration);
            shield.SetActive(false);
        }
    }

    void UpdateHealthUI()
    {
        if (playerHealthText != null)
            playerHealthText.text = $"{playerCurrentHealth}/{playerMaxHealth}";
        if (enemyHealthText != null)
            enemyHealthText.text = $"{enemyCurrentHealth}/{enemyMaxHealth}";
    }

    string GetRussianActionName(string action)
    {
        switch (action)
        {
            case "Attack": return "Атака";
            case "Heal": return "Хил";
            case "Shield": return "Щит";
            case "Idle": return "Бездействие";
            default: return action;
        }
    }

    void EndGame()
    {
        isExecuting = false;
        isChoosing = false;

        // Останавливаем все возможные корутины, чтобы бой не шёл дальше
        StopAllCoroutines();

        if (playerCurrentHealth <= 0)
        {
            if (actionText != null) actionText.text = "ПОРАЖЕНИЕ... Босс сильнее...";
            Debug.Log("Игрок проиграл!");

            // Меняем спрайт игрока на лежачий
            if (playerSpriteRenderer != null && playerDeadSprite != null)
                playerSpriteRenderer.sprite = playerDeadSprite;
        }
        else if (enemyCurrentHealth <= 0)
        {
            if (actionText != null) actionText.text = "ПОБЕДА! Ты одолел босса!";
            Debug.Log("Игрок победил!");

            // Меняем спрайт врага на лежачий
            if (enemySpriteRenderer != null && enemyDeadSprite != null)
                enemySpriteRenderer.sprite = enemyDeadSprite;
        }

        // Показываем панель Game Over
        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }
    public void RestartGame()
    {
        // Сбрасываем здоровье
        playerCurrentHealth = playerMaxHealth;
        enemyCurrentHealth = enemyMaxHealth;

        // Сбрасываем действия
        playerActions.Clear();
        enemyActions.Clear();

        // Сбрасываем индекс раунда босса (если хотите начинать с первого набора)
        currentBossRound = 0;

        
        // Обновляем UI здоровья
        UpdateHealthUI();

        // Скрываем GameOver панель
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        // Возвращаем панель планирования в активное состояние и запускаем новый выбор
        if (planningPanel != null) planningPanel.SetActive(true);

        // Включаем кнопки выбора обратно
        if (attackButton != null) attackButton.interactable = true;
        if (healButton != null) healButton.interactable = true;
        if (shieldButton != null) shieldButton.interactable = true;

        // Скрываем кнопку старта (она появится снова после выбора 6 действий)
        if (startBattleButton != null) startBattleButton.gameObject.SetActive(false);

        // Запускаем новый раунд выбора
        StartNewChoice();
    }
}
