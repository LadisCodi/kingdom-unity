using System;
using UnityEngine;

namespace Codigames.Game.Data
{
    // So much of something, by id: a price line, a reward line.
    [Serializable]
    public class Amount
    {
        [SerializeField] private string _id;
        [SerializeField] private double _value;

        public Amount()
        {
        }

        public Amount(string id, double value)
        {
            _id = id;
            _value = value;
        }

        public string Id => _id;
        public double Value => _value;
    }
}
