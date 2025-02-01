namespace MapGeneration.Plot;

using System.Linq;

using GeneralAlgorithms.DataStructures.Common;

using MapGeneration.Core.Doors;
using MapGeneration.Interfaces.Core.MapLayouts;

using ScottPlot;

public static class PlotExtension
{
    public static Plot ToPlot<TNode>(this IMapLayout<TNode> layout)
    {
        var plot = new Plot();
        plot.Axes.SetLimits(-10, 10, -10, 10);
        plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
        plot.Axes.Left.TickGenerator = new ScottPlot.TickGenerators.NumericFixedInterval(1);
        foreach (var room in layout.Rooms)
        {
            var polygon = room.Shape + room.Position;
            var points = polygon.GetPoints().Select(p => new Coordinates(p.X, p.Y)).ToArray();
            var plotPoly = plot.Add.Polygon(points);

            if (room.IsCorridor) continue;

            var doors = DoorHandler.DefaultHandler.GetDoorPositions(polygon, room.RoomDescription.DoorsMode);
            foreach (var door in doors)
            {
                var line = door.Line;
                var from = line.From;
                var to = line.To + door.Length * line.GetDirectionVector();
                var plotLine = plot.Add.Line(from.X, from.Y, to.X, to.Y);
                plotLine.MarkerShape = MarkerShape.FilledCircle;
                plotLine.MarkerColor = plotPoly.LineColor;
                plotLine.LineColor = plotPoly.LineColor;
                plotLine.LineWidth = 5;
            }

            var tmp = polygon.BoundingRectangle.A + polygon.BoundingRectangle.B;
            var text = plot.Add.Text(room.Node.ToString(), new(tmp.X / 2f, tmp.Y / 2f));
            text.LabelAlignment = Alignment.MiddleCenter;
            text.LabelBold = true;
        }
        return plot;
    }
}
