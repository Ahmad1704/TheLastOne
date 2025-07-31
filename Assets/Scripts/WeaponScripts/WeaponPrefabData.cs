using UnityEngine;

public class WeaponPrefabData : MonoBehaviour
{
    [Header("Fire Point Settings")]
    public string firePointName = "FirePoint";
    public Vector3 firePointOffset = new(0, 0, 1f);
    public string[] multipleFirePointNames = { "FirePoint1", "FirePoint2" };

    private Transform cachedFirePoint;
    private Transform[] cachedMultipleFirePoints;
    private bool hasSearched;

    private void Awake() => FindFirePoints();

    private void FindFirePoints()
    {
        if (hasSearched) return;
        hasSearched = true;

        cachedFirePoint = FindTransform(firePointName) ?? FindTransformFromList(new[]
        {
            "FirePoint", "Muzzle", "BarrelEnd", "BulletSpawn", "Barrel"
        });

        if (multipleFirePointNames.Length > 0)
        {
            cachedMultipleFirePoints = new Transform[multipleFirePointNames.Length];
            for (int i = 0; i < multipleFirePointNames.Length; i++)
                cachedMultipleFirePoints[i] = FindTransform(multipleFirePointNames[i]);
        }
    }

    private Transform FindTransform(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        return transform.Find(name) ?? FindChildRecursive(transform, name);
    }

    private Transform FindTransformFromList(string[] names)
    {
        foreach (var name in names)
        {
            var result = FindTransform(name);
            if (result) return result;
        }
        return null;
    }

    private static Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                return child;

            var found = FindChildRecursive(child, name);
            if (found) return found;
        }
        return null;
    }

    public Transform GetFirePoint()
    {
        if (!hasSearched) FindFirePoints();
        return cachedFirePoint ?? CreateTempFirePoint();
    }

    public Transform GetFirePoint(int index)
    {
        if (!hasSearched) FindFirePoints();
        return (cachedMultipleFirePoints != null && index >= 0 && index < cachedMultipleFirePoints.Length)
            ? cachedMultipleFirePoints[index] ?? GetFirePoint()
            : GetFirePoint();
    }

    public int GetFirePointCount()
    {
        if (!hasSearched) FindFirePoints();

        if (cachedMultipleFirePoints != null)
        {
            int count = 0;
            foreach (var fp in cachedMultipleFirePoints)
            {
                if (fp != null) count++;
            }
            return count;
        }

        return cachedFirePoint != null ? 1 : 0;
    }
    private Transform CreateTempFirePoint()
    {
        var existing = transform.Find("_TempFirePoint");
        if (existing != null) return existing;

        GameObject temp = new("_TempFirePoint");
        temp.transform.SetParent(transform);
        temp.transform.localPosition = firePointOffset;
        temp.transform.localRotation = Quaternion.identity;
        return temp.transform;
    }

    private void OnDrawGizmosSelected()
    {
        if (!hasSearched) FindFirePoints();

        if (cachedFirePoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(cachedFirePoint.position, 0.1f);
            Gizmos.DrawRay(cachedFirePoint.position, cachedFirePoint.forward * 0.5f);
        }
        else
        {
            Gizmos.color = Color.yellow;
            var tempPos = transform.TransformPoint(firePointOffset);
            Gizmos.DrawWireSphere(tempPos, 0.1f);
            Gizmos.DrawRay(tempPos, transform.forward * 0.5f);
        }

        if (cachedMultipleFirePoints == null) return;

        Gizmos.color = Color.blue;
        foreach (var fp in cachedMultipleFirePoints)
        {
            if (fp == null) continue;
            Gizmos.DrawWireSphere(fp.position, 0.08f);
            Gizmos.DrawRay(fp.position, fp.forward * 0.3f);
        }
    }
}
