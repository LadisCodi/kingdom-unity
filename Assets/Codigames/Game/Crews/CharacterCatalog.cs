using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Crews
{
    // Every animated person on the map: per character, the frames of each pose and how fast each pose runs.
    [CreateAssetMenu(fileName = "CharacterCatalog", menuName = "Kingdom/Characters/Character Catalog")]
    public class CharacterCatalog : ScriptableObject
    {
        [SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<Character> _characters = new();

        [Header("Frame time (ms)")]
        [SerializeField] private float _idleMs = 550;
        [SerializeField] private float _walkMs = 220;
        [SerializeField] private float _workMs = 260;

        // The frame a character shows in a pose at a moment; a pose it lacks falls back to idle.
        public Sprite Frame(string character, Pose pose, float seconds)
        {
            var found = Find(character);
            if (found == null) return null;

            var (frames, ms) = pose switch
            {
                Pose.Walk when found.Walk.Length > 0 => (found.Walk, _walkMs),
                Pose.Work when found.Work.Length > 0 => (found.Work, _workMs),
                _ => (found.Idle, _idleMs),
            };

            return frames.Length == 0 ? null : frames[(int)(seconds * 1000 / ms) % frames.Length];
        }

        private Character Find(string id)
        {
            foreach (var character in _characters)
            {
                if (character.Id == id) return character;
            }

            return null;
        }

        [Serializable]
        private class Character
        {
            [SerializeField] private string _id;
            [SerializeField, PreviewField] private Sprite[] _idle = Array.Empty<Sprite>();
            [SerializeField, PreviewField] private Sprite[] _walk = Array.Empty<Sprite>();
            [SerializeField, PreviewField, Tooltip("Its work: a swing, a dig, a cast of the net.")] private Sprite[] _work = Array.Empty<Sprite>();

            public string Id => _id;
            public Sprite[] Idle => _idle;
            public Sprite[] Walk => _walk;
            public Sprite[] Work => _work;
        }
    }

    public enum Pose
    {
        Idle,
        Walk,
        Work,
    }
}
