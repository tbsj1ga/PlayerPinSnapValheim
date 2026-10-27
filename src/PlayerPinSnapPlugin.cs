using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace PlayerPinSnap
{
    // Other players' map pins jump to them after a teleport instead of crawling across the map,
    // and their positions reach the map more often.
    //
    // The map draws other players from ZNet's player list (ZNet.GetOtherPublicPlayers). Each
    // frame Minimap.UpdatePlayerPins moves a pin towards the listed position at 200 m/s
    // (Vector3.MoveTowards(pin, pos, 200 * dt)): smooth while walking, but after a portal across
    // the world the pin takes half a minute to get there. The mod swaps that MoveTowards call for
    // PinStep, which puts the pin straight on the position when it is further than SnapDistance,
    // and glides it otherwise (at GlideSpeed).
    //
    // The list itself is refreshed every 2 seconds, both ways (ZNet.SendPeriodicData): every
    // client sends its position to the server (SendServerSyncPlayerData), and the server sends
    // the list of everyone's positions to every client (SendPlayerList) - so a position on the
    // map is up to 4 seconds old. With SendInterval below 2 the mod makes the same calls more
    // often, on top of the vanilla ones: on the server the list, on a client its own position.
    // Each side works on its own: the mod on the server makes the list fresher for everyone, the
    // mod on a client makes that player's own position reach the server sooner.
    [BepInPlugin(Guid, Name, Version)]
    public class PlayerPinSnapPlugin : BaseUnityPlugin
    {
        public const string Guid = "j1ga.playerpinsnap";
        public const string Name = "PlayerPinSnap";
        public const string Version = "0.1.0";

        private const float VanillaGlideSpeed = 200f;   // m/s, in Minimap.UpdatePlayerPins
        private const float VanillaSendInterval = 2f;   // s, in ZNet.SendPeriodicData

        private static ConfigEntry<bool> _enabled;
        private static ConfigEntry<float> _snapDistance;
        private static ConfigEntry<float> _glideSpeed;
        private static ConfigEntry<float> _sendInterval;

        private static BepInEx.Logging.ManualLogSource _log;
        private static MethodInfo _sendPlayerList;        // ZNet.SendPlayerList()
        private static MethodInfo _sendServerSyncData;    // ZNet.SendServerSyncPlayerData(ZNetPeer)
        private float _sendTimer;
        private bool _sendFailed;

        private Harmony _harmony;

        private void Awake()
        {
            _log = Logger;
            _enabled = Config.Bind("01 General", "Enabled", true, "Off: pins glide and positions are sent as in the game.");
            _snapDistance = Config.Bind("02 Map", "SnapDistance", 100f,
                new ConfigDescription("A player's pin further than this from their new position (meters) jumps there at once " +
                    "(a teleport, a portal). Closer, it glides. 0: pins always jump.", new AcceptableValueRange<float>(0f, 10000f)));
            _glideSpeed = Config.Bind("02 Map", "GlideSpeed", VanillaGlideSpeed,
                new ConfigDescription("Speed of a gliding pin, meters per second. In the game: 200.", new AcceptableValueRange<float>(10f, 10000f)));
            _sendInterval = Config.Bind("03 Network", "SendInterval", 0.5f,
                new ConfigDescription("How often player positions are sent, seconds: on the server the list of everyone's positions, " +
                    "on a client its own position. In the game: every 2 seconds; 2 here leaves it as in the game.",
                    new AcceptableValueRange<float>(0.1f, 2f)));

            _sendPlayerList = AccessTools.Method(typeof(ZNet), "SendPlayerList");
            _sendServerSyncData = AccessTools.Method(typeof(ZNet), "SendServerSyncPlayerData", new Type[] { typeof(ZNetPeer) });
            if (_sendPlayerList == null || _sendServerSyncData == null)
                Logger.LogWarning("ZNet.SendPlayerList or ZNet.SendServerSyncPlayerData not found: positions are sent as in the game.");

            _harmony = new Harmony(Guid);
            foreach (Type t in new Type[] { typeof(Minimap_UpdatePlayerPins_Patch) })
            {
                try { _harmony.CreateClassProcessor(t).Patch(); }
                catch (Exception e) { Logger.LogError("Patch " + t.Name + " failed: " + e); }
            }
            Logger.LogInfo(Name + " " + Version + " loaded.");
        }

        private void OnDestroy()
        {
            if (_harmony != null) _harmony.UnpatchSelf();
        }

        // The extra sends, on top of the vanilla ones every 2 seconds.
        private void Update()
        {
            ZNet net = ZNet.instance;
            float interval = _sendInterval.Value;
            if (net == null || !_enabled.Value || _sendFailed || interval >= VanillaSendInterval)
            {
                _sendTimer = 0f;
                return;
            }
            _sendTimer += Time.unscaledDeltaTime;
            if (_sendTimer < interval) return;
            _sendTimer = 0f;

            try
            {
                if (net.IsServer())
                {
                    if (_sendPlayerList != null && net.GetPeers().Count > 0) _sendPlayerList.Invoke(net, null);
                }
                else
                {
                    ZNetPeer server = net.GetServerPeer();
                    if (server != null && server.IsReady() && _sendServerSyncData != null)
                        _sendServerSyncData.Invoke(net, new object[] { server });
                }
            }
            catch (Exception e)
            {
                _sendFailed = true;
                Logger.LogError("Sending positions failed, positions are sent as in the game from now on: " + e);
            }
        }

        // Replaces Vector3.MoveTowards(pin, position, 200 * dt) in Minimap.UpdatePlayerPins.
        public static Vector3 PinStep(Vector3 current, Vector3 target, float maxDelta)
        {
            if (_enabled.Value)
            {
                float snap = _snapDistance.Value;
                if ((target - current).sqrMagnitude > snap * snap) return target;
                maxDelta *= _glideSpeed.Value / VanillaGlideSpeed;
            }
            return Vector3.MoveTowards(current, target, maxDelta);
        }

        [HarmonyPatch(typeof(Minimap), "UpdatePlayerPins")]
        private static class Minimap_UpdatePlayerPins_Patch
        {
            private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo moveTowards = AccessTools.Method(typeof(Vector3), "MoveTowards");
                MethodInfo step = AccessTools.Method(typeof(PlayerPinSnapPlugin), "PinStep");
                int replaced = 0;
                foreach (CodeInstruction ci in instructions)
                {
                    if (ci.Calls(moveTowards))
                    {
                        ci.opcode = OpCodes.Call;
                        ci.operand = step;
                        replaced++;
                    }
                    yield return ci;
                }
                if (replaced != 1)
                    _log.LogWarning("Minimap.UpdatePlayerPins: expected one Vector3.MoveTowards, replaced " + replaced + ".");
            }
        }
    }
}
