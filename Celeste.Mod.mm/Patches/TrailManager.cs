using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;
using System;
using System.Reflection.Emit;

namespace Celeste {
    class patch_TrailManager : TrailManager {
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value
        private bool dirty;
        private Snapshot[] snapshots;
        private VirtualRenderTarget buffer;
#pragma warning restore CS0649 // Field is never assigned to, and will always have its default value

        [MonoModConstructor]
        [MonoModIgnore]
        [PatchTrailManagerConstructor]
        public extern void ctor();

        public patch_TrailManager() : base() {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }
        public static void Add(Entity entity, Color color, float duration = 1f) {
            Add(entity, color, duration, false, false);
        }
        private VirtualRenderTarget[] buffers;



        [MonoModReplace]
        private void Dispose() {
            for (int i = 0; i < buffers.Length; i++) {
                buffers[i]?.Dispose();
                buffers[i] = null;
            }
            buffers = new VirtualRenderTarget[snapshots.Length];
        }

        [MonoModIgnore]
        [PatchTrailManagerBeforeRender]
        private extern void BeforeRender();
        private void BeforeRenderPatch() {
            if (!dirty)
                return;

            Snapshot[] snapshotsBak = snapshots;
            for (int i = 0; i < snapshotsBak.Length; i++) {
                if (snapshotsBak[i] != null && !snapshotsBak[i].Drawn) {
                    dirty = true;
                    snapshots = new Snapshot[1] { snapshotsBak[i] };
                    buffers[i] ??= VirtualContent.CreateRenderTarget("trail-manager-snapshot-" + i, 512, 512, false, true, 0);
                    buffer = buffers[i];
                    BeforeRender();
                }
            }
            snapshots = snapshotsBak;
            dirty = false;
            buffer = null;
        }

        class patch_Snapshot : Snapshot {
            public patch_Snapshot() : base() {
                // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
            }

            [MonoModIgnore]
            [PatchTrailManagerSnapshotRender]
            public new extern void Render();
        }
    }
}

