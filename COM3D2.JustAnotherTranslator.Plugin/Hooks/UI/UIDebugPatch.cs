using System;
using System.Linq;
using System.Reflection;
using COM3D2.JustAnotherTranslator.Plugin.Utils;
using HarmonyLib;
using I2.Loc;

namespace COM3D2.JustAnotherTranslator.Plugin.Hooks.UI;

public class UIDebugPatch
{
    /// <summary>
    ///     Hooks into every ILocalizeTarget implementation to log calls to DoLocalize
    /// </summary>
    public static class LocalizeTargetPatcher
    {
        // Keep track of patched types to avoid patching multiple times
        private static bool _patched;

        public static void ApplyPatch(Harmony harmonyInstance)
        {
            if (_patched)
            {
                LogManager.Debug("ILocalizeTarget patch has already been applied.");
                return;
            }

            LogManager.Debug("Applying ILocalizeTarget patch...");

            var targetType = typeof(ILocalizeTarget);

            // Find all types that implement ILocalizeTarget in all loaded assemblies
            var implementingTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(assembly =>
                {
                    try
                    {
                        return assembly.GetTypes();
                    }
                    catch (ReflectionTypeLoadException)
                    {
                        // Ignore assemblies that fail to load types
                        return Type.EmptyTypes;
                    }
                })
                .Where(type => targetType.IsAssignableFrom(type) && !type.IsAbstract && !type.IsInterface)
                .ToList();

            if (!implementingTypes.Any())
            {
                LogManager.Debug("No implementations of ILocalizeTarget found to patch.");
                return;
            }

            var prefix = new HarmonyMethod(typeof(DoLocalizePatches),
                nameof(DoLocalizePatches.LogDoLocalizeCallPrefix));
            var patchedCount = 0;

            foreach (var type in implementingTypes)
            {
                // We need to find the specific implementation of DoLocalize in this type.
                // Using DeclaredOnly helps to get the override in the current class, not the base abstract method.
                var method = type.GetMethod("DoLocalize",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);

                if (method != null)
                    try
                    {
                        harmonyInstance.Patch(method, prefix);
                        patchedCount++;
                        LogManager.Debug($"Successfully patched {type.FullName}.DoLocalize");
                    }
                    catch (Exception e)
                    {
                        LogManager.Debug($"Failed to patch {type.FullName}.DoLocalize: {e.Message}");
                    }
            }

            LogManager.Debug(
                $"Patching complete. Patched {patchedCount} out of {implementingTypes.Count} implementations of ILocalizeTarget.");
            _patched = true;
        }
    }

    /// <summary>
    ///     This prefix will run before every call to a patched DoLocalize method
    /// </summary>
    public static class DoLocalizePatches
    {
        public static void LogDoLocalizeCallPrefix(ILocalizeTarget __instance, Localize cmp, string mainTranslation,
            string secondaryTranslation)
        {
            try
            {
                var instanceType = __instance.GetType().FullName;
                var gameObjectName = cmp != null && cmp.gameObject != null ? cmp.gameObject.name : "N/A";

                var logMessage =
                    $"[DoLocalize] Called on Type: {instanceType}\n" +
                    $"  - GameObject: {gameObjectName}\n" +
                    $"  - Main Translation: '{mainTranslation}'\n" +
                    $"  - Secondary Translation: '{secondaryTranslation}'";

                LogManager.Debug(logMessage);
            }
            catch (Exception e)
            {
                LogManager.Debug($"Error in DoLocalize patch prefix: {e.Message}");
            }
        }
    }
}