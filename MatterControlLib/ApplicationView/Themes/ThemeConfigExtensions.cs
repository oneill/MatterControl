﻿/*
Copyright (c) 2022, Lars Brubaker, John Lewin
All rights reserved.

Redistribution and use in source and binary forms, with or without
modification, are permitted provided that the following conditions are met:

1. Redistributions of source code must retain the above copyright notice, this
   list of conditions and the following disclaimer.
2. Redistributions in binary form must reproduce the above copyright notice,
   this list of conditions and the following disclaimer in the documentation
   and/or other materials provided with the distribution.

THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND
ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR
ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
(INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
(INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those
of the authors and should not be interpreted as representing official policies,
either expressed or implied, of the FreeBSD Project.
*/

using MatterHackers.Agg;
using MatterHackers.Agg.Platform;
using MatterHackers.Agg.UI;
using MatterHackers.ImageProcessing;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.CustomWidgets;
using MatterHackers.MatterControl.PartPreviewWindow;
using MatterHackers.MatterControl.PrinterCommunication;
using System.Collections.Generic;

namespace MatterHackers.MatterControl
{
    public static class ThemeConfigExtensions
    {
        public static double MicroButtonHeight => 20 * GuiWidget.DeviceScale;

        private static double MicroButtonWidth => 30 * GuiWidget.DeviceScale;

        public static SectionWidget ApplyBoxStyle(this ThemeConfig config, SectionWidget sectionWidget)
        {
            return config.ApplyBoxStyle(
                sectionWidget,
                config.SectionBackgroundColor,
                margin: new BorderDouble(config.DefaultContainerPadding, 0, config.DefaultContainerPadding, config.DefaultContainerPadding));
        }

        // ApplySquareBoxStyle
        public static SectionWidget ApplyBoxStyle(this ThemeConfig config, SectionWidget sectionWidget, BorderDouble margin)
        {
            sectionWidget.BackgroundColor = config.SectionBackgroundColor;
            sectionWidget.Margin = 0;
            sectionWidget.Border = new BorderDouble(bottom: 1);
            sectionWidget.BorderColor = config.RowBorder;

            return sectionWidget;
        }

        public static SectionWidget ApplyBoxStyle(this ThemeConfig config, SectionWidget sectionWidget, Color backgroundColor, BorderDouble margin)
        {
            // Enforce panel padding
            // sectionWidget.ContentPanel.Padding = new BorderDouble(10, 0, 10, 2);
            // sectionWidget.ContentPanel.Padding = 0;

            sectionWidget.BorderColor = Color.Transparent;
            sectionWidget.BorderRadius = 5;
            sectionWidget.Margin = margin;
            sectionWidget.BackgroundColor = backgroundColor;

            return sectionWidget;
        }

        public static void ApplyPrimaryActionStyle(this ThemeConfig config, GuiWidget guiWidget)
        {
            guiWidget.BackgroundColor = new Color(config.AccentMimimalOverlay, 50);

            Color hoverColor = config.AccentMimimalOverlay;

            switch (guiWidget)
            {
                case PopupMenuButton menuButton:
                    menuButton.HoverColor = hoverColor;
                    break;

                case SimpleFlowButton flowButton:
                    flowButton.HoverColor = hoverColor;
                    break;

                case SimpleButton button:
                    button.HoverColor = hoverColor;
                    break;
            }
        }

        public static SolidSlider ApplySliderStyle(this ThemeConfig config, SolidSlider solidSlider)
        {
            solidSlider.View.TrackColor = config.SlightShade;
            solidSlider.View.TrackRadius = 4;

            return solidSlider;
        }

        public static DoubleSolidSlider ApplySliderStyle(this ThemeConfig config, DoubleSolidSlider solidSlider)
        {
            solidSlider.View.TrackColor = config.SlightShade;
            solidSlider.View.TrackRadius = 4;

            return solidSlider;
        }

        public static JogControls.ExtrudeButton CreateExtrudeButton(this ThemeConfig config, PrinterConfig printer, string label, double movementFeedRate, int extruderNumber, bool levelingButtons = false)
        {
            return new JogControls.ExtrudeButton(printer, label, movementFeedRate, extruderNumber, config)
            {
                BackgroundColor = config.MinimalShade,
                BorderColor = config.BorderColor40,
                BackgroundOutlineWidth = 1,
                VAnchor = VAnchor.Absolute,
                HAnchor = HAnchor.Absolute,
                Margin = 0,
                Padding = 0,
                Height = (levelingButtons ? 45 : 40) * GuiWidget.DeviceScale,
                Width = (levelingButtons ? 90 : 40) * GuiWidget.DeviceScale,
            };
        }

