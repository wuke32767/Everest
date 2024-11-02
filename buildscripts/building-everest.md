# Building Everest

In order to build everest two steps are required: building the source files 
and copying and installing them.
This process can be automated through the build scripts.
All requirements stated in "Compiling Everest yourself" in the [README.md](../README.md) apply here as well.

- `complete_build_and_install.sh`: This script's goal is to take a vanilla version of celeste and
 generate, copy and install all the files in order to get everest running. It is somewhat slow
 since it makes no assumptions on what is currently there. <br>
 Use it to install everest from source for the first time, or to reset an installation to a working
 state.
- `quick_patch.sh`: This script's goal is to get any changes in `Celeste.Mod.mm` as quickly as possible
 to the game, thus it skips over a lot of steps. In order to run it you need a working installation
 of everest already in the target location.
 Use it to quickly test any modifications to everest itself.

Both scripts need to know where your game is, as such a variable called `CELESTEPATHGAME` is present
in both of them, adjust it to fit yours.

If the scripts are integrated into an IDE workflow, it is recommended to force the ide to skip building, 
since both of them will build the files already.