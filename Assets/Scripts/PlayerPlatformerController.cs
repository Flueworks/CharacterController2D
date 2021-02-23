using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Animator))]
public class PlayerPlatformerController : PhysicsObject
{
    public float moveSpeed = 7;
    
    public float jumpTakeOffSpeed = 7;
    [Tooltip("Gravity applied when space bar is held")]
    public float highJumpGravityModifier = 1f;

    [Header("Jump forgiveness")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter = 0;

    public float jumpBuffer = 0.1f;
    private float jumpBufferCounter = 0;
    private float wallJumpBufferCounter = 0;

    [Header("Wall jump")]
    public Vector2 walljumpSpeed = new Vector2(4,7);
    public float wallGrabSlideSpeed = -1.5f;


    // Freeze direction in case of a slide or wall jump
    private Vector2 directionFreeze = Vector2.zero;
    private float directionFreezeTime = 0.15f;
    private float directionFreezeCounter = 0;
    
    private SpriteRenderer spriteRenderer;
    private Animator animator;
    private static readonly int Grounded = Animator.StringToHash("grounded");
    private static readonly int Speed = Animator.StringToHash("speed");
    private static readonly int Verticalspeed = Animator.StringToHash("verticalspeed");
    private static readonly int Hang = Animator.StringToHash("hang");

    void Awake () 
    {
        spriteRenderer = GetComponent<SpriteRenderer> ();    
        animator = GetComponent<Animator> ();
    }
    
    protected override void ComputeVelocity()
    {
        BufferJumpInput();
        
        if (CanMove())
        {
            DoGravity();
            
            // Player input movement
            DoJump();
            
            var x = Input.GetAxisRaw("Horizontal");
            horizontalVelocity = x * moveSpeed;

            DoWallSlide(x);
        }
        else
        {
            horizontalVelocity = directionFreeze.x;
            velocity.y = directionFreeze.y;
        }
        
        FlipSprite(horizontalVelocity);

        animator.SetBool (Grounded, grounded);
        animator.SetFloat (Speed, Mathf.Abs (horizontalVelocity) / moveSpeed);
        animator.SetFloat (Verticalspeed, velocity.y);
    }

    private void DoWallSlide(float x)
    {
        if (!grounded && velocity.y <= 0 && (wallHit == WallHit.Left && x < 0 || wallHit == WallHit.Right && x > 0))
        {
            wallJumpBufferCounter = jumpBuffer;
            velocity.y = wallGrabSlideSpeed;
            currentGravityModifier = 0;
            animator.SetBool(Hang, true);
        }
        else
        {
            animator.SetBool(Hang, false);
        }

        if (wallJumpBufferCounter > 0)
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                wallJumpBufferCounter = 0;
                velocity.y = walljumpSpeed.y;
                directionFreezeCounter = directionFreezeTime;
                directionFreeze = new Vector2(wallHit == WallHit.Left ? walljumpSpeed.x : -walljumpSpeed.x, walljumpSpeed.y);
                wallHit = WallHit.None;
                animator.SetBool(Hang, false);
            }
            else
                wallJumpBufferCounter -= Time.deltaTime;
        }
    }

    private void FlipSprite(float x)
    {
        if (x != 0)
        {
            spriteRenderer.flipX = x < 0f;
            //Debug.Log(spriteRenderer.flipX);
        }
    }

    private bool CanMove()
    {
        if (directionFreezeCounter <= 0)
            return true;
        directionFreezeCounter -= Time.deltaTime;
        return false;
    }

    private void DoJump()
    {
        if (jumpBufferCounter > 0 && coyoteTimeCounter > 0) 
        {
            velocity.y = jumpTakeOffSpeed;
            jumpBufferCounter = 0;
            coyoteTimeCounter = 0;
        }
    }

    private void DoGravity()
    {
        if (Input.GetKey(KeyCode.Space) && (velocity.y > 0 ))
        {
            currentGravityModifier = highJumpGravityModifier;
        }
        else
        {
            currentGravityModifier = gravityModifier;
        }
    }
    
    private void BufferJumpInput()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpBufferCounter = jumpBuffer;
        }
        else if (jumpBufferCounter > 0)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    protected override void PostFixedUpdate()
    {
        // Must buffer coyote time after movement has happened in fixed update, not in update.
        // If not, we would register ground after a jump has occurred, even though we haven't moved yet,
        // granting the player a double jump
        BufferCoyoteTime();
    }
    
    private void BufferCoyoteTime()
    {
        if (grounded)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else if (coyoteTimeCounter > 0)
        {
            coyoteTimeCounter -= Time.deltaTime;
        }
    }
}
