﻿using Bang;
using Bang.Entities;
using Bang.Systems;
using Murder.Components;
using Murder.Core;
using Murder.Core.Ai;
using Murder.Core.Geometry;
using Murder.Services;
using Murder.Utilities;
using System.Collections.Immutable;

namespace Murder.Systems
{
    [Filter(typeof(CarveComponent))]
    [Watch(typeof(PositionComponent), typeof(ColliderComponent), typeof(NotSolidComponent))]
    internal class MapCarveCollisionSystem : IReactiveSystem
    {
        public ValueTask OnAdded(World world, ImmutableArray<Entity> entities)
        {
            Map map = world.GetUnique<MapComponent>().Map;
            foreach (Entity e in entities)
            {
                UntrackEntityOnGrid(map, e);
                TrackEntityOnGrid(map, e);
            }

            PathfindServices.UpdatePathfind(world);

            return default;
        }

        public ValueTask OnModified(World world, ImmutableArray<Entity> entities)
        {
            // We currently do not support moving carve entities.
            return default;
        }

        public ValueTask OnRemoved(World world, ImmutableArray<Entity> entities)
        {
            Map map = world.GetUnique<MapComponent>().Map;
            foreach (Entity e in entities)
            {
                UntrackEntityOnGrid(map, e);
                TrackEntityOnGrid(map, e);
            }

            PathfindServices.UpdatePathfind(world);

            return default;
        }

        private void TrackEntityOnGrid(Map map, Entity e)
        {
            PositionComponent position = e.GetGlobalPosition();
            ColliderComponent collider = e.GetCollider();

            if (IsValidCarve(e, collider))
            {
                CarveComponent carve = e.GetCarve();

                IntRectangle rect = collider.GetCarveBoundingBox(position);
                map.SetOccupiedAsCarve(rect, carve.BlockVision, carve.Obstacle, carve.Weight);
            }
        }

        private void UntrackEntityOnGrid(Map map, Entity e)
        {
            PositionComponent position = e.GetGlobalPosition();
            ColliderComponent collider = e.GetCollider();
            CarveComponent carve = e.GetCarve();

            if (!IsValidCarve(e, collider))
            {
                IntRectangle rect = collider.GetCarveBoundingBox(position);
                map.SetUnoccupiedCarve(rect, carve.BlockVision, carve.Obstacle, carve.Weight);
            }
        }

        private bool IsValidCarve(Entity e, ColliderComponent collider) =>
            collider.Solid && !e.HasNotSolid() && !e.IsDestroyed;
    }
}