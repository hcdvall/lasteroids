using Unity.Entities;

public partial class BulletAgeSystem : SystemBase
{
    private BeginSimulationEntityCommandBufferSystem m_BeginSimEcb;

    protected override void OnCreate()
    {
        m_BeginSimEcb = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
    }

    protected override void OnUpdate()
    {
        var commandBuffer = m_BeginSimEcb.CreateCommandBuffer().AsParallelWriter();

        var deltaTime = Time.DeltaTime;

        Entities
            .ForEach((Entity entity, int entityInQueryIndex, ref BulletAgeComponent age) =>
        {
            age.age += deltaTime;
            if (age.age > age.maxAge)
                commandBuffer.DestroyEntity(entityInQueryIndex, entity);

        }).ScheduleParallel();
        m_BeginSimEcb.AddJobHandleForProducer(Dependency);
    }
}