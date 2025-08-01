using UnityEngine;

public class BulletCollisionHandler : MonoBehaviour
{
    private Bullet parentBullet;
    
    private void Awake()
    {
        // Find the parent Bullet component
        parentBullet = GetComponentInParent<Bullet>();
        
        if (parentBullet == null)
        {
            Debug.LogError("BulletCollisionHandler could not find parent Bullet component!");
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (parentBullet != null)
        {
            parentBullet.HandleCollision(other);
        }
    }
}