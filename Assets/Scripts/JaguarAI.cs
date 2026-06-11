using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(AudioSource))]
public class JaguarAI : MonoBehaviour
{
    [Header("Distance Settings")]
    public float walkRange = 15f;
    public float runRange = 10f;
    public float killRange = 2f;

    [Header("Speed Settings")]
    public float walkSpeed = 2.5f;
    public float runSpeed = 6.5f;

    private NavMeshAgent agent;
    private Animator anim;
    private AudioSource roarAudio;
    private Transform player;
    private bool isMoving;
    private Vector3 lastPosition;

    private  bool isPlayerDead = false;
    private bool hasRoared = false;

    public bool IsMoving => isMoving;

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        roarAudio = GetComponent<AudioSource>();
        isPlayerDead = false;
        player = FindPlayerTransform();
        lastPosition = transform.position;

        if (player == null)
        {
            Debug.LogError("JaguarAI: Could not find the player target. Please tag it as 'Player' or ensure Main Camera exists.");
        }

        isPlayerDead = false;

        agent.stoppingDistance = killRange;
        agent.acceleration = 12f;
        agent.angularSpeed = 250f;
    }

    private void Update()
    
    {
        //调试
            if (player == null)
        {
            Debug.LogWarning("Player is null, trying to find again...");
            player = FindPlayerTransform();
            if (player != null) Debug.Log($"Player found: {player.name}");
        }

        if (isPlayerDead)
            Debug.Log("isPlayerDead is true, jaguar won't move");

        if (player != null)
        {
            float distance1 = Vector3.Distance(transform.position, player.position);
            //Debug.Log($"Distance: {distance1}, walkRange: {walkRange}, runRange: {runRange}, killRange: {killRange}");
        }
        //调试结束


        if (player == null)
        {
            player = FindPlayerTransform();
        }

        // if (isPlayerDead || player == null)
        // {
        //     StopMovement();
        //     return;
        // }

        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= killRange)
        {
            HandleKill();
        }
        else if (distance <= runRange)
        {
            MoveTowardsPlayer(runSpeed);
        }
        else if (distance <= walkRange)
        {
            MoveTowardsPlayer(walkSpeed);
        }
        else
        {
            StopMovement();
        }
    }

    private void MoveTowardsPlayer(float targetSpeed)
    {
        if (!agent.isOnNavMesh)
        {
            isMoving = false;
            return;
        }

        agent.isStopped = false;
        agent.speed = targetSpeed;
        agent.SetDestination(player.position);

        // Treat having a path as "moving" so other scripts can react immediately.
        isMoving = agent.hasPath || agent.velocity.sqrMagnitude > 0.01f;
        anim.SetFloat("Speed", agent.velocity.magnitude);
    }

    private void LateUpdate()
    {
        bool movedThisFrame = (transform.position - lastPosition).sqrMagnitude > 0.0001f;

        if (agent != null && agent.isOnNavMesh)
        {
            bool agentIntentToMove = !agent.isStopped && (agent.hasPath || agent.velocity.sqrMagnitude > 0.01f);
            isMoving = movedThisFrame || agentIntentToMove;
        }
        else
        {
            isMoving = movedThisFrame;
        }

        lastPosition = transform.position;
    }

    private void StopMovement()
    {
        isMoving = false;

        if (agent.isOnNavMesh)
        {
            agent.isStopped = true;
        }

        //if (anim.HasParameter("Speed"))
           // anim.SetFloat("Speed", 0f);
    }

    private void HandleKill()
    {
        isPlayerDead = true;
        

        Vector3 lookDirection = (player.position - transform.position).normalized;
        lookDirection.y = 0f;
        if (lookDirection != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }

        if (roarAudio != null && !hasRoared)
        {
            roarAudio.Play();
            hasRoared = true;
        }
        StopMovement();
        Debug.Log("Player was caught by the jaguar. Trigger death UI here.");
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, walkRange);
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, runRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, killRange);
    }

    private Transform FindPlayerTransform()
    {
        GameObject playerObject = FindTaggedObject("MainCamera");
        if (playerObject == null)
        {
            playerObject = FindTaggedObject("mainCamera");
        }

        if (playerObject != null)
        {
            return playerObject.transform;
        }

        return Camera.main != null ? Camera.main.transform : null;
    }

    private GameObject FindTaggedObject(string tagName)
    {
        try
        {
            return GameObject.FindGameObjectWithTag(tagName);
        }
        catch (UnityException)
        {
            return null;
        }
    }
}
