using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ограничитель спавна объектов
/// </summary>
public class SimpleSpawnLimiter : MonoBehaviour
{
    [Header("Настройки")]
    [SerializeField]
    [Tooltip("Перетащите компонент Object Spawner сюда (компонент, а не GameObject)")]
    private MonoBehaviour m_SpawnerComponent;

    [SerializeField]
    [Tooltip("Ограничить количество объектов до одного")]
    private bool m_LimitToOne = true;

    [SerializeField]
    [Tooltip("Заменить старый объект новым")]
    private bool m_ReplaceOld = true;

    // Делегат для события спавна объекта
    private System.Action<GameObject> m_SpawnEvent;

    // Текущий объект
    private GameObject m_CurrentObject;

    // Флаг для предотвращения рекурсии
    private bool m_IsProcessing = false;

    void Start()
    {
        InitializeSpawner();

        // Очищаем существующие объекты при старте
        if (m_LimitToOne)
        {
            CleanupExistingObjects();
        }
    }

    void InitializeSpawner()
    {
        if (m_SpawnerComponent == null)
        {
            Debug.LogError("SimpleSpawnLimiter: Spawner Component не назначен в инспекторе!", this);
            enabled = false;
            return;
        }

        // Получаем тип компонента
        System.Type componentType = m_SpawnerComponent.GetType();

        // Проверяем, есть ли у компонента событие objectSpawned
        var objectSpawnedEvent = componentType.GetEvent("objectSpawned");

        if (objectSpawnedEvent == null)
        {
            Debug.LogError($"SimpleSpawnLimiter: У компонента {componentType.Name} нет события objectSpawned!", this);
            enabled = false;
            return;
        }

        // Создаем делегат для подписки на событие
        m_SpawnEvent = new System.Action<GameObject>(OnObjectSpawned);

        // Подписываемся на событие через рефлексию
        try
        {
            objectSpawnedEvent.AddEventHandler(m_SpawnerComponent, m_SpawnEvent);
            Debug.Log($"SimpleSpawnLimiter: Успешно подключен к {componentType.Name} на {m_SpawnerComponent.gameObject.name}", this);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"SimpleSpawnLimiter: Не удалось подключиться к событию: {e.Message}", this);
            enabled = false;
        }
    }

    void OnDestroy()
    {
        // Отписываемся от события при уничтожении
        if (m_SpawnerComponent != null && m_SpawnEvent != null)
        {
            try
            {
                System.Type componentType = m_SpawnerComponent.GetType();
                var objectSpawnedEvent = componentType.GetEvent("objectSpawned");

                if (objectSpawnedEvent != null)
                {
                    objectSpawnedEvent.RemoveEventHandler(m_SpawnerComponent, m_SpawnEvent);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"SimpleSpawnLimiter: Ошибка при отписке от события: {e.Message}", this);
            }
        }
    }

    private void OnObjectSpawned(GameObject spawnedObject)
    {
        if (!m_LimitToOne || m_IsProcessing)
            return;

        m_IsProcessing = true;

        Debug.Log($"SimpleSpawnLimiter: Получен объект {spawnedObject.name}", spawnedObject);

        // Если есть текущий объект
        if (m_CurrentObject != null)
        {
            if (m_ReplaceOld)
            {
                // Заменяем старый объект новым
                Debug.Log($"SimpleSpawnLimiter: Заменяю старый объект {m_CurrentObject.name} на {spawnedObject.name}");
                Destroy(m_CurrentObject);
                m_CurrentObject = spawnedObject;
            }
            else
            {
                // Не заменяем - уничтожаем новый объект
                Debug.Log($"SimpleSpawnLimiter: Объект уже существует, уничтожаю новый {spawnedObject.name}");
                Destroy(spawnedObject);
                m_IsProcessing = false;
                return;
            }
        }
        else
        {
            // Сохраняем новый объект
            m_CurrentObject = spawnedObject;
            Debug.Log($"SimpleSpawnLimiter: Сохраняю новый объект {spawnedObject.name}");
        }

        // Настраиваем автоматическое удаление при уничтожении
        SetupObjectTracking(spawnedObject);

        m_IsProcessing = false;
    }

    private void SetupObjectTracking(GameObject obj)
    {
        // Добавляем компонент для отслеживания уничтожения
        var tracker = obj.AddComponent<SpawnedObjectTracker>();
        tracker.Initialize(this, obj);
    }

    /// <summary>
    /// Вызывается при уничтожении отслеживаемого объекта
    /// </summary>
    public void OnTrackedObjectDestroyed(GameObject destroyedObject)
    {
        if (m_CurrentObject == destroyedObject)
        {
            Debug.Log($"SimpleSpawnLimiter: Объект {destroyedObject.name} уничтожен, очищаю ссылку");
            m_CurrentObject = null;
        }
    }

    /// <summary>
    /// Очистить все существующие объекты кроме последнего
    /// </summary>
    public void CleanupExistingObjects()
    {
        if (!m_LimitToOne) return;

        // Найти все спавненные объекты (которые являются клонами)
        var allObjects = FindObjectsOfType<GameObject>();
        List<GameObject> toDestroy = new List<GameObject>();

        foreach (var obj in allObjects)
        {
            // Ищем объекты, которые являются клонами префабов
            if (obj.name.Contains("(Clone)") && obj != m_CurrentObject && obj != this.gameObject)
            {
                // Проверяем, не является ли этот объект частью UI или системным
                if (obj.GetComponent<Camera>() == null &&
                    obj.GetComponent<Canvas>() == null &&
                    obj.transform.parent == null) // Не удаляем дочерние объекты
                {
                    toDestroy.Add(obj);
                }
            }
        }

        Debug.Log($"SimpleSpawnLimiter: Найдено {toDestroy.Count} лишних объектов для удаления");

        foreach (var obj in toDestroy)
        {
            Debug.Log($"SimpleSpawnLimiter: Уничтожаю лишний объект: {obj.name}");
            Destroy(obj);
        }
    }

    /// <summary>
    /// Принудительно очищает текущий объект
    /// </summary>
    public void ClearCurrentObject()
    {
        if (m_CurrentObject != null)
        {
            Debug.Log($"SimpleSpawnLimiter: Принудительно очищаю объект {m_CurrentObject.name}");
            m_CurrentObject = null;
        }
    }

    /// <summary>
    /// Принудительно уничтожает текущий объект
    /// </summary>
    public void DestroyCurrentObject()
    {
        if (m_CurrentObject != null)
        {
            Debug.Log($"SimpleSpawnLimiter: Принудительно уничтожаю объект {m_CurrentObject.name}");
            Destroy(m_CurrentObject);
            m_CurrentObject = null;
        }
    }

    /// <summary>
    /// Возвращает текущий активный объект
    /// </summary>
    public GameObject GetCurrentObject()
    {
        return m_CurrentObject;
    }

    /// <summary>
    /// Вспомогательный класс для отслеживания объектов
    /// </summary>
    private class SpawnedObjectTracker : MonoBehaviour
    {
        private SimpleSpawnLimiter m_Limiter;
        private GameObject m_TrackedObject;

        public void Initialize(SimpleSpawnLimiter limiter, GameObject trackedObject)
        {
            m_Limiter = limiter;
            m_TrackedObject = trackedObject;
        }

        void OnDestroy()
        {
            if (m_Limiter != null && m_TrackedObject != null)
            {
                m_Limiter.OnTrackedObjectDestroyed(m_TrackedObject);
            }
        }
    }

    // Для отладки в редакторе
#if UNITY_EDITOR
    void OnValidate()
    {
        if (m_SpawnerComponent == null)
        {
            // Ищем любой компонент с событием objectSpawned
            var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();

            foreach (var mb in allMonoBehaviours)
            {
                if (mb == null) continue;

                System.Type type = mb.GetType();
                var objectSpawnedEvent = type.GetEvent("objectSpawned");

                if (objectSpawnedEvent != null)
                {
                    m_SpawnerComponent = mb;
                    Debug.Log($"SimpleSpawnLimiter: Автоматически назначен {type.Name} на {mb.gameObject.name}", this);
                    break;
                }
            }
        }
    }
#endif
}