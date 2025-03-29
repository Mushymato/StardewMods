/*
[game] Error initializing the Galaxy API.
TypeInitializationException: The type initializer for 'Galaxy.Api.GalaxyInstancePINVOKE' threw an exception.
 ---> TypeInitializationException: The type initializer for 'SWIGExceptionHelper' threw an exception.
 ---> DllNotFoundException: Unable to load shared library 'GalaxyCSharpGlue' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: libGalaxyCSharpGlue: cannot open shared object file: No such file or directory
   at Galaxy.Api.GalaxyInstancePINVOKE.SWIGExceptionHelper.SWIGRegisterExceptionCallbacks_GalaxyInstance(ExceptionDelegate applicationDelegate, ExceptionDelegate arithmeticDelegate, ExceptionDelegate divideByZeroDelegate, ExceptionDelegate indexOutOfRangeDelegate, ExceptionDelegate invalidCastDelegate, ExceptionDelegate invalidOperationDelegate, ExceptionDelegate ioDelegate, ExceptionDelegate nullReferenceDelegate, ExceptionDelegate outOfMemoryDelegate, ExceptionDelegate overflowDelegate, ExceptionDelegate systemExceptionDelegate)
   at Galaxy.Api.GalaxyInstancePINVOKE.SWIGExceptionHelper..cctor()
   --- End of inner exception stack trace ---
   at Galaxy.Api.GalaxyInstancePINVOKE.SWIGExceptionHelper..ctor()
   at Galaxy.Api.GalaxyInstancePINVOKE..cctor()
   --- End of inner exception stack trace ---
   at Galaxy.Api.GalaxyInstancePINVOKE.new_InitParams__SWIG_3(String jarg1, String jarg2, String jarg3)
   at StardewValley.SDKs.Steam.SteamHelper.Initialize() in D:\GitlabRunner\builds\Gq5qA5P4\1\ConcernedApe\stardewvalley\Farmer\Farmer\SDKs\Steam\SteamHelper.cs:line 90
[SMAPI] Type 'help' for help, or 'help <cmd>' for a command's usage
[game] Galaxy SignInSteam failed with an exception:
TypeInitializationException: The type initializer for 'Galaxy.Api.GalaxyInstance' threw an exception.
 ---> TypeInitializationException: The type initializer for 'CustomExceptionHelper' threw an exception.
 ---> DllNotFoundException: Unable to load shared library 'GalaxyCSharpGlue' or one of its dependencies. In order to help diagnose loading problems, consider setting the LD_DEBUG environment variable: libGalaxyCSharpGlue: cannot open shared object file: No such file or directory
   at Galaxy.Api.GalaxyInstance.CustomExceptionHelper.CustomExceptionRegisterCallback(CustomExceptionDelegate customCallback)
   at Galaxy.Api.GalaxyInstance.CustomExceptionHelper..cctor()
   --- End of inner exception stack trace ---
   at Galaxy.Api.GalaxyInstance.CustomExceptionHelper..ctor()
   at Galaxy.Api.GalaxyInstance..cctor()
   --- End of inner exception stack trace ---
   at Galaxy.Api.GalaxyInstance.User()
   at StardewValley.SDKs.Steam.SteamHelper.onEncryptedAppTicketResponse(EncryptedAppTicketResponse_t response, Boolean ioFailure) in D:\GitlabRunner\builds\Gq5qA5P4\1\ConcernedApe\stardewvalley\Farmer\Farmer\SDKs\Steam\SteamHelper.cs:line 251

*/
using HarmonyLib;
using StardewModdingAPI;
using StardewValley.SDKs.Steam;

namespace MatoTweaks.Tweak;

internal static class SuppressSteamHelper
{
    public static void Patch(Harmony patcher)
    {
        try
        {
            // skip initializing steam and galaxy
            patcher.Patch(
                original: AccessTools.DeclaredMethod(typeof(SteamHelper), nameof(SteamHelper.Initialize)),
                prefix: new HarmonyMethod(typeof(SuppressSteamHelper), nameof(SteamHelper_Initialize_Skip))
            );
        }
        catch (Exception err)
        {
            ModEntry.Log($"Failed to patch SuppressSteamHelper:\n{err}", LogLevel.Error);
        }
    }

    private static bool SteamHelper_Initialize_Skip()
    {
        return false;
    }
}
