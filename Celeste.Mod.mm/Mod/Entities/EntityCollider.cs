using Monocle;
using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    [Tracked(false)]
    public class EntityCollider<T> : Component where T : Entity {
        public readonly string entityType = typeof(T).Name;

        public Action<T> OnEntityAction;

        public Collider Collider;

        public EntityCollider(Action<T> onEntityAction, Collider collider)
            : base(active: true, visible: true) {
            OnEntityAction = onEntityAction;
            Collider = collider;
        }

        public override void EntityAdded(Scene scene) {
            if (scene.Tracker.IsEntityTracked<T>()) {
                (scene.Tracker as patch_Tracker).AddEntityToTracker(typeof(T));
            }
            base.EntityAdded(scene);
        }

        public override void Update() {
            if (OnEntityAction == null) {
                return;
            }

            Collider collider = Entity.Collider;
            if (Collider != null) {
                Entity.Collider = Collider;
            }

            Entity.CollideDo(OnEntityAction);

            Entity.Collider = collider;
        }

        public override void DebugRender(Camera camera) {
            if (Collider != null) {
                Collider collider = Entity.Collider;
                Entity.Collider = Collider;
                Collider.Render(camera, Color.HotPink);
                Entity.Collider = collider;
            }
        }
    }
}
