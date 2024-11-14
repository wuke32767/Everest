using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.Entities {
    [CustomEntity("everest/activateDreamBlocksTrigger")]
    public class ActivateDreamBlocksTrigger : Trigger {
        private bool rumble;
        private bool activate;
        private bool fastAnimation;

        public ActivateDreamBlocksTrigger(EntityData data, Vector2 offset)
            : base(data, offset) {
            rumble = data.Bool("fullRoutine");
            activate = data.Bool("activate", true);
            fastAnimation = data.Bool("fastAnimation");
        }

        public override void OnEnter(Player player) {
            Level level = Scene as Level;
            if (activate && !level.Session.Inventory.DreamDash) {
                level.Session.Inventory.DreamDash = true;
                foreach (patch_DreamBlock dreamBlock in level.Tracker.GetEntities<DreamBlock>()) {
                    if (rumble) {
                        if (fastAnimation)
                            dreamBlock.Add(new Coroutine(dreamBlock.UpdateFastRoutine(), true));
                        else
                            dreamBlock.Add(new Coroutine(dreamBlock.UpdateRoutine(), true));
                    } else
                        dreamBlock.UpdateNoRoutine();
                }
            } else if (!activate && level.Session.Inventory.DreamDash) {
                level.Session.Inventory.DreamDash = false;
                foreach (DreamBlock dreamBlock in level.Tracker.GetEntities<DreamBlock>()) {
                    if (rumble) {
                        if (fastAnimation)
                            dreamBlock.Add(new Coroutine(((patch_DreamBlock) dreamBlock).UpdateFastRoutine(), true));
                        else
                            dreamBlock.Add(new Coroutine(((patch_DreamBlock) dreamBlock).UpdateRoutine(), true));
                    } else
                        ((patch_DreamBlock) dreamBlock).UpdateNoRoutine();
                }
            }
        }
    }
}
