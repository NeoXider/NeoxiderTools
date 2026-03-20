namespace Neo.NPC.Combat
{
    /// <summary>
    ///     Stateless decision helper for modular NPC combat brains.
    /// </summary>
    public static class NpcCombatDecisionCore
    {
        public enum Decision
        {
            AcquireTarget,
            ClearTarget,
            ChaseTarget,
            HoldPosition,
            Attack
        }

        public static Decision Decide(bool hasTarget,
            bool canAct,
            bool canAttack,
            float distanceToTarget,
            float preferredAttackDistance,
            float loseTargetDistance)
        {
            if (!hasTarget)
            {
                return Decision.AcquireTarget;
            }

            if (loseTargetDistance > 0f && distanceToTarget > loseTargetDistance)
            {
                return Decision.ClearTarget;
            }

            if (!canAct)
            {
                return Decision.HoldPosition;
            }

            if (distanceToTarget <= preferredAttackDistance)
            {
                return canAttack ? Decision.Attack : Decision.HoldPosition;
            }

            return Decision.ChaseTarget;
        }
    }
}
