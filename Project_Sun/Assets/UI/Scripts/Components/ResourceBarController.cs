using System;
using UnityEngine.UIElements;
using ProjectSun.UI.Util;

namespace ProjectSun.UI.Components
{
    /// <summary>
    /// Controls the resource HUD bar.
    /// Animates resource value changes with count-up/down tweens.
    /// </summary>
    public class ResourceBarController
    {
        private readonly Label _basicLabel;
        private readonly Label _advancedLabel;
        private readonly Label _relicLabel;
        private readonly Label _turnLabel;

        private readonly float[] _displayed = new float[3]; // basic, advanced, relic
        private readonly int[] _tweenIds = new int[3];

        public ResourceBarController(VisualElement root)
        {
            _basicLabel = root.Q<Label>("basic-value");
            _advancedLabel = root.Q<Label>("advanced-value");
            _relicLabel = root.Q<Label>("relic-value");
            _turnLabel = root.Q<Label>("turn-number");
        }

        public void SetResourcesImmediate(int basic, int advanced, int relic, int turn)
        {
            _displayed[0] = basic;
            _displayed[1] = advanced;
            _displayed[2] = relic;

            _basicLabel.text = basic.ToString();
            _advancedLabel.text = advanced.ToString();
            _relicLabel.text = relic.ToString();
            _turnLabel.text = turn.ToString();
        }

        public void AnimateResourceChange(int newBasic, int newAdvanced, int newRelic)
        {
            AnimateLabel(0, newBasic, _basicLabel);
            AnimateLabel(1, newAdvanced, _advancedLabel);
            AnimateLabel(2, newRelic, _relicLabel);
        }

        public void AnimateTurnChange(int newTurn)
        {
            _turnLabel.text = newTurn.ToString();

            // Scale pop on turn change
            var turnDisplay = _turnLabel.parent;
            turnDisplay.style.scale = new Scale(new UnityEngine.Vector2(1.3f, 1.3f));
            SimpleTween.To(
                () => 1.3f,
                v => turnDisplay.style.scale = new Scale(new UnityEngine.Vector2(v, v)),
                1f, 0.3f,
                SimpleTween.EaseType.OutBack);
        }

        private void AnimateLabel(int index, int target, Label label)
        {
            if (_tweenIds[index] != 0)
                SimpleTween.Kill(_tweenIds[index]);

            int idx = index; // capture for lambda
            _tweenIds[idx] = SimpleTween.To(
                () => _displayed[idx],
                v =>
                {
                    _displayed[idx] = v;
                    label.text = ((int)Math.Round(v)).ToString();
                },
                target, 0.4f,
                SimpleTween.EaseType.OutQuad,
                () =>
                {
                    _displayed[idx] = target;
                    label.text = target.ToString();
                    _tweenIds[idx] = 0;
                });
        }
    }
}
