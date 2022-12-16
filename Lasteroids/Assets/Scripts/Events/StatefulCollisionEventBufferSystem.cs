using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Unity.Physics.Stateful
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public partial class StatefulCollisionEventBufferSystem : SystemBase
    {
        private StepPhysicsWorld m_StepPhysicsWorld = default;
        private BuildPhysicsWorld m_BuildPhysicsWorld = default;
        private EntityQuery m_Query = default;

        private StatefulSimulationEventBuffers<StatefulCollisionEvent> m_StateFulEventBuffers;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_BuildPhysicsWorld = World.GetOrCreateSystem<BuildPhysicsWorld>();
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(StatefulCollisionEvent)
                }
            });

            m_StateFulEventBuffers = new StatefulSimulationEventBuffers<StatefulCollisionEvent>();
        }

        protected override void OnDestroy()
        {
            m_StateFulEventBuffers.Dispose();
        }

        protected override void OnStartRunning()
        {
            base.OnStartRunning();
            this.RegisterPhysicsRuntimeSystemReadOnly();
        }

        protected override void OnUpdate()
        {
            if (m_Query.CalculateEntityCount() == 0)
            {
                return;
            }

            Entities
                .WithName("ClearCollisionEventDynamicBuffersJobParallel")
                .WithBurst()
                .ForEach((ref DynamicBuffer<StatefulCollisionEvent> buffer) =>
                {
                    buffer.Clear();
                }).ScheduleParallel();

            m_StateFulEventBuffers.SwapBuffers();

            var currentEvents = m_StateFulEventBuffers.Current;
            var previousEvents = m_StateFulEventBuffers.Previous;

            var eventDetails = GetComponentDataFromEntity<StatefulCollisionEventDetails>(true);
            var eventBuffers = GetBufferFromEntity<StatefulCollisionEvent>();

            Dependency = new StatefulEventCollectionJobs.CollectCollisionEventsWithDetails
            {
                CollisionEvents = currentEvents,
                PhysicsWorld = m_BuildPhysicsWorld.PhysicsWorld,
                EventDetails = eventDetails
            }.Schedule(m_StepPhysicsWorld.Simulation, Dependency);

            Job
                .WithName("ConvertCollisionEventStreamToDynamicBufferJob")
                .WithBurst()
                .WithCode(() =>
                {
                    var statefulEvents = new NativeList<StatefulCollisionEvent>(currentEvents.Length, Allocator.Temp);

                    StatefulSimulationEventBuffers<StatefulCollisionEvent>.GetStatefulEvents(previousEvents, currentEvents, statefulEvents);

                    for (int i = 0; i < statefulEvents.Length; i++)
                    {
                        var statefulEvent = statefulEvents[i];

                        var addToEntityA = eventBuffers.HasComponent(statefulEvent.EntityA);
                        var addToEntityB = eventBuffers.HasComponent(statefulEvent.EntityB);

                        if (addToEntityA)
                        {
                            eventBuffers[statefulEvent.EntityA].Add(statefulEvent);
                        }
                        if (addToEntityB)
                        {
                            eventBuffers[statefulEvent.EntityB].Add(statefulEvent);
                        }
                    }
                }).Schedule();
        }
    }
}