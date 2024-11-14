#pragma warning disable CS0626 // extern
using Microsoft.Xna.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Monocle;
using MonoMod;
using MonoMod.Cil;
using System;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Linq;
using MonoMod.InlineRT;
using MonoMod.Utils;

namespace Celeste {
    class patch_DreamBlock : DreamBlock {
        static object DreamBlockPatch;

        internal Vector2 movementCounter {
            [MonoModLinkTo("Celeste.Platform", "get__movementCounter")] get;
        }


        private bool flagState;
        private bool playerHasDreamDash;
        private LightOcclude occlude;
        private float whiteHeight;
        private float whiteFill;
        private Shaker shaker;
        private Vector2 shake;
        private int randomSeed = Calc.Random.Next();
        private string? flag;

        public bool DeactivatedIsSolid { [MethodImpl(MethodImplOptions.NoInlining)] get; private set; }

        public string? Flag {
            get => flag;
            set {
                flag = value;
                if (Scene is not null) {
                    UpdateNoRoutine();
                }
            }
        }

        /// <summary>
        /// determine if a dream block is activated.
        /// you can add your custom state here. 
        /// 
        /// this will not update visual state automatically.
        /// if your custom state is changed, update it manually.
        /// as a reference, see <seealso cref="CheckFlags"/>.
        /// 
        /// be aware that there can be a "reverse" thing, 
        /// that is, sometimes Activated will always not equal to your state.
        /// better to have a thing similar to <see cref="flagState"/> to determine if you should update visual.
        /// </summary>
        public bool Activated {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => Flag is null ? SceneAs<patch_Level>().Session.Inventory.DreamDash : flagState;
        }

        /// <summary>
        /// determine if a dream block can be dash through.
        /// mainly used for some temp state that should not change visual state.
        /// for example, for Tera Helper, if DreamBlock is Fairy type and Madeline is Dragon type, there will be no effect.
        /// then this property returns false. 
        /// </summary>
        public bool ActivatedPlus {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => Activated;
        }

        [MonoModIgnore]
        [PatchDreamBlockAdded]
        public override extern void Added(Scene scene);

        [MonoModIgnore]
        [PatchDreamBlockUpdate]
        public override extern void Update();

        public patch_DreamBlock(EntityData data, Vector2 offset)
            : base(data, offset) {
            // no-op. MonoMod ignores this - we only need this to make the compiler shut up.
        }

        [MonoModLinkTo("Celeste.DreamBlock", "System.Void .ctor(Microsoft.Xna.Framework.Vector2,System.Single,System.Single,System.Nullable`1<Microsoft.Xna.Framework.Vector2>,System.Boolean,System.Boolean,System.Boolean)")]
        [MonoModForceCall]
        [MonoModRemove]
        public extern void ctor(Vector2 position, float width, float height, Vector2? node, bool fastMoving, bool oneUse, bool below);
        [MonoModConstructor]
        public void ctor(Vector2 position, float width, float height, Vector2? node, bool fastMoving, bool oneUse) {
            ctor(position, width, height, node, fastMoving, oneUse, false);
        }

        public extern void orig_ctor(EntityData data, Vector2 offset);

        [MonoModConstructor]
        public void ctor(EntityData data, Vector2 offset) {
            orig_ctor(data, offset);
            Flag = data.Attr("flag", null);
            DeactivatedIsSolid = data.Bool("deactivatedIsSolid", false);
        }
        public void CheckFlags() {
            bool fs = SceneAs<patch_Level>().Session.GetFlag(Flag);
            if (flagState != fs) {
                flagState = fs;
                UpdateNoRoutine();
            }
        }

        internal static bool Init(bool _, patch_DreamBlock self) {
            self.flagState = self.SceneAs<patch_Level>().Session.GetFlag(self.Flag);
            return self.Activated;
        }

        public void UpdateVisual(bool routine, bool fast) {
            if (routine) {
                if (fast) {
                    Add(new Coroutine(UpdateFastRoutine()));
                } else {
                    Add(new Coroutine(UpdateRoutine()));
                }
            } else {
                UpdateRoutine();
            }
        }

#pragma warning disable CS0618 // obsolete
        private static IEnumerator Empty() {
            yield break;
        }
        public void UpdateNoRoutine() {
            bool activated = Activated;
            if (playerHasDreamDash != activated) {
                if (activated) {
                    ActivateNoRoutine();
                } else {
                    DeactivateNoRoutine();
                }
            }
        }
        public IEnumerator UpdateRoutine() {
            bool activated = Activated;
            if (playerHasDreamDash != activated) {
                if (activated) {
                    return Activate();
                } else {
                    return Deactivate();
                }
            }
            return Empty();
        }
        public IEnumerator UpdateFastRoutine() {
            bool activated = Activated;
            if (playerHasDreamDash != activated) {
                if (activated) {
                    return Activate();
                } else {
                    return Deactivate();
                }
            }
            return Empty();
        }
#pragma warning restore CS0618 

        [MonoModIgnore]
        [PatchDreamBlockSetup]
        public new extern void Setup();

