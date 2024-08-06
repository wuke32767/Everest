using Microsoft.Xna.Framework;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;
using System;

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
        private extern void BeforeRender();

        private void BeforeRenderPatch() {
            if (!dirty) return;

            Snapshot[] snapshotsBak = snapshots;
            for (int i = 0; i < snapshotsBak.Length; i++) {
                dirty = true;

                snapshots = new Snapshot[snapshotsBak.Length];
                snapshots[i] = snapshotsBak[i];

                buffers[i] ??= VirtualContent.CreateRenderTarget("trail-manager-snapshot-" + i, 512, 512, false, true, 0);
                buffer = buffers[i];

                BeforeRender();
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

        public static void PatchTrailManagerSnapshotRender(ILContext context, CustomAttribute attrib) {
            TypeDefinition t_TrailManager = MonoModRule.Modder.Module.GetType("Celeste.TrailManager");
            FieldReference m_TrailManager_buffers = t_TrailManager.FindField("buffers");

            FieldReference f_Snapshot_Index = context.Method.DeclaringType.FindField("Index");

            ILCursor cursor = new ILCursor(context);

            // buffer => buffers[Index]
            cursor.GotoNext(instr => instr.MatchLdfld("Celeste.TrailManager", "buffer"));
            cursor.Remove();
            cursor.EmitLdfld(m_TrailManager_buffers);
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_Snapshot_Index);
            cursor.EmitLdelemRef();
        }
    }
}
