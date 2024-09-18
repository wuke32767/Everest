using Celeste.Mod.Helpers;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;
using System;
using System.Runtime.CompilerServices;

namespace Celeste.Editor {
    class patch_LevelTemplate : LevelTemplate {
        public patch_LevelTemplate(LevelData data)
            : base(data) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModConstructor]
        [MonoModIgnore] // we don't want to change anything in the method...
        [PatchTrackableStrawberryCheck] // except manipulating it with MonoModRules
        public extern void ctor(LevelData data);

        [AddLevelTemplateCulling]
        [MonoModIgnore]
        public new extern void RenderContents(Camera camera, System.Collections.Generic.List<LevelTemplate> allLevels);

        [AddLevelTemplateCulling]
        [MonoModIgnore]
        public new extern void RenderOutline(Camera camera);

        [AddLevelTemplateCulling]
        [MonoModIgnore]
        public new extern void RenderHighlight(Camera camera, bool hovered, bool selected);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsVisible(Camera camera) {
            return CullHelper.IsRectangleVisible(X, Y, Width, Height, camera: camera);
        }
    }
}

namespace MonoMod {
    /// <summary>
    /// Patch a LevelTemplate method to add camera culling
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.AddLevelTemplateCulling))]
    class AddLevelTemplateCullingAttribute : Attribute { }

    static partial class MonoModRules {
        public static void AddLevelTemplateCulling(ILContext il, CustomAttribute attrib) {
            var cursor = new ILCursor(il);
            var label = cursor.DefineLabel();

            /*
            Add cull check at the start of the method:
            + if (!IsVisible(camera))
            +    return;
            */
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Call, il.Method.DeclaringType.FindMethod("System.Boolean IsVisible(Monocle.Camera)")!);
            cursor.Emit(OpCodes.Brtrue, label);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(label);
        }
    }
}