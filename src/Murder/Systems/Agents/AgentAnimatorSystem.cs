﻿using Bang.Components;
using Bang.Contexts;
using Bang.Entities;
using Bang.Systems;
using Murder.Assets.Graphics;
using Murder.Attributes;
using Murder.Components;
using Murder.Core.Geometry;
using Murder.Core.Graphics;
using Murder.Helpers;
using Murder.Messages;
using Murder.Services;
using Murder.Utilities;

namespace Murder.Systems
{
    [Filter(ContextAccessorFilter.AllOf, typeof(ITransformComponent), typeof(AgentSpriteComponent), typeof(FacingComponent))]
    [ShowInEditor]
    public class AgentAnimatorSystem : IMonoRenderSystem
    {
        public void Draw(RenderContext render, Context context)
        {
            foreach (var e in context.Entities)
            {
                IMurderTransformComponent transform = e.GetGlobalTransform();
                AgentSpriteComponent sprite = e.GetAgentSprite();

                 if (Game.Data.GetAsset<SpriteAsset>(sprite.AnimationGuid) is not SpriteAsset SpriteAsset)
                    continue;

                Vector2 renderPosition;
                if (e.TryGetVerticalPosition() is VerticalPositionComponent verticalPosition)
                {
                    renderPosition = transform.Vector2 + new Vector2(0, -verticalPosition.Z);
                }
                else
                {
                    renderPosition = transform.Vector2;
                }

                // This is as early as we can to check for out of bounds
                if (!render.Camera.Bounds.Touches(new Rectangle(renderPosition - SpriteAsset.Origin, SpriteAsset.Size)))
                    continue;

                FacingComponent facing = e.GetFacing();

                var ySort = RenderServices.YSort(transform.Y + sprite.YSortOffset);
                Vector2 impulse = Vector2.Zero;

                if (e.TryGetAgentImpulse() is AgentImpulseComponent imp) impulse = imp.Impulse;

                float start = NoiseHelper.Simple01(e.EntityId * 10) * 5f;
                var prefix = sprite.IdlePrefix;

                if (impulse.HasValue && !e.HasDisableAgent())
                {
                    prefix = sprite.WalkPrefix;

                    if (e.TryFetchChild("Particle") is Entity particle)
                    {
                        particle.RemoveDisableParticleSystem();
                    }
                }
                else
                {
                    if (e.TryFetchChild("Particle") is Entity particle)
                    {
                        particle.SetDisableParticleSystem();
                    }
                }

                // Pause animation if this is a one-shot animation, like a spell cast.
                // For looping animations we don't need to pause
                // TODO: Check if this works for all animations
                bool forcePause = false;

                AnimationOverloadComponent? overload = null;
                if (e.TryGetAnimationOverload() is AnimationOverloadComponent o)
                {
                    overload = o;
                    prefix = $"{o.CurrentAnimation}_";
                    
                    start = o.Start;
                    forcePause = !o.Loop;
                    if (o.CustomSprite is SpriteAsset customSprite)
                        SpriteAsset = customSprite;
                }

                var angle = facing.Direction.Angle() / (MathF.PI * 2); // Gives us an angle from 0 to 1, with 0 being right and 0.5 being left
                (string suffix, bool flip) = DirectionHelper.GetSuffixFromAngle(sprite, angle);

                if (overload is not null && overload.Value.IgnoreFacing)
                    suffix = string.Empty;

                if (string.IsNullOrEmpty(suffix))
                    prefix = prefix.Trim('_');

                float speed = overload?.Duration ?? -1;
                AnimationSpeedOverload? speedOverload = e.TryGetAnimationSpeedOverload();

                if (speedOverload is not null)
                {
                    if (speed > 0)
                        speed = speed * speedOverload.Value.Rate;
                    else
                    {
                        if (SpriteAsset.Animations.TryGetValue(prefix + suffix, out var animation))
                        {
                            speed = animation.AnimationDuration / speedOverload.Value.Rate;
                        }
                    }
                }

                Microsoft.Xna.Framework.Vector3 blend;
                // Handle flashing
                if (e.HasFlashSprite())
                {
                    blend = RenderServices.BLEND_WASH;
                }
                else
                {
                    blend = RenderServices.BLEND_NORMAL;
                }

                // Handle alpha
                Color color;
                if (e.TryGetAlpha() is AlphaComponent alphaComponent)
                {
                    color = Color.White * alphaComponent.Alpha;
                }
                else
                {
                    color = Color.White;
                }

                TargetSpriteBatches target = TargetSpriteBatches.Gameplay;
                if (e.TryGetCustomTargetSpriteBatch() is CustomTargetSpriteBatchComponent renderTarget)
                    target = renderTarget.TargetBatch;


                if (impulse.HasValue && SpriteAsset.Animations.TryGetValue(prefix + sprite.WalkPrefix + suffix, out _) && !e.HasDisableAgent())
                {
                    prefix += sprite.WalkPrefix;
                }


                var complete = RenderServices.DrawSprite(
                    render.GetSpriteBatch(target),
                    renderPosition,
                    prefix + suffix,
                    SpriteAsset,
                    start,
                    speed,
                    Vector2.Zero,
                    flip,
                    0,
                    Vector2.One,
                    color,
                    blend,
                    ySort,
                    useScaledTime: forcePause || e.HasPauseAnimation()
                    );

                if (complete && overload != null)
                {
                    if (overload.Value.AnimationCount > 1)
                    {
                        if (overload.Value.Current < overload.Value.AnimationCount - 1)
                        {
                            e.SetAnimationOverload(overload.Value.PlayNext());
                        }
                        else
                        {
                            e.SendMessage<AnimationCompleteMessage>();
                        }
                    }
                    else if (!overload.Value.Loop)
                    {
                        e.RemoveAnimationOverload();
                        e.SendMessage<AnimationCompleteMessage>();
                    }
                    else
                    {
                        e.SendMessage<AnimationCompleteMessage>();
                    }

                    if (speedOverload is not null && speedOverload.Value.Persist)
                    {
                        e.RemoveAnimationSpeedOverload();
                    }
                }
            }
        }
    }
}
