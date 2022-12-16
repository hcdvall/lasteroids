using Unity.Entities;
using Unity.Assertions;

namespace Unity.Physics.Stateful
{
    public struct StatefulTriggerEvent : IBufferElementData, IStatefulSimulationEvent<StatefulTriggerEvent>
    {
        public Entity EntityA { get; set; }
        public Entity EntityB { get; set; }
        public int BodyIndexA { get; set; }
        public int BodyIndexB { get; set; }
        public ColliderKey ColliderKeyA { get; set; }
        public ColliderKey ColliderKeyB { get; set; }
        public StatefulEventState State { get; set; }

        public StatefulTriggerEvent(TriggerEvent triggerEvent)
        {
            EntityA = triggerEvent.EntityA;
            EntityB = triggerEvent.EntityB;
            BodyIndexA = triggerEvent.BodyIndexA;
            BodyIndexB = triggerEvent.BodyIndexB;
            ColliderKeyA = triggerEvent.ColliderKeyA;
            ColliderKeyB = triggerEvent.ColliderKeyB;
            State = default;
        }

        public Entity GetOtherEntity(Entity entity)
        {
            Assert.IsTrue((entity == EntityA) || (entity == EntityB));
            return (entity == EntityA) ? EntityB : EntityA;
        }

        public int CompareTo(StatefulTriggerEvent other) => ISimulationEventUtilities.CompareEvents(this, other);
    }
}