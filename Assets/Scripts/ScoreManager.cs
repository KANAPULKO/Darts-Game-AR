using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System.Collections;

public class TargetScoreSystem : MonoBehaviour
{
    [System.Serializable]
    public class ScoreZone
    {
        public Collider zoneCollider;
        public int scoreValue;
        public bool isTrigger = true;
    }

    public List<ScoreZone> scoreZones = new List<ScoreZone>();
    public TMP_Text scoreTextUI;

    [Header("Settings")]
    public float zoneDetectionRadius = 0.1f;
    public bool disableDartColliderOnHit = true;

    [Header("Score Effects")]
    public string[] scoreMessages = new string[]
    {
        "Начинающий!",
        "Неплохо!",
        "Хорошо!",
        "Отлично!",
        "Профи!",
        "Легенда!"
    };

    public int[] scoreThresholds = new int[] { 50, 100, 150, 200, 250, 300 };

    public Color[] scoreColors = new Color[]
    {
        Color.white,
        Color.green,
        Color.blue,
        Color.yellow,
        new Color(1f, 0.5f, 0f),
        Color.red
    };

    private Dictionary<Collider, ScoreZone> zoneLookup = new Dictionary<Collider, ScoreZone>();
    private Dictionary<GameObject, float> dartHitTimes = new Dictionary<GameObject, float>();
    private int totalScore = 0;
    private int currentLevel = 0;

    void Start()
    {
        foreach (var zone in scoreZones)
        {
            if (zone.zoneCollider != null)
            {
                zoneLookup[zone.zoneCollider] = zone;
                zone.zoneCollider.isTrigger = zone.isTrigger;
            }
        }
        UpdateScoreUI();
    }

    void OnTriggerEnter(Collider other)
    {
        ProcessDartHit(other, true);
    }

    void OnCollisionEnter(Collision collision)
    {
        ProcessDartHit(collision.collider, false);
    }

    void ProcessDartHit(Collider dartCollider, bool isTrigger)
    {
        GameObject dart = dartCollider.gameObject;
        if (!dart.CompareTag("Dart")) return;

        if (dartHitTimes.ContainsKey(dart) && Time.time - dartHitTimes[dart] < 0.1f) return;
        dartHitTimes[dart] = Time.time;

        Rigidbody rb = dart.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (disableDartColliderOnHit)
        {
            foreach (Collider col in dart.GetComponentsInChildren<Collider>())
            {
                col.enabled = false;
            }
        }

        ScoreZone hitZone = DetermineHitZone(dartCollider);
        if (hitZone != null)
        {
            AddScore(hitZone.scoreValue);
        }
        else
        {
            AddScore(5);
        }
    }

    ScoreZone DetermineHitZone(Collider dartCollider)
    {
        foreach (var zone in scoreZones)
        {
            if (zone.zoneCollider == null) continue;
            if (zone.zoneCollider.bounds.Intersects(dartCollider.bounds))
            {
                return zone;
            }
        }

        ScoreZone closestZone = null;
        float closestDistance = float.MaxValue;

        foreach (var zone in scoreZones)
        {
            if (zone.zoneCollider == null) continue;

            Vector3 closestPoint = zone.zoneCollider.ClosestPoint(dartCollider.transform.position);
            float distance = Vector3.Distance(dartCollider.transform.position, closestPoint);

            if (distance < zoneDetectionRadius && distance < closestDistance)
            {
                closestDistance = distance;
                closestZone = zone;
            }
        }

        return closestZone;
    }

    void AddScore(int points)
    {
        totalScore += points;
        CheckLevelUp();
        UpdateScoreUI();
    }

    void CheckLevelUp()
    {
        int newLevel = 0;
        for (int i = 0; i < scoreThresholds.Length; i++)
        {
            if (totalScore >= scoreThresholds[i])
            {
                newLevel = i + 1;
            }
            else
            {
                break;
            }
        }

        if (newLevel != currentLevel)
        {
            currentLevel = newLevel;
            if (currentLevel > 0 && currentLevel <= scoreMessages.Length && scoreTextUI != null)
            {
                StartCoroutine(LevelUpEffect());
            }
        }
    }

    void UpdateScoreUI()
    {
        if (scoreTextUI != null)
        {
            string message = $"SCORE: {totalScore}";
            if (currentLevel > 0 && currentLevel <= scoreMessages.Length)
            {
                message = $"{scoreMessages[currentLevel - 1]}\nSCORE: {totalScore}";
            }

            scoreTextUI.text = message;

            if (currentLevel > 0 && currentLevel <= scoreColors.Length)
            {
                scoreTextUI.color = scoreColors[currentLevel - 1];
            }
        }
    }

    IEnumerator LevelUpEffect()
    {
        Transform textTransform = scoreTextUI.transform;
        Vector3 originalScale = textTransform.localScale;
        Color originalColor = scoreTextUI.color;

        for (float t = 0; t < 1f; t += Time.deltaTime * 2f)
        {
            float scale = Mathf.Lerp(1f, 1.5f, Mathf.PingPong(t * 2f, 1f));
            textTransform.localScale = originalScale * scale;
            yield return null;
        }

        textTransform.localScale = originalScale;
        scoreTextUI.color = originalColor;
    }

    void OnDrawGizmosSelected()
    {
        foreach (var zone in scoreZones)
        {
            if (zone.zoneCollider != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawWireCube(zone.zoneCollider.bounds.center, zone.zoneCollider.bounds.size);
            }
        }
    }

    public void ResetScore()
    {
        totalScore = 0;
        currentLevel = 0;
        UpdateScoreUI();
    }

    public int GetCurrentScore()
    {
        return totalScore;
    }
}