        public static FlowLayoutWidget CreateMenuItems(this ThemeConfig config, PopupMenu popupMenu, IEnumerable<NamedAction> menuActions)
        {
            // Create menu items in the DropList for each element in this.menuActions
            foreach (var menuAction in menuActions)
            {
                if (menuAction is ActionSeparator)
                {
                    popupMenu.CreateSeparator();
                }
                else
                {
                    if (menuAction is NamedActionGroup namedActionButtons)
                    {
                        var content = new FlowLayoutWidget()
                        {
                            HAnchor = HAnchor.Fit | HAnchor.Stretch
                        };

                        var textWidget = new TextWidget(menuAction.Title, pointSize: config.DefaultFontSize, textColor: config.TextColor)
                        {
                            // Padding = MenuPadding,
                            VAnchor = VAnchor.Center
                        };
                        content.AddChild(textWidget);

                        content.AddChild(new HorizontalSpacer());

                        foreach (var actionButton in namedActionButtons.Group)
                        {
                            var button = new TextButton(actionButton.Title, config)
                            {
                                Border = new BorderDouble(1, 0, 0, 0),
                                BorderColor = config.MinimalShade,
                                HoverColor = config.AccentMimimalOverlay,
                                Enabled = actionButton.IsEnabled()
                            };

                            content.AddChild(button);

                            if (actionButton.IsEnabled())
                            {
                                button.Click += (s, e) =>
                                {
                                    actionButton.Action();
                                    popupMenu.Unfocus();
                                };
                            }
                        }

                        var menuItem = new PopupMenu.MenuItem(content, config)
                        {
                            HAnchor = HAnchor.Fit | HAnchor.Stretch,
                            VAnchor = VAnchor.Fit,
                            HoverColor = Color.Transparent,
                        };
                        popupMenu.AddChild(menuItem);
                        menuItem.Padding = new BorderDouble(menuItem.Padding.Left,
                            menuItem.Padding.Bottom,
                            0,
                            menuItem.Padding.Top);
                    }
                    else
                    {
                        PopupMenu.MenuItem menuItem;

                        if (menuAction is NamedBoolAction boolAction)
                        {
                            menuItem = popupMenu.CreateBoolMenuItem(menuAction.Title, boolAction.GetIsActive, boolAction.SetIsActive);
                        }
                        else
                        {
                            menuItem = popupMenu.CreateMenuItem(menuAction.Title, menuAction.Icon, menuAction.Shortcut);
                        }

                        menuItem.Name = $"{menuAction.Title} Menu Item";

                        menuItem.Enabled = menuAction is NamedActionGroup
                            || (menuAction.Action != null && menuAction.IsEnabled?.Invoke() != false);

                        menuItem.ClearRemovedFlag();

                        if (menuItem.Enabled)
                        {
                            menuItem.Click += (s, e) =>
                            {
                                menuAction.Action();
                            };
                        }
                    }
                }
            }

            return popupMenu;
        }

        public static RadioTextButton CreateMicroRadioButton(this ThemeConfig config, string text, IList<GuiWidget> siblingRadioButtonList = null)
        {
            var radioButton = new RadioTextButton(text, config, config.FontSize8)
            {
                SiblingRadioButtonList = siblingRadioButtonList,
                Padding = new BorderDouble(5, 0),
                SelectedBackgroundColor = config.SlightShade,
                UnselectedBackgroundColor = config.SlightShade,
                HoverColor = config.AccentMimimalOverlay,
                Margin = new BorderDouble(right: 1),
                HAnchor = HAnchor.Absolute,
                Height = config.MicroButtonHeight,
                Width = MicroButtonWidth
            };

            // Add to sibling list if supplied
            siblingRadioButtonList?.Add(radioButton);

            return radioButton;
        }

        public static JogControls.MoveButton CreateMoveButton(this ThemeConfig config, PrinterConfig printer, string label, PrinterConnection.Axis axis, double movementFeedRate, bool levelingButtons = false)
        {
            return new JogControls.MoveButton(label, printer, axis, movementFeedRate, config)
            {
                BackgroundColor = config.MinimalShade,
                BorderColor = config.BorderColor40,
                BackgroundOutlineWidth = 1,
                VAnchor = VAnchor.Absolute,
                HAnchor = HAnchor.Absolute,
                Margin = 0,
                Padding = 0,
                Height = (levelingButtons ? 45 : 40) * GuiWidget.DeviceScale,
                Width = (levelingButtons ? 90 : 40) * GuiWidget.DeviceScale,
            };
        }

