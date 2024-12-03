using Monocle;
using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    /// <summary>
    /// Base type of <see cref="EntityCollider{T}"/>.
    /// In case you don't know the exact type.
    /// </summary>
    [Tracked(false)]
    public abstract class EntityCollider : Component {
        public abstract Type EntityType { get; }

        public Action<Entity> OnEntityAction { get; protected set; }

        public Collider Collider;

        public EntityCollider(bool active, bool visible) : base(active, visible) {
        }
    }
    /// <summary>
    /// Allows for Collision with any type of entity in the game, similar to a PlayerCollider or PufferCollider.
    /// Performs the Action provided on collision. 
    /// </summary>
    /// <typeparam name="T">The specific type of Entity this component should try to collide with</typeparam>
    public class EntityCollider<T> : EntityCollider where T : Entity {
        /// <summary>
        /// Provides a simple way to know the Entity type of the specific Collider
        /// </summary>
        public override Type EntityType => typeof(T);

        /// <summary>
        /// The Action invoked on Collision, with the Entity collided with passed as a parameter
        /// </summary>
        public new Action<T> OnEntityAction;

        public EntityCollider(Action<T> onEntityAction, Collider collider = null)
            : base(active: true, visible: true) {
            base.OnEntityAction = e => {
                OnEntityAction((T) e);
            };
            OnEntityAction = onEntityAction;
            Collider = collider;
        }

        public override void Added(Entity entity) {
            base.Added(entity);
            //Only called if Component is added post Scene Begin and Entity Adding and Awake time.
            if (Scene != null) {
                if (!Scene.Tracker.IsEntityTracked<T>()) {
                    patch_Tracker.AddTypeToTracker(typeof(T));
                }
                if (!Scene.Tracker.IsComponentTracked<EntityCollider<T>>()) {
                    patch_Tracker.AddTypeToTracker(typeof(EntityCollider<T>));
                    patch_Tracker.AddTypeToTracker(typeof(EntityCollider<T>), typeof(EntityCollider));
                }
                (Scene.Tracker as patch_Tracker).Refresh();
            }
        }

        public override void EntityAdded(Scene scene) {
            if (!scene.Tracker.IsEntityTracked<T>()) {
                patch_Tracker.AddTypeToTracker(typeof(T));
            }
            if (!scene.Tracker.IsComponentTracked<EntityCollider<T>>()) {
                patch_Tracker.AddTypeToTracker(typeof(EntityCollider<T>));
                patch_Tracker.AddTypeToTracker(typeof(EntityCollider<T>), typeof(EntityCollider));
            }
            base.EntityAdded(scene);
        }

        public override void EntityAwake() {
            (Scene.Tracker as patch_Tracker).Refresh();
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
