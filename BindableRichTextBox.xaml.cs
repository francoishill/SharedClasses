using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;

namespace FsWpfControls.FsRichTextBox
{
    /// <summary>
    /// Interaction logic for FsRichTextBox.xaml
    /// </summary>
    public partial class FsRichTextBox : UserControl
    {
        // Member variables
        private int m_InternalUpdatePending;
        private bool m_TextHasChanged;

		// Document property
        public static readonly DependencyProperty DocumentProperty = 
            DependencyProperty.Register("Document", typeof(FlowDocument), 
            typeof(FsRichTextBox), new PropertyMetadata(OnDocumentChanged));

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FsRichTextBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// The WPF FlowDocument contained in the control.
        /// </summary>
        public FlowDocument Document
        {
            get { return (FlowDocument)GetValue(DocumentProperty); }
            set { SetValue(DocumentProperty, value); }
        }

        /// <summary>
        /// Called when the Document property is changed
        /// </summary>
        private static void OnDocumentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            /* For unknown reasons, this method gets called twice when the 
             * Document property is set. Until we figure out why, we initialize
             * the flag to 2 and decrement it each time through this method. */

            // Initialize
            var thisControl = (FsRichTextBox)d;

            // Exit if this update was internally generated
            if (thisControl.m_InternalUpdatePending > 0)
            {

                // Decrement flags and exit
                thisControl.m_InternalUpdatePending--;
                return;
            }

            // Set Document property on RichTextBox
            thisControl.TextBox.Document = (e.NewValue == null) ? new FlowDocument() : (FlowDocument)e.NewValue;

            // Reset flag
            thisControl.m_TextHasChanged = false;
        } 

        /// <summary>
        /// Formats code blocks.
        /// </summary>
        private void OnCodeBlockClick(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(TextBox.Selection.Start, TextBox.Selection.End);
            textRange.ApplyPropertyValue(TextElement.FontFamilyProperty, "Consolas");
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, "FireBrick");
            textRange.ApplyPropertyValue(TextElement.FontSizeProperty, 11D);
            textRange.ApplyPropertyValue(Block.MarginProperty, new Thickness(0));
        }

        /// <summary>
        /// Formats inline code.
        /// </summary>
        private void OnInlineCodeClick(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(TextBox.Selection.Start, TextBox.Selection.End);
            textRange.ApplyPropertyValue(TextElement.FontFamilyProperty, "Consolas");
            textRange.ApplyPropertyValue(TextElement.FontSizeProperty, 11D);
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, "FireBrick");
        }

        /// <summary>
        /// Formats regular text
        /// </summary>
        private void OnNormalTextClick(object sender, RoutedEventArgs e)
        {
            var textRange = new TextRange(TextBox.Selection.Start, TextBox.Selection.End);
            textRange.ApplyPropertyValue(TextElement.FontFamilyProperty, FontFamily);
            textRange.ApplyPropertyValue(TextElement.FontSizeProperty, FontSize);
            textRange.ApplyPropertyValue(TextElement.ForegroundProperty, Foreground);
            textRange.ApplyPropertyValue(Block.MarginProperty, new Thickness(Double.NaN));
        }

        /// <summary>
        ///  Invoked when the user changes text in this user control.
        /// </summary>
        private void OnTextChanged(object sender, TextChangedEventArgs e)
        {
            // Set the TextChanged flag
            m_TextHasChanged = true; 
        }

        /// Forces an update of the Document property.
        /// </summary>
        public void UpdateDocumentBindings()
        {
            // Exit if text hasn't changed
            if (!m_TextHasChanged) return;

            // Set 'Internal Update Pending' flag
            m_InternalUpdatePending = 2;

            // Set Document property
            SetValue(DocumentProperty, this.TextBox.Document); 
        }
    }
}
