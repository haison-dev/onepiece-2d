using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds and validates combat targets independently from player input.
/// </summary>
[DisallowMultipleComponent]
public sealed class TargetSystem : MonoBehaviour
{
    public EnemyHealth FindNearestEnemy(float searchRadius)
    {
        EnemyHealth nearestTarget = null;
        float nearestSqrDistance = searchRadius * searchRadius;

        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );

        foreach (EnemyHealth enemy in enemies)
        {
            if (!IsValid(enemy, searchRadius))
                continue;

            float sqrDistance =
                (enemy.transform.position - transform.position).sqrMagnitude;

            if (sqrDistance > nearestSqrDistance)
                continue;

            nearestSqrDistance = sqrDistance;
            nearestTarget = enemy;
        }

        return nearestTarget;
    }

    public EnemyHealth FindNextEnemy(EnemyHealth currentTarget, float searchRadius)
    {
        EnemyHealth[] enemies = FindObjectsByType<EnemyHealth>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
        List<EnemyHealth> validTargets = new List<EnemyHealth>(enemies.Length);

        foreach (EnemyHealth enemy in enemies)
        {
            if (IsValid(enemy, searchRadius))
                validTargets.Add(enemy);
        }

        if (validTargets.Count == 0)
            return null;

        validTargets.Sort((left, right) =>
        {
            float leftDistance =
                (left.transform.position - transform.position).sqrMagnitude;
            float rightDistance =
                (right.transform.position - transform.position).sqrMagnitude;
            return leftDistance.CompareTo(rightDistance);
        });

        int currentIndex = validTargets.IndexOf(currentTarget);
        return validTargets[(currentIndex + 1) % validTargets.Count];
    }

    public bool IsValid(EnemyHealth target, float searchRadius)
    {
        if (target == null || target.IsDead)
            return false;

        float sqrDistance =
            (target.transform.position - transform.position).sqrMagnitude;
        return sqrDistance <= searchRadius * searchRadius;
    }
}
