using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Newtonsoft.Json;
using UnityEngine;

namespace CetPlugin;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInProcess("Mini Motorways.exe")]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;

    private Type vehicleViewType;
    private int frameCounter = 0;
    private GameState currentState = new GameState();
    private TcpClient client;
    private NetworkStream stream;

    private void Awake()
    {
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        vehicleViewType = AccessTools.TypeByName("Motorways.Views.VehicleView");

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
        if (vehicleViewType == null)
            return;

        frameCounter++;
        if (frameCounter % 60 != 0)
            return;

        CaptureGameState();
    }

    void CaptureGameState()
    {
        currentState.vehicles.Clear();

        var vehicleViewType = AccessTools.TypeByName("Motorways.Views.VehicleView");
        if (vehicleViewType == null)
        {
            Logger.LogWarning("Tipo VehicleView não encontrado!");
            return;
        }

        var allVehicleViews = Resources.FindObjectsOfTypeAll(vehicleViewType);

        foreach (var viewObj in allVehicleViews)
        {
            if (viewObj == null)
                continue;

            var viewComponent = viewObj as MonoBehaviour;
            if (viewComponent == null)
                continue;

            var go = viewComponent.gameObject;
            if (go == null || !go.activeInHierarchy)
                continue;

            var cityProp = AccessTools.Property(vehicleViewType, "City");
            if (cityProp == null)
                continue;

            var cityInstance = cityProp.GetValue(viewComponent);
            if (cityInstance == null)
                continue;

            var definitionProp = AccessTools.Property(cityInstance.GetType(), "Definition");
            if (definitionProp == null)
                continue;

            var definitionInstance = definitionProp.GetValue(cityInstance);
            if (definitionInstance == null)
                continue;

            var definitionMono = definitionInstance as MonoBehaviour;
            if (definitionMono == null)
                continue;

            var cityDefinitionName = definitionMono.gameObject?.name ?? "null";

            if (cityDefinitionName == "MenuCity")
            {
                continue;
            }

            var pos = go.transform.position;
            var snapshot = new VehicleSnapshot { x = pos.x, y = pos.y };

            currentState.vehicles.Add(snapshot);
        }

        Logger.LogInfo($"[GameState] Capturados {currentState.vehicles.Count} veículos!");
        // SaveGameStateToJson();
        SendGameState();
    }

    void SaveGameStateToJson()
    {
        try
        {
            string json = JsonConvert.SerializeObject(currentState, Formatting.Indented);

            string folderPath = Path.Combine(Paths.BepInExRootPath, "GameStates");
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            string filePath = Path.Combine(folderPath, "latest_gamestate.json");

            File.WriteAllText(filePath, json);

            Logger.LogInfo($"[GameState] Snapshot salvo em {filePath}");
        }
        catch (Exception ex)
        {
            Logger.LogError($"Erro ao salvar GameState em JSON: {ex.Message}");
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
}

public class VehicleSnapshot
{
    public float x;
    public float y;
}

public class GameState
{
    public List<VehicleSnapshot> vehicles = new List<VehicleSnapshot>();
}
