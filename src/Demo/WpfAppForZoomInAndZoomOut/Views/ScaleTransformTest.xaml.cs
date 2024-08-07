﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace WpfAppForZoomInAndZoomOut;

public partial class ScaleTransformTest : UserControl
{
    public ScaleTransformTest()
    {
        InitializeComponent();
    }


    private void Button80_Click(object sender, RoutedEventArgs e)
    {
        Scale(0.8);
    }

    private void Scale(double scale)
    {
        SliderForScale.Value = scale;
    }


    private void Button100_Click(object sender, RoutedEventArgs e)
    {
        Scale(1);
    }

    private void Button120_Click(object sender, RoutedEventArgs e)
    {
        Scale(1.2);
    }

    private void Button150_Click(object sender, RoutedEventArgs e)
    {
        Scale(1.5);
    }

    private void Button200_Click(object sender, RoutedEventArgs e)
    {
        Scale(2);
    }

    private void Button300_Click(object sender, RoutedEventArgs e)
    {
        Scale(3);
    }

    private void Window_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers != ModifierKeys.Control) return;

        var slideValue = SliderForScale.Value;
        if (e.Delta > 0)
            slideValue += 0.2;
        else
            slideValue -= 0.2;

        if (slideValue < SliderForScale.Minimum) slideValue = SliderForScale.Minimum;

        if (slideValue > SliderForScale.Maximum) slideValue = SliderForScale.Maximum;

        Scale(slideValue);
    }
}