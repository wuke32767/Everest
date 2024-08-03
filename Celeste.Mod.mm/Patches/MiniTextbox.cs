#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using System;
using System.Collections;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.InlineRT;
using MonoMod.Utils;

namespace Celeste {
    class patch_MiniTextbox : MiniTextbox {

        // We're effectively in MiniTextbox, but still need to "expose" private fields to our mod.
        private int index;
        private FancyText.Text text;
        private Sprite portrait;
        private FancyText.Portrait portraitData;
        private SoundSource talkerSfx;
        
        public patch_MiniTextbox(string dialogId)
            : base(dialogId) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModIgnore]
        [PatchMiniTextboxRoutine]
        private extern IEnumerator Routine();

        private void _startTalking() {
            talkerSfx?.Param("dialogue_portrait", portraitData?.SfxExpression ?? 1.0f);
            talkerSfx?.Param("dialogue_end", 0f);
            if (portrait != null && portraitData != null && portrait.Has(portraitData.TalkAnimation) && portrait.CurrentAnimationID != portraitData.TalkAnimation) {
                portrait.Play(portraitData.TalkAnimation);
            }
        }
        
        private void _handleDialogNode(ref float delay) {
            if (text[index] is FancyText.Wait wait) {
                delay += wait.Duration;
            }
            
            if (delay > 0.5f)
            {
                talkerSfx?.Param("dialogue_portrait", 0f);
                talkerSfx?.Param("dialogue_end", 1f);
                if (portrait != null && portraitData != null && portrait.Has(portraitData.IdleAnimation) && portrait.CurrentAnimationID != portraitData.IdleAnimation)
                {
                    portrait.Play(portraitData.IdleAnimation);
                }
            }
        }
    }
}

namespace MonoMod {
    /// <summary>
    /// Patches the method to fix mini textbox not closing when it's expanding and another textbox is triggered.
    /// Also adds additional dialog feature support, like regular text boxes, including anchors, waits and multiple pages
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchMiniTextboxRoutine))]
    class PatchMiniTextboxRoutine : Attribute { }

    static partial class MonoModRules {

        public static void PatchMiniTextboxRoutine(MethodDefinition method, CustomAttribute attrib) {
            MethodDefinition m_MiniTextbox_startTalking = method.DeclaringType.FindMethod("_startTalking")!;
            MethodDefinition m_MiniTextbox_handleNode = method.DeclaringType.FindMethod("_handleDialogNode")!;
            
            FieldDefinition f_MiniTextbox_closing = method.DeclaringType.FindField("closing")!;
            
            TypeDefinition closureRoutineType = MonoModRule.Modder.Module.GetType("Celeste.BirdPath/<Routine>d__18");
            FieldReference closureThisField = closureRoutineType.FindField("<>4__this")!;

            // The routine is stored in a compiler-generated method.
            method = method.GetEnumeratorMoveNext();

            new ILContext(method).Invoke(il => {
                ILCursor cursor = new(il);

                /*
                    Change:

                        while ((this.ease += Engine.DeltaTime * 4f) < 1f)) {
                            continueLoopTarget:
                            yield return null;
                        }
                        this.ease = 1f;

                    to:

                        while ((this.ease += Engine.DeltaTime * 4f) < 1f)) {
                            continueLoopTarget:
                            if (this.closing) {
                                yield break;
                            }
                            yieldReturnNullTarget:
                            yield return null;
                        }
                        this.ease = 1f;
                */
                ILLabel continueLoopTarget = cursor.DefineLabel();
                cursor.GotoNext(MoveType.After,
                    instr => instr.MatchLdloc(6),
                    instr => instr.MatchLdcR4(1f),
                    instr => instr.MatchBlt(out continueLoopTarget));

                cursor.Goto(continueLoopTarget.Target, MoveType.AfterLabel);

                ILLabel yieldReturnNullTarget = cursor.DefineLabel();
                cursor.Emit(OpCodes.Ldloc_1);
                cursor.Emit(OpCodes.Ldfld, f_MiniTextbox_closing);
                cursor.Emit(OpCodes.Brfalse, yieldReturnNullTarget);
                cursor.Emit(OpCodes.Ldc_I4_0);
                cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(yieldReturnNullTarget);
            });
            
            new ILContext(method).Invoke(il => {
                ILCursor cursor = new(il);
                
                // Before: 'if (text.Nodes[index] is FancyText.Char) { ... }' 
                cursor.GotoNext(instr => instr.MatchIsinst("Celeste.FancyText.Char"));
                cursor.GotoPrev(instr => instr.MatchLdloc1());
                cursor.GotoPrev(instr => instr.MatchLdloc1());
                
                cursor.EmitLdarg0();
                cursor.EmitCall(m_MiniTextbox_startTalking);
                
                // Before: 'index++;'
                cursor.GotoNext(
                    instr => instr.MatchLdloc1(),
                    instr => instr.MatchLdloc1(),
                    instr => instr.MatchLdfld("Celeste.MiniTextbox", "index"));
                
                cursor.EmitLdarg0();
                cursor.EmitLdloca(3); // ref float delay
                cursor.EmitCall(m_MiniTextbox_handleNode);
            });
        }
    }
}
