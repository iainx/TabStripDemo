using System;

using AppKit;
using Foundation;

namespace tabstrip
{
    public partial class ViewController : NSViewController
    {
        public ViewController(IntPtr handle) : base(handle)
        {
        }

        TabStrip tabStrip = new TabStrip();

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            View.WantsLayer = true;

            // Do any additional setup after loading the view.
            View.AddSubview(tabStrip);

            tabStrip.TranslatesAutoresizingMaskIntoConstraints = false;
            tabStrip.LeadingAnchor.ConstraintEqualToAnchor(View.LeadingAnchor, 20).Active = true;
            tabStrip.TrailingAnchor.ConstraintEqualToAnchor(View.TrailingAnchor, -20).Active = true;
            tabStrip.HeightAnchor.ConstraintEqualToConstant(32).Active = true;
            tabStrip.TopAnchor.ConstraintEqualToAnchor(View.TopAnchor, 20).Active = true;

            tabStrip.AddTab(new TabView("Tab 1") { Color = NSColor.SystemBlueColor });
            tabStrip.AddTab(new TabView("Tab 2") { Color = NSColor.SystemRedColor });
            tabStrip.AddTab(new TabView("Tab 3") { Color = NSColor.SystemOrangeColor });

            var layerHost = new NSView()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                WantsLayer = true
            };
            View.AddSubview(layerHost);
            tabStrip.LeadingAnchor.ConstraintEqualToAnchor(layerHost.LeadingAnchor).Active = true;
            tabStrip.TrailingAnchor.ConstraintEqualToAnchor(layerHost.TrailingAnchor).Active = true;
            tabStrip.BottomAnchor.ConstraintEqualToAnchor(layerHost.TopAnchor, -8).Active = true;
            layerHost.HeightAnchor.ConstraintEqualToConstant(32).Active = true;

            var button = new NSButton()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Title = "Use Layers",
            };
            button.SetButtonType(NSButtonType.Switch);
            button.Activated += (o, e) =>
            {
                tabStrip.UseLayers = button.State == NSCellStateValue.On;
            };

            View.AddSubview(button);
            View.LeadingAnchor.ConstraintEqualToAnchor(button.LeadingAnchor, -20).Active = true;
            View.BottomAnchor.ConstraintEqualToAnchor(button.BottomAnchor, 20).Active = true;

            var button2 = new NSButton()
            {
                TranslatesAutoresizingMaskIntoConstraints = false,
                Title = "Show Layers"
            };
            button2.SetButtonType(NSButtonType.Switch);
            button2.Activated += (o, e) =>
            {
                tabStrip.LayerHost = button2.State == NSCellStateValue.On ? layerHost : null;
            };
            View.AddSubview(button2);
            button.TrailingAnchor.ConstraintEqualToAnchor(button2.LeadingAnchor, -20).Active = true;
            View.BottomAnchor.ConstraintEqualToAnchor(button2.BottomAnchor, 20).Active = true;
        }

        public override NSObject RepresentedObject
        {
            get
            {
                return base.RepresentedObject;
            }
            set
            {
                base.RepresentedObject = value;
                // Update the view, if already loaded.
            }
        }
    }
}
