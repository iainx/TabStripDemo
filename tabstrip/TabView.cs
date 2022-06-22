using AppKit;
using CoreGraphics;

namespace tabstrip
{
    public class TabView : NSView
    {
        public bool Dragging { get; set; }
        public NSColor Color { get; set; }
        public float Width { get; set; } = 150;

        public TabView(string title)
        {
            var label = NSTextField.CreateLabel(title);
            label.TranslatesAutoresizingMaskIntoConstraints = false;

            AddSubview(label);
            CenterXAnchor.ConstraintEqualToAnchor(label.CenterXAnchor).Active = true;
            CenterYAnchor.ConstraintEqualToAnchor(label.CenterYAnchor).Active = true;
        }

        public override CGSize IntrinsicContentSize => new CGSize(Width, 32);

        public override void DrawRect(CGRect dirtyRect)
        {
            Color.Set();
            NSBezierPath.FillRect(dirtyRect);
        }
    }
}
