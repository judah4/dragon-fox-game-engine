using Svelto.ECS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DragonGameEngine.Core.Ecs.Components
{
    public struct CounterEntityComponent : IEntityComponent
    {
        public int counter;
    }

    public class SimpleEntityDescriptor : GenericEntityDescriptor<CounterEntityComponent>
    {

    }
}
