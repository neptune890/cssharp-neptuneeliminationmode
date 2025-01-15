// Counter-Strike 2 plugin for elimination mode using CounterStrikeSharp

using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using Microsoft.Extensions.Logging;

namespace NeptuneEliminationMode
{
    [MinimumApiVersion(80)]
    public class NeptuneEliminationMode : BasePlugin
    {
        // Dictionary to track who has killed whom
        private readonly Dictionary<int, List<int>> playerKillMap = new();

        public override string ModuleName => "Neptune Elimination Mode";
        public override string ModuleVersion => "1.0.0";
        public override string ModuleAuthor => "Neptune890";
        public override string ModuleDescription => "A plugin that implements elimination mode for Counter-Strike 2.";

        public override void Load(bool hotReload)
        {
            Logger.LogInformation("Neptune Elimination Mode plugin is loading.");
        }

        public override void Unload(bool hotReload)
        {
            Logger.LogInformation("Neptune Elimination Mode plugin is unloading.");
        }

        [GameEventHandler]
        public HookResult OnPlayerDeath(EventPlayerDeath @event, GameEventInfo info)
        {
            var killer = @event.Attacker; // Adjust to correct property
            var victim = @event.Userid;   // Use correct property for the victim

            if (victim == null || victim.UserId == null)
            {
                Logger.LogWarning("Victim is null or UserId is not set in OnPlayerDeath event.");
                return HookResult.Continue;
            }

            // Check if the victim's team is eliminated
            bool isTeamEliminated = true;
            var allPlayers = Utilities.GetPlayers(); // Retrieve all players
            foreach (var player in allPlayers)
            {
                if (player.Team == victim.Team && player.PawnIsAlive)
                {
                    isTeamEliminated = false;
                    break;
                }
            }

            // If the team is eliminated, end the round normally
            if (isTeamEliminated)
            {
                return HookResult.Continue;
            }

            // If there is a killer, track the kill
            if (killer != null && killer.UserId.HasValue && killer.UserId != victim.UserId)
            {
                if (!playerKillMap.ContainsKey(killer.UserId.Value))
                {
                    playerKillMap[killer.UserId.Value] = new List<int>();
                }
                playerKillMap[killer.UserId.Value].Add(victim.UserId.Value);
            }

            // Check if the victim has killed anyone and respawn them
            if (playerKillMap.ContainsKey(victim.UserId.Value))
            {
                foreach (var respawnPlayerId in playerKillMap[victim.UserId.Value])
                {
                    var respawnPlayer = Utilities.GetPlayerFromUserid(respawnPlayerId); // Retrieve player by ID
                    respawnPlayer?.Respawn();
                }
                playerKillMap.Remove(victim.UserId.Value);
            }

            return HookResult.Continue;
        }

        [GameEventHandler]
        public HookResult OnRoundStart(EventRoundStart @event, GameEventInfo info)
        {
            // Clear the kill map at the start of a new round
            playerKillMap.Clear();
            Logger.LogInformation("Round has started. Kill map cleared.");
            return HookResult.Continue;
        }
    }
}
