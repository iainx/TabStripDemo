using System;
using System.Collections.Generic;
using AppKit;
using CoreAnimation;
using CoreGraphics;
using Foundation;

namespace tabstrip
{
    public class TabStrip : NSStackView, INSDraggingSource
    {
        public const string TabUTI = "com.microsoft.visualstudio.ShellTab";

        public bool UseLayers { get; set; } = false;
        public NSView LayerHost { get; set; }

        public TabStrip()
        {
            Spacing = 0;
            WantsLayer = true;
        }

        public void AddTab(TabView tabView)
        {
            AddArrangedSubview(tabView);
        }

        bool dragging;
        bool detached;
        TabView draggingView, selectedView;
        int selectedIndex;
        TabViewLayer draggingLayer;
        CGPoint clickOffset;

        CALayer tabStripLayer;
        List<CGRect> tabRects;

        public override void MouseDown(NSEvent theEvent)
        {
            var locationInView = ConvertPointFromView(theEvent.LocationInWindow, null);

            selectedIndex = 0;
            foreach(var view in ArrangedSubviews)
            {
                if (view.Frame.Contains(locationInView))
                {
                    selectedView = view as TabView;
                    break;
                }
                selectedIndex++;
            }

            if (selectedView == null)
            {
                return;
            }

            clickOffset = new CGPoint(selectedView.Frame.X - locationInView.X, selectedView.Frame.Y - locationInView.Y);
        }

        public override void MouseUp(NSEvent theEvent)
        {
            tabStripLayer = new CALayer();
            tabStripLayer.BackgroundColor = NSColor.Green.CGColor;
            tabStripLayer.Frame = Layer.Bounds;
            tabStripLayer.ZPosition = 1;

            Layer.AddSublayer(tabStripLayer);

            tabRects = new List<CGRect>();
            int i = 0;
            TabViewLayer deleteLayer = null;
            foreach (var view in ArrangedSubviews)
            {
                var viewLayer = new TabViewLayer(view);
                viewLayer.ZPosition = view == draggingView ? 2 : 1;

                if (i == 1)
                {
                    deleteLayer = viewLayer;
                }

                tabStripLayer.AddSublayer(viewLayer);

                tabRects.Add(viewLayer.Frame);
                i++;
            }

            deleteLayer.View.RemoveFromSuperview();
            deleteLayer.RemoveFromSuperLayer();

            UpdateLayout();
        }

        // Only care about the first MouseDragged event
        // after that it gets passed over to the Cocoa drag and drop session
        public override void MouseDragged(NSEvent theEvent)
        {
            if (selectedView == null)
            {
                return;
            }

            if (!dragging)
            {
                dragging = true;
                draggingView = selectedView;

                SetupLayerDrag();

                StartDragSession(draggingLayer, theEvent);
            }
        }

        // Set up for a drag session
        void StartDragSession(TabViewLayer tabLayer, NSEvent dragEvent)
        {
            var pbItem = new NSPasteboardItem();
            pbItem.SetStringForType("Internal tab drag", TabUTI);

            var draggingItem = new NSDraggingItem(pbItem);

            var originFrame = ConvertRectFromView(tabLayer.View.Bounds, tabLayer.View);
            draggingItem.DraggingFrame = originFrame;

            var session = BeginDraggingSession(new[] { draggingItem }, dragEvent, this);
            session.AnimatesToStartingPositionsOnCancelOrFail = false;
        }

        [Export("draggingSession:movedToPoint:")]
        public void DraggingSessionMovedToPoint(NSDraggingSession session, CGPoint point)
        {
            var pointInWindow = Window.ConvertPointFromScreen(point);
            var locationInView = ConvertPointFromView(pointInWindow, null);

            if (locationInView.Y <= Bounds.Y - 50 || locationInView.Y >= Bounds.GetMaxY() + 50)
            {
                DetachTab(session, locationInView);
            }
            else
            {
                DragTab(session, locationInView);
            }
        }

        void DragTab(NSDraggingSession session, CGPoint locationInView)
        {
            var frame = draggingLayer.Frame;
            frame.X = locationInView.X + clickOffset.X;

            if (detached)
            {
                selectedIndex = -1;
                ForeachDraggingItem(session, RemoveDragItemImage);
                tabStripLayer.AddSublayer(draggingLayer);
            }
            detached = false;

            {
                CATransaction.Begin();
                CATransaction.DisableActions = true;
                draggingLayer.Frame = frame;
                CATransaction.Commit();
            }

            // Find drop location
            int dropIndex = 0;
            foreach (var rect in tabRects)
            {
                if (draggingLayer.Frame.GetMidX() >= rect.X && draggingLayer.Frame.GetMidX() <= rect.GetMaxX())
                {
                    break;
                }
                dropIndex++;
            }

            dropIndex = Math.Min(dropIndex, tabStripLayer.Sublayers.Length - 1);
            if (dropIndex != selectedIndex)
            {
                draggingLayer.View.RemoveFromSuperview();
                InsertArrangedSubview(draggingLayer.View, dropIndex);

                UpdateLayout();

                selectedIndex = dropIndex;
            }
        }

