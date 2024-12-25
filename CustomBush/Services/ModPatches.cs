using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using LeFauxMods.Common.Utilities;
using LeFauxMods.CustomBush.Models;
using LeFauxMods.CustomBush.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Netcode;
using StardewValley.Characters;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace LeFauxMods.CustomBush.Services;

internal static class ModPatches
{
    private static readonly MethodInfo CheckItemPlantRules;

    private static Func<Dictionary<string, CustomBushData>> getData = null!;
    private static Func<string, Texture2D> getTexture = null!;
    private static IModHelper helper = null!;

    static ModPatches() =>
        CheckItemPlantRules =
            typeof(GameLocation).GetMethod("CheckItemPlantRules", BindingFlags.NonPublic | BindingFlags.Instance) ??
            throw new MethodAccessException("Unable to access CheckItemPlantRules");

    private static Dictionary<string, CustomBushData> Data => getData();

    public static void Init(IModHelper modHelper, Func<Dictionary<string, CustomBushData>> getDataFunc,
        Func<string, Texture2D> getTextureFunc)
    {
        helper = modHelper;
        getData = getDataFunc;
        getTexture = getTextureFunc;

        var harmony = new Harmony(Constants.ModId);

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
            AccessTools.DeclaredMethod(typeof(Bush), nameof(Bush.performToolAction)),
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

    [SuppressMessage("Style", "IDE1006:Naming Styles", Justification = "Harmony.")]
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static bool Bush_draw_prefix(
        Bush __instance,
        SpriteBatch spriteBatch,
        float ___shakeRotation,
        NetRectangle ___sourceRect,
        float ___yDrawOffset)
    {
        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id) ||
            !Data.TryGetValue(id, out var bushModel))
        {
            return true;
        }

