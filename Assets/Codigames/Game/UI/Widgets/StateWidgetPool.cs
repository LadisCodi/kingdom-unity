using UnityEngine;
using VContainer;

namespace Codigames.Game.UI.Widgets
{
    // A pool of state-driven widgets: Spawn applies the data.
    public class StateWidgetPool<TWidget, TData, TState> : WidgetPool<TWidget>
        where TWidget : StateDrivenWidget<TData, TState>
        where TState : System.Enum
    {
        public StateWidgetPool(TWidget prefab, Transform container, IObjectResolver resolver, int defaultCapacity = 4, int maxSize = 16)
            : base(prefab, container, resolver, defaultCapacity, maxSize)
        {
        }

        public TWidget Spawn(TData data)
        {
            var widget = Spawn();
            widget.SetData(data);
            return widget;
        }
    }
}
