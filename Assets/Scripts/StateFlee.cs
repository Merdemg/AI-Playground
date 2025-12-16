using UnityEngine;
using UnityEngine.AI;

public class StateFlee : IState
{
    // Settings for the "Tactical Search"
    private int _sampleCount = 12; // How many rays to cast? (Higher = smarter but more CPU)
    private float _searchRadius = 10f; // How far to look for cover?

    // Logic variables
    private Vector3 _bestCoverSpot;
    private bool _hasCover;

    public void Enter(AIBrain brain)
    {
        Debug.Log("Entering Flee - RUN!");
        brain.Agent.isStopped = false;
        brain.Agent.speed = 6f; // Panic speed

        // Calculate initial cover immediately
        FindCover(brain);
    }

    public void Execute(AIBrain brain)
    {
        // 1. If we reached our cover spot, check if we are still safe
        if (_hasCover && brain.Agent.remainingDistance < 0.5f)
        {
            // We are hiding. Check if the player can see us now.
            if (CanPlayerSeePoint(brain, brain.transform.position))
            {
                // We are compromised! Find new cover.
                FindCover(brain);
            }
        }
        // 2. If we are moving but the player has moved significantly, re-evaluate
        else if (Time.frameCount % 30 == 0) // Optimization: Don't calc every frame
        {
            // If we don't have cover, keep looking
            if (!_hasCover) FindCover(brain);
        }

        // 3. Exit Condition: If player is far away, go back to Idle
        float dist = Vector3.Distance(brain.transform.position, brain.playerTarget.position);
        if (dist > brain.fleeDistance)
        {
            brain.ChangeState(brain.IdleState);
        }
    }

    public void Exit(AIBrain brain)
    {
        brain.Agent.speed = 3.5f; // Reset speed
        _hasCover = false;
    }

    // --- THE CORE ALGORITHM ---
    private void FindCover(AIBrain brain)
    {
        Vector3 bestSpot = brain.transform.position;
        float bestScore = -Mathf.Infinity;
        bool foundAnyCover = false;

        // Loop through points in a circle around the AI
        for (int i = 0; i < _sampleCount; i++)
        {
            float angle = i * (360f / _sampleCount);
            // Get direction relative to World
            Vector3 dir = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 testPos = brain.transform.position + dir * _searchRadius;

            // 1. Check if point is valid on NavMesh
            NavMeshHit navHit;
            if (NavMesh.SamplePosition(testPos, out navHit, 2.0f, NavMesh.AllAreas))
            {
                // 2. Check if this point is HIDDEN from the player
                // We raycast FROM the player TO the spot. If we hit an obstacle, it's cover.
                if (!CanPlayerSeePoint(brain, navHit.position))
                {
                    // It is hidden! Now score it.
                    // Score logic: Further from player is better, Closer to me is better?
                    // Let's keep it simple: Hidden is priority.

                    float distToPlayer = Vector3.Distance(navHit.position, brain.playerTarget.position);
                    float score = distToPlayer; // Simply pick the cover furthest from player

                    if (score > bestScore)
                    {
                        bestScore = score;
                        bestSpot = navHit.position;
                        foundAnyCover = true;
                    }
                }
            }
        }

        if (foundAnyCover)
        {
            _hasCover = true;
            _bestCoverSpot = bestSpot;
            brain.Agent.SetDestination(_bestCoverSpot);
        }
        else
        {
            // FALBACK: No cover found! Just run directly away from player.
            _hasCover = false;
            Vector3 dirToPlayer = (brain.transform.position - brain.playerTarget.position).normalized;
            Vector3 runSpot = brain.transform.position + dirToPlayer * 5f;
            brain.Agent.SetDestination(runSpot);
        }
    }

    // Helper: Returns TRUE if the player has Line of Sight to the point
    private bool CanPlayerSeePoint(AIBrain brain, Vector3 point)
    {
        Vector3 origin = brain.playerTarget.position + Vector3.up * 1.5f; // Eye level
        Vector3 target = point + Vector3.up * 0.5f; // Knee level (cover check)
        Vector3 direction = target - origin;
        float distance = direction.magnitude;

        // Raycast against the Obstacle Layer
        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, brain.obstacleLayer))
        {
            // If we hit something, the player CANNOT see the point (Line of sight blocked)
            return false;
        }

        // If we didn't hit anything, Line of Sight is clear
        return true;
    }

    // --- VISUAL DEBUGGING ---
    public void OnGizmos(AIBrain brain)
    {
        Gizmos.color = _hasCover ? Color.green : Color.red;
        if (_hasCover)
        {
            Gizmos.DrawSphere(_bestCoverSpot, 0.5f);
            Gizmos.DrawLine(brain.transform.position, _bestCoverSpot);
        }

        // Visualize the search ring (Optional: Recalculate just for drawing to show the "Scan")
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(brain.transform.position, _searchRadius);
    }
}