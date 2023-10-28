using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance;
    public float PoolableObjectQuantityFactor = 1.0f;
    // Pools Settings
    public List<PoolableSettings> PoolableSettings;
    // Pools
    private Dictionary<PoolName, Pool> Pools;
    // Events
    public static event Action OnPoolingReady;
    public static bool PoolReady = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    IEnumerator Start()
    {
        // Create Pools
        Pools = new Dictionary<PoolName, Pool>();
        foreach (PoolableSettings poolableSettings in PoolableSettings)
        {
            poolableSettings.NumberOfObjects = (int)(PoolableObjectQuantityFactor * (float)poolableSettings.NumberOfObjects);
            CreatePool(poolableSettings);
            yield return null;
            yield return null;
        }
        OnPoolingReady?.Invoke();
        PoolReady = true;
        StartCoroutine(AutoPushBackToPool());
    }

    private void CreatePool(PoolableSettings Settings)
    {
        Pool pool = new Pool(Settings);
        Pools.Add(pool.Name, pool);
    }

    public bool PoolExist(PoolName PoolName)
    {
        return Pools.ContainsKey(PoolName);
    }

    public PoolableObject GetPoolable(PoolName PoolName, float lifeTime = float.MaxValue)
    {
        return Pools[PoolName].GetPoolable(lifeTime);
    }

    public Pool GetPool(PoolName PoolName)
    {
        return Pools[PoolName];
    }

    public List<T> GetComponentsOnPoolables<T>(PoolName PoolName)
    {
        List<T> components = new List<T>();
        Pool pool = Pools[PoolName];
        foreach (PoolableObject poolable in pool.Poolables)
        {
            T component = poolable.GameObject.GetComponent<T>();
            if (component == null)
                component = poolable.GameObject.GetComponentInChildren<T>();
            if (component != null)
                components.Add(component);
        }
        return components;
    }

    public void PushBackToPool(PoolableObject PoolableObject)
    {
        Pools[PoolableObject.PoolName].PushBackToPool(PoolableObject);
    }

    public void PushBackAllPool(PoolName pool)
    {
        Pools[pool].PushBackAllPool();
    }

    IEnumerator AutoPushBackToPool()
    {
        while (true)
        {
            foreach (var pool in Pools)
            {
                foreach (PoolableObject poolable in pool.Value.Poolables)
                {
                    if (poolable.TimeToPushBackToPull < Time.unscaledTime)
                    {
                        pool.Value.PushBackToPool(poolable);
                    }
                }
                yield return null;
            }
        }
    }
}

public enum PoolName
{
    POCell,
    Rock
}

[System.Serializable]
public class PoolableSettings
{
    public PoolName Name;
    public GameObject[] GameObjects;
    public int NumberOfObjects = 50;
}

public class Pool
{
    public PoolName Name;
    public Queue<PoolableObject> Poolables;
    public Transform parent;

    public Pool(PoolableSettings Settings)
    {
        Name = Settings.Name;
        parent = new GameObject(Name + "_PoolContainer").transform;
        parent.transform.SetParent(PoolManager.Instance.transform);
        Poolables = new Queue<PoolableObject>();

        // Create pooled objects
        for (int i = 0; i < Settings.NumberOfObjects; i++)
        {
            // Create Poolable Object
            PoolableObject poolableObject = new PoolableObject();
            poolableObject.GameObject = GameObject.Instantiate(Settings.GameObjects[UnityEngine.Random.Range(0, Settings.GameObjects.Length - 2)], parent);
            poolableObject.Transform = poolableObject.GameObject.transform;
            poolableObject.Transform.localPosition = Vector3.zero;
            poolableObject.Transform.rotation = Quaternion.identity;
            poolableObject.GameObject.SetActive(false);
            poolableObject.IPoolable = poolableObject.GameObject.GetComponentInChildren<IPoolable>(true);
            poolableObject.PoolName = Settings.Name;
            poolableObject.SetTime(float.MaxValue);
            // play particle system to avoid lag on first use
            ParticleSystem poolableObjectParticleSystem = poolableObject.GameObject.GetComponentInChildren<ParticleSystem>(true);
            // Get IPoolable on gameObject and initialize it
            if (poolableObject.IPoolable != null)
                poolableObject.IPoolable.OnPoolableObjectInitialized(poolableObject);
            // Enqueue Poolable Object
            Poolables.Enqueue(poolableObject);
        }
    }

    public PoolableObject GetPoolable(float lifeTime = float.MaxValue)
    {
        int nbInQueue = Poolables.Count;
        PoolableObject poolable = Poolables.Dequeue();
        int nbLocked = 0;
        while (poolable.Locked)
        {
            Poolables.Enqueue(poolable);
            poolable = Poolables.Dequeue();
            nbLocked++;
            if (nbLocked >= nbInQueue)
                poolable.UnLock();
        }

        if (!poolable.GameObject.activeSelf)
            poolable.GameObject.SetActive(true);
        poolable.SetTime(lifeTime);
        poolable.IPoolable?.OnGetPoolable(poolable);
        Poolables.Enqueue(poolable);
        poolable.Fire_OnGetPoolable();
        return poolable;
    }

    public void PushBackToPool(PoolableObject poolable)
    {
        if (poolable.Pooled)
            return;
        poolable.UnLock();
        poolable.IPoolable?.OnPushBackToPool(poolable);
        poolable.Transform.SetParent(parent);
        poolable.Fire_OnPushBacktoPool();
        poolable.GameObject.SetActive(false);
        poolable.SetTime(float.MaxValue);
    }

    public void PushBackAllPool()
    {
        foreach (var poolable in Poolables)
            PushBackToPool(poolable);
    }
}

public class PoolableObject
{
    public GameObject GameObject;
    public Transform Transform;
    public IPoolable IPoolable;
    public PoolName PoolName;
    public bool Pooled { get; private set; }
    public bool Locked { get; private set; }
    public float TimeToPushBackToPull { get; private set; }
    public event Action OnGetPoolable;
    public event Action OnPushBacktoPool;

    public void Fire_OnGetPoolable()
    {
        Pooled = false;
        OnGetPoolable?.Invoke();
    }

    public void Fire_OnPushBacktoPool()
    {
        Pooled = true;
        OnPushBacktoPool?.Invoke();
    }

    public PoolableObject()
    {
        Locked = false;
        Pooled = false;
    }

    public void Lock()
    {
        Locked = true;
    }

    public void UnLock()
    {
        Locked = false;
    }

    /// <summary>
    /// set the fixed time to kill (don't use if you don't know, prefere SetLifeTime)
    /// </summary>
    /// <param name="timeToPushBackToPull"></param>
    public void SetTime(float timeToPushBackToPull)
    {
        TimeToPushBackToPull = timeToPushBackToPull;
    }

    /// <summary>
    /// Set the lifetime of the poolable object, will disapear after that time (in second)
    /// </summary>
    /// <param name="lifeTime">life time in second</param>
    public void SetLifeTime(float lifeTime)
    {
        TimeToPushBackToPull = Time.unscaledTime + lifeTime;
    }

    public void Kill()
    {
        UnLock();
        PoolManager.Instance.PushBackToPool(this);
    }
}

public interface IPoolable
{
    void OnGetPoolable(PoolableObject poolableObject);

    void OnPushBackToPool(PoolableObject poolableObject);

    void OnPoolableObjectInitialized(PoolableObject poolableObject);
}