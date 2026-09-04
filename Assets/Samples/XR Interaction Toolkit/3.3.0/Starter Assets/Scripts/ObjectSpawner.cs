using System;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Utilities;

namespace UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets
{
    /// <summary>
    /// Behavior with an API for spawning objects from a given set of prefabs.
    /// </summary>
    public class ObjectSpawner : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The list of prefabs available to spawn.")]
        List<GameObject> m_ObjectPrefabs = new List<GameObject>();

        /// <summary>
        /// The list of prefabs available to spawn.
        /// </summary>
        public List<GameObject> objectPrefabs
        {
            get => m_ObjectPrefabs;
            set => m_ObjectPrefabs = value;
        }

        [SerializeField]
        [Tooltip("Optional prefab to spawn for each spawned object. Use a prefab with the Destroy Self component to make " +
            "sure the visualization only lives temporarily.")]
        GameObject m_SpawnVisualizationPrefab;

        /// <summary>
        /// Optional prefab to spawn for each spawned object.
        /// </summary>
        /// <remarks>Use a prefab with <see cref="DestroySelf"/> to make sure the visualization only lives temporarily.</remarks>
        public GameObject spawnVisualizationPrefab
        {
            get => m_SpawnVisualizationPrefab;
            set => m_SpawnVisualizationPrefab = value;
        }

        [SerializeField]
        [Tooltip("The index of the prefab to spawn. If outside the range of the list, this behavior will select " +
            "a random object each time it spawns.")]
        int m_SpawnOptionIndex = -1;

        /// <summary>
        /// The index of the prefab to spawn. If outside the range of <see cref="objectPrefabs"/>, this behavior will
        /// select a random object each time it spawns.
        /// </summary>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public int spawnOptionIndex
        {
            get => m_SpawnOptionIndex;
            set => m_SpawnOptionIndex = value;
        }

        /// <summary>
        /// Whether this behavior will select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="RandomizeSpawnOption"/>
        public bool isSpawnOptionRandomized => m_SpawnOptionIndex < 0 || m_SpawnOptionIndex >= m_ObjectPrefabs.Count;

        [SerializeField]
        [Tooltip("Whether to spawn each object as a child of this object.")]
        bool m_SpawnAsChildren;

        /// <summary>
        /// Whether to spawn each object as a child of this object.
        /// </summary>
        public bool spawnAsChildren
        {
            get => m_SpawnAsChildren;
            set => m_SpawnAsChildren = value;
        }

        [SerializeField]
        [Tooltip("Отступ от стены (чтобы объект не утопал в стене)")]
        float m_WallOffset = 0.01f;

        /// <summary>
        /// Отступ от стены (чтобы объект не утопал в стене)
        /// </summary>
        public float wallOffset
        {
            get => m_WallOffset;
            set => m_WallOffset = value;
        }

        [SerializeField]
        [Tooltip("Ориентировать объект вертикально (вверх по оси Y мира)")]
        bool m_OrientVertically = true;

        /// <summary>
        /// Ориентировать объект вертикально (вверх по оси Y мира)
        /// </summary>
        public bool orientVertically
        {
            get => m_OrientVertically;
            set => m_OrientVertically = value;
        }

        /// <summary>
        /// Event invoked after an object is spawned.
        /// </summary>
        /// <seealso cref="TrySpawnObject"/>
        public event Action<GameObject> objectSpawned;

        /// <summary>
        /// Sets this behavior to select a random object from <see cref="objectPrefabs"/> each time it spawns.
        /// </summary>
        /// <seealso cref="spawnOptionIndex"/>
        /// <seealso cref="isSpawnOptionRandomized"/>
        public void RandomizeSpawnOption()
        {
            m_SpawnOptionIndex = -1;
        }

        /// <summary>
        /// Sets the <see cref="spawnOptionIndex"/> so that a specific object will spawn. If the index is out
        /// of bounds of the list defined in <see cref="objectPrefabs"/>, the index will not be changed.
        /// </summary>
        /// <param name="index">Index of the object to be spawned.</param>
        /// <seealso cref="objectPrefabs"/>
        /// <seealso cref="spawnOptionIndex"/>
        public void SetSpawnObjectIndex(int index)
        {
            if (index < m_ObjectPrefabs.Count)
                m_SpawnOptionIndex = index;
            else
                Debug.LogWarning("Object index specified larger than number of Object Prefabs.", this);
        }

        /// <summary>
        /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        /// <returns>Returns <see langword="true"/> if the spawner successfully spawned an object. Otherwise returns
        /// <see langword="false"/>.</returns>
        public bool TrySpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            var objectIndex = isSpawnOptionRandomized ? UnityEngine.Random.Range(0, m_ObjectPrefabs.Count) : m_SpawnOptionIndex;
            var newObject = Instantiate(m_ObjectPrefabs[objectIndex]);

            if (m_SpawnAsChildren)
                newObject.transform.parent = transform;

            // Позиция с небольшим отступом от стены
            var adjustedPosition = spawnPoint + spawnNormal * m_WallOffset;
            newObject.transform.position = adjustedPosition;

            // Если объект смотрит в стену, просто поворачиваем его на 180 градусов вокруг оси Y
            // Это самый простой способ исправить ориентацию

            // Сначала ориентируем объект как обычно
            if (m_OrientVertically)
            {
                Vector3 right = Vector3.Cross(spawnNormal, Vector3.up).normalized;

                if (right.magnitude < 0.001f)
                {
                    right = Vector3.Cross(spawnNormal, Vector3.forward).normalized;
                }

                Vector3 up = Vector3.Cross(right, -spawnNormal).normalized;
                newObject.transform.rotation = Quaternion.LookRotation(-spawnNormal, up);
            }
            else
            {
                newObject.transform.rotation = Quaternion.LookRotation(-spawnNormal, Vector3.up);
            }

            // Поворачиваем на 180 градусов, если объект смотрит в стену
            newObject.transform.Rotate(0, 180f, 180f, Space.Self);

            // Уменьшаем размер объекта в 4 раза
            newObject.transform.localScale = newObject.transform.localScale / 4f;

            if (m_SpawnVisualizationPrefab != null)
            {
                var visualizationTrans = Instantiate(m_SpawnVisualizationPrefab).transform;
                visualizationTrans.position = adjustedPosition;
                visualizationTrans.rotation = newObject.transform.rotation;
            }

            objectSpawned?.Invoke(newObject);
            return true;
        }

        /// <summary>
        /// Attempts to spawn an object from <see cref="objectPrefabs"/> at the given position.
        /// </summary>
        /// <param name="spawnPoint">The world space position at which to spawn the object.</param>
        /// <param name="spawnNormal">The world space normal of the spawn surface.</param>
        public void SpawnObject(Vector3 spawnPoint, Vector3 spawnNormal)
        {
            if (!TrySpawnObject(spawnPoint, spawnNormal))
                Debug.LogWarning("Could not spawn object.", this);
        }
    }
}