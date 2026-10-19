using System.Collections.Generic;

namespace CinliMahzen.Core
{
    /// <summary>
    /// Placeholder until Jinn implements <see cref="IJinnEnergyService"/>. By default every player has a full tank
    /// (GameBalanceConfig.EnergyMax, or unlimited when no config is registered) and every spend succeeds without changing anything.
    /// Once <see cref="SetEnergy"/> is called for a player, that player has a real balance: spends deduct from it and fail when it is too low.
    /// </summary>
    public sealed class StubJinnEnergyService : IJinnEnergyService
    {
        private readonly Dictionary<byte, float> _balances = new Dictionary<byte, float>();

        /// <summary>Sum of every successful positive spend (all players), for tests and debug dumps.</summary>
        public float TotalSpent { get; private set; }

        public float Get(PlayerId player)
        {
            if (_balances.TryGetValue(player.Value, out float balance))
                return balance;
            GameBalanceConfig config = GameServices.Config;
            return config != null ? config.EnergyMax : float.PositiveInfinity;
        }

        public bool TrySpendAuthority(PlayerId player, float amount)
        {
            if (amount <= 0f)
                return true;

            if (_balances.TryGetValue(player.Value, out float balance))
            {
                if (balance < amount)
                    return false;
                _balances[player.Value] = balance - amount;
            }
            TotalSpent += amount;
            return true;
        }

        /// <summary>Gives <paramref name="player"/> a real balance of <paramref name="energy"/> (clamped at 0).</summary>
        public void SetEnergy(PlayerId player, float energy)
        {
            _balances[player.Value] = energy > 0f ? energy : 0f;
        }

        /// <summary>Back to the default (full tank, spends always succeed) for <paramref name="player"/>.</summary>
        public void ClearEnergy(PlayerId player)
        {
            _balances.Remove(player.Value);
        }
    }
}
