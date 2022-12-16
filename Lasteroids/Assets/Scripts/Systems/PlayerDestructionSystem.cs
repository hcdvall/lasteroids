using Unity.Entities;
using Unity.Jobs;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class PlayerDestructionSystem : SystemBase
{
    private EndSimulationEntityCommandBufferSystem m_EndSimEcb;    

    protected override void OnCreate()
    {
        m_EndSimEcb = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }
    
    protected override void OnUpdate()
    {
        var commandBuffer = m_EndSimEcb.CreateCommandBuffer().AsParallelWriter();

        Entities
        .WithAll<DestroyTag, PlayerTag>()
        .ForEach((Entity entity, int entityInQueryIndex) =>
        {
            commandBuffer.DestroyEntity(entityInQueryIndex, entity);

        }).WithBurst().ScheduleParallel();

        m_EndSimEcb.AddJobHandleForProducer(Dependency);
    }
}