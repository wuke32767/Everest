using Monocle;
using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    [Tracked(false)]
    public class EntityColliderByComponent<T> : Component where T : Component {
        public readonly string componentType = typeof(T).Name;

        public Action<T> OnComponentAction;

        public Collider Collider;

        public EntityColliderByComponent(Action<T> onComponentAction, Collider collider = null)
            : base(active: true, visible: true) {
            OnComponentAction = onComponentAction;
            Collider = collider;
        }

        public override void Added(Entity entity) {
            if (!Engine.Scene.Tracker.IsComponentTracked<T>()) {
                patch_Tracker.AddComponentToTracker(typeof(T));
            }
            base.Added(entity);
        }

        public override void Update() {
            if (OnComponentAction == null) {
                return;
            }

            Collider collider = Entity.Collider;
            if (Collider != null) {
                Entity.Collider = Collider;
            }

            Entity.CollideDoByComponent(OnComponentAction);

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
