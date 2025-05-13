using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CetPlugin.Models;
using HarmonyLib;
using UnityEngine;

namespace CetPlugin;

public class GameStateManager
{
    public static List<HouseSnapshot> CaptureHouses()
    {
        var result = new List<HouseSnapshot>();
        var houseViewType = AccessTools.TypeByName("Motorways.Views.HouseView");
        if (houseViewType == null)
            return result;

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

            var city = AccessTools.Property(houseViewType, "City")?.GetValue(viewComponent);
            var definition = AccessTools.Property(city.GetType(), "Definition")?.GetValue(city);
            var definitionMono = definition as MonoBehaviour;
            if (definitionMono?.gameObject?.name == "MenuCity")
                continue;

            var tile = (Vector2Int)tileField.GetValue(viewComponent);
            var color = (int)AccessTools.Field(houseViewType, "groupIndex").GetValue(viewComponent);

            result.Add(
                new HouseSnapshot
                {
                    x = tile.x,
                    y = tile.y,
                    color = color,
                }
            );
        }

        return result;
    }

    public static List<DestinationSnapshot> CaptureDestinations()
    {
        var result = new List<DestinationSnapshot>();
        var destinationViewType = AccessTools.TypeByName("Motorways.Views.DestinationView");
        if (destinationViewType == null)
            return result;

        var allViews = Resources.FindObjectsOfTypeAll(destinationViewType);

        foreach (var viewObj in allViews)
        {
            var viewComponent = viewObj as MonoBehaviour;
            if (viewComponent == null)
                continue;

            var go = viewComponent.gameObject;
            if (go == null || !go.activeInHierarchy)
                continue;

            var city = AccessTools.Property(destinationViewType, "City")?.GetValue(viewComponent);
            var definition = AccessTools.Property(city.GetType(), "Definition")?.GetValue(city);
            var defMono = definition as MonoBehaviour;
            if (defMono?.gameObject?.name == "MenuCity")
                continue;

            var modelProp = AccessTools.Property(destinationViewType, "Model");
            var modelInstance = modelProp?.GetValue(viewComponent);
            if (modelInstance == null)
                continue;

            var tileModelsProp = AccessTools.Property(modelInstance.GetType(), "TileModels");
            var tileModels = tileModelsProp?.GetValue(modelInstance) as IList;
            if (tileModels == null || tileModels.Count == 0)
                continue;

            var tileModel = tileModels[0];
            var coordinatesProp = AccessTools.Property(tileModel.GetType(), "Coordinates");
            var tileCoordinates = (Vector2Int)coordinatesProp.GetValue(tileModel);

            var groupIndex = (int)
                AccessTools.Field(destinationViewType, "groupIndex")?.GetValue(viewComponent);
            var visibility = (int)
                AccessTools.Field(destinationViewType, "_visibility")?.GetValue(viewComponent);

            result.Add(
                new DestinationSnapshot
                {
                    x = tileCoordinates.x,
                    y = tileCoordinates.y,
                    color = groupIndex,
                    demand = 0,
                    type = visibility,
                }
            );
        }

        return result;
    }
}
