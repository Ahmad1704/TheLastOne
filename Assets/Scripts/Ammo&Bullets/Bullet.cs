using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Bullet : MonoBehaviour
{
    private float damage;
    private float range;
    private float speed;
    private float distanceTraveled;
    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }

    public void Initialize(float damage, float range, float speed)
    {
        this.damage = damage;
        this.range = range;
        this.speed = speed;
        distanceTraveled = 0f;
        
        // Set velocity instead of moving in Update
        rb.linearVelocity = transform.forward * speed;
    }

    private void FixedUpdate()
    {
        // Track distance by accumulating movement
        distanceTraveled += rb.linearVelocity.magnitude * Time.fixedDeltaTime;
        
        // Check range
        if (distanceTraveled > range)
        {
            Destroy(gameObject);
        }
    }

    // This method is called by the BulletCollisionHandler on the child collider
    public void HandleCollision(Collider other)
    {
        if (other.TryGetComponent<IDamageable>(out var damageable))
        {
            damageable.TakeDamage(damage);
            Destroy(gameObject);
        }
    }
}