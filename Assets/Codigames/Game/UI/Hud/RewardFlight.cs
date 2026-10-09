using System.Collections.Generic;
using System.Linq;
using Codigames.Game.Audio;
using Codigames.Game.Data.Economy;
using Codigames.Game.UI.Menus;
using Codigames.Modules.Audio;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Codigames.Game.UI.Hud
{
    // A reward flying into the header, as the web plays it: it bursts out of where it was claimed as a handful of
    // fragments of each currency, which hang for a beat and fly in an arc, one after another, into that
    // currency's slot; the header counts each one in as it lands, with a pulse and a clink. Presentation only: the
    // treasury already holds the reward.
    public class RewardFlight
    {
        private const float STAGGER = 0.07f;
        private const float BURST = 0.26f;
        private const float FLY = 0.62f;
        private const float BETWEEN_KINDS = 0.18f;
        private const float FRAGMENT_SIZE = 76f;
        private static readonly string[] MONEY = { "Gold", "Gems" };

        private readonly UIRoot _root;
        private readonly Codigames.Modules.UI.IMenuViewFactory _views;
        private readonly RewardHold _hold;
        private readonly ICurrencyIcons _icons;
        private readonly ISoundService _sounds;
        private readonly Stack<Image> _pool = new();
        private RectTransform _layer;

        public RewardFlight(UIRoot root, Codigames.Modules.UI.IMenuViewFactory views, RewardHold hold, ICurrencyIcons icons, ISoundService sounds)
        {
            _root = root;
            _views = views;
            _hold = hold;
            _icons = icons;
            _sounds = sounds;
        }

        private HeaderMenu Header => _views.Resolve<HeaderMenu>();

        // From a point on the screen; `fragments` says how many pieces each currency flies as.
        public void Fly(IReadOnlyDictionary<string, double> haul, Vector2 fromScreen, System.Func<string, double, int> fragments)
        {
            var delay = 0f;
            foreach (var (currency, amount) in haul.Where(h => h.Value > 0))
            {
                var slot = Header.SlotIcon(currency);
                if (slot == null) continue;

                var count = Mathf.Max(1, fragments(currency, amount));
                _hold.Hold(currency, amount);
                var shares = Shares(amount, count);
                for (var i = 0; i < count; i++) Launch(currency, shares[i], fromScreen, slot, delay + i * STAGGER, i);
                delay += count * STAGGER + BETWEEN_KINDS;
            }
        }

        private void Launch(string currency, double share, Vector2 from, RectTransform slot, float delay, int index)
        {
            var layer = Layer();
            var fragment = _pool.Count > 0 ? _pool.Pop() : NewFragment(layer);
            fragment.gameObject.SetActive(true);
            fragment.sprite = _icons.IconOf(currency);
            var rect = fragment.rectTransform;
            rect.position = from;
            rect.localScale = Vector3.one * 0.3f;
            fragment.color = new Color(1f, 1f, 1f, 0f);

            var angle = Random.value * Mathf.PI * 2f;
            var unit = layer.lossyScale.x;
            var spot = from + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (40f + Random.value * 50f) * unit;

            var sequence = DOTween.Sequence().SetDelay(delay);
            sequence.Append(rect.DOMove(spot, BURST * 0.7f).SetEase(Ease.OutQuad));
            sequence.Join(rect.DOScale(1.1f, BURST * 0.7f));
            sequence.Join(fragment.DOFade(1f, BURST * 0.5f));
            sequence.Append(rect.DOScale(1f, BURST * 0.3f));

            // A quadratic arc into the slot, speeding up as it goes, shrinking a little on the way in.
            var bow = (120f + Random.value * 160f) * unit * (Random.value < 0.5f ? -1f : 1f);
            var progress = 0f;
            sequence.Append(DOTween.To(() => progress, p =>
            {
                progress = p;
                var to = (Vector2)slot.position;
                var control = new Vector2((spot.x + to.x) / 2f + bow, Mathf.Max(spot.y, to.y) + 60f * unit);
                var mt = 1f - p;
                rect.position = mt * mt * spot + 2f * mt * p * control + p * p * to;
                rect.localScale = Vector3.one * (1f - 0.35f * p);
            }, 1f, FLY).SetEase(Ease.InQuad));

            sequence.OnComplete(() =>
            {
                fragment.gameObject.SetActive(false);
                _pool.Push(fragment);
                _hold.Release(currency, share);
                Header.Pulse(currency);
                _sounds.Play(MONEY.Contains(currency) ? "rewardCoin" : "rewardPop");
            });
        }

        // Splits an amount into whole shares that add back up to it exactly.
        private static double[] Shares(double amount, int count)
        {
            var shares = new double[count];
            for (var i = 0; i < count; i++) shares[i] = System.Math.Floor(amount * (i + 1) / count) - System.Math.Floor(amount * i / count);
            return shares;
        }

        private RectTransform Layer()
        {
            if (_layer != null) return _layer;

            var go = new GameObject(nameof(RewardFlight), typeof(RectTransform));
            _layer = (RectTransform)go.transform;
            _layer.SetParent(_root.Container, false);
            _layer.anchorMin = Vector2.zero;
            _layer.anchorMax = Vector2.one;
            _layer.offsetMin = _layer.offsetMax = Vector2.zero;
            _layer.SetAsLastSibling();
            return _layer;
        }

        private static Image NewFragment(RectTransform layer)
        {
            var go = new GameObject("Fragment", typeof(RectTransform), typeof(Image));
            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            image.preserveAspect = true;
            image.rectTransform.SetParent(layer, false);
            image.rectTransform.sizeDelta = new Vector2(FRAGMENT_SIZE, FRAGMENT_SIZE);
            return image;
        }
    }
}
