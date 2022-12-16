using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace Unity.Physics.Stateful
{
    public class StatefulSimulationEventBuffers<T> where T : unmanaged, IStatefulSimulationEvent<T>
    {
        public NativeList<T> Previous;
        public NativeList<T> Current;

        public StatefulSimulationEventBuffers()
        {
            Previous = new NativeList<T>(Allocator.Persistent);
            Current = new NativeList<T>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if (Previous.IsCreated) Previous.Dispose();
            if (Current.IsCreated) Current.Dispose();
        }

        public void SwapBuffers()
        {
            var tmp = Previous;
            Previous = Current;
            Current = tmp;
            Current.Clear();
        }

        public void GetStatefulEvents(NativeList<T> statefulEvents, bool sortCurrent = true) => GetStatefulEvents(Previous, Current, statefulEvents, sortCurrent);

        public static void GetStatefulEvents(NativeList<T> previousEvents, NativeList<T> currentEvents, NativeList<T> statefulEvents, bool sortCurrent = true)
        {
            if (sortCurrent) currentEvents.Sort();

            statefulEvents.Clear();

            int c = 0;
            int p = 0;
            while (c < currentEvents.Length && p < previousEvents.Length)
            {
                int r = previousEvents[p].CompareTo(currentEvents[c]);
                if (r == 0)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Stay;
                    statefulEvents.Add(currentEvent);
                    c++;
                    p++;
                }
                else if (r < 0)
                {
                    var previousEvent = previousEvents[p];
                    previousEvent.State = StatefulEventState.Exit;
                    statefulEvents.Add(previousEvent);
                    p++;
                }
                else //(r > 0)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Enter;
                    statefulEvents.Add(currentEvent);
                    c++;
                }
            }
            if (c == currentEvents.Length)
            {
                while (p < previousEvents.Length)
                {
                    var previousEvent = previousEvents[p];
                    previousEvent.State = StatefulEventState.Exit;
                    statefulEvents.Add(previousEvent);
                    p++;
                }
            }
            else if (p == previousEvents.Length)
            {
                while (c < currentEvents.Length)
                {
                    var currentEvent = currentEvents[c];
                    currentEvent.State = StatefulEventState.Enter;
                    statefulEvents.Add(currentEvent);
                    c++;
                }
            }
        }
    }

    public static class StatefulEventCollectionJobs
    {
        [BurstCompile]
        public struct CollectTriggerEvents : ITriggerEventsJob
        {
            public NativeList<StatefulTriggerEvent> TriggerEvents;
            public void Execute(TriggerEvent triggerEvent) => TriggerEvents.Add(new StatefulTriggerEvent(triggerEvent));
        }

        [BurstCompile]
        public struct CollectCollisionEvents : ICollisionEventsJob
        {
            public NativeList<StatefulCollisionEvent> CollisionEvents;
            public void Execute(CollisionEvent collisionEvent) => CollisionEvents.Add(new StatefulCollisionEvent(collisionEvent));
        }

        [BurstCompile]
        public struct CollectCollisionEventsWithDetails : ICollisionEventsJob
        {
            public NativeList<StatefulCollisionEvent> CollisionEvents;
            [ReadOnly] public PhysicsWorld PhysicsWorld;
            [ReadOnly] public ComponentDataFromEntity<StatefulCollisionEventDetails> EventDetails;
            public bool ForceCalculateDetails;

            public void Execute(CollisionEvent collisionEvent)
            {
                var statefulCollisionEvent = new StatefulCollisionEvent(collisionEvent);

                // Check if we should calculate the collision details
                bool calculateDetails = ForceCalculateDetails;
                if (!calculateDetails && EventDetails.HasComponent(collisionEvent.EntityA))
                {
                    calculateDetails = EventDetails[collisionEvent.EntityA].CalculateDetails;
                }
                if (!calculateDetails && EventDetails.HasComponent(collisionEvent.EntityB))
                {
                    calculateDetails = EventDetails[collisionEvent.EntityB].CalculateDetails;
                }
                if (calculateDetails)
                {
                    var details = collisionEvent.CalculateDetails(ref PhysicsWorld);
                    statefulCollisionEvent.CollisionDetails = new StatefulCollisionEvent.Details(
                        details.EstimatedContactPointPositions.Length,
                        details.EstimatedImpulse,
                        details.AverageContactPointPosition);
                }

                CollisionEvents.Add(statefulCollisionEvent);
            }
        }
    }
}