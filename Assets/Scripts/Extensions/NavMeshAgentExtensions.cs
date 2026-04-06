using UnityEngine;
using UnityEngine.AI;

public static class NavMeshAgentExtensions
{
    public static void StopPath(this NavMeshAgent agent)
    {
        if (!agent || !agent.isOnNavMesh)
        {
            return;
        }

        agent.isStopped = true;
        agent.ResetPath();
    }

    public static bool TrySnapToNavMesh(this NavMeshAgent agent, Transform transform, float maxDistance)
    {
        return agent
            && (agent.isOnNavMesh || NavMesh.SamplePosition(transform.position, out NavMeshHit hit, maxDistance, agent.areaMask) && agent.Warp(hit.position));
    }

    public static bool TrySetDestination(this NavMeshAgent agent, Transform transform, Vector3 worldPosition, float sampleDistance, float snapDistance, out Vector3 sampledPosition)
    {
        sampledPosition = worldPosition;
        if (!agent.TrySnapToNavMesh(transform, snapDistance) || !NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleDistance, agent.areaMask))
        {
            return false;
        }

        sampledPosition = hit.position;
        return agent.SetDestination(sampledPosition);
    }

    public static bool TryWarpTo(this NavMeshAgent agent, Transform transform, Vector3 worldPosition, float sampleDistance, float snapDistance, out Vector3 sampledPosition)
    {
        sampledPosition = worldPosition;
        if (!agent.TrySnapToNavMesh(transform, snapDistance) || !NavMesh.SamplePosition(worldPosition, out NavMeshHit hit, sampleDistance, agent.areaMask))
        {
            return false;
        }

        sampledPosition = hit.position;
        return agent.Warp(sampledPosition);
    }
}
