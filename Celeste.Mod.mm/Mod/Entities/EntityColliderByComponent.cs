using Monocle;
using System;
using Microsoft.Xna.Framework;

namespace Celeste.Mod.Entities {
    /// <summary>
    /// Base type of <see cref="EntityColliderByComponent{T}"/>.
    /// In case you don't know the exact type.
    /// </summary>
    [Tracked(false)]
    public abstract class EntityColliderByComponent : Component {
        public abstract Type ComponentType { get; }

        public virtual Action<Component> OnComponentAction => throw new NotImplementedException();

        public Collider Collider;

        public EntityColliderByComponent(bool active, bool visible) : base(active, visible) {
        }
    }
    /// <summary>
    /// Allows for Collision with any type of entity in the game, similar to a PlayerCollider or PufferCollider.
    /// Collision is done by component, as in, it will get all the components of the type and try to collide with their entities.
    /// Performs the Action provided on collision. 
    /// </summary>
    /// <typeparam name="T">The specific type of Component this component should try to collide with</typeparam>
    public class EntityColliderByComponent<T> : EntityColliderByComponent where T : Component {
        /// <summary>
        /// Provides a simple way to know the Component type of the specific Collider
        /// </summary>
        public override Type ComponentType => typeof(T);

        /// <summary>
        /// The Action invoked on Collision, with the Component collided with passed as a parameter
        /// </summary>
        public new Action<T> OnComponentAction;

        [MonoMod.NotPatchEntityColliderMakeVirtual]
        public Action<Component> get_OnEntityAction() => c => {
            OnComponentAction((T) c);
        };

        public EntityColliderByComponent(Action<T> onComponentAction, Collider collider = null)
            : base(active: true, visible: true) {
            OnComponentAction = onComponentAction;
            Collider = collider;
        }

        public override void Added(Entity entity) {
            base.Added(entity);
            //Only called if Component is added post Scene Begin and Entity Adding and Awake time.
            if (Scene != null) {
                if (!Scene.Tracker.IsComponentTracked<T>()) {
                    patch_Tracker.AddTypeToTracker(typeof(T));
                }
                if (!Scene.Tracker.IsComponentTracked<EntityColliderByComponent<T>>()) {
                    patch_Tracker.AddTypeToTracker(typeof(EntityColliderByComponent<T>));
                    patch_Tracker.AddTypeToTracker(typeof(EntityColliderByComponent<T>), typeof(EntityColliderByComponent));
                }
                (Scene.Tracker as patch_Tracker).Refresh();
            }
        }

        public override void EntityAdded(Scene scene) {
            if (!scene.Tracker.IsComponentTracked<T>()) {
                patch_Tracker.AddTypeToTracker(typeof(T));
            }
            if (!scene.Tracker.IsComponentTracked<EntityColliderByComponent<T>>()) {
                patch_Tracker.AddTypeToTracker(typeof(EntityColliderByComponent<T>));
                patch_Tracker.AddTypeToTracker(typeof(EntityColliderByComponent<T>), typeof(EntityColliderByComponent));
            }
            base.EntityAdded(scene);
        }

        public override void EntityAwake() {
            (Scene.Tracker as patch_Tracker).Refresh();
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
