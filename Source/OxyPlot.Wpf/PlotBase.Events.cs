﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="PlotBase.Events.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Represents a control that displays a <see cref="PlotModel" />.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Controls;
    using System.Windows.Input;
    using System.Linq;

    /// <summary>
    /// Represents a control that displays a <see cref="PlotModel" />.
    /// </summary>
    public partial class PlotBase
    {
        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.KeyDown" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.Handled)
            {
                return;
            }

            var args = new OxyKeyEventArgs { ModifierKeys = Keyboard.GetModifierKeys(), Key = e.Key.Convert() };
            e.Handled = this.ActualController.HandleKeyDown(this, args);
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationStarted" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationStarted(ManipulationStartedEventArgs e)
        {
            base.OnManipulationStarted(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchStarted(this, e.ToTouchEventArgs(this));
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationDelta" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationDelta(ManipulationDeltaEventArgs e)
        {
            base.OnManipulationDelta(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchDelta(this, e.ToTouchEventArgs(this));
        }

        /// <summary>
        /// Called when the <see cref="E:System.Windows.UIElement.ManipulationCompleted" /> event occurs.
        /// </summary>
        /// <param name="e">The data for the event.</param>
        protected override void OnManipulationCompleted(ManipulationCompletedEventArgs e)
        {
            base.OnManipulationCompleted(e);
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleTouchCompleted(this, e.ToTouchEventArgs(this));
        }

        /// <summary>
        /// Called before the <see cref="E:System.Windows.UIElement.MouseWheel" /> event occurs to provide handling for the event in a derived class without attaching a delegate.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Input.MouseWheelEventArgs" /> that contains the event data.</param>
        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            if (e.Handled || !this.IsMouseWheelEnabled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseWheel(this, e.ToMouseWheelEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseDown attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. This event data reports details about the mouse button that was pressed and the handled state.</param>
        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Handled)
            {
                return;
            }

            this.Focus();
            this.CaptureMouse();

            // store the mouse down point, check it when mouse button is released to determine if the context menu should be shown
            this.mouseDownPoint = e.GetPosition(this).ToScreenPoint();

            e.Handled = this.ActualController.HandleMouseDown(this, e.ToMouseDownEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled MouseMove attached event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            UpdateToolTip();

            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseMove(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// The string representation of the ToolTip. In its setter there isn't any check of the value to be different than the previous value, and the ToolTip is always made visible from the setter if it is not null and not empty, else it is hidden.
        /// </summary>
        protected string OxyToolTipString
        {
            get
            {
                return this.OxyToolTip != null ? this.OxyToolTip.Content.ToString() : null;
            }
            set
            {
                if (this.previouslyHoveredPlotElement == this.currentlyHoveredPlotElement)
                {
                    return;
                }

                // tooltip removal
                if (value == null)
                {
                    if (this.tokenSource != null)
                    {
                        this.tokenSource.Cancel();
                        this.tokenSource.Dispose();
                        this.tokenSource = null;
                    }

                    if (this.OxyToolTip != null)
                    {
                        this.Dispatcher.Invoke(new Action(() =>
                        {
                            this.OxyToolTip.IsOpen = false;
                        }), System.Windows.Threading.DispatcherPriority.Send);
                    }
                }
                // setting the tooltip string
                else if (value != null)
                {
                    if (this.tokenSource != null)
                    {
                        this.tokenSource.Cancel();
                        this.tokenSource.Dispose();
                        this.tokenSource = null;
                    }

                    this.tokenSource = new CancellationTokenSource();
                    this.toolTipTask = ShowToolTip(value, tokenSource.Token);
                }
            }
        }

        protected async Task ShowToolTip(string value, CancellationToken ct)
        {
            if (secondToolTipTask != null)
            {
                await secondToolTipTask;
            }

            if (ct.IsCancellationRequested)
            {
                return;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.OxyToolTip.IsOpen = false;
            }));

            if (ct.IsCancellationRequested)
            {
                return;
            }

            int initialShowDelay = ToolTipService.GetInitialShowDelay(this);

            await Task.Delay(initialShowDelay, ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            this.OxyToolTip.Content = value;

            if (ct.IsCancellationRequested)
            {
                return;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.OxyToolTip.IsOpen = true;
            }));

            _ = HideToolTip(ct);
        }

        protected async Task HideToolTip(CancellationToken ct)
        {
            if (ct.IsCancellationRequested)
            {
                return;
            }

            int betweenShowDelay = ToolTipService.GetBetweenShowDelay(this);

            secondToolTipTask = Task.Delay(betweenShowDelay);
            _ = secondToolTipTask.ContinueWith(new Action<Task>((t) =>
            {
                secondToolTipTask = null;
            }));

            if (ct.IsCancellationRequested)
            {
                return;
            }

            int showDuration = ToolTipService.GetShowDuration(this);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            await Task.Delay(showDuration, ct);

            if (ct.IsCancellationRequested)
            {
                return;
            }

            this.Dispatcher.Invoke(new Action(() =>
            {
                this.OxyToolTip.IsOpen = false;
            }));

            if (ct.IsCancellationRequested)
            {
                return;
            }
        }

        /// <summary>
        /// Returns true if the event is handled.
        /// </summary>
        /// <returns></returns>
        private bool HandleTitleToolTip(ScreenPoint sp)
        {
            bool v = this.ActualModel.TitleArea.Contains(sp);

            if (v && this.ActualModel.Title != null)
            {
                // these 2 lines must be before the third which calls the setter of OxyToolTipString
                this.previouslyHoveredPlotElement = this.currentlyHoveredPlotElement;
                this.currentlyHoveredPlotElement = new ToolTippedPlotElement(true);

                // set the tooltip to be the tooltip of the plot title
                this.OxyToolTipString = this.ActualModel.TitleToolTip;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Returns true if the event is handled.
        /// </summary>
        /// <returns></returns>
        private bool HandlePlotElementsToolTip(ScreenPoint sp)
        {
            bool found = false;

            // it may be possible that the 5 constant in this line needs to be replaced with some other value
            System.Collections.Generic.IEnumerable<HitTestResult> r =
                this.ActualModel.HitTest(new HitTestArguments(sp, 5)).Reverse();

            foreach (HitTestResult rtr in r)
            {
                if (rtr.Element != null)
                {
                    if (rtr.Element is PlotElement pe)
                    {
                        if (pe != this.previouslyHoveredPlotElement)
                        {
                            // these 2 lines must be before the third which calls the setter of OxyToolTipString
                            this.previouslyHoveredPlotElement = this.currentlyHoveredPlotElement;
                            this.currentlyHoveredPlotElement = new ToolTippedPlotElement(pe);

                            // show the tooltip
                            this.OxyToolTipString = pe.ToolTip;
                        }
                        found = true;
                        break;
                    }
                }
            }

            if (!found)
            {
                this.previouslyHoveredPlotElement = this.currentlyHoveredPlotElement;
                this.currentlyHoveredPlotElement = new ToolTippedPlotElement();
            }

            return found;
        }

        /// <summary>
        /// Updates the custom tooltip system's tooltip.
        /// </summary>
        private void UpdateToolTip()
        {
            if (this.ActualModel == null || !this.UseCustomToolTipSystem)
            {
                if (this.UseCustomToolTipSystem)
                {
                    this.OxyToolTipString = null;
                }
                return;
            }

            ScreenPoint sp = Mouse.GetPosition(this).ToScreenPoint();


            bool handleTitle = HandleTitleToolTip(sp);
            bool handleOthers = false;

            if (!handleTitle)
            {
                handleOthers = HandlePlotElementsToolTip(sp);
            }

            if (!handleTitle && !handleOthers)
            {
                this.OxyToolTipString = null;
            }
        }

        /// <summary>
        /// Invoked when an unhandled MouseUp routed event reaches an element in its route that is derived from this class. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseButtonEventArgs" /> that contains the event data. The event data reports that the mouse button was released.</param>
        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.Handled)
            {
                return;
            }

            this.ReleaseMouseCapture();

            e.Handled = this.ActualController.HandleMouseUp(this, e.ToMouseReleasedEventArgs(this));

            // Open the context menu
            var p = e.GetPosition(this).ToScreenPoint();
            var d = p.DistanceTo(this.mouseDownPoint);

            if (this.ContextMenu != null)
            {
                if (Math.Abs(d) < 1e-8 && e.ChangedButton == MouseButton.Right)
                {
                    // TODO: why is the data context not passed to the context menu??
                    this.ContextMenu.DataContext = this.DataContext;
                    this.ContextMenu.PlacementTarget = this;
                    this.ContextMenu.Visibility = System.Windows.Visibility.Visible;
                    this.ContextMenu.IsOpen = true;
                }
                else
                {
                    this.ContextMenu.Visibility = System.Windows.Visibility.Collapsed;
                    this.ContextMenu.IsOpen = false;
                }
            }
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseEnter" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseEnter(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateToolTip();
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseEnter(this, e.ToMouseEventArgs(this));
        }

        /// <summary>
        /// Invoked when an unhandled <see cref="E:System.Windows.Input.Mouse.MouseLeave" /> attached event is raised on this element. Implement this method to add class handling for this event.
        /// </summary>
        /// <param name="e">The <see cref="T:System.Windows.Input.MouseEventArgs" /> that contains the event data.</param>
        protected override void OnMouseLeave(MouseEventArgs e)
        {
            base.OnMouseEnter(e);
            UpdateToolTip();
            if (e.Handled)
            {
                return;
            }

            e.Handled = this.ActualController.HandleMouseLeave(this, e.ToMouseEventArgs(this));
        }
    }
}
