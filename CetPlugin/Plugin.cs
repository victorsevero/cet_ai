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
    private bool wasGameNullLastFrame = true;
    private bool waitingResume = false;
    private const float SPEED = 10f;

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
        var containerType = AccessTools.TypeByName("Motorways.Views.GameContainerScreen");
        var containers = Resources.FindObjectsOfTypeAll(containerType);
        if (containers.Length == 0)
            return;

        var getGame = AccessTools.Method(containerType, "GetActiveGame");
        var game = getGame?.Invoke(containers[0], null);

        if (wasGameNullLastFrame && game != null)
        {
            Logger.LogInfo("[Game] Active Game detected.");
            CaptureAndSendGameState();
            SetGameSpeed(SPEED);
            waitingResume = true;
        }

        wasGameNullLastFrame = (game == null);

        if (waitingResume && game != null)
        {
            var getTimeScale = AccessTools.Method(game.GetType(), "GetTimeScale");
            if (getTimeScale != null)
            {
                var timeScale = getTimeScale.Invoke(game, null);
                var scaleProp = timeScale.GetType().GetProperty("Scale");
                var scale = (float)scaleProp.GetValue(timeScale);
                if (scale == 0f)
                {
                    Logger.LogInfo("[Game] Game is paused.");
                    CaptureAndSendGameState();
                    SetGameSpeed(SPEED);
                }
            }
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
        stream.Flush();

        byte[] buffer = new byte[16];
        int bytesRead = stream.Read(buffer, 0, buffer.Length);
        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();

        if (response.ToLower() != "ack")
        {
            Logger.LogWarning($"[Socket] Unexpected response: {response}");
        }
        else
        {
            Logger.LogInfo("[Socket] ACK received.");
        }
    }

    private void ConnectSocket()
    {
        try
        {
            client = new TcpClient("localhost", 5000);
            stream = client.GetStream();
            stream.ReadTimeout = 2000;
            Logger.LogInfo("[Socket] Connected to Python.");
        }
        catch (Exception e)
        {
            Logger.LogError($"[Socket] Error: {e.Message}");
        }
    }
}