namespace MonoMod {
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchTrailManagerConstructor))]
    class PatchTrailManagerConstructorAttribute : Attribute {
    }

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchTrailManagerBeforeRender))]
    class PatchTrailManagerBeforeRenderAttribute : Attribute {
    }

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchTrailManagerSnapshotRender))]
    class PatchTrailManagerSnapshotRenderAttribute : Attribute {
    }

    static partial class MonoModRules {
        public static void PatchTrailManagerConstructor(ILContext context, CustomAttribute attrib) {
            TypeDefinition t_VirtualRenderTarget = MonoModRule.Modder.Module.GetType("Monocle.VirtualRenderTarget");

            TypeDefinition t_TrailManager = MonoModRule.Modder.Module.GetType("Celeste.TrailManager");
            FieldReference m_TrailManager_buffers = t_TrailManager.FindField("buffers");
            MethodReference m_TrailManager_BeforeRenderPatch = t_TrailManager.FindMethod("BeforeRenderPatch");

            ILCursor cursor = new ILCursor(context);

            // buffers = new VirtualRenderTarget[64];
            cursor.EmitLdarg0();
            cursor.EmitLdcI4(64);
            cursor.EmitNewarr(t_VirtualRenderTarget);
            cursor.EmitStfld(m_TrailManager_buffers);

            cursor.GotoNext(instr => instr.MatchLdftn("Celeste.TrailManager", "BeforeRender"));
            cursor.Next.Operand = m_TrailManager_BeforeRenderPatch;
        }

        public static void PatchTrailManagerBeforeRender(ILContext context, CustomAttribute attrib) {
            TypeDefinition t_VirtualAsset = MonoModRule.Modder.Module.GetType("Monocle.VirtualAsset");
            MethodReference m_VirtualAsset_Width = t_VirtualAsset.FindProperty("Width").GetMethod;
            MethodReference m_VirtualAsset_Height = t_VirtualAsset.FindProperty("Height").GetMethod;

            FieldReference f_TrailManager_buffer = context.Method.DeclaringType.FindField("buffer");
            ILCursor cursor = new ILCursor(context);

            // Draw.Rect(i % 8 * 64, i / 8 * 64, 64f, 64f, Color.Transparent) => (0f, 0f, buffer.Width, buffer.Height, Color.Transparent)
            cursor.GotoNext(instr => instr.MatchLdfld("Celeste.TrailManager/Snapshot", "Drawn"));
            cursor.GotoNext(instr => instr.MatchLdloc0());
            cursor.RemoveRange(6);
            cursor.EmitLdcR4(0f);
            cursor.RemoveRange(6);
            cursor.EmitLdcR4(0f);
            cursor.Remove();
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_TrailManager_buffer);
            cursor.EmitCallvirt(m_VirtualAsset_Width);
            cursor.EmitConvR4();
            cursor.Remove();
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_TrailManager_buffer);
            cursor.EmitCallvirt(m_VirtualAsset_Height);
            cursor.EmitConvR4();

            // new Vector2((j % 8) + 0.5f) * 64f), (j / 8) + 0.5f) * 64f)) => (buffer.Width * 0.5f, buffer.Height * 0.5f)
            cursor.GotoNext(MoveType.After, instr => instr.MatchStloc2());
            cursor.RemoveRange(8);
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_TrailManager_buffer);
            cursor.EmitCallvirt(m_VirtualAsset_Width);
            cursor.EmitConvR4();
            cursor.EmitLdcR4(0.5f);
            cursor.EmitMul();
            cursor.RemoveRange(7);
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_TrailManager_buffer);
            cursor.EmitCallvirt(m_VirtualAsset_Height);
            cursor.EmitConvR4();
            cursor.EmitLdcR4(0.5f);
        }

        public static void PatchTrailManagerSnapshotRender(ILContext context, CustomAttribute attrib) {
            TypeDefinition t_TrailManager = MonoModRule.Modder.Module.GetType("Celeste.TrailManager");
            FieldReference m_TrailManager_buffers = t_TrailManager.FindField("buffers");

            TypeDefinition t_VirtualAsset = MonoModRule.Modder.Module.GetType("Monocle.VirtualAsset");
            MethodReference m_VirtualAsset_Width = t_VirtualAsset.FindProperty("Width").GetMethod;
            MethodReference m_VirtualAsset_Height = t_VirtualAsset.FindProperty("Height").GetMethod;

            FieldReference f_Snapshot_Index = context.Method.DeclaringType.FindField("Index");

            ILCursor cursor = new ILCursor(context);

            // buffer => buffers[Index]
            cursor.GotoNext(instr => instr.MatchLdfld("Celeste.TrailManager", "buffer"));
            cursor.Remove();
            cursor.EmitLdfld(m_TrailManager_buffers);
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_Snapshot_Index);
            cursor.EmitLdelemRef();

            // new Rectangle(Index % 8 * 64, Index / 8 * 64, 64, 64) => (0, 0, buffer.Width, buffer.Height)
            cursor.GotoNext(instr => instr.MatchLdarg0());
            cursor.RemoveRange(6);
            cursor.EmitLdcI4(0);
            cursor.RemoveRange(6);
            cursor.EmitLdcI4(0);
            cursor.Remove();
            cursor.EmitLdcI4(512); // Direct emit (512), because i didn't figure out how to emit (buffer?.Width ?? 512)
            cursor.Remove();
            cursor.EmitLdcI4(512);

            // new Vector2(64f, 64f) => (buffer.Width, buffer.Height)
            cursor.GotoNext(instr => instr.MatchCall("Monocle.Draw", "get_SpriteBatch"));
            cursor.GotoNext(instr => instr.MatchLdcR4(64f));
            cursor.Remove();
            cursor.EmitLdloc0();
            cursor.EmitCallvirt(m_VirtualAsset_Width);
            cursor.EmitConvR4();
            cursor.Remove();
            cursor.EmitLdloc0();
            cursor.EmitCallvirt(m_VirtualAsset_Height);
            cursor.EmitConvR4();
        }
    }
}