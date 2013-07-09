﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Media.Animation;
using System.Reflection;
using System.IO;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace UnhandledExceptions
{
	/// <summary>
	/// Interaction logic for UnhandledExceptionsWindow.xaml
	/// </summary>
	public partial class UnhandledExceptionsWindow : Window
	{
		public UnhandledExceptionsWindow(Exception exception)
		{
			InitializeComponent();

			this.DataContext = exception;
		}

		public static void ShowUnHandledException(Exception exc)
		{
			UnhandledExceptionsWindow uew = new UnhandledExceptionsWindow(exc);
			uew.ShowDialog();
		}

		private void TextBlock_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			//TODO: Should still actually report error to developer here
			System.Windows.Forms.MessageBox.Show("This function is soon to be incorporated.");
		}

		private void TextBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (e.ChangedButton != MouseButton.Left) return;
			(sender as TextBox).SelectAll();
		}
	}

	public static class NavigationService
	{
		// Copied from http://geekswithblogs.net/casualjim/archive/2005/12/01/61722.aspx
		private static readonly Regex RE_URL = new Regex(@"(?#Protocol)(?:(?:ht|f)tp(?:s?)\:\/\/|~/|/)?(?#Username:Password)(?:\w+:\w+@)?(?#Subdomains)(?:(?:[-\w]+\.)+(?#TopLevel Domains)(?:com|org|net|gov|mil|biz|info|mobi|name|aero|jobs|museum|travel|[a-z]{2}))(?#Port)(?::[\d]{1,5})?(?#Directories)(?:(?:(?:/(?:[-\w~!$+|.,=]|%[a-f\d]{2})+)+|/)+|\?|#)?(?#Query)(?:(?:\?(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)(?:&(?:[-\w~!$+|.,*:]|%[a-f\d{2}])+=(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)*)*(?#Anchor)(?:#(?:[-\w~!$+|.,*:=]|%[a-f\d]{2})*)?");

		public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
				"Text",
				typeof(string),
				typeof(NavigationService),
				new PropertyMetadata(null, OnTextChanged)
		);

		public static string GetText(DependencyObject d)
		{ return d.GetValue(TextProperty) as string; }

		public static void SetText(DependencyObject d, string value)
		{ d.SetValue(TextProperty, value); }

		private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var text_block = d as TextBlock;
			if (text_block == null)
				return;

			text_block.Inlines.Clear();

			var new_text = (string)e.NewValue;
			if (string.IsNullOrEmpty(new_text))
				return;

			// Find all URLs using a regular expression
			int last_pos = 0;
			foreach (Match match in RE_URL.Matches(new_text))
			{
				// Copy raw string from the last position up to the match
				if (match.Index != last_pos)
				{
					var raw_text = new_text.Substring(last_pos, match.Index - last_pos);
					text_block.Inlines.Add(new Run(raw_text));
				}

				// Create a hyperlink for the match
				var link = new Hyperlink(new Run(match.Value))
				{
					NavigateUri = new Uri(match.Value)
				};
				link.Click += OnUrlClick;

				text_block.Inlines.Add(link);

				// Update the last matched position
				last_pos = match.Index + match.Length;
			}

			// Finally, copy the remainder of the string
			if (last_pos < new_text.Length)
				text_block.Inlines.Add(new Run(new_text.Substring(last_pos)));
		}

		private static void OnUrlClick(object sender, RoutedEventArgs e)
		{
			var link = (Hyperlink)sender;
			// Do something with link.NavigateUri
		}
	}
}