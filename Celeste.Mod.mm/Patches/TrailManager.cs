#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monocle;
using MonoMod;
using System.Collections.Generic;

namespace Celeste {
    class patch_TrailManager : TrailManager {
        private bool dirty;
        private Snapshot[] snapshots;
        private static BlendState MaxBlendState;

        public patch_TrailManager() : base() {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }
        public static void Add(Entity entity, Color color, float duration = 1f) {
            Add(entity, color, duration, false, false);
        }
        private VirtualRenderTarget[] buffers;

        [MonoModReplace]
        private void Dispose() {
            if (buffers != null) {
                for (int i = 0; i < buffers.Length; i++) {
                    if (buffers[i] != null) {
                        buffers[i].Dispose();
                    }
                    buffers[i] = null;
                }
            }
            buffers = null;
        }
        [MonoModReplace]
        private void BeforeRender() {
            if (!dirty) {
                return;
            }
            buffers ??= new VirtualRenderTarget[snapshots.Length];

            for (int i = 0; i < snapshots.Length; i++) {
                if (snapshots[i] != null && !snapshots[i].Drawn) {
                    patch_Snapshot snapshot = snapshots[i] as patch_Snapshot;
                    VirtualRenderTarget buffer = buffers[i] ??= VirtualContent.CreateRenderTarget($"trail-manager-snapshot{i}", 512, 512, false, true, 0);

                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, LightingRenderer.OccludeBlendState);
                    Engine.Graphics.GraphicsDevice.SetRenderTarget(buffer);
                    Draw.Rect(0, 0, buffer.Width, buffer.Height, Color.Transparent);

                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, null, RasterizerState.CullNone);

                    Vector2 value = new Vector2(buffer.Width, buffer.Height) * 0.5f - snapshot.Position;
                    if (snapshot.Hair != null) {
                        List<Vector2> nodes = snapshot.Hair.Nodes;
                        for (int j = 0; j < nodes.Count; j++) {
                            nodes[j] += value;
                        }
                        snapshot.Hair.Render();
                        for (int k = 0; k < nodes.Count; k++) {
                            nodes[k] -= value;
                        }
                    }
                    Vector2 scale = snapshot.Sprite.Scale;
                    snapshot.Sprite.Scale = snapshot.SpriteScale;
                    snapshot.Sprite.Position += value;
                    snapshot.Sprite.Render();
                    snapshot.Sprite.Scale = scale;
                    snapshot.Sprite.Position -= value;
                    snapshot.Drawn = true;

                    Draw.SpriteBatch.End();
                    Draw.SpriteBatch.Begin(SpriteSortMode.Deferred, MaxBlendState);

                    Draw.Rect(0, 0, buffer.Width, buffer.Height, Color.White);
                    Draw.SpriteBatch.End();
                }
            }
            dirty = false;
        }
        class patch_Snapshot : Snapshot {
            public patch_Snapshot() : base() {
                // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
            }

            [MonoModReplace]
            public override void Render() {
                float scale = (Duration > 0f) ? (0.75f * (1f - Ease.CubeOut(Percent))) : 1f;
                VirtualRenderTarget buffer = (Manager as patch_TrailManager).buffers[Index];
                if (buffer != null) {
                    Draw.SpriteBatch.Draw(buffer, Position, null, Color * scale, 0f, new Vector2(buffer.Width, buffer.Height) * 0.5f, Vector2.One, SpriteEffects.None, 0f);
                }
            }
        }
    }
}
