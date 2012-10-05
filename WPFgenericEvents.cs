using System;
using System.Windows;
using System.Windows.Input;

namespace SharedClasses
{
	public abstract class EventItem<T> where T : RoutedEventArgs
	{
		public enum EventTypes
		{
			Click,
			PreviewMouseDoubleClick, PreviewMouseDown, PreviewMouseMove, PreviewMouseUp, PreviewMouseWheel,
			MouseDoubleClick, MouseDown, MouseEnter, MouseLeave, MouseMove, MouseUp, MouseWheel,
			DragOver, DragEnter, Drop, DragLeave,
			PreviewDragOver, PreviewDragEnter, PreviewDrop, PreviewDragLeave
		};
		public EventTypes MouseEventType;
		internal Action<object, T> OnEvent;
		public EventItem(EventTypes MouseEventType, Action<object, T> OnEvent)
		{
			this.MouseEventType = MouseEventType;
			this.OnEvent = OnEvent;
		}
	}

	public abstract class EventItemWithPreview<T> : EventItem<T> where T : RoutedEventArgs//MouseEventArgs
	{
		public bool IsPreview;
		public EventItemWithPreview(EventTypes MouseEventType, bool IsPreview, Action<object, T> OnEvent)
			: base(MouseEventType, OnEvent)
		{
			this.IsPreview = IsPreview;
		}
	}

	public sealed class ClickEvent : EventItem<RoutedEventArgs>
	{
		public ClickEvent(Action<object, RoutedEventArgs> OnMouseClick) : base(EventTypes.Click, OnMouseClick) { }
	}

	public sealed class MouseDoubleClickEvent : EventItemWithPreview<MouseButtonEventArgs>
	{
		public MouseDoubleClickEvent(bool IsPreview, Action<object, MouseButtonEventArgs> OnMouseDoubleClick)
			: base(IsPreview ? EventTypes.PreviewMouseDoubleClick : EventTypes.MouseDoubleClick, IsPreview, OnMouseDoubleClick) { }
	}

	public sealed class MouseDownEvent : EventItemWithPreview<MouseButtonEventArgs>
	{
		public MouseDownEvent(bool IsPreview, Action<object, MouseButtonEventArgs> OnMouseDown)
			: base(IsPreview ? EventTypes.PreviewMouseDown : EventTypes.MouseDown, IsPreview, OnMouseDown) { }
	}

	public sealed class MouseEnterEvent : EventItem<MouseEventArgs>
	{
		public MouseEnterEvent(Action<object, MouseEventArgs> OnMouseEnter)
			: base(EventTypes.MouseEnter, OnMouseEnter) { }
	}

	public sealed class MouseMoveEvent : EventItemWithPreview<MouseEventArgs>
	{
		public MouseMoveEvent(bool IsPreview, Action<object, MouseEventArgs> OnMouseMove)
			: base(IsPreview ? EventTypes.PreviewMouseMove : EventTypes.MouseMove, IsPreview, OnMouseMove) { }
	}

	public sealed class MouseLeaveEvent : EventItem<MouseEventArgs>
	{
		public MouseLeaveEvent(Action<object, MouseEventArgs> OnMouseLeave)
			: base(EventTypes.MouseLeave, OnMouseLeave) { }
	}

	public sealed class MouseUpEvent : EventItemWithPreview<MouseButtonEventArgs>
	{
		public MouseUpEvent(bool IsPreview, Action<object, MouseButtonEventArgs> OnMouseUp)
			: base(IsPreview ? EventTypes.PreviewMouseUp : EventTypes.MouseUp, IsPreview, OnMouseUp) { }
	}

	public sealed class MouseWheelEvent : EventItemWithPreview<MouseEventArgs>
	{
		public MouseWheelEvent(bool IsPreview, Action<object, MouseEventArgs> OnMouseWheel)
			: base(IsPreview ? EventTypes.PreviewMouseWheel : EventTypes.MouseWheel, IsPreview, OnMouseWheel) { }
	}

	public sealed class DragOverEvent : EventItemWithPreview<DragEventArgs>
	{
		public DragOverEvent(bool IsPreview, Action<object, DragEventArgs> OnDragOver)
			: base(IsPreview ? EventTypes.PreviewDragOver : EventTypes.DragOver, IsPreview, OnDragOver) { }
	}

	public sealed class DragEnterEvent : EventItemWithPreview<DragEventArgs>
	{
		public DragEnterEvent(bool IsPreview, Action<object, DragEventArgs> OnDragEnter)
			: base(IsPreview ? EventTypes.PreviewDragEnter : EventTypes.DragEnter, IsPreview, OnDragEnter) { }
	}

	public sealed class DropEvent : EventItemWithPreview<DragEventArgs>
	{
		public DropEvent(bool IsPreview, Action<object, DragEventArgs> OnDrop)
			: base(IsPreview ? EventTypes.PreviewDrop : EventTypes.Drop, IsPreview, OnDrop) { }
	}

	public sealed class DragLeaveEvent : EventItemWithPreview<DragEventArgs>
	{
		public DragLeaveEvent(bool IsPreview, Action<object, DragEventArgs> OnDragLeave)
			: base(IsPreview ? EventTypes.PreviewDragLeave : EventTypes.DragLeave, IsPreview, OnDragLeave) { }
	}
}