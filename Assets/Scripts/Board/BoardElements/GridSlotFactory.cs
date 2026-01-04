using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class GridSlotFactory : MonoBehaviour 
{
    [SerializeField] bool collectionCheck = true;
    [SerializeField] int defaultCapacity = 10;
    [SerializeField] int maxPoolSize = 100;
    public static GridSlotFactory instance;
    readonly Dictionary<string, IObjectPool<GridSlot>> pools = new();

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static GridSlot Spawn(EltType s) => instance.GetPoolFor(s)?.Get();
    public static void ReturnToPool(GridSlot g) => instance.GetPoolFor(g.type)?.Release(g);


    IObjectPool<GridSlot> GetPoolFor(EltType settings)
    {
        IObjectPool<GridSlot> pool;

        if (pools.TryGetValue(settings.EltName, out pool)) return pool;

        pool = new ObjectPool<GridSlot>(
                settings.Create,
                settings.OnGet,
                settings.OnRelease,
                settings.OnDestroyPoolObject,
                collectionCheck,
                defaultCapacity,
                maxPoolSize
        );
        pools.Add(settings.EltName, pool);
        return pool;
    }
}
