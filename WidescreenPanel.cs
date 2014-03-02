﻿/*
Copyright (c) 2014, Lars Brubaker
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MatterHackers.Agg;
using MatterHackers.Agg.Transform;
using MatterHackers.Agg.Image;
using MatterHackers.Agg.VertexSource;
using MatterHackers.Agg.UI;
using MatterHackers.Agg.Font;
using MatterHackers.VectorMath;

using MatterHackers.MatterControl;
using MatterHackers.MatterControl.PrintQueue;
using MatterHackers.MatterControl.SlicerConfiguration;
using MatterHackers.MatterControl.PrintLibrary;
using MatterHackers.MatterControl.DataStorage;
using MatterHackers.Localizations;
using MatterHackers.MatterControl.PartPreviewWindow;

namespace MatterHackers.MatterControl
{
    public class WidescreenPanel : FlowLayoutWidget
    {        
        static WidescreenPanel globalInstance;
        TabControl advancedControlsTabControl;
        SliceSettingsWidget sliceSettingsWidget;
        TabControl advancedControls;
        public TabPage AboutTabPage;
        TextImageButtonFactory advancedControlsButtonFactory = new TextImageButtonFactory();
        RGBA_Bytes unselectedTextColor = ActiveTheme.Instance.TabLabelUnselected;
        SliceSettingsWidget.UiState sliceSettingsUiState;

        FlowLayoutWidget ColumnOne;
        FlowLayoutWidget ColumnTwo;
        int ColumnTwoMinWidth = 1390;
        FlowLayoutWidget ColumnThree;
        int ColumnThreeMinWidth = 990;

        View3DTransformPart part3DView;
        GcodeViewBasic partGcodeView;

        public WidescreenPanel()
            : base(FlowDirection.LeftToRight)
        {
            ActivePrinterProfile.Instance.ActivePrinterChanged.RegisterEvent(LoadSettingsOnPrinterChanged, ref unregisterEvents);
            {

                //PrintQueueControl.Instance.Initialize();
                BackgroundColor = RGBA_Bytes.Gray;

                ColumnOne = new FlowLayoutWidget(FlowDirection.TopToBottom);
                ColumnTwo = new FlowLayoutWidget(FlowDirection.TopToBottom);
                ColumnThree = new FlowLayoutWidget(FlowDirection.TopToBottom);

                ColumnOne.VAnchor = VAnchor.ParentBottomTop;                
                ColumnOne.AddChild(new ActionBarPlus());
                ColumnOne.AddChild(new PrintProgressBar());
                ColumnOne.AddChild(new QueueTab());
                ColumnOne.Width = 480; //Ordering here matters - must go after children are added
                
                ColumnOne.Padding = new BorderDouble(4);
                ColumnTwo.Padding = new BorderDouble(4);
                ColumnThree.Padding = new BorderDouble(4);
                

                LoadColumnTwo();
                
                ColumnThree.VAnchor = VAnchor.ParentBottomTop;
                
                
                {
                    advancedControlsTabControl = CreateNewAdvancedControlsTab(new SliceSettingsWidget.UiState());
                    ColumnThree.AddChild(advancedControlsTabControl);
                    ColumnThree.Width = 590; //Ordering here matters - must go after children are added
                }

                AddChild(ColumnOne);
                AddChild(ColumnTwo);
                AddChild(ColumnThree);                
            }

            AnchorAll();
            AddHandlers();
            SetVisibleStatus();
            
        }

        void onBoundsChanges(Object sender, EventArgs e)
        {
            SetVisibleStatus();
        }

        void onMouseEnterBoundsAdvancedControlsLink(Object sender, EventArgs e)
        {
            HelpTextWidget.Instance.ShowHoverText("View Manual Printer Controls and Slicing Settings");
        }

        void onMouseLeaveBoundsAdvancedControlsLink(Object sender, EventArgs e)
        {
            HelpTextWidget.Instance.HideHoverText();
        }

        void onMouseEnterBoundsPrintQueueLink(Object sender, EventArgs e)
        {
            HelpTextWidget.Instance.ShowHoverText("View Queue and Library");
        }

        void onMouseLeaveBoundsPrintQueueLink(Object sender, EventArgs e)
        {
            HelpTextWidget.Instance.HideHoverText();
        }

        public static WidescreenPanel Instance
        {
            get
            {
                if (globalInstance == null)
                {
                    globalInstance = new WidescreenPanel();
                }
                return globalInstance;
            }
        }

        public override void OnClosed(EventArgs e)
        {
            if (unregisterEvents != null)
            {
                unregisterEvents(this, null);
            }
            base.OnClosed(e);
        }

        void DoNotChangePanel()
        {
            //Empty function used as placeholder
        }

        
        void DoChangePanel(object state)
        {
            // remember which tab we were on
            int topTabIndex = this.advancedControlsTabControl.SelectedTabIndex;

            // remove the advance control and replace it with new ones build for the selected printer
            int advancedControlsWidgetIndex = ColumnThree.GetChildIndex(this.advancedControlsTabControl);
            ColumnThree.RemoveChild(advancedControlsWidgetIndex);
            this.advancedControlsTabControl = CreateNewAdvancedControlsTab(sliceSettingsUiState);
            ColumnThree.AddChild(this.advancedControlsTabControl, advancedControlsWidgetIndex);
            ColumnThree.Width = 590;

            // set the selected tab back to the one it was before we replace the control
            this.advancedControlsTabControl.SelectTab(topTabIndex);
        }


        TabControl CreateNewAdvancedControlsTab(SliceSettingsWidget.UiState sliceSettingsUiState)
        {
            advancedControls = new TabControl();
            advancedControls.BackgroundColor = ActiveTheme.Instance.PrimaryAccentColor;
            advancedControls.TabBar.BorderColor = RGBA_Bytes.White;
            advancedControls.TabBar.Margin = new BorderDouble(0, 0);
            advancedControls.TabBar.Padding = new BorderDouble(0, 2);

            advancedControlsButtonFactory.invertImageLocation = false;

            GuiWidget manualPrinterControls = new ManualPrinterControls();
            ScrollableWidget manualPrinterControlsScrollArea = new ScrollableWidget(true);
            manualPrinterControlsScrollArea.ScrollArea.HAnchor |= Agg.UI.HAnchor.ParentLeftRight;
            manualPrinterControlsScrollArea.AnchorAll();
            manualPrinterControlsScrollArea.AddChild(manualPrinterControls);

            //Add the tab contents for 'Advanced Controls'
            string printerControlsLabel = new LocalizedString("Controls").Translated;
            advancedControls.AddTab(new SimpleTextTabWidget(new TabPage(manualPrinterControlsScrollArea, printerControlsLabel), 18,
            ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes()));

            string sliceSettingsLabel = new LocalizedString("Slice Settings").Translated;
            sliceSettingsWidget = new SliceSettingsWidget(sliceSettingsUiState);
            advancedControls.AddTab(new SimpleTextTabWidget(new TabPage(sliceSettingsWidget, sliceSettingsLabel), 18,
                        ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes()));

            string configurationLabel = new LocalizedString("Configuration").Translated;
            ScrollableWidget configurationControls = new ConfigurationPage();
            advancedControls.AddTab(new SimpleTextTabWidget(new TabPage(configurationControls, configurationLabel), 18,
                        ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes()));

            return advancedControls;
        }

        event EventHandler unregisterEvents;
        void AddHandlers()
        {
            ActiveTheme.Instance.ThemeChanged.RegisterEvent(onThemeChanged, ref unregisterEvents);
            PrinterCommunication.Instance.ActivePrintItemChanged.RegisterEvent(onActivePrintItemChanged, ref unregisterEvents);
            MainSlidePanel.Instance.ReloadPanelTrigger.RegisterEvent(ReloadBackPanel, ref unregisterEvents);
            this.BoundsChanged += new EventHandler(onBoundsChanges);
        }

        void onActivePrintItemChanged(object sender, EventArgs e)
        {
            LoadColumnTwo();
        }

        void LoadColumnTwo()
        {            
            ColumnTwo.RemoveAllChildren();

            double buildHeight = ActiveSliceSettings.Instance.BuildHeight;
            part3DView = new View3DTransformPart(PrinterCommunication.Instance.ActivePrintItem, new Vector3(ActiveSliceSettings.Instance.BedSize, buildHeight), ActiveSliceSettings.Instance.BedShape);
            part3DView.Margin = new BorderDouble(bottom: 4);
            part3DView.AnchorAll();

            partGcodeView = new GcodeViewBasic(PrinterCommunication.Instance.ActivePrintItem, ActiveSliceSettings.Instance.GetBedSize, ActiveSliceSettings.Instance.GetBedCenter);
            partGcodeView.AnchorAll();

            ColumnTwo.AddChild(part3DView);
            ColumnTwo.AddChild(partGcodeView);
            ColumnTwo.AnchorAll();
            SetVisibleStatus();
        }

        void SetVisibleStatus()
        {
            if (this.Width < ColumnThreeMinWidth)
            {                
                ColumnThree.Visible = false;
                ColumnTwo.Visible = false;

                ColumnOne.AnchorAll();
                ColumnOne.Visible = true;

            }
            else if (this.Width < ColumnTwoMinWidth)
            {
                ColumnThree.Visible = true;
                ColumnTwo.Visible = false;

                ColumnOne.AnchorAll();
                ColumnOne.Visible = true;
            }
            else
            {
                ColumnThree.Visible = true;
                ColumnTwo.Visible = true;

                ColumnOne.HAnchor = Agg.UI.HAnchor.None;
                ColumnOne.Width = 480;
                ColumnOne.Visible = true;
            }
        }


        private void onThemeChanged(object sender, EventArgs e)
        {
            this.advancedControls.BackgroundColor = ActiveTheme.Instance.PrimaryAccentColor;
            this.advancedControls.Invalidate();
        }

        public void ReloadBackPanel(object sender, EventArgs widgetEvent)
        {
            sliceSettingsUiState = new SliceSettingsWidget.UiState(sliceSettingsWidget);
            UiThread.RunOnIdle(DoChangePanel);
        }

        public void LoadSettingsOnPrinterChanged(object sender, EventArgs e)
        {
            ActiveSliceSettings.Instance.LoadSettingsForPrinter();
            MainSlidePanel.Instance.ReloadBackPanel();
        }
    }

    class QueueTab : TabControl
    {

        TabPage QueueTabPage;
        TabPage LibraryTabPage;
        TabPage AboutTabPage;
        SimpleTextTabWidget AboutTabView;
        RGBA_Bytes unselectedTextColor = ActiveTheme.Instance.TabLabelUnselected;
        GuiWidget addedUpdateMark = null;

        public QueueTab()
        {
            this.TabBar.BackgroundColor = ActiveTheme.Instance.PrimaryBackgroundColor;
            this.TabBar.BorderColor = new RGBA_Bytes(0, 0, 0, 0);
            this.TabBar.Margin = new BorderDouble(0, 0);
            this.TabBar.Padding = new BorderDouble(0, 2);

            QueueTabPage = new TabPage(new QueueControlsWidget(), "Queue");
            this.AddTab(new SimpleTextTabWidget(QueueTabPage, 18,
                    ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes()));

            LibraryTabPage = new TabPage(new PrintLibraryWidget(), "Library");
            this.AddTab(new SimpleTextTabWidget(LibraryTabPage, 18,
                    ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes()));

            AboutTabPage = new TabPage(new AboutPage(), "About");
            AboutTabView = new SimpleTextTabWidget(AboutTabPage, 18,
                        ActiveTheme.Instance.PrimaryTextColor, new RGBA_Bytes(), unselectedTextColor, new RGBA_Bytes());
            this.AddTab(AboutTabView);

            NumQueueItemsChanged(this, null);
            SetUpdateNotification(this, null);

        }

        void NumQueueItemsChanged(object sender, EventArgs widgetEvent)
        {
            string queueStringBeg = new LocalizedString("Queue").Translated;
            string queueString = string.Format("{1} ({0})", PrintQueue.PrintQueueControl.Instance.Count, queueStringBeg);
            QueueTabPage.Text = string.Format(queueString, PrintQueue.PrintQueueControl.Instance.Count);
        }

        event EventHandler unregisterEvents;
        void AddHandlers()
        {
            PrintQueue.PrintQueueControl.Instance.ItemAdded.RegisterEvent(NumQueueItemsChanged, ref unregisterEvents);
            PrintQueue.PrintQueueControl.Instance.ItemRemoved.RegisterEvent(NumQueueItemsChanged, ref unregisterEvents);
            MainSlidePanel.Instance.SetUpdateNotificationTrigger.RegisterEvent(SetUpdateNotification, ref unregisterEvents);            
        }

        public void SetUpdateNotification(object sender, EventArgs widgetEvent)
        {
            if (this.UpdateIsAvailable() || UpdateControl.NeedToCheckForUpdateFirstTimeEver)
            {
#if true
                if (addedUpdateMark == null)
                {
                    UpdateControl.NeedToCheckForUpdateFirstTimeEver = false;
                    addedUpdateMark = new NotificationWidget();
                    addedUpdateMark.OriginRelativeParent = new Vector2(63, 10);
                    AboutTabView.AddChild(addedUpdateMark);
                }
#else
                AboutTabPage.Text = string.Format("About (!)");
#endif
            }
            else
            {
                if (addedUpdateMark != null)
                {
                    addedUpdateMark.Visible = false;
                }
                AboutTabPage.Text = string.Format("About");
            }
        }

        bool UpdateIsAvailable()
        {
            string currentBuildToken = ApplicationSettings.Instance.get("CurrentBuildToken");
            string applicationBuildToken = VersionInfo.Instance.BuildToken;

            if (applicationBuildToken == currentBuildToken || currentBuildToken == null)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }

    class NotificationWidget : GuiWidget
    {
        public NotificationWidget()
            : base(12, 12)
        {
        }

        public override void OnDraw(Graphics2D graphics2D)
        {
            graphics2D.Circle(Width / 2, Height / 2, Width / 2, RGBA_Bytes.White);
            graphics2D.Circle(Width / 2, Height / 2, Width / 2 - 1, RGBA_Bytes.Red);
            graphics2D.FillRectangle(Width / 2 - 1, Height / 2 - 3, Width / 2 + 1, Height / 2 + 3, RGBA_Bytes.White);
            base.OnDraw(graphics2D);
        }
    }
}
