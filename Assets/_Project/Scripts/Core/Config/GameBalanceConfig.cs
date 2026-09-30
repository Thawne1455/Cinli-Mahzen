using UnityEngine;

namespace CinliMahzen.Core
{
    /// <summary>
    /// All tunable balance values (Teknik §4.9, Oyun §12). Fields are added in A0.7;
    /// the type exists from A0.5 so <see cref="GameServices.Config"/> compiles.
    /// </summary>
    [CreateAssetMenu(menuName = "CinliMahzen/Config/Game Balance Config", fileName = "GameBalanceConfig")]
    public sealed class GameBalanceConfig : ScriptableObject
    {
    }
}
