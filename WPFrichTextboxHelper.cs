using System;
using System.Windows.Documents;
using System.Windows.Controls;
using System.Windows;
using System.Collections.Generic;

namespace SharedClasses
{
	public abstract class InlineItem
	{
		public abstract Inline GetInline();

		public static List<Paragraph> GetParagraphsFromInlineItems(bool EachHasOwnParagrap, params InlineItem[] inlineItems)
		{
			if (!EachHasOwnParagrap)
			{
				Paragraph par = new Paragraph();
				foreach (var il in inlineItems)
					par.Inlines.Add(il);
				return new List<Paragraph>() { par };
			}
			else
			{
				List<Paragraph> paragraphs = new List<Paragraph>();
				foreach (var il in inlineItems)
					paragraphs.Add(new Paragraph(il));
				return paragraphs;
			}
		}

		public static implicit operator Inline(InlineItem thisItem)
		{
			return thisItem.GetInline();
		}
	}

	public sealed class InlineText : InlineItem
	{
		public string Text;
		public InlineText(string Text)
		{
			this.Text = Text;
		}
		public override Inline GetInline()
		{
			return new Run(Text);
		}
	}

	public sealed class InlineButton : InlineItem
	{
		public Button button;
		public BaselineAlignment inlineAlignment;

		public InlineButton(Button button, BaselineAlignment inlineAlignment = BaselineAlignment.Center, params /*EventItem<RoutedEventArgs>[]*/object[] Events)
		{
			this.button = button;
			this.inlineAlignment = inlineAlignment;
			HookEvents(Events);
		}
		public InlineButton(string ButtonContent, BaselineAlignment inlineAlignment = BaselineAlignment.Center)
		{
			this.button = new Button()
			{
				Content = ButtonContent ?? ""
			};
			this.inlineAlignment = inlineAlignment;
		}

		private void HookEvents(/*EventItem<RoutedEventArgs>[]*/object[] Events)
		{
			foreach (var ev in Events)
			{
				AddEvent(ev);
			}
		}

		private void AddEvent(object ev)
		{
			Type evtType = ev.GetType();
			if (TypeNamesMatch(evtType, typeof(ClickEvent)))
			{
				button.Click += (sn, evt) => (ev as ClickEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseDoubleClickEvent)))
			{
				if ((ev as MouseDoubleClickEvent).IsPreview)
					button.PreviewMouseDoubleClick += (sn, evt) => (ev as MouseDoubleClickEvent).OnEvent(sn, evt);
				else
					button.MouseDoubleClick += (sn, evt) => (ev as MouseDoubleClickEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseDownEvent)))
			{
				if ((ev as MouseDownEvent).IsPreview)
					button.PreviewMouseDown += (sn, evt) => (ev as MouseDownEvent).OnEvent(sn, evt);
				else
					button.MouseDown += (sn, evt) => (ev as MouseDownEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseEnterEvent)))
			{
				button.MouseEnter += (sn, evt) => (ev as MouseEnterEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseMoveEvent)))
			{
				if ((ev as MouseMoveEvent).IsPreview)
					button.PreviewMouseMove += (sn, evt) => (ev as MouseMoveEvent).OnEvent(sn, evt);
				else
					button.MouseMove += (sn, evt) => (ev as MouseMoveEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseLeaveEvent)))
			{
				button.MouseLeave += (sn, evt) => (ev as MouseLeaveEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseUpEvent)))
			{
				if ((ev as MouseUpEvent).IsPreview)
					button.PreviewMouseUp += (sn, evt) => (ev as MouseUpEvent).OnEvent(sn, evt);
				else
					button.MouseUp += (sn, evt) => (ev as MouseUpEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(MouseWheelEvent)))
			{
				if ((ev as MouseWheelEvent).IsPreview)
					button.PreviewMouseWheel += (sn, evt) => (ev as MouseWheelEvent).OnEvent(sn, evt);
				else
					button.MouseWheel += (sn, evt) => (ev as MouseWheelEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(DragOverEvent)))
			{
				//button.AllowDrop = true; NOT NECESSARY
				if ((ev as DragOverEvent).IsPreview)
					button.PreviewDragOver += (sn, evt) => (ev as DragOverEvent).OnEvent(sn, evt);
				else
					button.DragOver += (sn, evt) => (ev as DragOverEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(DragEnterEvent)))
			{
				//button.AllowDrop = true; NOT NECESSARY
				if ((ev as DragEnterEvent).IsPreview)
					button.PreviewDragEnter += (sn, evt) => (ev as DragEnterEvent).OnEvent(sn, evt);
				else
					button.DragEnter += (sn, evt) => (ev as DragEnterEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(DropEvent)))
			{
				//button.AllowDrop = true; NOT NECESSARY
				if ((ev as DropEvent).IsPreview)
					button.PreviewDrop += (sn, evt) => (ev as DropEvent).OnEvent(sn, evt);
				else
					button.Drop += (sn, evt) => (ev as DropEvent).OnEvent(sn, evt);
			}
			else if (TypeNamesMatch(evtType, typeof(DragLeaveEvent)))
			{
				//button.AllowDrop = true; NOT NECESSARY
				if ((ev as DragLeaveEvent).IsPreview)
					button.PreviewDragLeave += (sn, evt) => (ev as DragLeaveEvent).OnEvent(sn, evt);
				else
					button.DragLeave += (sn, evt) => (ev as DragLeaveEvent).OnEvent(sn, evt);
			}
			else
				//MessageBox.Show("Unable to register event type: " + ev.GetType().Name);
				throw new Exception("Unable to register event type: " + ev.GetType().Name);
		}

		private bool TypeNamesMatch(Type type1, Type type2, bool ignoreCase = true)
		{
			return
				ignoreCase
				? type1.Name.Equals(type2.Name, StringComparison.InvariantCultureIgnoreCase)
				: type1.Name.Equals(type2.Name);
		}

		public override Inline GetInline()
		{
			return new InlineUIContainer(button)
			{
				BaselineAlignment = inlineAlignment
			};
		}
	}
}