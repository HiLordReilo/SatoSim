![SatoSim logo. Pretty much just BST1 logo, but pinkish and with with Sato-san's ribbons. Can you tell I like original BeatStream?](SatoSim.Core/Content/Graphics/Title/logo.png)
# SatoSim
A simulator for a long-gone arcade rhythm game BeatStream.


# Features
To be added....

# Building prerequisites
You will need [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) to be installed on your machine, as well as **Android SDKs** for SDK levels 23 (minimum: Android 6.0) and 30 (target: Android 11)


> [!IMPORTANT]
> This project uses **FMOD Engine/FMOD Core** for music playback, but its libraries are not included in the repository due to their licensing.
> You will need to get the FMOD Engine libraries from [FMOD downloads page](https://www.fmod.com/download#fmodengine) for a platform you're building for and refer to the table below.
>
> **:warning: Recommended FMOD version: `2.02.25`**
>
> This version is the target version of [FmodForFoxes](https://github.com/Martenfur/FmodForFoxes) - an FMOD wrapper library used by this project - and thus guaranteed to work. You can try going with newer versions, but there is no guarantee that they will work.
> 
> | Build platform | Instructions |
> |----------------|--------------|
> | Andrew John Google | Download the Android .tar.gz archive and open the .tar archive it contains. Open the only directory it contains, then go to `api/core/lib`. You will see subdirectories for each architecture with .so files in them and a .jar file. Copy these subdirectories to `libs` directory of `SatoSim.Android` project and the .jar file into `Jars` directory of `FmodForFoxes.Samples.Android.Binding` project. Feel free to delete `libfmodL.so`, `libfmodstudio.so` and `libfmodstudioL.so` files from copied subdirectories, as they are not used anyway. Set the Build action property to "AndroidNativeLibrary" for .so files and "AndroidLibrary" for .jar file. |
> | Michaelsoft Binbows | Download the Windows installer and open it as archive (you don't have to install it). Go to `api/core/lib/`. You will see subdirectories for each architecture with .dll and .lib files in them. Copy `fmod.dll` for architecture of your choice to the root directory of `SatoSim.Desktop` project. In your IDE, set the Copy to output directory property to "Copy if newer" or "Copy always". |

# Libraries
- [MonoGame](https://monogame.net/) - Game framework.
- [MonoGame.Extended](https://github.com/MonoGame-Extended/Monogame-Extended) - Collection of utilities and extensions for MonoGame.
- [FontStashSharp](https://github.com/FontStashSharp/FontStashSharp) - TTF/OTF font rendering.
- [FMOD](https://www.fmod.com/) - Sound engine.
- [FmodForFoxes](https://github.com/Martenfur/FmodForFoxes) - FMOD in MonoGame made easier.
