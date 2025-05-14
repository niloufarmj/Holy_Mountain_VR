using UnityEngine;

public class SimpleWolfAI : MonoBehaviour
{
    public Animator animator;
    public float walkSpeed = 1.5f;
    public float runSpeed = 3.5f;
    public float changeDirInterval = 5f;
    public float actionInterval = 8f;

    public float rotationSpeed = 3f; 

    private Vector3 moveDirection;
    private float actionTimer;
    private float dirTimer;

    public GaiaLightingSwitcher lightingManager;

    void Start()
    {
        PickNewDirection();
        actionTimer = actionInterval;
        dirTimer = changeDirInterval;
    }

    void Update()
    {
        RaycastHit hit;
        Vector3 rayOrigin = transform.position + Vector3.up * 5f;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 10f))
        {
            // موقعیت Y
            Vector3 pos = transform.position;
            pos.y = hit.point.y;
            transform.position = pos;

            // همراستایی با سطح زمین
            Quaternion terrainRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, terrainRotation, Time.deltaTime * 5f);
        }

        if (lightingManager != null && lightingManager.IsNight())
        {
            animator.SetBool("IsSleeping", true);
            animator.SetBool("IsSitting", false);
            animator.SetFloat("Speed", 0);
            return;
        }

        animator.SetBool("IsSleeping", false);

        actionTimer -= Time.deltaTime;
        dirTimer -= Time.deltaTime;

        

        // حرکت کردن
        transform.position += moveDirection * Time.deltaTime;
        animator.SetFloat("Speed", moveDirection.magnitude);

        // اگر داره حرکت می‌کنه، جهت رو نرم بچرخون
        if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);
        }

        

        if (actionTimer <= 0f)
        {
            float rand = Random.value;

            if (rand < 0.3f)
            {
                moveDirection = Vector3.zero;
                animator.SetBool("IsSitting", true);
            }
            else
            {
                animator.SetBool("IsSitting", false);
                PickNewDirection();
            }

            actionTimer = Random.Range(5f, 10f);
        }

        if (dirTimer <= 0f && moveDirection != Vector3.zero)
        {
            PickNewDirection();
            dirTimer = Random.Range(3f, 6f);
        }
    }

    void PickNewDirection()
    {

        
        float isRunning = Random.value;

        Vector3 dir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        float speed = isRunning < 0.3f ? runSpeed : walkSpeed;
        

        float angle = Vector3.Angle(transform.forward, moveDirection.normalized);

        if (angle > 100f) // تغییر جهت ناگهانی
        {
            moveDirection = Vector3.zero;
            animator.SetFloat("Speed", 0f);
            animator.SetBool("IsSitting", false);
            animator.SetBool("IsSleeping", false);
            animator.Play("Wolf_Idle"); // یا animator.SetTrigger("Idle")

            Invoke("ResumeMovement", 1.2f); // مکث یک ثانیه
            return;
        }

        moveDirection = dir * speed;

        Quaternion targetRot = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * rotationSpeed);


    }

    void ResumeMovement()
    {
        PickNewDirection();
    }
}
