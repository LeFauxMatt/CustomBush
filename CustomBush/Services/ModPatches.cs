using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Integrations.CustomBush;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewValley.Tools;

namespace LeFauxMods.CustomBush.Services;

internal static class ModPatches
{
    private static readonly MethodInfo CheckItemPlantRules;

    private static IModHelper Helper = null!;

    static ModPatches() =>
        CheckItemPlantRules =
            typeof(GameLocation).GetMethod("CheckItemPlantRules", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new MethodAccessException("Unable to access CheckItemPlantRules");

    public static void Init(IModHelper modHelper)
    {
        Helper = modHelper;
        var harmony = new Harmony(ModConstants.ModId);

        // transpile stloc
        // bool inBloom = this.getAge() >= 20 && dayOfMonth >= 22 && (season != Season.Winter || this.IsSheltered());

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.draw), [typeof(SpriteBatch)]),
            new HarmonyMethod(typeof(ModPatches), nameof(Bush_draw_prefix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.GetShakeOffItem)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_GetShakeOffItem_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.inBloom)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_inBloom_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.isDestroyable)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_isDestroyable_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.performToolAction)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_performToolAction_postfix)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Bush_performToolAction_transpiler)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.setUpSourceRect)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Bush_setUpSourceRect_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.shake)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Bush_shake_transpiler)));

        _ = harmony.Patch(
            typeof(GameLocation).GetMethod(
                nameof(GameLocation.CheckItemPlantRules),
                BindingFlags.Public | BindingFlags.Instance) ??
            throw new MethodAccessException("Unable to access CheckItemPlantRules"),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(GameLocation_CheckItemPlantRules_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(HoeDirt), nameof(HoeDirt.canPlantThisSeedHere)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(HoeDirt_canPlantThisSeedHere_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(IndoorPot), nameof(IndoorPot.performObjectDropInAction)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(IndoorPot_performObjectDropInAction_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(JunimoHarvester), nameof(JunimoHarvester.update)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(JunimoHarvester_update_transpiler)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.IsTeaSapling)),
            postfix: new HarmonyMethod(typeof(ModPatches), nameof(Object_IsTeaSapling_postfix)));

        _ = harmony.Patch(
            AccessTools.DeclaredMethod(typeof(SObject), nameof(SObject.placementAction)),
            transpiler: new HarmonyMethod(typeof(ModPatches), nameof(Object_placementAction_transpiler)));
    }

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Harmony")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static bool Bush_draw_prefix(
        Bush __instance,
        SpriteBatch spriteBatch,
        float ___shakeRotation,
        NetRectangle ___sourceRect,
        float ___yDrawOffset)
    {
        if (!ModState.ManagedBushes.TryGetValue(__instance, out var managedBush))
        {
            return true;
        }

        var effectiveSize = __instance.size.Value switch
        {
            3 => 0,
            4 => 1,
            _ => __instance.size.Value
        };

        var x = (__instance.Tile.X + 0.5f) * Game1.tileSize;
        var y = ((__instance.Tile.Y + 1f) * Game1.tileSize) + ___yDrawOffset;
        if (__instance.drawShadow.Value)
        {
            if (effectiveSize > 0)
            {
                spriteBatch.Draw(
                    Game1.mouseCursors,
                    Game1.GlobalToLocal(Game1.viewport,
                        new Vector2(((__instance.Tile.X + (effectiveSize == 1 ? 0.5f : 1f)) * Game1.tileSize) - 51f,
                            (__instance.Tile.Y * Game1.tileSize) - 16f + ___yDrawOffset)),
                    Bush.shadowSourceRect,
                    Color.White,
                    0f,
                    Vector2.Zero,
                    Game1.pixelZoom,
                    __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                    1E-06f);
            }
            else
            {
                spriteBatch.Draw(
                    Game1.shadowTexture,
                    Game1.GlobalToLocal(Game1.viewport,
                        new Vector2((__instance.Tile.X * Game1.tileSize) + 32f,
                            (__instance.Tile.Y * Game1.tileSize) + Game1.tileSize - 4f + ___yDrawOffset)),
                    Game1.shadowTexture.Bounds,
                    Color.White,
                    0f,
                    Game1.shadowTexture.Bounds.Center.ToVector2(),
                    Game1.pixelZoom,
                    SpriteEffects.None,
                    1E-06f);
            }
        }

        var xOffset = __instance.tileSheetOffset.Value == 0
            ? 0
            : managedBush.SpriteOffset * ___sourceRect.Value.Width;

        spriteBatch.Draw(
            managedBush.Texture,
            Game1.GlobalToLocal(Game1.viewport,
                new Vector2((__instance.Tile.X * Game1.tileSize) + ((effectiveSize + 1) * Game1.tileSize / 2),
                    ((__instance.Tile.Y + 1f) * Game1.tileSize) -
                    (effectiveSize > 0 && (!__instance.townBush.Value || effectiveSize != 1) &&
                     __instance.size.Value != 4
                        ? Game1.tileSize
                        : 0) + ___yDrawOffset)),
            ___sourceRect.Value with { X = ___sourceRect.Value.X + xOffset },
            Color.White,
            ___shakeRotation,
            new Vector2((effectiveSize + 1) * 16 / 2, 32f),
            Game1.pixelZoom,
            __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            ((__instance.getBoundingBox().Center.Y + 48) / 10000f) - (__instance.Tile.X / 1000000f));

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    private static void Bush_GetShakeOffItem_postfix(Bush __instance, ref string? __result)
    {
        if (!ModState.ManagedBushes.TryGetValue(__instance, out var managedBush))
        {
            return;
        }

        __result = managedBush.Item?.QualifiedItemId;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Bush_inBloom_postfix(Bush __instance, ref bool __result)
    {
        if (!ModState.ManagedBushes.TryGetValue(__instance, out var managedBush))
        {
            return;
        }

        __result = managedBush.Item is not null;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Bush_isDestroyable_postfix(Bush __instance, ref bool __result)
    {
        if (ModState.ManagedBushes.TryGetValue(__instance, out _))
        {
            __result = true;
        }
    }

    private static void Bush_performToolAction_postfix(Bush __instance, Tool t, int explosion, Vector2 tileLocation)
    {
        if (__instance.size.Value == Bush.greenTeaBush ||
            !ModState.ManagedBushes.TryGetValue(__instance, out var managedBush))
        {
            return;
        }

        if (t is MeleeWeapon { ItemId: "66" })
        {
            __instance.shake(tileLocation, true);
        }

        if (__instance.health <= -1)
        {
            Game1.createItemDebris(ItemRegistry.Create(managedBush.Id), tileLocation * Game1.tileSize, 2,
                __instance.Location);
        }
    }

    private static IEnumerable<CodeInstruction> Bush_performToolAction_transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var method = AccessTools.GetDeclaredMethods(typeof(ItemRegistry))
            .First(static method => method.Name == nameof(ItemRegistry.Create) && !method.IsGenericMethod);

        return new CodeMatcher(instructions).MatchStartForward(new CodeMatch(instruction => instruction.Calls(method)))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(CreateBushItem)))
            .InstructionEnumeration();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void Bush_setUpSourceRect_postfix(Bush __instance, NetRectangle ___sourceRect)
    {
        if (!ModState.ManagedBushes.TryGetValue(__instance, out var managedBush))
        {
            return;
        }

        var stage = managedBush.Stage;
        var assetName = Helper.GameContent.ParseAssetName(stage.Texture);
        if (assetName.IsEquivalentTo("TileSheets/bushes"))
        {
            return;
        }

        ___sourceRect.Value = new Rectangle(
            stage.SpritePosition.X,
            stage.SpritePosition.Y,
            stage.BushType.GetWidth(),
            stage.BushType.GetHeight());
    }

    private static IEnumerable<CodeInstruction> Bush_shake_transpiler(
        IEnumerable<CodeInstruction> instructions,
        ILGenerator generator) =>
        new CodeMatcher(instructions, generator)
            .MatchStartForward(new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldc_I4_3),
                new CodeMatch(OpCodes.Beq_S))
            .CreateLabelWithOffsets(3, out var ifFalse)
            .InsertAndAdvance(new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(TryOverrideDrop)),
                new CodeInstruction(OpCodes.Brfalse, ifFalse),
                new CodeInstruction(OpCodes.Ret))
            .InstructionEnumeration();

    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    private static Item CreateBushItem(
        string itemId,
        int amount,
        int quality,
        bool allowNull,
        Bush bush)
    {
        if (ModState.ManagedBushes.TryGetValue(bush, out var managedBush))
        {
            itemId = managedBush.Id;
        }

        return ItemRegistry.Create(itemId, amount, quality, allowNull);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void GameLocation_CheckItemPlantRules_postfix(
        GameLocation __instance,
        ref bool __result,
        string itemId,
        bool isGardenPot,
        bool defaultAllowed,
        ref string deniedMessage)
    {
        var metadata = ItemRegistry.GetMetadata(itemId);
        if (metadata is null || !ModState.Data.TryGetValue(metadata.QualifiedItemId, out var data))
        {
            return;
        }

        var parameters = new object[] { data.PlantableLocationRules, isGardenPot, defaultAllowed, null! };
        __result = (bool)CheckItemPlantRules.Invoke(__instance, parameters)!;
        deniedMessage = (string)parameters[3];
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void HoeDirt_canPlantThisSeedHere_postfix(string itemId, ref bool __result)
    {
        if (!__result || !ModState.Data.ContainsKey($"(O){itemId}"))
        {
            return;
        }

        __result = false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    private static void IndoorPot_performObjectDropInAction_postfix(
        IndoorPot __instance,
        Item dropInItem,
        bool probe,
        ref bool __result)
    {
        if (__result ||
            !ModState.Data.TryGetValue(dropInItem.QualifiedItemId, out var customBush) ||
            !customBush.Stages.TryGetValue(customBush.InitialStage, out var stage))
        {
            return;
        }

        __result = __instance.hoeDirt.Value.crop is null && __instance.bush.Value is null;
        if (probe || !__result)
        {
            return;
        }

        __instance.bush.Value = new Bush(__instance.TileLocation, (int)stage.BushType, __instance.Location)
        {
            modData = { [ModConstants.IdKey] = dropInItem.QualifiedItemId }, inPot = { Value = true }
        };

        if (__instance.Location.IsOutdoors)
        {
            return;
        }

        __instance.bush.Value.loadSprite();
        _ = Game1.playSound("coin");
    }

    private static Item JunimoHarvester_update_CreateItem(Item i, Bush bush) =>
        ModState.ManagedBushes.TryGetValue(bush, out var managedBush)
            ? managedBush.Item ?? i
            : i;

    private static IEnumerable<CodeInstruction>
        JunimoHarvester_update_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(OpCodes.Ldstr, "(O)815"),
                new CodeMatch(OpCodes.Ldc_I4_1),
                new CodeMatch(OpCodes.Ldc_I4_0),
                new CodeMatch(OpCodes.Ldc_I4_0))
            .Advance(2)
            .Insert(
                new CodeInstruction(OpCodes.Ldloc_S, (short)7),
                CodeInstruction.Call(typeof(ModPatches), nameof(JunimoHarvester_update_CreateItem)))
            .InstructionEnumeration();

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony")]
    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    private static void Object_IsTeaSapling_postfix(SObject __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (ModState.Data.TryGetValue(__instance.QualifiedItemId, out var customBush) &&
            customBush.Stages.TryGetValue(customBush.InitialStage, out var stage) &&
            stage.BushType is BushType.Tea)
        {
            __result = true;
        }
    }

    [SuppressMessage("ReSharper", "SuggestBaseTypeForParameter", Justification = "Harmony")]
    private static Bush Object_placementAction_AddModData(Bush bush, SObject obj)
    {
        if (!ModState.Data.TryGetValue(obj.QualifiedItemId, out var data))
        {
            return bush;
        }

        var managedBush = new ManagedBush(bush) { Id = obj.QualifiedItemId, StageId = data.InitialStage };

        ModState.ManagedBushes.Add(bush, managedBush);
        bush.setUpSourceRect();
        return bush;
    }

    private static IEnumerable<CodeInstruction>
        Object_placementAction_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchEndForward(
                new CodeMatch(
                    OpCodes.Newobj,
                    AccessTools.Constructor(
                        typeof(Bush),
                        [typeof(Vector2), typeof(int), typeof(GameLocation), typeof(int)])))
            .Advance(1)
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(Object_placementAction_AddModData)))
            .InstructionEnumeration();

    private static bool TryOverrideDrop(Bush bush)
    {
        var originalTileSheetOffset = bush.tileSheetOffset.Value;
        bush.tileSheetOffset.Value = 1;
        if (!ModState.ManagedBushes.TryGetValue(bush, out var managedBush) || managedBush.Item is not { } item)
        {
            bush.tileSheetOffset.Value = originalTileSheetOffset;
            return false;
        }

        bush.tileSheetOffset.Value = originalTileSheetOffset;
        managedBush.Item = null;

        Game1.createItemDebris(
            item,
            bush.getBoundingBox().Center.ToVector2(),
            Game1.random.Next(1, 4));

        return true;
    }
}