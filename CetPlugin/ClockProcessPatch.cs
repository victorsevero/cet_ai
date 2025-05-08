using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;

namespace CetPlugin;

[HarmonyPatch]
public static class ClockProcess_Step_Patch
{
    private static double? lastClockTime = null;

    [HarmonyTargetMethod]
    public static MethodBase TargetMethod()
    {
        var type = AccessTools.TypeByName("Motorways.Processes.ClockProcess");
        return AccessTools.Method(type, "Step");
    }

    [HarmonyPostfix]
    public static void Postfix(object __instance, object simulation, object deltaTime)
    {
        var clockModelType = AccessTools.TypeByName("Motorways.Models.ClockModel");
        if (clockModelType == null)
            return;

        var getModelsMethod = simulation
            .GetType()
            .GetMethods()
            .FirstOrDefault(m => m.Name == "GetModels" && m.IsGenericMethod);
        if (getModelsMethod == null)
            return;

        var getModelsGeneric = getModelsMethod.MakeGenericMethod(clockModelType);
        var modelList = getModelsGeneric.Invoke(simulation, null);
        if (modelList == null)
            return;

        var itemsField = modelList
            .GetType()
            .GetField("_models", BindingFlags.Instance | BindingFlags.NonPublic);
        var items = itemsField?.GetValue(modelList) as IEnumerable;
        if (items == null)
            return;

        foreach (var model in items)
        {
            var timeProp = AccessTools.Property(model.GetType(), "Time");
            var timeVal = timeProp?.GetValue(model);
            if (timeVal == null)
                continue;

            double currentTime = Convert.ToDouble(timeVal.ToString());

            int prevDay = (int)((lastClockTime ?? 0.0) / 20.0);
            int currDay = (int)(currentTime / 20.0);
            if (currDay > prevDay)
            {
                Plugin.Logger.LogInfo($"[ClockProcess Patch] Novo dia: {currDay}. Pausando jogo.");
                Plugin.Instance.PauseGame();
            }

            lastClockTime = currentTime;
            break;
        }
    }
}
