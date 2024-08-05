#pragma warning disable CS0626 // Method, operator, or accessor is marked external and has no attributes on it
#pragma warning disable CS0649 // Field is never assigned to, and will always have its default value

using Microsoft.Xna.Framework;
using System;
using System.Collections;
using Mono.Cecil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using MonoMod.Utils;

namespace Celeste {
    class patch_MiniTextbox : MiniTextbox {

        // We're effectively in MiniTextbox, but still need to "expose" private fields to our mod.
        private int index;
        private FancyText.Text text;
        private Sprite portrait;
        private FancyText.Portrait portraitData;
        private SoundSource talkerSfx;

        private int start;
        private FancyText.Anchors anchor;

        public patch_MiniTextbox(string dialogId)
            : base(dialogId) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        public extern void orig_ctor(string dialogId);
        [MonoModConstructor]
        public void ctor(string dialogId) {
            orig_ctor(dialogId);

            // Find the anchor
            foreach (FancyText.Node node in text.Nodes) {
                if (node is FancyText.Anchor anchorPos) {
                    anchor = anchorPos.Position;
                }
            }
        }

        [MonoModIgnore]
        [PatchMiniTextboxRoutine]
        private extern IEnumerator Routine();

        [MonoModIgnore]
        [PatchMiniTextboxRender]
        public override extern void Render();

        // Start and stop SFX / animations based on delays
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
            } else if (text[index] is FancyText.NewPage) {
                start = index + 1;
            }

            if (delay > 0.5f) {
                talkerSfx?.Param("dialogue_portrait", 0f);
                talkerSfx?.Param("dialogue_end", 1f);
                if (portrait != null && portraitData != null && portrait.Has(portraitData.IdleAnimation) && portrait.CurrentAnimationID != portraitData.IdleAnimation) {
                    portrait.Play(portraitData.IdleAnimation);
                }
            }
        }

        private void _applyAnchor(ref Vector2 center) {
            if (anchor == FancyText.Anchors.Bottom) {
                center = new Vector2(Engine.Width / 2, Engine.Height - BoxHeight / 2.0f - (Engine.Width - BoxWidth) / 4f);
            } else if (anchor == FancyText.Anchors.Middle) {
                center = new Vector2(Engine.Width / 2, Engine.Height / 2);
            }
        }
    }
}

namespace MonoMod {
    /// <summary>
    /// Patches the method to fix mini textbox not closing when it's expanding and another textbox is triggered.
    /// Also adds support for the following dialog nodes: anchors, waits and multiple pages
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchMiniTextboxRoutine))]
    class PatchMiniTextboxRoutine : Attribute { }

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchMiniTextboxRender))]
    class PatchMiniTextboxRender : Attribute { }

    static partial class MonoModRules {

        public static void PatchMiniTextboxRoutine(MethodDefinition method, CustomAttribute attrib) {
            MethodDefinition m_MiniTextbox_startTalking = method.DeclaringType.FindMethod("_startTalking")!;
            MethodDefinition m_MiniTextbox_handleNode = method.DeclaringType.FindMethod("_handleDialogNode")!;

            FieldDefinition f_MiniTextbox_closing = method.DeclaringType.FindField("closing")!;

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
                cursor.EmitLdloc1();
                cursor.EmitLdfld(f_MiniTextbox_closing);
                cursor.EmitBrfalse(yieldReturnNullTarget);
                cursor.EmitLdcI4(0);
                cursor.EmitRet();
                cursor.MarkLabel(yieldReturnNullTarget);
            });

            new ILContext(method).Invoke(il => {
                ILCursor cursor = new(il);

                // Before: 'if (text.Nodes[index] is FancyText.Char) { ... }' 
                cursor.GotoNext(MoveType.AfterLabel, instr => instr.MatchIsinst("Celeste.FancyText/Char"));
                cursor.GotoPrev(MoveType.AfterLabel, instr => instr.MatchLdloc1());
                cursor.GotoPrev(MoveType.AfterLabel, instr => instr.MatchLdloc1());

                cursor.EmitLdloc1(); // this
                cursor.EmitCall(m_MiniTextbox_startTalking);

                // Before: 'index++;'
                cursor.GotoNext(MoveType.AfterLabel,
                    instr => instr.MatchLdloc1(),
                    instr => instr.MatchLdloc1(),
                    instr => instr.MatchLdfld("Celeste.MiniTextbox", "index"));

                cursor.EmitLdloc1(); // this
                cursor.EmitLdloca(3); // ref float delay
                cursor.EmitCall(m_MiniTextbox_handleNode);
            });
        }

        public static void PatchMiniTextboxRender(ILContext il, CustomAttribute attrib) {
            MethodDefinition m_MiniTextbox_applyAnchor = il.Method.DeclaringType.FindMethod("_applyAnchor")!;

            FieldDefinition f_MiniTextbox_start = il.Method.DeclaringType.FindField("start")!;

            ILCursor cursor = new(il);

            // After: 'Vector2 center = new Vector2(Engine.Width / 2, BoxHeight / 2.0f + (Engine.Width - BoxWidth) / 4f);'
            cursor.GotoNext(MoveType.After, instr => instr.MatchCall("Microsoft.Xna.Framework.Vector2", ".ctor"));

            cursor.EmitLdarg0();
            cursor.EmitLdloca(1); // ref Vector2 center
            cursor.EmitCall(m_MiniTextbox_applyAnchor);

            // Replace: 'text.Draw(new Vector2(topleft.X + portraitSize + 32f, center.Y), new Vector2(0f, 0.5f), new Vector2(1f, ease) * 0.75f, 1f, 0, index);'
            // With:    'text.Draw(new Vector2(topleft.X + portraitSize + 32f, center.Y), new Vector2(0f, 0.5f), new Vector2(1f, ease) * 0.75f, 1f, start, index);'
            cursor.Index = il.Instrs.Count - 1;
            cursor.GotoPrev(instr => instr.MatchLdcI4(0));
            cursor.Remove();
            cursor.EmitLdarg0();
            cursor.EmitLdfld(f_MiniTextbox_start);
        }
    }
}
