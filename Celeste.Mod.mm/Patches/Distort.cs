using Celeste.Mod.Core;
using MonoMod;

namespace Celeste {
    class patch_Distort {

        private static float anxiety = 0f;
        
        [MonoModReplace]
        public new static float Anxiety {
            get 
            {
                return anxiety;
            }
            set 
            {
                anxiety = value;
                GFX.FxDistort.Parameters["anxiety"].SetValue((CoreModule.Settings.AllowDistort) ? anxiety : 0f);
            } 
        }
    }
}