        [PatchDreamBlockAddObsolete($"Use {nameof(UpdateNoRoutine)} instead")]
        [MonoModIgnore]
        public new extern void ActivateNoRoutine();

        [Obsolete($"Use {nameof(UpdateNoRoutine)} instead")]
        public void DeactivateNoRoutine() {
            if (playerHasDreamDash) {
                playerHasDreamDash = false;

                Setup();

                if (occlude == null) {
                    occlude = new LightOcclude(1f);
                }
                Add(occlude);

                whiteHeight = 1f;
                whiteFill = 0f;

                if (shaker != null) {
                    shaker.On = false;
                }

                SurfaceSoundIndex = 11;
            }
        }

        [PatchDreamBlockAddObsolete($"Use {nameof(UpdateRoutine)} instead")]
        [MonoModIgnore]
        public new extern IEnumerator Activate();

        [Obsolete($"Use {nameof(UpdateRoutine)} instead")]
        public IEnumerator Deactivate() {
            Level level = SceneAs<Level>();
            yield return 1f;

            Input.Rumble(RumbleStrength.Light, RumbleLength.Long);
            if (shaker == null) {
                shaker = new Shaker(true, t => {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.5f;

            DeactivateNoRoutine();

            whiteHeight = 1f;
            whiteFill = 1f;
            for (float yOffset = 1f; yOffset > 0f; yOffset -= Engine.DeltaTime * 0.5f) {
                whiteHeight = yOffset;
                if (level.OnInterval(0.1f)) {
                    for (int xOffset = 0; xOffset < Width; xOffset += 4) {
                        level.ParticlesFG.Emit(Strawberry.P_WingsBurst, new Vector2(X + xOffset, Y + Height * whiteHeight + 1f));
                    }
                }
                if (level.OnInterval(0.1f)) {
                    level.Shake(0.3f);
                }
                Input.Rumble(RumbleStrength.Strong, RumbleLength.Short);
                yield return null;
            }
            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
        }

        [Obsolete($"Use {nameof(UpdateFastRoutine)} instead")]
        public IEnumerator FastDeactivate() {
            Level level = SceneAs<Level>();
            yield return null;

            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            if (shaker == null) {
                shaker = new Shaker(true, t => {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime * 3f) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.1f;

            DeactivateNoRoutine();

            whiteHeight = 1f;
            whiteFill = 1f;

            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, TopCenter, Vector2.UnitX * Width / 2, Color.White, (float) Math.PI);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, BottomCenter, Vector2.UnitX * Width / 2, Color.White, 0);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterLeft, Vector2.UnitY * Height / 2, Color.White, (float) Math.PI * 1.5f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterRight, Vector2.UnitY * Height / 2, Color.White, (float) Math.PI / 2);
            level.Shake(0.3f);
            yield return 0.1f;

            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
        }

        [Obsolete($"Use {nameof(UpdateFastRoutine)} instead")]
        public IEnumerator FastActivate() {
            Level level = SceneAs<Level>();
            yield return null;

            Input.Rumble(RumbleStrength.Light, RumbleLength.Short);
            if (shaker == null) {
                shaker = new Shaker(true, t => {
                    shake = t;
                });
            }
            Add(shaker);
            shaker.Interval = 0.02f;
            shaker.On = true;
            for (float alpha = 0f; alpha < 1f; alpha += Engine.DeltaTime * 3f) {
                whiteFill = Ease.CubeIn(alpha);
                yield return null;
            }
            shaker.On = false;
            yield return 0.1f;

            ActivateNoRoutine();

            whiteHeight = 1f;
            whiteFill = 1f;

            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, TopCenter, Vector2.UnitX * Width / 2, Color.White, (float) Math.PI);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Width, BottomCenter, Vector2.UnitX * Width / 2, Color.White, 0);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterLeft, Vector2.UnitY * Height / 2, Color.White, (float) Math.PI * 1.5f);
            level.ParticlesFG.Emit(Strawberry.P_WingsBurst, (int) Height, CenterRight, Vector2.UnitY * Height / 2, Color.White, (float) Math.PI / 2);
            level.Shake(0.3f);
            yield return 0.1f;

            while (whiteFill > 0f) {
                whiteFill -= Engine.DeltaTime * 3f;
                yield return null;
            }
        }

        [MonoModReplace]
        private Vector2 PutInside(Vector2 pos) {
            // vanilla used loops here to move the particle inside the dream block step by step,
            // which can decrease the performance when the dream block is very far from (0, 0)
            if (pos.X > Right) {
                pos.X -= (float) Math.Ceiling((pos.X - Right) / Width) * Width;
            } else if (pos.X < Left) {
                pos.X += (float) Math.Ceiling((Left - pos.X) / Width) * Width;
            }
            if (pos.Y > Bottom) {
                pos.Y -= (float) Math.Ceiling((pos.Y - Bottom) / Height) * Height;
            } else if (pos.Y < Top) {
                pos.Y += (float) Math.Ceiling((Top - pos.Y) / Height) * Height;
            }
            return pos;
        }

