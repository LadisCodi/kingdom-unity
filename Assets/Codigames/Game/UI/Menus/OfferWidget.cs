using System;
using Codigames.Game.UI.Stage;
using Codigames.Game.UI.Store;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Menus
{
    // THE OFFERS ON THE MAP (the web's offerWidget): one icon at a time on a soft golden glow, turning to the next each
    // time its sign has scrolled its words by, and a small wooden sign whose words scroll: the offer's name and time
    // left, the time until tomorrow's part, or Claim!. A tap opens the one on show. Hidden behind any sheet.
    public class OfferWidget : Menu
    {
        // One lap of the sign: its words carried by once (offer.css ofw-scroll).
        private const float LAP_SECONDS = 7f;
        private const float BREATHE_SECONDS = 3.6f;
        private const float BOB_SECONDS = 2.8f;
        private const float BOB = 3f * 2.8f;
        private const float TURN_SECONDS = 0.6f;
        // The space after the words before they come round again (offer.css .ofw-text padding).
        private const float GAP = 24f * 2.8f;
        private const float TWINKLE_SECONDS = 3.6f;
        // Each sparkle on its own delay, so they never light together.
        private static readonly float[] DELAYS = { 0f, 1.3f, 2.4f };

        [SerializeField] private Button _button;
        [SerializeField] private CanvasGroup _body;
        [SerializeField] private Image _glow;
        [SerializeField] private RectTransform _track;
        [SerializeField] private TMP_Text _text;
        [SerializeField] private TMP_Text _echo;
        [SerializeField] private RectTransform _slot;
        [SerializeField] private CanvasGroup _fade;
        [SerializeField] private CanvasGroup _signFade;
        [SerializeField] private Image[] _sparkles;
        [SerializeField] private OfferIconView _icon;

        private float _offset;
        private string _words;
        private float _turnedAt = float.NegativeInfinity;

        public event Action Tapped;
        // The sign has scrolled its words by once: time for the next offer.
        public event Action Lapped;

        protected override void InitializeInternal()
        {
            CoachTarget.Tag(_button, "offer-widget");
            _button.onClick.AddListener(() => Tapped?.Invoke());
        }

        public void SetShown(bool shown)
        {
            _body.alpha = shown ? 1f : 0f;
            _body.blocksRaycasts = shown;
        }

        // The next offer on show; `turned` fades it in, as one replacing another does.
        public void ShowOffer(Sprite icon, Sprite bust, string kind, bool turned)
        {
            _icon.Show(icon, bust, kind);
            if (turned) _turnedAt = Time.unscaledTime;
        }

        // The sign's words, written in place: its scroll does not restart for them.
        public void SetWords(string words)
        {
            if (words == _words) return;
            _words = words;
            _text.text = words;
            _echo.text = words;
        }

        private void Update()
        {
            var now = Time.unscaledTime;
            Breathe(now);
            if (string.IsNullOrEmpty(_words)) return;
            var width = _text.preferredWidth;
            if (width <= 0) return;
            width += GAP;
            _offset += width / LAP_SECONDS * Time.unscaledDeltaTime;
            if (_offset >= width)
            {
                _offset -= width;
                Lapped?.Invoke();
            }

            _track.anchoredPosition = new Vector2(-_offset, _track.anchoredPosition.y);
            _echo.rectTransform.anchoredPosition = new Vector2(width, _echo.rectTransform.anchoredPosition.y);
        }

        // The glow breathes, the icon bobs, a sparkle lights now and then, and a turn fades in.
        private void Breathe(float now)
        {
            var breath = 0.5f - 0.5f * Mathf.Cos(now / BREATHE_SECONDS * Mathf.PI * 2f);
            _glow.transform.localScale = Vector3.one * Mathf.Lerp(0.94f, 1.04f, breath);
            var glow = _glow.color;
            _glow.color = new Color(glow.r, glow.g, glow.b, Mathf.Lerp(0.6f, 0.9f, breath));
            var bob = 0.5f - 0.5f * Mathf.Cos(now / BOB_SECONDS * Mathf.PI * 2f);
            _slot.anchoredPosition = new Vector2(0f, bob * BOB);
            for (var i = 0; i < _sparkles.Length; i++) Twinkle(_sparkles[i], (now - DELAYS[i % DELAYS.Length]) / TWINKLE_SECONDS);
            var fade = Mathf.Clamp01((now - _turnedAt) / TURN_SECONDS);
            fade = 1f - (1f - fade) * (1f - fade);
            _fade.alpha = _signFade.alpha = fade;
        }

        // offer.css ofs-twinkle: dark until 55% of its cycle, lit and turned at 70%, fading by 100%.
        private static void Twinkle(Image sparkle, float cycle)
        {
            var k = cycle - Mathf.Floor(cycle);
            float alpha, scale, turn;
            if (k < 0.55f) (alpha, scale, turn) = (0f, 0.2f, 0f);
            else if (k < 0.7f)
            {
                var t = Mathf.SmoothStep(0f, 1f, (k - 0.55f) / 0.15f);
                (alpha, scale, turn) = (t, Mathf.Lerp(0.2f, 1f, t), 25f * t);
            }
            else if (k < 0.85f)
            {
                var t = Mathf.SmoothStep(0f, 1f, (k - 0.7f) / 0.15f);
                (alpha, scale, turn) = (Mathf.Lerp(1f, 0.7f, t), Mathf.Lerp(1f, 0.75f, t), Mathf.Lerp(25f, 45f, t));
            }
            else
            {
                var t = Mathf.SmoothStep(0f, 1f, (k - 0.85f) / 0.15f);
                (alpha, scale, turn) = (Mathf.Lerp(0.7f, 0f, t), Mathf.Lerp(0.75f, 0.2f, t), Mathf.Lerp(45f, 0f, t));
            }

            var colour = sparkle.color;
            sparkle.color = new Color(colour.r, colour.g, colour.b, alpha);
            sparkle.rectTransform.localScale = Vector3.one * scale;
            sparkle.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -turn);
        }
    }
}
