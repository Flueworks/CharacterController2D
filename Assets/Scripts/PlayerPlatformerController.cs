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

    [Header("Wall jump")]
    public Vector2 walljumpSpeed = new Vector2(4,15);
    public float wallGrabSlideSpeed = -1.5f;

    public float wallStickTime = 0.1f;
    private float wallStickCounter = 0;
    protected WallHit wallHit;



    // Freeze direction in case of a slide or wall jump
    private Vector2 directionFreeze = Vector2.zero;
    private float directionFreezeTime = 0.15f;
    private float directionFreezeCounter = 0;
    private float directionFreezeGravity = 0;
    private bool directionFreezeY;

    private SpriteRenderer spriteRenderer;
    private Animator animator;
    
    private static readonly int Grounded = Animator.StringToHash("grounded");
    private static readonly int Speed = Animator.StringToHash("speed");
    private static readonly int Verticalspeed = Animator.StringToHash("verticalspeed");
    private static readonly int Hang = Animator.StringToHash("hang");
    private static readonly int Attack = Animator.StringToHash("attack");
    private static readonly int Slide = Animator.StringToHash("slide");
    private bool slidingOnWall;
    private bool noGravity;
    private bool instantStick = false;

    void Awake () 
    {
        spriteRenderer = GetComponent<SpriteRenderer> ();    
        animator = GetComponent<Animator> ();
    }
    
    protected override void ComputeVelocity()
    {
        BufferJumpInput();

        // reset variables
        noGravity = false;
        
        var x = Input.GetAxisRaw("Horizontal");

        if (CanMove())
        {
            // Player input movement
            DoJump();

            horizontalVelocity = x * moveSpeed;

            DoWall(x);

            if (Input.GetButtonDown("Attack"))
            {
                animator.SetTrigger(Attack);
            }

            if (Input.GetButtonDown("Dash"))
            {
                animator.SetBool(Slide, true);
                var direction = spriteRenderer.flipX ? -moveSpeed : moveSpeed;
                if (slidingOnWall)
                    direction = -direction;
                directionFreeze = new Vector2(direction * 1.5f, 0);
                directionFreezeY = true;
                directionFreezeGravity = 0;
                directionFreezeCounter = 0.3f;
            }
            else
            {
                animator.SetBool(Slide, false);
            }
        }
        else
        {
            horizontalVelocity = directionFreeze.x;
            if(directionFreezeY)
                velocity.y = directionFreeze.y;
            currentGravityModifier = directionFreezeGravity;
        }
        
        DoGravity();

        
        FlipSprite(horizontalVelocity);

        animator.SetBool (Grounded, grounded);
        animator.SetFloat (Speed, Mathf.Abs (horizontalVelocity) / moveSpeed);
        animator.SetFloat (Verticalspeed, velocity.y);
    }


    

    private void DoWall(float x)
    {
        if (grounded)
        {
            instantStick = false;
            animator.SetBool(Hang, false);
            wallStickCounter = 0;
            return;
        }

        if (wallHit == WallHit.None)
        {
            // no walls in sight, disregard everything
            slidingOnWall = false;
            animator.SetBool(Hang, false);
            return;
        }

        // touching wall

        // sliding on wall: touching wall AND not pressing away from wall for over 0.1 sec
        var pressingAwayFromWall = (wallHit == WallHit.Left && x > 0 || wallHit == WallHit.Right && x < 0);

        if (pressingAwayFromWall)
        {
            // start timer to detach
            wallStickCounter -= Time.deltaTime;

            slidingOnWall = wallStickCounter > 0;
        }
        else
        {
            // reset time to detach
            wallStickCounter = wallStickTime;
            slidingOnWall = true;
        }
        
        // horizontal movement

        if (slidingOnWall)
        {
            horizontalVelocity = 0;
        }

        // vertical movement
        if (slidingOnWall)
        {
            if (velocity.y <= 0 || instantStick)
            {
                velocity.y = wallGrabSlideSpeed;
                noGravity = true;

                animator.SetBool(Hang, true);
            }

            if (Input.GetButtonDown("Jump"))
            {
                wallStickCounter = 0;
                slidingOnWall = false;
                noGravity = false;
                
                velocity.y = jumpTakeOffSpeed;
                horizontalVelocity = wallHit == WallHit.Left ? moveSpeed : -moveSpeed;
                
                directionFreezeCounter = directionFreezeTime;
                directionFreeze = new Vector2(horizontalVelocity, walljumpSpeed.y);
                directionFreezeY = false;
                directionFreezeGravity = 0;

                instantStick = true;
                
                wallHit = WallHit.None; // to prevent user from grabbing wall again before frame has updated
            }
        }
    }

    private void FlipSprite(float x)
    {
        if (x != 0) spriteRenderer.flipX = x < 0f;
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
        if (noGravity)
        {
            currentGravityModifier = 0.0f;
            return;
        }
        if (Input.GetButton("Jump") && (velocity.y > 0 ))
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
        if (Input.GetButtonDown("Jump"))
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

        CheckWallHits();
    }

    private void CheckWallHits()
    {
        wallHit = WallHit.None;

        if(grounded) // don't check for walls when grounded
            return;

        var hits = rb2d.Cast(Vector2.left, contactFilter, hitBuffer, 0.1f);

        for (int i = 0; i < hits; i++)
        {
            if (hitBuffer[i].normal.y == 0)
            {
                wallHit = WallHit.Left;
                return;
            }
        }

        hits = rb2d.Cast(Vector2.right, contactFilter, hitBuffer, 0.1f);

        for (int i = 0; i < hits; i++)
        {
            if (hitBuffer[i].normal.y == 0)
            {
                wallHit = WallHit.Right;
                return;
            }
        }
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
