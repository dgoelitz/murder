﻿using Bang.Contexts;
using Bang.Entities;
using Bang;
using Bang.Systems;
using Murder.Components;
using Murder.Core.Graphics;
using Murder.Editor.Attributes;
using Murder.Editor.Components;
using Murder.Editor.Utilities;
using Murder.Core.Geometry;
using ImGuiNET;
using Bang.Components;
using Murder.Core;
using Murder.Core.Physics;
using Murder.Interactions;
using Bang.Interactions;

namespace Murder.Editor.Systems.Sounds
{
    [SoundEditor]
    [Filter(typeof(SoundComponent), typeof(MusicComponent), typeof(SoundParameterComponent))]
    internal class SoundCreationEditorSystem : IMonoRenderSystem
    {
        public void Draw(RenderContext render, Context context)
        {
            EditorHook hook = context.World.GetUnique<EditorComponent>().EditorHook;

            DrawAddEntity(hook);
        }

        private bool DrawAddEntity(EditorHook hook)
        {
            ImGui.PushID("Popup Sound!");

            if (ImGui.BeginPopupContextItem())
            {
                if (ImGui.Selectable("\uf2a2 Add sound trigger"))
                {
                    Point cursorWorldPosition = hook.CursorWorldPosition;
                    CreateNewSoundTriggerArea(hook, cursorWorldPosition);
                }

                ImGui.EndPopup();
            }

            ImGui.PopID();

            return true;
        }

        private void CreateNewSoundTriggerArea(EditorHook hook, Vector2 position)
        {
            hook.AddEntityWithStage?.Invoke(
                new IComponent[]
                {
                    new PositionComponent(position),
                    new SoundParameterComponent(),
                    new InteractOnCollisionComponent(playerOnly: true),
                    new ColliderComponent(
                        shape: new BoxShape(Vector2.Zero, Point.Zero, width: Grid.CellSize * 2, height: Grid.CellSize * 2),
                        layer: CollisionLayersBase.TRIGGER,
                        color: new Color(104 / 255f, 234 / 255f, 137 / 255f)),
                    new InteractiveComponent<SetSoundOnInteraction>(new SetSoundOnInteraction())
                },
                /* group */ "Sounds",
                /* name */ "Sound Trigger Area");
        }
    }
}