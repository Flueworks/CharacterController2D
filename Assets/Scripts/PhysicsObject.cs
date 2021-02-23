using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PhysicsObject : MonoBehaviour
{
    public float minGroundNormalY = .65f;
    public float gravityModifier = 1f;
    public float maxFallSpeed = 10;

    protected float horizontalVelocity;
    protected bool grounded;
    protected WallHit wallHit;
    protected Vector2 groundNormal;
    protected Rigidbody2D rb2d;
    protected Vector2 velocity;
    protected ContactFilter2D contactFilter;
    protected readonly RaycastHit2D[] hitBuffer = new RaycastHit2D[16];

    protected const float minMoveDistance = 0.001f;
    protected const float shellRadius = 0.02f;

    protected float currentGravityModifier = 0;

    protected bool customGravity = false;

    void OnEnable()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        contactFilter.useTriggers = false;
        contactFilter.SetLayerMask (Physics2D.GetLayerCollisionMask (gameObject.layer));
        contactFilter.useLayerMask = true;
    }

    private void Update()
    {
        horizontalVelocity = 0;
        currentGravityModifier = gravityModifier;
        ComputeVelocity ();
    }

    protected virtual void ComputeVelocity()
    {

    }

    private void FixedUpdate()
    {
        velocity += Physics2D.gravity * (currentGravityModifier * Time.deltaTime);

        velocity.y = Mathf.Max(-maxFallSpeed, velocity.y);
        velocity.x = horizontalVelocity;

        grounded = false;
        wallHit = WallHit.None;

        var deltaPosition = velocity * Time.deltaTime;

        var moveAlongGround = new Vector2 (groundNormal.y, -groundNormal.x);

        var move = moveAlongGround * deltaPosition.x;

        Movement (move, false);

        move = Vector2.up * deltaPosition.y;

        Movement (move, true);

        PostFixedUpdate();

    }

    protected virtual void PostFixedUpdate()
    {
        
    }

    void Movement(Vector2 move, bool yMovement)
    {
        var distance = move.magnitude;

        if (distance > minMoveDistance) 
        {
            var count = rb2d.Cast (move, contactFilter, hitBuffer, distance + shellRadius);

            for (var i = 0; i < count; i++) 
            {
                var currentNormal = hitBuffer [i].normal;
                if (currentNormal.y > minGroundNormalY) 
                {
                    grounded = true;
                    if (yMovement) 
                    {
                        groundNormal = currentNormal;
                        currentNormal.x = 0;
                    }
                }

                if (currentNormal.y == 0)
                {
                    wallHit = currentNormal.x > 0 ? WallHit.Left : WallHit.Right;
                }

                var projection = Vector2.Dot (velocity, currentNormal);
                if (projection < 0) 
                {
                    velocity = velocity - projection * currentNormal;
                }

                var modifiedDistance = hitBuffer [i].distance - shellRadius;
                distance = modifiedDistance < distance ? modifiedDistance : distance;
            }
            
        }

        rb2d.position = rb2d.position + move.normalized * distance;
    }

}

public enum WallHit
{
    None,
    Left,
    Right
}
