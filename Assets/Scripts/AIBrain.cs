using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class AIBrain : MonoBehaviour
{
    [Header("References")]
    public Transform playerTarget;
    public NavMeshAgent Agent { get; private set; }

    [Header("Config")]
    public float detectionRadius = 10f;
    public float fleeDistance = 15f;
    public LayerMask obstacleLayer;

    // The State Machine instance
    private StateMachine _stateMachine;

    // Define States Here (or instantiate them in Awake)
    // We keep them public or accessible so states can switch to each other
    public IState IdleState { get; private set; }
    public IState GatherState { get; private set; }
    public IState FleeState { get; private set; }

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        _stateMachine = new StateMachine();

        // Initialize concrete states (will write these next)
        // For now, these will be placeholders
        IdleState = new StateIdle();
        GatherState = new StateGather();
        FleeState = new StateFlee();
    }

    private void Start()
    {
        // Start in Idle
        _stateMachine.Initialize(IdleState, this);
    }

    private void Update()
    {
        _stateMachine.Update(this);
    }

    // --- VISUAL DEBUGGING ---
    // This draws the current state above the head.
    private void OnDrawGizmos()
    {
        if (_stateMachine != null && _stateMachine.CurrentState != null)
        {
            // Draw detection radius
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRadius);

            // Label the current state (Unity Editor only)
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"State: {_stateMachine.CurrentState.GetType().Name}");
#endif
        }
    }

    // Helper to check for threats (Common logic used by multiple states)
    public bool IsThreatDetected()
    {
        if (playerTarget == null) return false;

        float distanceToPlayer = Vector3.Distance(transform.position, playerTarget.position);

        // Simple distance check. 
        // TODO: Add LineOfSight checks 
        // or DotProduct checks for field of view.
        return distanceToPlayer < detectionRadius;
    }

    public void ChangeState(IState newState)
    {
        _stateMachine.ChangeState(newState, this);
    }
}

public class StateIdle : IState
{
    public void Enter(AIBrain brain) { Debug.Log("Entering Idle"); }

    public void Execute(AIBrain brain)
    {
        // Example Transition Logic
        if (brain.IsThreatDetected())
        {
            brain.ChangeState(brain.FleeState);
        }
    }

    public void Exit(AIBrain brain) { }
}

public class StateGather : IState
{
    public void Enter(AIBrain brain) { Debug.Log("Entering Gather"); }
    public void Execute(AIBrain brain)
    {
        if (brain.IsThreatDetected())
        {
            brain.ChangeState(brain.FleeState); // Priority interrupt
            return;
        }
    }
    public void Exit(AIBrain brain) { }
}

public class StateFlee : IState
{
    public void Enter(AIBrain brain)
    {
        Debug.Log("Entering Flee - RUN!");
        brain.Agent.isStopped = false;
        brain.Agent.speed = 6f; // Run faster
    }

    public void Execute(AIBrain brain)
    {
        // If safe, go back to idle
        if (!brain.IsThreatDetected())
        {
            brain.ChangeState(brain.IdleState);
        }
    }

    public void Exit(AIBrain brain)
    {
        brain.Agent.speed = 3.5f; // Reset speed
    }
}

// extension method to AIBrain or StateMachine to make switching easier
//public static class AIStateExtensions
//{
//    public static void ChangeState(this AIBrain brain, IState newState)
//    {
//        // Accessing the private state machine via a public method wrapper 
//        // (You might want to expose a public method in AIBrain to do this cleanly)
//    }
//}