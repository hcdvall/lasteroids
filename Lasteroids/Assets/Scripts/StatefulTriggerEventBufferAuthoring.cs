using Unity.Entities;
using UnityEngine;


namespace Unity.Physics.Stateful
{
    public struct StatefulTriggerEventExclude : IComponentData {}

    public class StatefulTriggerEventBufferAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddBuffer<StatefulTriggerEvent>(entity);
        }
    }
}