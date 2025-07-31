using UnityEngine;
public static class FirePointFinder
{
    public static Transform FindFirePoint(Transform weaponRoot, string preferredName = "FirePoint")
    {
        // Try preferred name first
        if (!string.IsNullOrEmpty(preferredName))
        {
            Transform found = FindByName(weaponRoot, preferredName);
            if (found != null) return found;
        }
        
        // Try common fire point names
        string[] commonNames = { 
            "FirePoint", "Muzzle", "BarrelEnd", "BulletSpawn", 
            "Barrel", "Gun_End", "Weapon_Tip" 
        };
        
        foreach (string name in commonNames)
        {
            Transform found = FindByName(weaponRoot, name);
            if (found != null) return found;
        }
        
        // If nothing found, create one at weapon tip
        return CreateFirePointAtTip(weaponRoot);
    }
    
    private static Transform FindByName(Transform parent, string name)
    {
        // Direct search first
        Transform direct = parent.Find(name);
        if (direct != null) return direct;
        
        // Recursive search
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name.IndexOf(name, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return child;
                
            Transform found = FindByName(child, name);
            if (found != null) return found;
        }
        return null;
    }
    
    private static Transform CreateFirePointAtTip(Transform weaponRoot)
    {
        // Find the bounds of the weapon to place fire point at the front
        Renderer[] renderers = weaponRoot.GetComponentsInChildren<Renderer>();
        if (renderers.Length > 0)
        {
            Bounds combinedBounds = renderers[0].bounds;
            foreach (var renderer in renderers)
            {
                combinedBounds.Encapsulate(renderer.bounds);
            }
            
            // Create fire point at the front of the weapon
            GameObject firePoint = new GameObject("Auto_FirePoint");
            firePoint.transform.SetParent(weaponRoot);
            
            // Position at front of weapon bounds
            Vector3 localFrontPoint = weaponRoot.InverseTransformPoint(
                combinedBounds.center + weaponRoot.forward * (combinedBounds.size.z * 0.5f)
            );
            
            firePoint.transform.localPosition = localFrontPoint;
            firePoint.transform.localRotation = Quaternion.identity;
            
            return firePoint.transform;
        }
        
        // Fallback: create at weapon root with forward offset
        GameObject fallbackFirePoint = new GameObject("Fallback_FirePoint");
        fallbackFirePoint.transform.SetParent(weaponRoot);
        fallbackFirePoint.transform.localPosition = Vector3.forward;
        fallbackFirePoint.transform.localRotation = Quaternion.identity;
        
        return fallbackFirePoint.transform;
    }
}