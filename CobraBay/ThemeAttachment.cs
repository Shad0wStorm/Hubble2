using System;
using System.Windows;
using System.Windows.Media;

namespace CobraBay
{
    /// <summary>
    /// Provides attached properties for theme-related UI elements.
    /// </summary>
    public static class ThemeAttachment
    {
        /// <summary>
        /// Gets the default brush applied to buttons.
        /// </summary>
        /// <param name="obj">The object to retrieve the attached property from.</param>
        /// <returns>The default brush for the button.</returns>
        public static Brush GetButtonDefaultBrush(DependencyObject obj) =>
            (Brush)obj.GetValue(ButtonDefaultBrushProperty);

        /// <summary>
        /// Sets the default brush applied to buttons.
        /// </summary>
        /// <param name="obj">The object to set the attached property on.</param>
        /// <param name="value">The brush to apply to the button.</param>
        public static void SetButtonDefaultBrush(DependencyObject obj, Brush value) =>
            obj.SetValue(ButtonDefaultBrushProperty, value);

        /// <summary>
        /// Defines the attached property for the default button brush.
        /// </summary>
        public static readonly DependencyProperty ButtonDefaultBrushProperty =
            DependencyProperty.RegisterAttached(
                "ButtonDefaultBrush",              // Name of the attached property
                typeof(Brush),                     // Type of the property value
                typeof(ThemeAttachment),           // The owner type of this property
                new FrameworkPropertyMetadata(     // Metadata for the property
                    Brushes.Orange,                // Default value
                    FrameworkPropertyMetadataOptions.Inherits)  // The property is inherited by child elements
            );
    }
}
