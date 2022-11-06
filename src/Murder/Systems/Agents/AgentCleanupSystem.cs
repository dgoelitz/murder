﻿using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using Murder.Components;

namespace Murder.Systems
{

    [Filter(typeof(AgentComponent))]
    [Filter(ContextAccessorFilter.AnyOf, typeof(VelocityComponent), typeof(AgentImpulseComponent))]
    internal class AgentCleanupSystem : IFixedUpdateSystem
    {
        public ValueTask FixedUpdate(Context context)
        {
            foreach (var e in context.Entities)
            {
                var agent = e.GetAgent();
                if (!e.RemoveAgentImpulse())     // Cleanup the impulse
                {
                    // Set the friction if there is no impulse
                    e.SetFriction(agent.Friction);
                }
            }

            return default;
        }
    }
}