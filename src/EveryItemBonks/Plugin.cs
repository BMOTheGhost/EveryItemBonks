using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

namespace EveryItemBonks
{
    [BepInPlugin("com.MusicallyUntalented.EveryItemBonks", "Every Items Bonk", "1.0.0")]
    public class Plugin : BaseUnityPlugin, IConnectionCallbacks, IMatchmakingCallbacks
    {
        public static ConfigEntry<float> BonkingPower { get; set; }
        internal static ManualLogSource Log;

        private void Awake()
        {
            BonkingPower = Config.Bind("General",
                "BonkingPower",
                400f,
                "The force of the bonking (Default=400f, Host-Dependant) note: doesn't have any effect unfortunately, mainly for testing");

            Log = Logger;

            PhotonNetwork.AddCallbackTarget(this); //register for Photon callbacks

            new Harmony("com.MusicallyUntalented.EveryItemsBonk").PatchAll(typeof(Patcher));
            Log.LogInfo("EveryItemBonks plugin loaded.");
        }

        public void OnJoinedRoom()
        {
            if (PhotonNetwork.IsMasterClient)
            {
                var props = new Hashtable { { "BonkingPower", BonkingPower.Value } };
                PhotonNetwork.CurrentRoom.SetCustomProperties(props);
                Log.LogInfo($"Host set BonkingPower room property: {BonkingPower.Value}");
            }
        }

        // Required interface methods (can be left empty)
        public void OnConnected() {}
        public void OnConnectedToMaster() {}
        public void OnDisconnected(DisconnectCause cause)
        {
            throw new System.NotImplementedException();
        }

        public void OnRegionListReceived(RegionHandler regionHandler) {}
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
        {
            throw new System.NotImplementedException();
        }
        
        public void OnCustomAuthenticationFailed(string debugMessage) {}
        public void OnFriendListUpdate(List<FriendInfo> friendList) {}
        void IMatchmakingCallbacks.OnFriendListUpdate(List<FriendInfo> friendList)
        {
            OnFriendListUpdate(friendList);
        }

        public void OnCreatedRoom() {}
        public void OnCreateRoomFailed(short returnCode, string message) {}
        public void OnJoinRoomFailed(short returnCode, string message) {}
        public void OnJoinRandomFailed(short returnCode, string message) {}
        public void OnLeftRoom() {}

        private class Patcher
        {
            [HarmonyPatch(typeof(Item), "Start")]
            [HarmonyPostfix]
            public static void ItemStartPostfix(Item __instance)
            {
                if (__instance != null)
                {
                    Bonkable bonkable = __instance.gameObject.GetComponent<Bonkable>();
                    if (bonkable == null)
                    {
                        bonkable = __instance.gameObject.AddComponent<Bonkable>();

                        // Get synced bonking power from room property
                        float power = 400f; // fallback if null
                        if (PhotonNetwork.InRoom &&
                            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("BonkingPower", out object value) &&
                            value is float syncedValue)
                        {
                            power = syncedValue;
                        }

                        bonkable.bonkCooldown = 1f;
                        bonkable.bonkForce = power;
                        bonkable.bonkRange = 1f;
                        bonkable.minBonkVelocity = 10f;
                        bonkable.ragdollTime = 1f;

                        Plugin.Log?.LogInfo($"Bonkable added to {__instance.name} with force: {power}");
                    }
                }
            }
        }
    }
}