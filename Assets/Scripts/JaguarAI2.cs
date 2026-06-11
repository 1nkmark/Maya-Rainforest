using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JaguarAI2 : MonoBehaviour
{
    [Header("Distance Settings")]
    public float walkRange = 15f;
    public float stopDistance = 2f;

    [Header("Roar Settings")]
    public float roarDistance = 5f;
    public AudioClip roarClip;
    [Range(0f, 1f)]
    public float roarVolume = 0.8f;

    [Header("Speed Settings")]
    public float walkSpeed = 2.5f;

    private NavMeshAgent agent;
    private Animator anim;
    private AudioSource audioSource;
    private Transform player;

    private bool hasRoared = false;
    private bool isMoving = false; // 新增：状态锁，防止每帧重复调用

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        player = FindPlayerTransform();

        agent.speed = walkSpeed;
        agent.stoppingDistance = stopDistance; // 建议将这个交给 Agent 自动处理一部分
        agent.updateRotation = true;

        if (audioSource != null && roarClip != null)
        {
            audioSource.clip = roarClip;
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
            audioSource.volume = roarVolume;
        }
    }

    void Update()
    {
        if (player == null)
        {
            player = FindPlayerTransform();
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);

        if (!hasRoared && distance <= roarDistance)
        {
            PlayRoar();
        }

        // 优化后的移动逻辑
        if (distance <= stopDistance)
        {
            if (isMoving) StopMovement();
        }
        else if (distance <= walkRange)
        {
            MoveTowardsPlayer();
        }
        else
        {
            if (isMoving) StopMovement();
        }
    }

    private void MoveTowardsPlayer()
    {
        if (!agent.isOnNavMesh) return;

        // 持续更新目标点，但动画状态只触发一次
        agent.isStopped = false;
        agent.SetDestination(player.position);
        
        if (!isMoving)
        {
            anim.SetBool("IsWalking", true);
            isMoving = true;
        }
    }

    private void StopMovement()
    {
        if (!agent.isOnNavMesh) return;

        agent.isStopped = true;
        agent.ResetPath(); 
        anim.SetBool("IsWalking", false);
        isMoving = false;
    }

    private void PlayRoar()
    {
        if (audioSource != null && roarClip != null)
        {
            audioSource.PlayOneShot(roarClip, roarVolume);
            hasRoared = true;
        }
    }

    private Transform FindPlayerTransform()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("player");
        if (playerObj != null) return playerObj.transform;

        playerObj = GameObject.FindGameObjectWithTag("MainCamera");
        if (playerObj != null) return playerObj.transform;

        if (Camera.main != null) return Camera.main.transform;
        return null;
    }
}