        public static GuiWidget CreateSearchButton(this ThemeConfig config)
        {
            return new IconButton(StaticData.Instance.LoadIcon("icon_search_24x24.png", 16, 16).SetToColor(config.TextColor), config)
            {
                ToolTipText = "Search".Localize(),
            };
        }

        public static GuiWidget CreateSmallResetButton(this ThemeConfig config)
        {
            return new HoverImageWidget(config.RestoreNormal, config.RestoreHover)
            {
                VAnchor = VAnchor.Center,
                Margin = new BorderDouble(0, 0, 5, 0)
            };
        }

        public static PopupMenuButton CreateSplitButton(this ThemeConfig config, SplitButtonParams buttonParams, OperationGroup operationGroup = null)
        {
            PopupMenuButton menuButton = null;

            GuiWidget innerButton;
            if (buttonParams.ButtonText == null)
            {
                innerButton = new IconButton(buttonParams.Icon, config)
                {
                    Name = buttonParams.ButtonName + " Inner SplitButton",
                    Enabled = buttonParams.ButtonEnabled,
                    ToolTipText = buttonParams.ButtonTooltip,
                };

                // Remove right Padding for drop style
                innerButton.Padding = innerButton.Padding.Clone(right: 0);
            }
            else
            {
                if (buttonParams.Icon == null)
                {
                    innerButton = new TextButton(buttonParams.ButtonText, config)
                    {
                        Name = buttonParams.ButtonName,
                        Enabled = buttonParams.ButtonEnabled,
                        ToolTipText = buttonParams.ButtonTooltip,
                    };
                }
                else
                {
                    innerButton = new TextIconButton(buttonParams.ButtonText, buttonParams.Icon, config)
                    {
                        Name = buttonParams.ButtonName,
                        Enabled = buttonParams.ButtonEnabled,
                        ToolTipText = buttonParams.ButtonTooltip,
                        Padding = new BorderDouble(5, 0, 5, 0)
                    };
                }
            }

            innerButton.Click += (s, e) =>
            {
                buttonParams.ButtonAction.Invoke(menuButton);
            };

            if (operationGroup == null)
            {
                menuButton = new PopupMenuButton(innerButton, config);
            }
            else
            {
                menuButton = new OperationGroupButton(operationGroup, innerButton, config);
            }

            var theme = ApplicationController.Instance.MenuTheme;
            menuButton.DynamicPopupContent = () =>
            {
                var popupMenu = new PopupMenu(theme);
                buttonParams.ExtendPopupMenu?.Invoke(popupMenu);

                return popupMenu;
            };

            menuButton.Name = buttonParams.ButtonName + " Menu SplitButton";
            menuButton.BackgroundColor = buttonParams.BackgroundColor;
            if (menuButton.BackgroundColor == Color.Transparent)
            {
                menuButton.BackgroundColor = config.ToolbarButtonBackground;
            }

            menuButton.HoverColor = config.ToolbarButtonHover;
            menuButton.MouseDownColor = config.ToolbarButtonDown;
            menuButton.DrawArrow = true;
            menuButton.Margin = config.ButtonSpacing;
            menuButton.DistinctPopupButton = true;
            menuButton.BackgroundRadius = new RadiusCorners(theme.ButtonRadius * GuiWidget.DeviceScale, theme.ButtonRadius * GuiWidget.DeviceScale, 0, 0);

            innerButton.Selectable = true;
            return menuButton;
        }

        public static void RebuildTheme(this ThemeConfig config)
        {
            config.GeneratingThumbnailIcon = StaticData.Instance.LoadIcon("building_thumbnail_40x40.png", 40, 40).SetToColor(config.TextColor);
        }

        public static void RemovePrimaryActionStyle(this ThemeConfig config, GuiWidget guiWidget)
        {
            guiWidget.BackgroundColor = Color.Transparent;

            // Buttons in toolbars should revert to ToolbarButtonHover when reset
            bool parentIsToolbar = guiWidget.Parent?.Parent is Toolbar;

            switch (guiWidget)
            {
                case SimpleFlowButton flowButton:
                    flowButton.HoverColor = parentIsToolbar ? config.ToolbarButtonHover : Color.Transparent;
                    break;

                case SimpleButton button:
                    button.HoverColor = parentIsToolbar ? config.ToolbarButtonHover : Color.Transparent;
                    break;
            }
        }
    }
}