using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Physics;

public partial class InputSpawnSystem : SystemBase
{
    private EntityQuery m_PlayerQuery;
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;
    private Entity m_PlayerPrefab;
    private Entity m_BulletPrefab;
    private float m_PerSecond = 10f;
    private float m_NextTime = 0;

    protected override void OnCreate()
    {
        m_PlayerQuery = GetEntityQuery(ComponentType.ReadWrite<PlayerTag>());
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        RequireSingletonForUpdate<GameSettingsComponent>();
    }
    
    protected override void OnUpdate()
    {
        if (m_PlayerPrefab == Entity.Null || m_BulletPrefab == Entity.Null)
        {
            m_PlayerPrefab = GetSingleton<PlayerAuthoringComponent>().Prefab;
            m_BulletPrefab = GetSingleton<BulletAuthoringComponent>().Prefab;

            return;
        }

        byte shoot, selfDestruct;
        shoot = selfDestruct = 0;
        var playerCount = m_PlayerQuery.CalculateEntityCountWithoutFiltering();

        if (Input.GetKey("space"))
        {
            shoot = 1;
        }
        if (Input.GetKey("p"))
        {
            selfDestruct = 1;
        }

        // Create a player
        if (shoot == 1 && playerCount < 1)
        {
            var entity = EntityManager.Instantiate(m_PlayerPrefab);
            return;
        }

        var commandBuffer = m_BeginSimECB.CreateCommandBuffer().AsParallelWriter();
        var gameSettings = GetSingleton<GameSettingsComponent>();
        var bulletPrefab = m_BulletPrefab;

        //Limit at what rate the player is able to shoot
        var canShoot = false;
        if (UnityEngine.Time.time >= m_NextTime)
        {
            canShoot = true;
            m_NextTime += (1/m_PerSecond);
        }

        Entities
        .WithAll<PlayerTag>()
        .ForEach((Entity entity, int entityInQueryIndex, in Translation position, in Rotation rotation,
                in PhysicsVelocity velocity, in BulletSpawnOffsetComponent bulletOffset) =>
        {
            if(selfDestruct == 1)
            {
                commandBuffer.AddComponent(entityInQueryIndex, entity, new DestroyTag {});
            }
            if (shoot != 1 || !canShoot)
            {
                return;
            }

            var bulletEntity = commandBuffer.Instantiate(entityInQueryIndex, bulletPrefab);
            var newPosition = new Translation {Value = position.Value + math.mul(rotation.Value, bulletOffset.Value).xyz};
            commandBuffer.SetComponent(entityInQueryIndex, bulletEntity, newPosition);

            var vel = new PhysicsVelocity {Linear = (gameSettings.bulletVelocity * math.mul(rotation.Value, new float3(0,0,1)).xyz) + velocity.Linear};

            commandBuffer.SetComponent(entityInQueryIndex, bulletEntity, vel);

        }).ScheduleParallel();
        
        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}