        // Patch XNA/FNA jank in Tween.OnUpdate lambda
        [MonoModPatch("<>c__DisplayClass22_0")]
        class patch_AddedLambdas {
            
            [MonoModPatch("<>4__this")]
            private patch_DreamBlock _this = default;
            private Vector2 start = default, end = default;

            [MonoModReplace]
            [MonoModPatch("<Added>b__0")]
            public void TweenUpdateLambda(Tween t) {
                // Patch this to always behave like XNA
                // This is absolutely hecking ridiculous and a perfect example of why we want to switch to .NET Core
                // The Y member gets downcast but not the X one because of JIT jank
                double lerpX = start.X + ((double) end.X - start.X) * t.Eased, lerpY = start.Y + ((double) end.Y - start.Y) * t.Eased;
                float moveHDelta = (float) (lerpX - _this.Position.X - _this.movementCounter.X), moveVDelta = (float) ((double) JITBarrier((float) lerpY) - _this.Position.Y - _this.movementCounter.Y);
                if (_this.Collidable) {
                    _this.MoveH(moveHDelta);
                    _this.MoveV(moveVDelta);
                } else {
                    _this.MoveHNaive(moveHDelta);
                    _this.MoveVNaive(moveVDelta);
                }
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static float JITBarrier(float v) => v;

        }

    }
}

namespace MonoMod {
    /// <summary>
    /// Patches <see cref="Celeste.DreamBlock.Setup()" /> to not rely on
    /// non-IEEE 754 compliant .NET Framework jank anymore, by patching the
    /// dream block particle count calculation to be done using doubles (the x86
    /// .NET Framework JIT uses 80 bit x87 registers for this calculation,
    /// however 64 bit doubles seem to have enough precision to end up at the
    /// same results). This fixes issue #556.
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDreamBlockSetup))]
    class PatchDreamBlockSetupAttribute : Attribute { }

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDreamBlockAdded))]
    class PatchDreamBlockAddedAttribute : Attribute { }

    /// <summary>
    ///  BetterFreezeFrames is il hooking it
    /// </summary>
    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDreamBlockUpdate))]
    class PatchDreamBlockUpdateAttribute : Attribute { }

    [MonoModCustomMethodAttribute(nameof(MonoModRules.PatchDreamBlockAddObsolete))]
    class PatchDreamBlockAddObsolete : Attribute {
        public PatchDreamBlockAddObsolete(string v) {
            Info = v;
        }

        public string Info { get; set; }
    }

    static partial class MonoModRules {

        public static void PatchDreamBlockUpdate(ILContext context, CustomAttribute attrib) {
            MethodDefinition m_patch_DreamBlock_UpdateHasDreamDash = MonoModRule.Modder.Module.GetType("Celeste.DreamBlock").FindMethod(nameof(Celeste.patch_DreamBlock.CheckFlags));
            ILCursor cursor = new(context);
            cursor.EmitLdarg0();
            cursor.EmitCallvirt(m_patch_DreamBlock_UpdateHasDreamDash);
        }
        public static void PatchDreamBlockAddObsolete(ILContext context, CustomAttribute attrib) {

            var attr = new CustomAttribute(context.Import(typeof(ObsoleteAttribute).GetConstructor(new Type[] { typeof(string) })));

            attr.ConstructorArguments.Add(attrib.ConstructorArguments[0]);
            context.Method.CustomAttributes.Add(attr);
        }
        public static void PatchDreamBlockAdded(ILContext context, CustomAttribute attrib) {
            ILCursor cursor = new(context);
            TypeDefinition t_patch_DreamBlock = MonoModRule.Modder.Module.GetType("Celeste.DreamBlock");
            MethodDefinition m_patch_DreamBlock_Init = t_patch_DreamBlock.FindMethod(nameof(Celeste.patch_DreamBlock.Init));
            // this.playerHasDreamDash = base.SceneAs<Level>().Session.Inventory.DreamDash;
            cursor.GotoNext(MoveType.AfterLabel, i => i.MatchStfld("Celeste.DreamBlock", "playerHasDreamDash"));
            cursor.EmitLdarg0();
            cursor.EmitCall(m_patch_DreamBlock_Init);
        }

        public static void PatchDreamBlockSetup(ILContext context, CustomAttribute attrib) {
            // Patch instructions before the 'conv.i4' cast to use doubles instead of floats
            for (int i = 0; i < context.Instrs.Count; i++) {
                Instruction instr = context.Instrs[i];

                if (instr.MatchConvI4())
                    break;

                // call(virt) <method returning float>
                if (instr.MatchCallOrCallvirt(out MethodReference method) && method.ReturnType.MetadataType == MetadataType.Single)
                    context.Instrs.Insert(++i, Instruction.Create(OpCodes.Conv_R8)); // cast return value to double

                // ldc.r4 <float constant>
                if (instr.MatchLdcR4(out float val))
                    context.Instrs[i] = Instruction.Create(OpCodes.Ldc_R8, (double) val);
            }
        }

    }
}