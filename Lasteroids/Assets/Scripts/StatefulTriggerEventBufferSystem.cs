using Unity.Entities;
using Unity.Jobs;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;

namespace Unity.Physics.Stateful
{
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    [UpdateAfter(typeof(StepPhysicsWorld))]
    [UpdateBefore(typeof(EndFramePhysicsSystem))]
    public partial class StatefulTriggerEventBufferSystem : SystemBase
    {
        private StepPhysicsWorld m_StepPhysicsWorld = default;
        private EntityQuery m_Query = default;

        private StatefulSimulationEventBuffers<StatefulTriggerEvent> m_StateFulEventBuffers;

        protected override void OnCreate()
        {
            m_StepPhysicsWorld = World.GetOrCreateSystem<StepPhysicsWorld>();
            m_Query = GetEntityQuery(new EntityQueryDesc
            {
                All = new ComponentType[]
                {
                    typeof(StatefulTriggerEvent)
                },
                None = new ComponentType[]
                {
                    typeof(StatefulTriggerEventExclude)
                }
            });

            m_StateFulEventBuffers = new StatefulSimulationEventBuffers<StatefulTriggerEvent>();
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
                .WithName("ClearTriggerEventDynamicBuffersJobParallel")
                .WithBurst()
                .WithNone<StatefulTriggerEventExclude>()
                .ForEach((ref DynamicBuffer<StatefulTriggerEvent> buffer) =>
                {
                    buffer.Clear();
                }).ScheduleParallel();

            m_StateFulEventBuffers.SwapBuffers();

            var currentEvents = m_StateFulEventBuffers.Current;
            var previousEvents = m_StateFulEventBuffers.Previous;

            var eventExcludes = GetComponentDataFromEntity<StatefulTriggerEventExclude>(true);
            var eventBuffers = GetBufferFromEntity<StatefulTriggerEvent>();

            Dependency = new StatefulEventCollectionJobs.CollectTriggerEvents
            {
                TriggerEvents = currentEvents
            }.Schedule(m_StepPhysicsWorld.Simulation, Dependency);

            Job
                .WithName("ConvertTriggerEventStreamToDynamicBufferJob")
                .WithBurst()
                .WithReadOnly(eventExcludes)
                .WithCode(() =>
                {
                    var statefulEvents = new NativeList<StatefulTriggerEvent>(currentEvents.Length, Allocator.Temp);

                    StatefulSimulationEventBuffers<StatefulTriggerEvent>.GetStatefulEvents(previousEvents, currentEvents, statefulEvents, true);

                    for (int i = 0; i < statefulEvents.Length; i++)
                    {
                        var statefulEvent = statefulEvents[i];

                        var addToEntityA = eventBuffers.HasComponent(statefulEvent.EntityA) && !eventExcludes.HasComponent(statefulEvent.EntityA);
                        var addToEntityB = eventBuffers.HasComponent(statefulEvent.EntityB) && !eventExcludes.HasComponent(statefulEvent.EntityB);

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