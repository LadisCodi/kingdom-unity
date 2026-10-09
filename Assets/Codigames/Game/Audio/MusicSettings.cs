using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Codigames.Game.Audio
{
    // The music and ambience as the web plays them: the town's playlist, and the ambience beds by ground.
    [CreateAssetMenu(fileName = "MusicSettings", menuName = "Kingdom/Audio/Music Settings")]
    public class MusicSettings : ScriptableObject
    {
        [Title("Town")]
        [SerializeField, ListDrawerSettings(ShowFoldout = false)] private List<Song> _town = new();
        [SerializeField, Range(0f, 1f)] private float _townVolume = 0.35f;
        [SerializeField, SuffixLabel("s")] private float _townFadeIn = 1.5f;
        [SerializeField, SuffixLabel("s"), Tooltip("How long one song takes to hand over to the next.")] private float _crossfade = 5f;

        [Title("Moments"), Tooltip("Each takes the floor from the town while its screen is up, from its top.")]
        [SerializeField] private string _muster = "musicMuster";
        [SerializeField, Range(0f, 1f)] private float _musterVolume = 0.34f;
        [SerializeField, SuffixLabel("s")] private float _musterFadeIn = 0.9f;
        [SerializeField] private string _battle = "musicBattle";
        [SerializeField, Range(0f, 1f)] private float _battleVolume = 0.36f;
        [SerializeField, SuffixLabel("s")] private float _battleFadeIn = 0.3f;
        [SerializeField] private string _feast = "musicFeast";
        [SerializeField, Range(0f, 1f)] private float _feastVolume = 0.38f;
        [SerializeField, SuffixLabel("s")] private float _feastFadeIn = 0.6f;
        [SerializeField, SuffixLabel("s"), Tooltip("How long what loses the floor takes to fade out.")] private float _momentFadeOut = 0.6f;

        [Title("Ambience")]
        [SerializeField] private string _meadow = "ambienceMeadow";
        [SerializeField] private string _coast = "ambienceCoast";
        [SerializeField] private string _snow = "ambienceSnow";
        [SerializeField, Range(0f, 1f)] private float _ambienceVolume = 0.22f;
        [SerializeField, SuffixLabel("s")] private float _ambienceFade = 0.5f;
        [SerializeField, Tooltip("Water this many cells from the camera's centre makes it the coast.")] private int _coastReach = 3;

        public IReadOnlyList<Song> Town => _town;
        public float MomentFadeOut => _momentFadeOut;

        public (string Id, float Volume, float FadeIn) Moment(MusicMoment moment) => moment switch
        {
            MusicMoment.Muster => (_muster, _musterVolume, _musterFadeIn),
            MusicMoment.Battle => (_battle, _battleVolume, _battleFadeIn),
            _ => (_feast, _feastVolume, _feastFadeIn),
        };
        public float TownVolume => _townVolume;
        public float TownFadeIn => _townFadeIn;
        public float Crossfade => _crossfade;
        public string Meadow => _meadow;
        public string Coast => _coast;
        public string Snow => _snow;
        public float AmbienceVolume => _ambienceVolume;
        public float AmbienceFade => _ambienceFade;
        public int CoastReach => _coastReach;

        [Serializable]
        public class Song
        {
            [SerializeField] private string _soundId;
            [SerializeField, SuffixLabel("s"), Tooltip("How long its turn lasts: rounds of a short loop, or the whole song.")] private float _turnSeconds;

            public string SoundId => _soundId;
            public float TurnSeconds => _turnSeconds;
        }
    }
}