        void SetupLayerDrag()
        {
            tabStripLayer = new CALayer();
            tabStripLayer.BackgroundColor = NSColor.Blue.CGColor;
            tabStripLayer.Frame = Layer.Bounds;
            tabStripLayer.ZPosition = 1;

            if (UseLayers)
            {
                var layerHost = LayerHost != null ? LayerHost.Layer : Layer;
                layerHost.AddSublayer(tabStripLayer);
            }

            tabRects = new List<CGRect>();
            foreach (var view in ArrangedSubviews)
            {
                var viewLayer = new TabViewLayer(view);
                viewLayer.ZPosition = view == draggingView ? 2 : 1;

                if (view == draggingView)
                {
                    draggingLayer = viewLayer;
                }

                tabStripLayer.AddSublayer(viewLayer);

                tabRects.Add(viewLayer.Frame);
            }
        }

        void UpdateLayout()
        {
            LayoutSubtreeIfNeeded();

            int i = 0;
            var arrangedSubviews = ArrangedSubviews;

            foreach (var layer in tabStripLayer.Sublayers)
            {
                var tab = (TabViewLayer)layer;
                if (tab == draggingLayer)
                {
                    i++;
                    continue;
                }
                var tabFrame = tab.Frame;
                var newX = tab.View.Frame.X;

                tabFrame.X = newX;

                var position = new CGPoint(newX + tabFrame.Width / 2, tabFrame.GetMidY());

                var animation = CABasicAnimation.FromKeyPath("position");
                animation.To = NSValue.FromCGPoint(position);
                animation.From = NSValue.FromCGPoint(tab.Position);

                tab.AddAnimation(animation, "mySlide");
                tab.Position = position;
                i++;
            }
        }

        void DetachTab(NSDraggingSession session, CGPoint locationInView)
        {
            if (!detached)
            {
                ForeachDraggingItem(session, SetDragItemImage);

                draggingLayer.RemoveFromSuperLayer();
                draggingView.RemoveFromSuperview();

                UpdateLayout();

                tabRects = new List<CGRect>();
                foreach(var view in ArrangedSubviews)
                {
                    tabRects.Add(view.Frame);
                }
            }

            detached = true;
        }

        void SetDragItemImage(NSDraggingItem item, nint index, ref bool stop)
        {
            // Set the image for the drag
            item.SetDraggingFrame(item.DraggingFrame, new NSImage(draggingLayer.Contents, draggingLayer.Frame.Size));
        }

        void RemoveDragItemImage(NSDraggingItem item, nint index, ref bool stop)
        {
            // Remove the image from the drag so it doesn't look like a real drag
            item.SetDraggingFrame(item.DraggingFrame, new NSImage(new CGSize(0, 0)));
        }

        void ForeachDraggingItem(NSDraggingSession session, NSDraggingEnumerator enumerator)
        {
            var classes = NSArray.FromIntPtrs(new IntPtr[]
            {
                ObjCRuntime.Class.GetHandle(typeof(NSPasteboardItem))
            });

            session.EnumerateDraggingItems(NSDraggingItemEnumerationOptions.ClearNonenumeratedImages, this, classes, null, enumerator);
        }

        [Export("draggingSession:endedAtPoint:operation:")]
        public void DraggingSessionEndedAtPoint(NSDraggingSession session, CGPoint point, NSDragOperation operation)
        {
            // Reset the drag
            dragging = false;
            detached = false;
            selectedView = draggingView = null;
            draggingLayer = null;

            tabStripLayer.RemoveFromSuperLayer();
            tabStripLayer = null;
        }

        [Export("draggingSession:sourceOperationMaskFor:")]
        public NSDragOperation DraggingSession(NSDraggingSession session, NSDraggingContext context)
        {
            switch (context)
            {
                case NSDraggingContext.OutsideApplication:
                    return NSDragOperation.None;

                case NSDraggingContext.WithinApplication:
                    return NSDragOperation.None;
            }

            return NSDragOperation.None;
        }
    }

    class TabViewLayer : CALayer
    {
        readonly NSView view;
        public NSView View => view;
        public TabViewLayer(NSView view)
        {
            this.view = view;
            var image = view.BitmapImageRepForCachingDisplayInRect(view.Bounds);
            view.CacheDisplay(view.Bounds, image);

            Frame = view.Frame;
            Contents = image.CGImage;
        }
    }
}
