using CinliMahzen.Core;
using CinliMahzen.Core.Net;
using UnityEngine;

namespace CinliMahzen.Player
{
    /// <summary>
    /// The human's quest-item slot selection and drop (Oyun §11, Teknik §5.4, TODO 2.7). Sibling of <see cref="HumanController"/> on P_Human;
    /// reads input only while the pawn is played. Keys 1-3 (<see cref="IHumanInput.SelectSlot"/>) choose <see cref="SelectedSlot"/>; G
    /// (<see cref="IHumanInput.DropPressed"/>) asks the authority to drop it (<c>ReqDropItem{slot}</c>). The inventory itself is authoritative
    /// (Objectives); this component only sends the request. The HUD (TODO 3.11) reads <see cref="SelectedSlot"/>.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(HumanController))]
    public sealed class HumanItemDropper : MonoBehaviour
    {
        private HumanController _human;
        private int _selected;

        /// <summary>The highlighted inventory slot (0-based).</summary>
        public int SelectedSlot => _selected;

        private void Awake()
        {
            _human = GetComponent<HumanController>();
        }

        private void Update()
        {
            if (!_human.IsLocallyControlled || !(_human.Input is IHumanInput input))
                return;

            int select = input.SelectSlot;
            if (select >= 0)
                _selected = select;

            if (input.DropPressed)
            {
                INetBridge net = GameServices.Net;
                net?.SendToAuthority(ReqDropItemMsg.Encode(_selected));
            }
        }
    }
}