        var x = (__instance.Tile.X * 64) + 32;
        var y = (__instance.Tile.Y * 64) + 64 + ___yDrawOffset;
        if (__instance.drawShadow.Value)
        {
            spriteBatch.Draw(
                Game1.shadowTexture,
                Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y - 4)),
                Game1.shadowTexture.Bounds,
                Color.White,
                0,
                new Vector2(Game1.shadowTexture.Bounds.Center.X, Game1.shadowTexture.Bounds.Center.Y),
                4,
                SpriteEffects.None,
                1E-06f);
        }

        string path;
        if (!__instance.IsSheltered())
        {
            path = bushModel.Texture;
        }
        else if (!string.IsNullOrWhiteSpace(bushModel.IndoorTexture))
        {
            path = bushModel.IndoorTexture;
        }
        else
        {
            path = bushModel.Texture;
        }

        var texture = GetTexture(path);
        var xOffset = 0;
        if (__instance.modData.TryGetValue(Constants.ModDataSpriteOffset, out var spriteOffsetString) &&
            int.TryParse(spriteOffsetString, out var spriteOffset))
        {
            xOffset = spriteOffset * 16;
        }

        spriteBatch.Draw(
            texture,
            Game1.GlobalToLocal(Game1.viewport, new Vector2(x, y)),
            ___sourceRect.Value with { X = ___sourceRect.Value.X + xOffset },
            Color.White,
            ___shakeRotation,
            new Vector2(8, 32),
            4,
            __instance.flipped.Value ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
            ((__instance.getBoundingBox().Center.Y + 48) / 10000f) - (__instance.Tile.X / 1000000f));

        return false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Bush_GetShakeOffItem_postfix(Bush __instance, ref string __result)
    {
        if (__instance.modData.TryGetValue(Constants.ModDataItem, out var itemId) && !string.IsNullOrWhiteSpace(itemId))
        {
            __result = itemId;
        }
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    [SuppressMessage("ReSharper", "SeparateLocalFunctionsWithJumpStatement")]
    private static void Bush_inBloom_postfix(Bush __instance, ref bool __result)
    {
        if (__instance.TryGetCachedData(out _, out _, out _, out var condition))
        {
            if (string.IsNullOrWhiteSpace(condition) || TestCondition(condition))
            {
                __result = true;
                return;
            }

            Log.Trace("Cached item's condition does not pass: {0}\nClearing cache and recalculating item.", condition);
            __instance.ClearCachedData();
        }

        // Check if bush has custom bush data
        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id) ||
            !Data.TryGetValue(id, out var bushModel))
        {
            return;
        }

        var age = __instance.getAge();

        // Skip all checks if they were already performed at this age
        if (__instance.modData.TryGetValue(Constants.ModDataAge, out var ageString) &&
            int.TryParse(ageString, out var ageInt) &&
            age == ageInt)
        {
            __result = false;
            return;
        }

        // Check if bush meets the age requirement
        if (age < bushModel.AgeToProduce)
        {
            Log.Trace(
                "{0} will not produce. Age: {1} < {2}",
                id,
                age.ToString(CultureInfo.InvariantCulture),
                bushModel.AgeToProduce.ToString(CultureInfo.InvariantCulture));

            __result = false;
            __instance.modData[Constants.ModDataAge] = age.ToString(CultureInfo.InvariantCulture);
            return;
        }

        // Check if bush meets any condition requirement
        condition = bushModel.ConditionsToProduce.FirstOrDefault(TestCondition);
        if (string.IsNullOrWhiteSpace(condition))
        {
            Log.Trace("{0} will not produce. None of the required conditions was met.", id);
            __result = false;
            __instance.modData[Constants.ModDataAge] = age.ToString(CultureInfo.InvariantCulture);
            return;
        }

        // Try to produce item
        Log.Trace("{0} attempting to produce random item.", id);
        if (!__instance.TryProduceAny(out var item, out var drop, bushModel))
        {
            Log.Trace("{0} will not produce. No item was produced.", id);
            __result = false;
            __instance.modData[Constants.ModDataAge] = age.ToString(CultureInfo.InvariantCulture);
            return;
        }

        Log.Trace(
            "{0} selected {1} to grow with quality {2} and quantity {3}.",
            id,
            item.QualifiedItemId,
            item.Quality,
            item.Stack);

        __result = true;
        __instance.modData[Constants.ModDataAge] = age.ToString(CultureInfo.InvariantCulture);
        __instance.modData[Constants.ModDataCondition] = condition;
        __instance.modData[Constants.ModDataItem] = item.QualifiedItemId;
        __instance.modData[Constants.ModDataQuality] = item.Quality.ToString(CultureInfo.InvariantCulture);
        __instance.modData[Constants.ModDataStack] = item.Stack.ToString(CultureInfo.InvariantCulture);
        __instance.modData[Constants.ModDataSpriteOffset] =
            drop.SpriteOffset.ToString(CultureInfo.InvariantCulture);

        bool TestCondition(string conditionToTest)
        {
            return GameStateQuery.CheckConditions(conditionToTest, __instance.Location, null, null, null, null,
                __instance.Location.SeedsIgnoreSeasonsHere() || __instance.IsSheltered()
                    ? GameStateQuery.SeasonQueryKeys
                    : null);
        }
    }

    private static IEnumerable<CodeInstruction> Bush_performToolAction_transpiler(
        IEnumerable<CodeInstruction> instructions)
    {
        var method = AccessTools.GetDeclaredMethods(typeof(ItemRegistry))
            .First(method => method.Name == nameof(ItemRegistry.Create) && !method.IsGenericMethod);

        return new CodeMatcher(instructions).MatchStartForward(new CodeMatch(instruction => instruction.Calls(method)))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(CreateBushItem)))
            .InstructionEnumeration();
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Bush_setUpSourceRect_postfix(Bush __instance, NetRectangle ___sourceRect)
    {
        if (!__instance.modData.TryGetValue(Constants.ModDataId, out var id) ||
            !Data.TryGetValue(id, out var bushModel)
            || string.IsNullOrWhiteSpace(bushModel.Texture))
        {
            return;
        }

        var assetName = helper.GameContent.ParseAssetName(bushModel.Texture);
        if (assetName.IsEquivalentTo("TileSheets/bushes"))
        {
            return;
        }

        if (!__instance.IsSheltered())
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.Texture;
        }
        else if (!string.IsNullOrWhiteSpace(bushModel.IndoorTexture))
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.IndoorTexture;
        }
        else
        {
            __instance.modData[Constants.ModDataTexture] = bushModel.Texture;
        }

        var age = __instance.getAge();
        var growthPercent = (float)age / bushModel.AgeToProduce;
        var x = (Math.Min(2, (int)(2 * growthPercent)) + __instance.tileSheetOffset.Value) * 16;
        var y = bushModel.TextureSpriteRow * 16;

        ___sourceRect.Value = new Rectangle(x, y, 16, 32);
    }

    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "Harmony.")]
    private static void Bush_shake_CreateObjectDebris(
        string id,
        int xTile,
        int yTile,
        int groundLevel,
        int itemQuality,
        float velocityMultiplier,
        GameLocation? location,
        Bush bush)
    {
        // Create cached item
        if (bush.TryGetCachedData(out var itemId, out itemQuality, out var itemStack, out _))
        {
            for (var i = 0; i < itemStack; i++)
            {
                Game1.createObjectDebris(
                    itemId,
                    xTile,
                    yTile,
                    groundLevel,
                    itemQuality,
                    velocityMultiplier,
                    location);
            }

            bush.ClearCachedData();
            return;
        }

        bush.ClearCachedData();

        // Try to create random item
        if (bush.TryProduceAny(out var item, out _))
        {
            Game1.createObjectDebris(
                item.QualifiedItemId,
                xTile,
                yTile,
                groundLevel,
                item.Quality,
                velocityMultiplier,
                location);

            return;
        }

        // Create vanilla item
        Game1.createObjectDebris(
            id,
            xTile,
            yTile,
            groundLevel,
            itemQuality,
            velocityMultiplier,
            location);
    }

    private static IEnumerable<CodeInstruction> Bush_shake_transpiler(IEnumerable<CodeInstruction> instructions) =>
        new CodeMatcher(instructions)
            .MatchStartForward(
                new CodeMatch(
                    instruction => instruction.Calls(
                        AccessTools.DeclaredMethod(
                            typeof(Game1),
                            nameof(Game1.createObjectDebris),
                            [
                                typeof(string),
                                typeof(int),
                                typeof(int),
                                typeof(int),
                                typeof(int),
                                typeof(float),
                                typeof(GameLocation)
                            ]))))
            .RemoveInstruction()
            .InsertAndAdvance(
                new CodeInstruction(OpCodes.Ldarg_0),
                CodeInstruction.Call(typeof(ModPatches), nameof(Bush_shake_CreateObjectDebris)))
            .InstructionEnumeration();

    private static Item CreateBushItem(
        string itemId,
        int amount,
        int quality,
        bool allowNull,
        Bush bush)
    {
        if (bush.modData.TryGetValue(Constants.ModDataId, out var bushId))
        {
            itemId = bushId;
        }

        return ItemRegistry.Create(itemId, amount, quality, allowNull);
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void GameLocation_CheckItemPlantRules_postfix(
        GameLocation __instance,
        ref bool __result,
        string itemId,
        bool isGardenPot,
        bool defaultAllowed,
        ref string deniedMessage)
    {
        var metadata = ItemRegistry.GetMetadata(itemId);
        if (metadata is null || !Data.TryGetValue(metadata.QualifiedItemId, out var bushModel))
        {
            return;
        }

        var parameters = new object[] { bushModel.PlantableLocationRules, isGardenPot, defaultAllowed, null! };
        __result = (bool)CheckItemPlantRules.Invoke(__instance, parameters)!;
        deniedMessage = (string)parameters[3];
    }

    private static Texture2D GetTexture(string path) => getTexture!(path);

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void HoeDirt_canPlantThisSeedHere_postfix(string itemId, ref bool __result)
    {
        if (!__result || !Data.ContainsKey($"(O){itemId}"))
        {
            return;
        }

        __result = false;
    }

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void IndoorPot_performObjectDropInAction_postfix(
        IndoorPot __instance,
        Item dropInItem,
        bool probe,
        ref bool __result)
    {
        if (!Data.ContainsKey(dropInItem.QualifiedItemId) ||
            __instance.QualifiedItemId != "(BC)62" ||
            __instance.hoeDirt.Value.crop != null)
        {
            return;
        }

        var empty = __instance.hoeDirt.Value.crop is null && __instance.bush.Value is null;
        if (!probe && empty)
        {
            __instance.bush.Value = new Bush(__instance.TileLocation, 3, __instance.Location)
            {
                modData = { [Constants.ModDataId] = dropInItem.QualifiedItemId },
                inPot = { Value = true }
            };

            if (!__instance.Location.IsOutdoors)
            {
                __instance.bush.Value.loadSprite();
                _ = Game1.playSound("coin");
            }
        }

        __result = empty;
    }

    private static Item JunimoHarvester_update_CreateItem(Item i, Bush bush)
    {
        // Return cached item
        if (bush.TryGetCachedData(out var itemId, out var itemQuality, out var itemStack, out _))
        {
            return ItemRegistry.Create(itemId, itemStack, itemQuality);
        }

        // Try to return random item else return vanilla item
        return bush.TryProduceAny(out var item, out _) ? item : i;
    }

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

    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Harmony.")]
    private static void Object_IsTeaSapling_postfix(SObject __instance, ref bool __result)
    {
        if (__result)
        {
            return;
        }

        if (Data.ContainsKey(__instance.QualifiedItemId))
        {
            __result = true;
        }
    }

    private static Bush Object_placementAction_AddModData(Bush bush, SObject obj)
    {
        if (!Data.ContainsKey(obj.QualifiedItemId))
        {
            return bush;
        }

        bush.modData[Constants.ModDataId] = obj.QualifiedItemId;
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
}
