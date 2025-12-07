using UnityEngine;
using System.Collections.Generic;

namespace Flexalon
{
    /// <summary>
    /// Use a flexible layout to position children linearly along the x, y, or z axis.
    /// The sizes of the children are considered so that they are evenly spaced.
    /// </summary>
    [AddComponentMenu("Flexalon/Flexalon Flexible Layout"), HelpURL("https://www.flexalon.com/docs/flexibleLayout")]
    public class FlexalonFlexibleLayout : LayoutBase
    {
        /// <summary> Determines how the space between children is distributed. </summary>
        public enum GapOptions
        {
            /// <summary> The Gap/WrapGap property determines the space between children. </summary>
            Fixed,

            /// <summary> Space is added between children to fill the available space. </summary>
            SpaceBetween,

            /// <summary>  </summary>
            SpaceAround,

            /// <summary> Space is added between children to fill the available space. </summary>
            SpaceEvenly
        }

        [SerializeField]
        private Direction _direction = Direction.PositiveX;
        /// <summary> The direction in which objects are placed, one after the other. </summary>
        public Direction Direction
        {
            get { return _direction; }
            set { _direction = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private bool _wrap;
        /// <summary> If set, then the flexible layout will attempt to position children in a line
        /// along the Direction axis until it runs out of space. Then it will start the next line by
        /// following the wrap direction. Wrapping will only occur if the size of the Direction axis is
        /// set to any value other than "Layout". </summary>
        public bool Wrap
        {
            get { return _wrap; }
            set { _wrap = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Direction _wrapDirection = Direction.NegativeY;
        /// <summary> The direction to start a new line when wrapping. </summary>
        public Direction WrapDirection
        {
            get { return _wrapDirection; }
            set { _wrapDirection = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _horizontalAlign = Align.Center;
        /// <summary> Determines how the entire layout horizontally aligns to the parent's box. </summary>
        public Align HorizontalAlign
        {
            get { return _horizontalAlign; }
            set { _horizontalAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalAlign = Align.Center;
        /// /// <summary> Determines how the entire layout vertically aligns to the parent's box. </summary>
        public Align VerticalAlign
        {
            get { return _verticalAlign; }
            set { _verticalAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _depthAlign = Align.Center;
        /// <summary> Determines how the entire layout aligns to the parent's box in depth. </summary>
        public Align DepthAlign
        {
            get { return _depthAlign; }
            set { _depthAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _horizontalInnerAlign = Align.Center;
        /// <summary> The inner align property along the Direction axis will change how wrapped lines align
        /// with each other. The inner align property along the other two axes will change how each object lines
        /// up with all other objects. </summary>
        public Align HorizontalInnerAlign
        {
            get { return _horizontalInnerAlign; }
            set { _horizontalInnerAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _verticalInnerAlign = Align.Center;
        /// <summary> The inner align property along the Direction axis will change how wrapped lines align
        /// with each other. The inner align property along the other two axes will change how each object lines
        /// up with all other objects. </summary>
        public Align VerticalInnerAlign
        {
            get { return _verticalInnerAlign; }
            set { _verticalInnerAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private Align _depthInnerAlign = Align.Center;
        /// <summary> The inner align property along the Direction axis will change how wrapped lines align
        /// with each other. The inner align property along the other two axes will change how each object lines
        /// up with all other objects. </summary>
        public Align DepthInnerAlign
        {
            get { return _depthInnerAlign; }
            set { _depthInnerAlign = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private GapOptions _gapType = GapOptions.Fixed;
        /// <summary> Determines how the space between children is distributed. </summary>
        public GapOptions GapType
        {
            get { return _gapType; }
            set { _gapType = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _gap;
        /// <summary> Adds a gap between objects on the Direction axis. </summary>
        public float Gap
        {
            get { return _gap; }
            set
            {
                _gap = value;
                _gapType = GapOptions.Fixed;
                Node.MarkDirty();
            }
        }

        [SerializeField]
        private GapOptions _wrapGapType = GapOptions.Fixed;
        /// <summary> Determines how the space between lines is distributed. </summary>
        public GapOptions WrapGapType
        {
            get { return _wrapGapType; }
            set { _wrapGapType = value; Node.MarkDirty(); }
        }

        [SerializeField]
        private float _wrapGap;
        /// <summary> Adds a gap between objects on the Wrap Direction axis. </summary>
        public float WrapGap
        {
            get { return _wrapGap; }
            set
            {
                _wrapGap = value;
                _wrapGapType = GapOptions.Fixed;
                Node.MarkDirty();
            }
        }

        private class Line
        {
            public Vector3 Size = Vector3.zero;
            public Vector3 Position = Vector3.zero;
            public List<FlexalonNode> Children = new List<FlexalonNode>();
            public List<Vector3> ChildSizes = new List<Vector3>();
            public List<Vector3> ChildPositions = new List<Vector3>();
        }

        private List<Line> _lines = new List<Line>();
        private List<FlexItem> _flexItems = new List<FlexItem>();

        private void CreateLines(FlexalonNode node, int flexAxis, int wrapAxis, int thirdAxis, bool wrap, Vector3 size, float maxLineSize, bool measure)
        {
            _lines.Clear();
            if (node.Children.Count == 0)
            {
                return;
            }

            // Divide children into lines considering: size, child sizes.
            var line = new Line();
            _lines.Add(line);
            bool addGap = false;
            int i = 0;
            foreach (var child in node.Children)
            {
                var gap = (addGap && _gapType == GapOptions.Fixed ? _gap : 0);
                var childSize = measure ? child.GetMeasureSize(size) : child.GetArrangeSize();
                if (line.ChildSizes.Count > 0 && wrap &&
                    line.Size[flexAxis] + childSize[flexAxis] + gap > maxLineSize)
                {
                    line = new Line();
                    _lines.Add(line);
                    addGap = false;
                    gap = 0;
                    i++;
                }

                FlexalonLog.Log("Flex | Add child to line", child, i);
                FlexalonLog.Log("Flex | Child Size", child, childSize);
                line.ChildSizes.Add(childSize);
                line.Size[flexAxis] += childSize[flexAxis] + gap;
                line.Size[wrapAxis] = Mathf.Max(line.Size[wrapAxis], childSize[wrapAxis]);
                line.Size[thirdAxis] = Mathf.Max(line.Size[thirdAxis], childSize[thirdAxis]);
                line.Children.Add(child);
                addGap = true;
            }
        }

        private Vector3 MeasureTotalLineSize(bool wrap, int flexAxis, int wrapAxis, int thirdAxis)
        {
            Vector3 layoutSize = Vector3.zero;
            foreach (var line in _lines)
            {
                if (wrap)
                {
                    layoutSize[flexAxis] = Mathf.Max(layoutSize[flexAxis], line.Size[flexAxis]);
                    layoutSize[wrapAxis] += line.Size[wrapAxis];
                    layoutSize[thirdAxis] = Mathf.Max(layoutSize[thirdAxis], line.Size[thirdAxis]);
                }
                else
                {
                    for (int axis = 0; axis < 3; axis++)
                    {
                        layoutSize[axis] = Mathf.Max(layoutSize[axis], line.Size[axis]);
                    }
                }
            }

            if (wrap && _wrapGapType == GapOptions.Fixed)
            {
                layoutSize[wrapAxis] += _wrapGap * (_lines.Count - 1);
            }

            return layoutSize;
        }

        private Vector3 MeasureLayoutSize(FlexalonNode node, bool wrap, int flexAxis, int wrapAxis, int thirdAxis, Vector3 size, Vector3 min, Vector3 max)
        {
            var layoutSize = MeasureTotalLineSize(wrap, flexAxis, wrapAxis, thirdAxis);

            for (int axis = 0; axis < 3; axis++)
            {
                if (node.GetSizeType((Axis)axis) == SizeType.Layout)
                {
                    layoutSize[axis] = Mathf.Clamp(layoutSize[axis], min[axis], max[axis]);
                }
                else
                {
                    layoutSize[axis] = size[axis];
                }
            }

            return layoutSize;
        }

        private void SetChildSize(Line line, int index, int axis, float size, float layoutSize)
        {
            var childSize = line.ChildSizes[index];
            line.Children[index].SetShrinkFillSize(axis, size, layoutSize, true);
            childSize[axis] = size;
            line.ChildSizes[index] = childSize;
        }

        private void FillFlexAxis(float size, int flexAxis)
        {
            var gap = _gapType == GapOptions.Fixed ? _gap : 0;

            foreach (var line in _lines)
            {
                _flexItems.Clear();
                for (int i = 0; i < line.Children.Count; i++)
                {
                    _flexItems.Add(Flex.CreateFlexItem(
                        line.Children[i], flexAxis, line.ChildSizes[i][flexAxis], line.Size[flexAxis], size));
                }

                Flex.GrowOrShrink(_flexItems, line.Size[flexAxis], size, gap);

                for (int i = 0; i < line.Children.Count; i++)
                {
                    SetChildSize(line, i, flexAxis, _flexItems[i].FinalSize, size);
                }
            }
        }

        private void FillWrapAxis(float size, int wrapAxis)
        {
            _flexItems.Clear();
            float remainingSpace = size;
            var gap = _wrapGapType == GapOptions.Fixed ? _wrapGap : 0;

            foreach (var line in _lines)
            {
                var item = new FlexItem();
                item.StartSize = line.Size[wrapAxis];
                item.MaxSize = line.Children[0].GetMaxSize(wrapAxis, size);
                item.ShrinkFactor = line.Size[wrapAxis] / size;
                item.FinalSize = line.Size[wrapAxis];

                for (int i = 0; i < line.Children.Count; i++)
                {
                    var child = line.Children[i];
                    if (child.CanShrink(wrapAxis))
                    {
                        item.MinSize = Mathf.Max(child.GetMinSize(wrapAxis, size), item.MinSize);
                    }
                    else
                    {
                        item.MinSize = Mathf.Max(line.ChildSizes[i][wrapAxis], item.MinSize);
                    }

                    item.MaxSize = Mathf.Max(child.GetMaxSize(wrapAxis, size), item.MaxSize);

                    if (child.GetSizeType(wrapAxis) == SizeType.Fill)
                    {
                        item.GrowFactor = Mathf.Max(child.SizeOfParent[wrapAxis], item.GrowFactor);
                    }
                }

                remainingSpace -= line.Size[wrapAxis];
                _flexItems.Add(item);
            }

            if (Mathf.Abs(remainingSpace) > 1e-6)
            {
                Flex.GrowOrShrink(_flexItems, size - remainingSpace, size, gap);
            }

            for (int l = 0; l < _lines.Count; l++)
            {
                var line = _lines[l];
                var item = _flexItems[l];

                for (int i = 0; i < line.Children.Count; i++)
                {
                    var child = line.Children[i];
                    if (child.GetSizeType(wrapAxis) == SizeType.Fill)
                    {
                        var newSize = child.SizeOfParent[wrapAxis] * size;
                        var minSize = child.GetMinSize(wrapAxis, size);
                        var maxSize = child.GetMaxSize(wrapAxis, size);
                        maxSize = Mathf.Min(maxSize, item.FinalSize);
                        newSize = Mathf.Clamp(newSize, minSize, maxSize);
                        SetChildSize(line, i, wrapAxis, newSize, size);
                    }
                    else
                    {
                        SetChildSize(line, i, wrapAxis, item.FinalSize, size);
                    }
                }
            }
        }

        private void FillThirdAxis(float size, int thirdAxis)
        {
            foreach (var child in Node.Children)
            {
                child.SetShrinkFillSize(thirdAxis, size, size);
            }
        }

        private void UpdateFillSizes(Vector3 size, int flexAxis, int wrapAxis, int thirdAxis)
        {
            FillFlexAxis(size[flexAxis], flexAxis);
            FillWrapAxis(size[wrapAxis], wrapAxis);
            FillThirdAxis(size[thirdAxis], thirdAxis);
        }

        /// <inheritdoc />
        public override Bounds Measure(FlexalonNode node, Vector3 size, Vector3 min, Vector3 max)
        {
            FlexalonLog.Log("FlexMeasure", node, size, min, max);

            // Gather useful data
            var flexAxis = (int) Math.GetAxisFromDirection(_direction);
            var otherAxes = Math.GetOtherAxes(flexAxis);
            bool childrenSizeFlexAxis = node.GetSizeType(flexAxis) == SizeType.Layout;
            var wrapAxis = (int) Math.GetAxisFromDirection(_wrapDirection);
            if (wrapAxis == flexAxis)
            {
                wrapAxis = otherAxes.Item1;
            }

            var thirdAxis = (wrapAxis == otherAxes.Item1 ? otherAxes.Item2 : otherAxes.Item1);
            bool wrap = (flexAxis != wrapAxis) && _wrap;
            var maxLineSize = childrenSizeFlexAxis ? max[flexAxis] : size[flexAxis];

            FlexalonLog.Log("FlexMeasure | Flex Axis", node,  flexAxis);
            FlexalonLog.Log("FlexMeasure | Wrap Axis", node,  wrapAxis);
            FlexalonLog.Log("FlexMeasure | Third Axis", node,  thirdAxis);
            FlexalonLog.Log("FlexMeasure | Wrap", node, wrap);

            CreateLines(node, flexAxis, wrapAxis, thirdAxis, wrap, size, maxLineSize, true);
            for (int i = 0; i < _lines.Count; i++)
            {
                FlexalonLog.Log("FlexMeasure | Line size " + i + " " + _lines[i].Size);
            }

            Vector3 layoutSize = MeasureLayoutSize(node, wrap, flexAxis, wrapAxis, thirdAxis, size, min, max);
            FlexalonLog.Log("FlexMeasure | Total Layout Size", node, layoutSize);

            UpdateFillSizes(layoutSize, flexAxis, wrapAxis, thirdAxis);

            return new Bounds(Vector3.zero, layoutSize);
        }

        /// <inheritdoc />
        public override void Arrange(FlexalonNode node, Vector3 layoutSize)
        {
            FlexalonLog.Log("FlexArrange | LayoutSize", node, layoutSize);

            // Gather useful data
            var flexAxis = (int) Math.GetAxisFromDirection(_direction);
            bool childrenSizeFlexAxis = node.GetSizeType(flexAxis) == SizeType.Layout;
            var otherAxes = Math.GetOtherAxes(flexAxis);
            var wrapAxis = (int) Math.GetAxisFromDirection(_wrapDirection);
            if (wrapAxis == flexAxis)
            {
                wrapAxis = otherAxes.Item1;
            }

            var thirdAxis = (wrapAxis == otherAxes.Item1 ? otherAxes.Item2 : otherAxes.Item1);
            bool wrap = (flexAxis != wrapAxis) && _wrap;
            var flexDirection = Math.GetPositiveFromDirection(_direction);
            var wrapDirection = Math.GetPositiveFromDirection(_wrapDirection);
            var align = new Align[] { _horizontalAlign, _verticalAlign, _depthAlign };
            var innerAlign = new Align[] { _horizontalInnerAlign, _verticalInnerAlign, _depthInnerAlign };

            FlexalonLog.Log("FlexArrange | Flex Direction", node, _direction);
            FlexalonLog.Log("FlexArrange | Wrap Direction", node, _wrapDirection);
            FlexalonLog.Log("FlexArrange | Third Axis", node, thirdAxis);
            FlexalonLog.Log("FlexArrange | Wrap", node, wrap);

            CreateLines(node, flexAxis, wrapAxis, thirdAxis, wrap, layoutSize, layoutSize[flexAxis] + 1e-4f, false);

            // Position children within _lines. Consider: line size, child size, flexInnerAlign
            {
                foreach (var line in _lines)
                {
                    float lineGap = 0;
                    float startGap = 0;
                    if (line.Children.Count > 1)
                    {
                        switch (_gapType)
                        {
                            case GapOptions.Fixed:
                                lineGap = _gap;
                                break;
                            case GapOptions.SpaceBetween:
                                lineGap = (layoutSize[flexAxis] - line.Size[flexAxis]) / (line.Children.Count - 1);
                                break;
                            case GapOptions.SpaceAround:
                                lineGap = (layoutSize[flexAxis] - line.Size[flexAxis]) / line.Children.Count;
                                startGap = lineGap / 2;
                                break;
                            case GapOptions.SpaceEvenly:
                                lineGap = (layoutSize[flexAxis] - line.Size[flexAxis]) / (line.Children.Count + 1);
                                startGap = lineGap;
                                break;
                        }

                        if (_gapType != GapOptions.Fixed)
                        {
                            line.Size[flexAxis] = layoutSize[flexAxis];
                        }
                    }

                    float nextChildPosition = flexDirection * (startGap + -line.Size[flexAxis] / 2);
                    foreach (var childSize in line.ChildSizes)
                    {
                        Vector3 childPosition = Vector3.zero;
                        childPosition[flexAxis] = nextChildPosition + flexDirection * childSize[flexAxis] / 2;
                        childPosition[otherAxes.Item1] = Math.Align(
                            childSize, line.Size, otherAxes.Item1, innerAlign[otherAxes.Item1]);
                        childPosition[otherAxes.Item2] = Math.Align(
                            childSize, line.Size, otherAxes.Item2, innerAlign[otherAxes.Item2]);
                        line.ChildPositions.Add(childPosition);
                        nextChildPosition += flexDirection * (childSize[flexAxis] + lineGap);
                    }
                }
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                for (int j = 0; j < _lines[i].ChildPositions.Count; j++)
                {
                    FlexalonLog.Log("FlexArrange | Child Size", _lines[i].Children[j], _lines[i].ChildSizes[j]);
                    FlexalonLog.Log("FlexArrange | Child Position", _lines[i].Children[j], _lines[i].ChildPositions[j]);
                }
            }

            Vector3 totalLineSize = MeasureTotalLineSize(wrap, flexAxis, wrapAxis, thirdAxis);
            FlexalonLog.Log("FlexArrange | Total Line Size", node, totalLineSize);

            // Position lines in total line size, consider: totalLineSize, innerAlign
            {
                if (wrap)
                {
                    float wrapGap = 0;
                    float startWrapGap = 0;
                    if (_lines.Count > 1)
                    {
                        switch (_wrapGapType)
                        {
                            case GapOptions.Fixed:
                                wrapGap = _wrapGap;
                                break;
                            case GapOptions.SpaceBetween:
                                wrapGap = (layoutSize[wrapAxis] - totalLineSize[wrapAxis]) / (_lines.Count - 1);
                                break;
                            case GapOptions.SpaceAround:
                                wrapGap = (layoutSize[wrapAxis] - totalLineSize[wrapAxis]) / _lines.Count;
                                startWrapGap = wrapGap / 2;
                                break;
                            case GapOptions.SpaceEvenly:
                                wrapGap = (layoutSize[wrapAxis] - totalLineSize[wrapAxis]) / (_lines.Count + 1);
                                startWrapGap = wrapGap;
                                break;
                        }

                        if (_wrapGapType != GapOptions.Fixed)
                        {
                            totalLineSize[wrapAxis] = layoutSize[wrapAxis];
                        }
                    }

                    float nextLinePosition = wrapDirection * (startWrapGap + -totalLineSize[wrapAxis] / 2);
                    foreach (var line in _lines)
                    {
                        line.Position[wrapAxis] = nextLinePosition + wrapDirection * line.Size[wrapAxis] / 2;
                        line.Position[flexAxis] = Math.Align(
                            line.Size, totalLineSize, flexAxis, innerAlign[flexAxis]);
                        line.Position[thirdAxis] = Math.Align(
                            line.Size, totalLineSize, thirdAxis, innerAlign[thirdAxis]);
                        nextLinePosition += wrapDirection * line.Size[wrapAxis] + wrapGap * wrapDirection;
                    }
                }
                else
                {
                    for (int axis = 0; axis < 3; axis++)
                    {
                        _lines[0].Position[axis] = Math.Align(
                            _lines[0].Size, totalLineSize, axis, innerAlign[axis]);
                    }
                }
            }

            for (int i = 0; i < _lines.Count; i++)
            {
                FlexalonLog.Log("FlexArrange | Line position " + i + " " + _lines[i].Position);
            }

            // Align the total line size within the size
            Vector3 alignOffset = Vector3.zero;
            for (int axis = 0; axis < 3; axis++)
            {
                alignOffset[axis] = Math.Align(totalLineSize, layoutSize, axis, align[axis]);
            }

            FlexalonLog.Log("FlexArrange | alignOffset", node, alignOffset);

            // Assign final child positions
            int childIndex = 0;
            foreach (var line in _lines)
            {
                foreach (var childPosition in line.ChildPositions)
                {
                    var child = node.Children[childIndex];
                    var result = alignOffset + line.Position + childPosition;
                    child.SetPositionResult(result);
                    child.SetRotationResult(Quaternion.identity);
                    FlexalonLog.Log("FlexArrange | FinalChildPosition", child, result);
                    childIndex++;
                }
            }

            _lines.Clear();
        }
    }
}