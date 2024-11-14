#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using Mono.Cecil.Cil;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using System;
using System.Collections;
using MonoMod.Utils;

namespace Celeste {
    class patch_CS02_Mirror : CS02_Mirror {

        public patch_CS02_Mirror(Player player, DreamMirror mirror) : base(player, mirror) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModPatch("<Cutscene>d__7")]
        class Cutscene {

            [MonoModIgnore]
            [PatchCS02_MirrorCutscene]
            private extern bool MoveNext();
        }

    }
}
namespace MonoMod {

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchCS02_MirrorCutscene))]
    class PatchCS02_MirrorCutsceneAttribute : Attribute { }

    static partial class MonoModRules {
        public static void PatchCS02_MirrorCutscene(ILContext context, CustomAttribute attrib) {
            ILCursor cursor = new(context);
            TypeDefinition t_patch_DreamBlock = MonoModRule.Modder.Module.GetType("Celeste.DreamBlock");
            MethodDefinition m_patch_DreamBlock_UpdateRoutine = t_patch_DreamBlock.FindMethod(nameof(Celeste.patch_DreamBlock.UpdateRoutine));
            // this.playerHasDreamDash = base.SceneAs<Level>().Session.Inventory.DreamDash;
            cursor.GotoNext(MoveType.AfterLabel, i => i.MatchCallOrCallvirt("Celeste.DreamBlock", "Activate"));
            cursor.Remove();
            cursor.EmitCall(m_patch_DreamBlock_UpdateRoutine);
        }
    }
}