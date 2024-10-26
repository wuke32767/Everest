#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it

using Monocle;
using MonoMod;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Celeste.Mod.Core;

namespace Celeste {
    class patch_LightningRenderer : LightningRenderer {

        // Required because Bolt is private. Yes, this is an excessive amount of vanilla code to repackage.
        // We have no choice without changing the building process to target a publicized Celeste.exe.
        // The class is mostly intact. Changes are denoted by comments.
        private class patch_Bolt {
            private List<Vector2> nodes = new List<Vector2>();

            private Coroutine routine;

            private bool visible;

            private float size;

            private float gap;

            private float alpha;

            private uint seed;

            private float flash;

            private readonly Color color;

            private readonly float scale;

            private readonly int width;

            private readonly int height;

            [MonoModConstructor]
            public patch_Bolt(Color color, float scale, int width, int height) {
                this.color = color;
                this.width = width;
                this.height = height;
                this.scale = scale;
                routine = new Coroutine(Run());
            }

            [MonoModReplace]
            public void Update(Scene scene) {
                routine.Update();
                flash = Calc.Approach(flash, 0f, Engine.DeltaTime * 2f);
            }

            [MonoModReplace]
            private IEnumerator Run() {
                yield return Calc.Random.Range(0f, 4f);
                while (true) {
                    List<Vector2> list = new List<Vector2>();
                    for (int k = 0; k < 3; k++) {
                        Vector2 item = Calc.Random.Choose(new Vector2(0f, Calc.Random.Range(8, height - 16)), new Vector2(Calc.Random.Range(8, width - 16), 0f), new Vector2(width, Calc.Random.Range(8, height - 16)), new Vector2(Calc.Random.Range(8, width - 16), height));
                        Vector2 item2 = ((item.X <= 0f || item.X >= (float) width) ? new Vector2((float) width - item.X, item.Y) : new Vector2(item.X, (float) height - item.Y));
                        list.Add(item);
                        list.Add(item2);
                    }
                    List<Vector2> list2 = new List<Vector2>();
                    for (int l = 0; l < 3; l++) {
                        list2.Add(new Vector2(Calc.Random.Range(0.25f, 0.75f) * (float) width, Calc.Random.Range(0.25f, 0.75f) * (float) height));
                    }
                    nodes.Clear();
                    foreach (Vector2 item4 in list) {
                        nodes.Add(item4);
                        nodes.Add(list2.ClosestTo(item4));
                    }
                    Vector2 item3 = list2[list2.Count - 1];
                    foreach (Vector2 item5 in list2) {
                        nodes.Add(item3);
                        nodes.Add(item5);
                        item3 = item5;
                    }
                    flash = 1f;
                    visible = true;
                    size = 5f;
                    gap = 0f;
                    alpha = 1f;
                    for (int j = 0; j < 4; j++) {
                        seed = (uint) Calc.Random.Next();
                        yield return 0.1f;
                    }
                    for (int j = 0; j < 5; j++) {
                        // Changed - respects everest's advanced photosensitivity settings.
                        if (CoreModule.Settings.AllowLightning) {
                            visible = false;
                        }
                        yield return 0.05f + (float) j * 0.02f;
                        float num = (float) j / 5f;
                        visible = true;
                        size = (1f - num) * 5f;
                        gap = num;
                        alpha = 1f - num;
                        visible = true;
                        seed = (uint) Calc.Random.Next();
                        yield return 0.025f;
                    }
                    visible = false;
                    yield return Calc.Random.Range(4f, 8f);
                }
            }

            [MonoModReplace]
            public void Render() {
                // Changed - respects everest's advanced photosensitivity settings.
                if (flash > 0f && CoreModule.Settings.AllowLightning) {
                    Draw.Rect(0f, 0f, width, height, Color.White * flash * 0.15f * scale);
                }
                if (visible) {
                    for (int i = 0; i < nodes.Count; i += 2) {
                        DrawFatLightning(seed, nodes[i], nodes[i + 1], size * scale, gap, color * alpha);
                    }
                }
            }
        }

        // MonoMod! Save my wretched soul!
        [MonoModIgnore]
        private static extern void DrawFatLightning(uint seed, Vector2 a, Vector2 b, float size, float gap, Color color);

    }
}
