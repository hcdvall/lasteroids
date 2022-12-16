using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.Jobs;
using UnityEngine;
using Unity.Physics;

public partial class InputMovementSystem : SystemBase
{
    protected override void OnCreate()
    {
        RequireSingletonForUpdate<GameSettingsComponent>();
    }

    protected override void OnUpdate()
    {
        var gameSettings = GetSingleton<GameSettingsComponent>();
        var deltaTime = Time.DeltaTime;

        byte right, left, thrust, reverseThrust;
        right = left = thrust = reverseThrust = 0;

        float mouseX = 0;

        if (Input.GetKey("d"))
        {
            right = 1;
        }
        if (Input.GetKey("a"))
        {
            left = 1;
        }
        if (Input.GetKey("w"))
        {
            thrust = 1;
        }
        if (Input.GetKey("s"))
        {
            reverseThrust = 1;
        }
        if (Input.GetMouseButton(1))
        {
            mouseX = Input.GetAxis("Mouse X");
        }
            
        Entities
        .WithAll<PlayerTag>()
        .ForEach((Entity entity, ref Rotation rotation, ref PhysicsVelocity velocity) =>
        {
            if (right == 1)
            {
                velocity.Linear += (math.mul(rotation.Value, new float3(1,0,0)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (left == 1)
            {
                velocity.Linear += (math.mul(rotation.Value, new float3(-1,0,0)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (thrust == 1)
            {
                velocity.Linear += (math.mul(rotation.Value, new float3(0,0,1)).xyz) * gameSettings.playerForce * deltaTime;
            }
            if (reverseThrust == 1)
            {
                velocity.Linear += (math.mul(rotation.Value, new float3(0,0,-1)).xyz) *  gameSettings.playerForce * deltaTime;
            }
            if (mouseX != 0)
            {   //move the mouse
                float lookSpeedH = 2f;
                Quaternion currentQuaternion = rotation.Value; 
                float yaw = currentQuaternion.eulerAngles.y;
                
                yaw += lookSpeedH * mouseX;
                Quaternion newQuaternion = Quaternion.identity;
                newQuaternion.eulerAngles = new Vector3(0, yaw, 0);
                rotation.Value = newQuaternion;
            }
        }).ScheduleParallel();
    }
}