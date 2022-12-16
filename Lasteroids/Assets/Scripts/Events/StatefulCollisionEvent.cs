using Unity.Assertions;
using Unity.Entities;
using Unity.Mathematics;

namespace Unity.Physics.Stateful
{
    public struct StatefulCollisionEvent : IBufferElementData, IStatefulSimulationEvent<StatefulCollisionEvent>
    {
        public Entity EntityA { get; set; }
        public Entity EntityB { get; set; }
        public int BodyIndexA { get; set; }
        public int BodyIndexB { get; set; }
        public ColliderKey ColliderKeyA { get; set; }
        public ColliderKey ColliderKeyB { get; set; }
        public StatefulEventState State { get; set; }
        public float3 Normal;

        internal Details CollisionDetails;

        public StatefulCollisionEvent(CollisionEvent collisionEvent)
        {
            EntityA = collisionEvent.EntityA;
            EntityB = collisionEvent.EntityB;
            BodyIndexA = collisionEvent.BodyIndexA;
            BodyIndexB = collisionEvent.BodyIndexB;
            ColliderKeyA = collisionEvent.ColliderKeyA;
            ColliderKeyB = collisionEvent.ColliderKeyB;
            State = default;
            Normal = collisionEvent.Normal;
            CollisionDetails = default;
        }

        public struct Details
        {
            internal bool IsValid;
            public int NumberOfContactPoints;
            public float EstimatedImpulse;
            public float3 AverageContactPointPosition;

            public Details(int numContactPoints, float estimatedImpulse, float3 averageContactPosition)
            {
                IsValid = (0 < numContactPoints); // Should we add a max check?
                NumberOfContactPoints = numContactPoints;
                EstimatedImpulse = estimatedImpulse;
                AverageContactPointPosition = averageContactPosition;
            }
        }

        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            return entity == EntityA ? EntityB : EntityA;
        }

        public float3 GetNormalFrom(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            return math.select(-Normal, Normal, entity == EntityB);
        }

        public bool TryGetDetails(out Details details)
        {
            details = CollisionDetails;
            return CollisionDetails.IsValid;
        }

        public int CompareTo(StatefulCollisionEvent other) => ISimulationEventUtilities.CompareEvents(this, other);
    }
}