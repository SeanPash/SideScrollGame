    using UnityEngine;
    using System.Collections;

    public class RegularCrabBehavior : MonoBehaviour
    {
        public float detectionRange = 6f;
        public float moveSpeed = 1f;
        public Transform player;
        public Transform groundCheck;
        public LayerMask groundLayer;
        public float groundCheckDistance = 0.1f;

        private Rigidbody2D rb;
        private Animator animator;

        private bool isAttacking = false;
        private bool movingRight = true;
        private int attackIndex = 0;

        private float wanderTimer = 0f;
        private float wanderDuration = 2f;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
        }

        private void Update()
        {
            if (isAttacking) return;

            float distance = Vector2.Distance(transform.position, player.position);

            if (distance <= detectionRange)
            {
                ChaseAndAttack();
            }
            else
            {
                Wander();
            }
        }

        private void ChaseAndAttack()
        {
            Vector2 direction = (player.position - transform.position).normalized;


            float distance = Vector2.Distance(transform.position, player.position);

            Vector3 scale = transform.localScale;
            scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;

            if (distance > .52f)
            {
                rb.linearVelocity = new Vector2(direction.x * moveSpeed, rb.linearVelocity.y);
                animator.Play("Enemy Run");
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
                StartCoroutine(DoAttack());
            }
        }

        private IEnumerator DoAttack()
        {
            isAttacking = true;

            string[] attackNames = { "Enemy Attack 1", "Enemy Attack 2", "Enemy Attack 3" };
            string attackName = attackNames[attackIndex % attackNames.Length];
            animator.Play(attackName);

            attackIndex++;

            yield return new WaitForSeconds(1f); 
            yield return new WaitForSeconds(1.5f);

            isAttacking = false;
        }

        private void Wander()
        {
            wanderTimer += Time.deltaTime;

            if (wanderTimer >= wanderDuration)
            {
                wanderTimer = 0f;
                movingRight = !movingRight; // switch direction
            }

            if (!IsGroundAhead())
            {
                movingRight = !movingRight;
                wanderTimer = 0f;
            }
            


            float direction = movingRight ? 1f : -1f;
            rb.linearVelocity = new Vector2(direction * moveSpeed, rb.linearVelocity.y);
            Vector3 scale = transform.localScale;
            scale.x = direction > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
            transform.localScale = scale;

            animator.Play("Enemy Run");
        }

        private bool IsGroundAhead()
        {
            Vector2 checkPos = groundCheck.position + Vector3.right * (movingRight ? 0.3f : -0.3f);
            RaycastHit2D hit = Physics2D.Raycast(checkPos, Vector2.down, groundCheckDistance, groundLayer);
            return hit.collider != null;
        }
    }
