﻿using System.Windows;
using System.Windows.Controls;

namespace HandyControl.Controls;

// A control featuring a range of loading indicating animations.
// https://github.com/zeluisping/LoadingIndicators.WPF

[TemplatePart(Name = "Border", Type = typeof(Border))]
public class LoadingIndicator : Control
{
    public static readonly DependencyProperty SpeedRatioProperty =
        DependencyProperty.Register("SpeedRatio", typeof(double), typeof(LoadingIndicator), new PropertyMetadata(1d, (o, e) => {
            LoadingIndicator li = (LoadingIndicator)o;

            if (li.PART_Border == null || li.IsActive == false)
            {
                return;
            }

            foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(li.PART_Border))
            {
                if (group.Name == "ActiveStates")
                {
                    foreach (VisualState state in group.States)
                    {
                        if (state.Name == "Active")
                        {
                            state.Storyboard.SetSpeedRatio(li.PART_Border, (double)e.NewValue);
                        }
                    }
                }
            }
        }));

    public static readonly DependencyProperty IsActiveProperty =
        DependencyProperty.Register("IsActive", typeof(bool), typeof(LoadingIndicator), new PropertyMetadata(true, (o, e) => {
            LoadingIndicator li = (LoadingIndicator)o;

            if (li.PART_Border == null)
            {
                return;
            }

            if ((bool)e.NewValue == false)
            {
                VisualStateManager.GoToElementState(li.PART_Border, "Inactive", false);
                li.PART_Border.Visibility = Visibility.Collapsed;
            }
            else
            {
                VisualStateManager.GoToElementState(li.PART_Border, "Active", false);
                li.PART_Border.Visibility = Visibility.Visible;

                foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(li.PART_Border))
                {
                    if (group.Name == "ActiveStates")
                    {
                        foreach (VisualState state in group.States)
                        {
                            if (state.Name == "Active")
                            {
                                state.Storyboard.SetSpeedRatio(li.PART_Border, li.SpeedRatio);
                            }
                        }
                    }
                }
            }
        }));

    // Variables
    protected Border PART_Border;

    /// <summary>
    /// Get/set the speed ratio of the animation.
    /// </summary>
    public double SpeedRatio
    {
        get { return (double)GetValue(SpeedRatioProperty); }
        set { SetValue(SpeedRatioProperty, value); }
    }

    /// <summary>
    /// Get/set whether the loading indicator is active.
    /// </summary>
    public bool IsActive
    {
        get { return (bool)GetValue(IsActiveProperty); }
        set { SetValue(IsActiveProperty, value); }
    }

    /// <summary>
    /// When overridden in a derived class, is invoked whenever application code
    /// or internal processes call System.Windows.FrameworkElement.ApplyTemplate().
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        PART_Border = (Border)GetTemplateChild("PART_Border");

        if (PART_Border != null)
        {
            VisualStateManager.GoToElementState(PART_Border, (this.IsActive ? "Active" : "Inactive"), false);
            foreach (VisualStateGroup group in VisualStateManager.GetVisualStateGroups(PART_Border))
            {
                if (group.Name == "ActiveStates")
                {
                    foreach (VisualState state in group.States)
                    {
                        if (state.Name == "Active")
                        {
                            state.Storyboard.SetSpeedRatio(PART_Border, this.SpeedRatio);
                        }
                    }
                }
            }

            PART_Border.Visibility = (this.IsActive ? Visibility.Visible : Visibility.Collapsed);
        }
    }

    public LoadingIndicator()
    {
    }
}
