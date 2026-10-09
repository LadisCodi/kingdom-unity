using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Fog
{
    // How a treasure looks: a closed chest, and the coin that rises out of it, by coin.
    [CreateAssetMenu(fileName = "TreasureArt", menuName = "Kingdom/Art/Treasure Art")]
    public class TreasureArt : ScriptableObject
    {
        [SerializeField, PreviewField(48), Required] private Sprite _closed;
        [SerializeField] private List<CoinArt> _coins = new();

        public Sprite Closed => _closed;

        public Sprite CoinOf(string coin) => _coins.Find(c => c.Coin == coin)?.Sprite;

        [Serializable]
        private class CoinArt
        {
            public string Coin;
            [PreviewField(48)] public Sprite Sprite;
        }
    }
}
