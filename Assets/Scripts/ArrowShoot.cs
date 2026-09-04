using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SimpleArrowShooter : MonoBehaviour
{
    [Header("Arrow Settings")]
    public GameObject arrowPrefab;
    public float shootForce = 20f;
    public Transform spawnPoint;

    [Header("Throw Settings")]
    public int maxThrows = 10;
    public float minSwipeDistance = 50f;

    [Header("UI Elements")]
    public TextMeshProUGUI throwsCounterText;
    public TextMeshProUGUI resultText; 

    [Header("Result Message")]
    [TextArea(2, 4)]
    public string gameOverMessage = "Броски закончились!\nВернитесь в меню для следующей попытки.";

    private int remainingThrows;
    private Vector2 touchStartPos;
    private float touchStartTime;
    private bool isTouching = false;
    private bool canShoot = true;

    void Start()
    {
        if (arrowPrefab != null && arrowPrefab.activeSelf)
        {
            arrowPrefab.SetActive(false);
        }

        remainingThrows = maxThrows;
        UpdateUI();

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (!canShoot) return;

        if (Touchscreen.current != null)
        {
            var touch = Touchscreen.current.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                touchStartPos = touch.position.ReadValue();
                touchStartTime = Time.time;
                isTouching = true;
            }

            if (touch.press.wasReleasedThisFrame && isTouching)
            {
                Vector2 touchEndPos = touch.position.ReadValue();
                float touchDuration = Time.time - touchStartTime;

                if (IsForwardSwipe(touchStartPos, touchEndPos, touchDuration))
                {
                    ShootArrowIfAllowed();
                }

                isTouching = false;
            }
        }
#if UNITY_EDITOR
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            touchStartPos = Mouse.current.position.ReadValue();
            touchStartTime = Time.time;
            isTouching = true;
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isTouching)
        {
            Vector2 touchEndPos = Mouse.current.position.ReadValue();
            float touchDuration = Time.time - touchStartTime;

            if (IsForwardSwipe(touchStartPos, touchEndPos, touchDuration))
            {
                ShootArrowIfAllowed();
            }

            isTouching = false;
        }
#endif
    }

    bool IsForwardSwipe(Vector2 startPos, Vector2 endPos, float duration)
    {
        Vector2 swipeVector = endPos - startPos;
        float swipeDistance = swipeVector.magnitude;

        if (swipeDistance < minSwipeDistance)
            return false;

        float swipeSpeed = swipeDistance / duration;
        float minSwipeSpeed = 100f;

        if (swipeSpeed < minSwipeSpeed)
            return false;

        float angle = Vector2.Angle(Vector2.up, swipeVector.normalized);

        return angle < 45f;
    }

    void ShootArrowIfAllowed()
    {
        if (remainingThrows > 0)
        {
            ShootArrow();
            remainingThrows--;
            UpdateUI();

            // Проверяем, закончились ли броски
            if (remainingThrows <= 0)
            {
                EndGame();
            }
        }
    }

    void ShootArrow()
    {
        if (arrowPrefab == null) return;

        Transform spawn = spawnPoint != null ? spawnPoint : transform;
        Vector3 spawnPosition = spawn.position + spawn.forward * 0.5f;

        GameObject arrow = Instantiate(arrowPrefab, spawnPosition, spawn.rotation);
        arrow.SetActive(true);

        Rigidbody rb = arrow.GetComponent<Rigidbody>();
        if (rb == null)
            rb = arrow.AddComponent<Rigidbody>();

        rb.AddForce(spawn.forward * shootForce, ForceMode.Impulse);
        Destroy(arrow, 5f);
    }

    void UpdateUI()
    {
        if (throwsCounterText != null)
        {
            throwsCounterText.text = $"Бросков: {remainingThrows}/{maxThrows}";
        }
    }

    void EndGame()
    {
        canShoot = false;

        if (throwsCounterText != null)
        {
            throwsCounterText.gameObject.SetActive(false);
        }

        if (resultText != null)
        {
            resultText.text = gameOverMessage;
            resultText.gameObject.SetActive(true);

            StartCoroutine(FadeInText(resultText, 1f));
        }

        Debug.Log("Игра окончена! Все броски использованы.");
    }

    System.Collections.IEnumerator FadeInText(TextMeshProUGUI textElement, float duration)
    {
        Color originalColor = textElement.color;
        textElement.color = new Color(originalColor.r, originalColor.g, originalColor.b, 0);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Clamp01(elapsed / duration);
            textElement.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        textElement.color = originalColor;
    }

    public void ResetGame()
    {
        remainingThrows = maxThrows;
        canShoot = true;

        if (throwsCounterText != null)
        {
            throwsCounterText.gameObject.SetActive(true);
        }

        if (resultText != null)
        {
            resultText.gameObject.SetActive(false);
        }

        UpdateUI();
    }

    public void AddThrows(int amount)
    {
        if (!canShoot) return; 

        remainingThrows = Mathf.Min(remainingThrows + amount, maxThrows);
        UpdateUI();
    }

    public int RemainingThrows => remainingThrows;
    public bool IsGameActive => canShoot;
}