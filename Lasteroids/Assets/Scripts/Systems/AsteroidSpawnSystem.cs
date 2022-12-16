using System.Diagnostics;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;
using Unity.Burst;
using Unity.Physics;

public partial class AsteroidSpawnSystem : SystemBase
{
    private EntityQuery m_AsteroidQuery;
    private BeginSimulationEntityCommandBufferSystem m_BeginSimECB;
    private EntityQuery m_GameSettingsQuery;
    private Entity m_Prefab;

    protected override void OnCreate()
    {
        m_AsteroidQuery = GetEntityQuery(ComponentType.ReadWrite<AsteroidTag>());
        m_BeginSimECB = World.GetOrCreateSystem<BeginSimulationEntityCommandBufferSystem>();
        m_GameSettingsQuery = GetEntityQuery(ComponentType.ReadWrite<GameSettingsComponent>());
        RequireForUpdate(m_GameSettingsQuery);
    }
    
    [BurstCompile]
    protected override void OnUpdate()
    {
        if (m_Prefab == Entity.Null)
        {
            m_Prefab = GetSingleton<AsteroidAuthoringComponent>().Prefab;
            return;
        }

        var settings = GetSingleton<GameSettingsComponent>();
        var commandBuffer = m_BeginSimECB.CreateCommandBuffer();
        var count = m_AsteroidQuery.CalculateEntityCountWithoutFiltering();
        var asteroidPrefab = m_Prefab;
        var rand = new Unity.Mathematics.Random((uint)Stopwatch.GetTimestamp());
        
        float padding = 0.1f;
        float minX = -settings.levelWidth/2-padding;
        float maxX = settings.levelWidth/2-padding;
        float minZ = -settings.levelDepth/2-padding;
        float maxZ = settings.levelDepth/2-padding;

        Job
        .WithCode(() => 
        {
            for (int i = count; i < settings.numAsteroids; ++i)
            {
                var xPos = rand.NextFloat(minX, maxX);
                var yPos = 0.0f; // Only playing/moving/spawning on th xz-plane
                var zPos = rand.NextFloat(minZ, maxZ);
                           
                var pos = new Translation{Value = new float3(xPos, yPos, zPos)};

                var e = commandBuffer.Instantiate(asteroidPrefab);
                commandBuffer.SetComponent(e, pos);

                // Create a semi-random velocity for the asteroid to travel in
                var randomVelocity = new Vector3(rand.NextFloat(-0.5f, 0.5f), 0, -1);
                randomVelocity.Normalize();
                randomVelocity = randomVelocity * settings.asteroidVelocity;
                var vel = new PhysicsVelocity{Linear = new float3(randomVelocity.x, 0, randomVelocity.z)};
                //now we set the velocity component in our asteroid prefab
                commandBuffer.SetComponent(e, vel);
            }
        }).Schedule();

        m_BeginSimECB.AddJobHandleForProducer(Dependency);
    }
}