using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using FixMath;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CetPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Mini Motorways.exe")]
public class Plugin : BaseUnityPlugin
{
    public static Plugin Instance { get; private set; }
    public static new ManualLogSource Logger { get; private set; }

    private Type houseViewType;
    private Type destinationViewType;

    // private int frameCounter = 0;
    private GameState currentState = new GameState();
    private TcpClient client;
    private NetworkStream stream;
    double? lastClockTime = null;
    private bool shouldSpeedUp = true;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        var harmony = new Harmony("cet.clockpatch");
        harmony.PatchAll();

        houseViewType = AccessTools.TypeByName("Motorways.Views.HouseView");
        if (houseViewType == null)
        {
            Logger.LogWarning("Tipo HouseView não encontrado!");
            return;
        }
        destinationViewType = AccessTools.TypeByName("Motorways.Views.DestinationView");
        if (destinationViewType == null)
        {
            Logger.LogWarning("Tipo DestinationView não encontrado!");
            return;
        }

        try
        {
            client = new TcpClient("localhost", 5000);
            stream = client.GetStream();
            Logger.LogInfo("[Socket] Conectado ao servidor Python.");
        }
        catch (Exception ex)
        {
            Logger.LogError($"[Socket] Falha na conexão: {ex.Message}");
        }
    }

    void Update()
    {
        // frameCounter++;
        // if (frameCounter % 60 != 0)
        //     return;

        if (shouldSpeedUp)
        {
            SetGameSpeed(100f);
        }
        // TryLogClockTime();
        // CaptureGameState();
    }

    void CaptureGameState()
    {
        currentState.houses.Clear();
        CaptureHouses();
        currentState.destinations.Clear();
        CaptureDestinations();

        Logger.LogInfo(
            $"[GameState] {currentState.houses.Count} casas, {currentState.destinations.Count} destinos."
        );
        SendGameState();
    }

    void CaptureHouses()
    {
        var allHouseViews = Resources.FindObjectsOfTypeAll(houseViewType);

        foreach (var viewObj in allHouseViews)
        {
            if (viewObj == null)
                continue;

            var viewComponent = viewObj as MonoBehaviour;
            if (viewComponent == null)
                continue;

            var go = viewComponent.gameObject;
            if (go == null || !go.activeInHierarchy)
                continue;

            var tileField = AccessTools.Field(houseViewType, "tilePosition");
            if (tileField == null)
                continue;

            var city = AccessTools.Property(houseViewType, "City").GetValue(viewComponent);
            var definition = AccessTools.Property(city.GetType(), "Definition").GetValue(city);
            var definitionMono = definition as MonoBehaviour;
            if (definitionMono.gameObject.name == "MenuCity")
                continue;

            Vector2Int tile = (Vector2Int)tileField.GetValue(viewComponent);
            int color = (int)AccessTools.Field(houseViewType, "groupIndex").GetValue(viewComponent);

            var snapshot = new HouseSnapshot
            {
                x = tile.x,
                y = tile.y,
                color = color,
            };

            currentState.houses.Add(snapshot);
        }
    }

    void CaptureDestinations()
    {
        var allDestinationViews = Resources.FindObjectsOfTypeAll(destinationViewType);

        foreach (var viewObj in allDestinationViews)
        {
            if (viewObj == null)
                continue;

            var viewComponent = viewObj as MonoBehaviour;
            if (viewComponent == null)
                continue;

            var go = viewComponent.gameObject;
            if (go == null || !go.activeInHierarchy)
                continue;

            var city = AccessTools.Property(destinationViewType, "City").GetValue(viewComponent);
            var definition = AccessTools.Property(city.GetType(), "Definition").GetValue(city);
            var definitionMono = definition as MonoBehaviour;
            if (definitionMono.gameObject.name == "MenuCity")
                continue;

            var modelProp = AccessTools.Property(destinationViewType, "Model");
            if (modelProp == null)
                continue;
            var modelInstance = modelProp.GetValue(viewComponent);
            if (modelInstance == null)
                continue;

            var tileModelsField = AccessTools.Property(modelInstance.GetType(), "TileModels");
            if (tileModelsField == null)
                continue;
            var tileModels = tileModelsField.GetValue(modelInstance) as System.Collections.IList;
            if (tileModels == null || tileModels.Count == 0)
                continue;

            var tileModel = tileModels[0];
            var coordinatesField = AccessTools.Property(tileModel.GetType(), "Coordinates");
            if (coordinatesField == null)
                continue;
            var tileCoordinates = (Vector2Int)coordinatesField.GetValue(tileModel);

            var groupField = AccessTools.Field(destinationViewType, "groupIndex");
            if (groupField == null)
                continue;
            int groupIndex = (int)groupField.GetValue(viewComponent);

            var demandField = AccessTools.Property(destinationViewType, "PinCount");
            if (demandField == null)
                continue;
            int demand = (int)demandField.GetValue(viewComponent);

            var visibilityField = AccessTools.Field(destinationViewType, "_visibility");
            if (visibilityField == null)
                continue;
            int visibility = (int)visibilityField.GetValue(viewComponent);

            var snapshot = new DestinationSnapshot
            {
                x = tileCoordinates.x,
                y = tileCoordinates.y,
                color = groupIndex,
                demand = demand,
                type = visibility,
            };

            currentState.destinations.Add(snapshot);
        }
    }

    void SendGameState()
    {
        if (client == null || !client.Connected)
            return;

        try
        {
            string json = JsonConvert.SerializeObject(currentState, Formatting.None);
            byte[] data = Encoding.UTF8.GetBytes(json + "\n"); // adiciona quebra de linha como separador
            stream.Write(data, 0, data.Length);
            stream.Flush();
        }
        catch (Exception ex)
        {
            Logger.LogError($"[Socket] Erro ao enviar dados: {ex.Message}");
        }
    }

    public void SetGameSpeed(float speed)
    {
        var containerType = AccessTools.TypeByName("Motorways.Views.GameContainerScreen");
        if (containerType == null)
        {
            Logger.LogWarning("Tipo GameContainerScreen não encontrado!");
            return;
        }

        var containers = Resources.FindObjectsOfTypeAll(containerType);
        if (containers.Length == 0)
        {
            Logger.LogWarning("Nenhuma instância de GameContainerScreen encontrada!");
            return;
        }

        var containerInstance = containers[0];

        var getActiveGameMethod = AccessTools.Method(containerType, "GetActiveGame");
        if (getActiveGameMethod == null)
        {
            Logger.LogWarning("Método GetActiveGame não encontrado!");
            return;
        }

        var gameInstance = getActiveGameMethod.Invoke(containerInstance, null);
        if (gameInstance == null)
        {
            Logger.LogWarning("GetActiveGame() retornou null!");
            return;
        }

        var timeScaleType = AccessTools.TypeByName("TimeScale");
        if (timeScaleType == null)
        {
            Logger.LogWarning("Tipo TimeScale não encontrado!");
            return;
        }

        var timeScaleInstance = Activator.CreateInstance(timeScaleType, new object[] { speed });

        var gameType = gameInstance.GetType();
        var setTimeScaleMethod = AccessTools.Method(gameType, "SetTimeScale");
        if (setTimeScaleMethod == null)
        {
            Logger.LogWarning("Método SetTimeScale não encontrado no Game!");
            return;
        }

        setTimeScaleMethod.Invoke(gameInstance, new object[] { timeScaleInstance });
        shouldSpeedUp = false;
    }

    void TryLogClockTime()
    {
        var sim = GetSimulation();
        if (sim == null)
            return;

        var clockModelType = AccessTools.TypeByName("Motorways.Models.ClockModel");
        var getModelsRaw = sim.GetType()
            .GetMethods()
            .FirstOrDefault(m => m.Name == "GetModels" && m.IsGenericMethod);
        var getModelsMethod = getModelsRaw?.MakeGenericMethod(clockModelType);
        var modelList = getModelsMethod?.Invoke(sim, null);
        if (modelList == null)
            return;

        var modelsField = modelList
            .GetType()
            .GetField("_models", BindingFlags.Instance | BindingFlags.NonPublic);
        var models = modelsField?.GetValue(modelList) as IEnumerable;
        if (models == null)
            return;

        foreach (var model in models)
        {
            var timeProp = AccessTools.Property(model.GetType(), "Time");
            var timeVal = timeProp?.GetValue(model);
            if (timeVal == null)
                continue;

            double currentTime = Convert.ToDouble(timeVal.ToString());
            Logger.LogInfo($"[ClockModel] Visual clock time: {currentTime:F4}");

            if (lastClockTime.HasValue)
            {
                int prevDay = (int)(lastClockTime.Value / 20.0);
                int currentDay = (int)(currentTime / 20.0);
                if (currentDay > prevDay)
                {
                    Logger.LogInfo($"[ClockModel] 🕒 Novo dia detectado! Dia #{currentDay}");
                    SetGameSpeed(0f);
                }
            }

            lastClockTime = currentTime;
            break;
        }
    }

    object GetSimulation()
    {
        var containerType = AccessTools.TypeByName("Motorways.Views.GameContainerScreen");
        if (containerType == null)
        {
            Logger.LogWarning("[GetSimulation] Tipo GameContainerScreen não encontrado.");
            return null;
        }

        var containers = Resources.FindObjectsOfTypeAll(containerType);
        if (containers == null || containers.Length == 0)
        {
            Logger.LogWarning("[GetSimulation] Nenhum GameContainerScreen encontrado.");
            return null;
        }

        var container = containers
            .Cast<MonoBehaviour>()
            .FirstOrDefault(c => c.gameObject.activeInHierarchy);
        if (container == null)
        {
            Logger.LogWarning("[GetSimulation] Nenhum GameContainerScreen ativo.");
            return null;
        }

        var getGameMethod = AccessTools.Method(containerType, "GetActiveGame");
        if (getGameMethod == null)
        {
            Logger.LogWarning("[GetSimulation] Método GetActiveGame não encontrado.");
            return null;
        }

        var game = getGameMethod.Invoke(container, null);
        if (game == null)
        {
            Logger.LogWarning("[GetSimulation] Game retornou null.");
            return null;
        }

        var simProp = AccessTools.Property(game.GetType(), "Simulation");
        if (simProp == null)
        {
            Logger.LogWarning("[GetSimulation] Propriedade Simulation não encontrada.");
            return null;
        }

        var simulation = simProp.GetValue(game);
        if (simulation == null)
        {
            Logger.LogWarning("[GetSimulation] Simulation retornou null.");
            return null;
        }

        return simulation;
    }
}

public class HouseSnapshot
{
    public int x;
    public int y;
    public int color;
}

public class DestinationSnapshot
{
    public int x;
    public int y;
    public int color;
    public int demand;
    public int type; // 0 = NotShown, 1 = Square, 2 = Circle
}

public class ResourceSnapshot
{
    public int roads;
    public int bridges;
    public int highways;
    public int tunnels;
    public int trafficLights;
}

public class GameState
{
    public List<HouseSnapshot> houses = new List<HouseSnapshot>();
    public List<DestinationSnapshot> destinations = new List<DestinationSnapshot>();
    public ResourceSnapshot resources = new ResourceSnapshot();
    public int time_tick;
}
