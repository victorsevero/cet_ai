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
using CetPlugin.Models;
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

    private TcpClient client;
    private NetworkStream stream;
    public bool waitingGameStart = true;

    private void Awake()
    {
        Instance = this;
        Logger = base.Logger;
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");

        ConnectSocket();
        new Harmony("cet.clockpatch").PatchAll();
    }

    private void Update()
    {
        if (waitingGameStart)
        {
            SetGameSpeed(10f);
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
        waitingGameStart = false;
    }

    public void PauseGame() => SetGameSpeed(0f);

    public void CaptureAndSendGameState()
    {
        var state = new GameState
        {
            houses = GameStateManager.CaptureHouses(),
            destinations = GameStateManager.CaptureDestinations(),
            // TODO: add other fields
        };

        var json = JsonConvert.SerializeObject(state);
        byte[] data = Encoding.UTF8.GetBytes(json + "\n");

        if (stream == null || !client.Connected)
        {
            Logger.LogWarning("[Socket] Sending attempt without an active connection.");
            return;
        }

        stream.Write(data, 0, data.Length);
    }

    private void ConnectSocket()
    {
        try
        {
            client = new TcpClient("localhost", 5000);
            stream = client.GetStream();
            Logger.LogInfo("[Socket] Connected to Python.");
        }
        catch (Exception e)
        {
            Logger.LogError($"[Socket] Error: {e.Message}");
        }
    }
}
