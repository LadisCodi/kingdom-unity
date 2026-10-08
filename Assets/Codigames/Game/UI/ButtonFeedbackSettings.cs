using DG.Tweening;
using UnityEngine;

namespace Codigames.Game.UI
{
    [CreateAssetMenu(
        fileName = "ButtonFeedbackSettings",
        menuName = "Kingdom/UI/Button Feedback Settings")]
    public class ButtonFeedbackSettings : ScriptableObject
    {
        [Header("Audio")]
        [SerializeField] private string _pressSound;
        [SerializeField] private float _soundVolume = 1f;

        [Header("Scale")]
        [SerializeField] private float _pressedScale = 0.9f;

        [Header("Timing")]
        [SerializeField] private float _pressDuration = 0.08f;
        [SerializeField] private float _releaseDuration = 0.12f;

        [Header("Easing")]
        [SerializeField] private Ease _pressEase = Ease.OutQuad;
        [SerializeField] private Ease _releaseEase = Ease.OutBack;

        public string PressSound => _pressSound;
        public float SoundVolume => _soundVolume;
        public float PressedScale => _pressedScale;
        public float PressDuration => _pressDuration;
        public float ReleaseDuration => _releaseDuration;
        public Ease PressEase => _pressEase;
        public Ease ReleaseEase => _releaseEase;
    }
}