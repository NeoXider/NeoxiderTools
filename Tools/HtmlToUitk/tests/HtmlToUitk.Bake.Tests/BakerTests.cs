using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace HtmlToUitk.Bake.Tests
{
    public class DMathTests
    {
        [TestCase(0.0)]
        [TestCase(0.5)]
        [TestCase(1.0)]
        [TestCase(2.0)]
        [TestCase(3.0)]
        [TestCase(-1.0)]
        [TestCase(-2.5)]
        [TestCase(10.0)]
        [TestCase(-10.0)]
        [TestCase(100.0)]
        [TestCase(6.283185307179586)]
        public void SinMatchesSystemMath(double x)
        {
            Assert.That(DMath.Sin(x), Is.EqualTo(Math.Sin(x)).Within(1e-9));
        }

        [TestCase(0.0)]
        [TestCase(0.7)]
        [TestCase(1.5707963267948966)]
        [TestCase(3.14159)]
        [TestCase(-4.0)]
        [TestCase(25.0)]
        public void CosMatchesSystemMath(double x)
        {
            Assert.That(DMath.Cos(x), Is.EqualTo(Math.Cos(x)).Within(1e-9));
        }

        [TestCase(0.0)]
        [TestCase(0.1)]
        [TestCase(0.5)]
        [TestCase(1.0)]
        [TestCase(2.0)]
        [TestCase(50.0)]
        [TestCase(-0.3)]
        [TestCase(-7.0)]
        public void AtanMatchesSystemMath(double x)
        {
            Assert.That(DMath.Atan(x), Is.EqualTo(Math.Atan(x)).Within(1e-9));
        }

        [TestCase(1.0, 1.0)]
        [TestCase(1.0, -1.0)]
        [TestCase(-1.0, -1.0)]
        [TestCase(-1.0, 1.0)]
        [TestCase(0.0, 1.0)]
        [TestCase(0.0, -1.0)]
        [TestCase(1.0, 0.0)]
        [TestCase(-1.0, 0.0)]
        [TestCase(0.0, 0.0)]
        [TestCase(3.0, -4.0)]
        public void Atan2MatchesSystemMath(double y, double x)
        {
            Assert.That(DMath.Atan2(y, x), Is.EqualTo(Math.Atan2(y, x)).Within(1e-9));
        }

        [TestCase(0.5, 1.0)]
        [TestCase(1.5, 2.0)]
        [TestCase(-0.5, 0.0)]
        [TestCase(2.4, 2.0)]
        public void RoundIsHalfUp(double value, double expected)
        {
            Assert.That(DMath.Round(value), Is.EqualTo(expected));
        }

        [Test]
        public void ClampKeepsRange()
        {
            Assert.That(DMath.Clamp(5, 0, 3), Is.EqualTo(3));
            Assert.That(DMath.Clamp(-5, 0, 3), Is.EqualTo(0));
            Assert.That(DMath.Clamp(2, 0, 3), Is.EqualTo(2));
        }
    }

    public class GradientTests
    {
        private static readonly Rgba8 Black = new Rgba8(0, 0, 0, 255);
        private static readonly Rgba8 White = new Rgba8(255, 255, 255, 255);
        private static readonly Rgba8 Red = new Rgba8(255, 0, 0, 255);
        private static readonly Rgba8 Blue = new Rgba8(0, 0, 255, 255);
        private static readonly Rgba8 Clear = new Rgba8(0, 0, 0, 0);

        private static GradientSpec Linear(double angle, params GradientStop[] stops)
        {
            return new GradientSpec { Kind = GradientKind.Linear, AngleDegrees = angle, Stops = stops };
        }

        [Test]
        public void ToBottomRunsTopToBottom()
        {
            RgbaImage image = GradientBaker.Bake(Linear(180, new GradientStop(0, Black), new GradientStop(1, White)), 4, 100);
            Assert.That(image.Get(0, 0).R, Is.LessThan(5));
            Assert.That(image.Get(0, 99).R, Is.GreaterThan(250));
            Assert.That(image.Get(0, 50).R, Is.InRange(124, 131));
            Assert.That(image.Get(3, 50).R, Is.EqualTo(image.Get(0, 50).R));
        }

        [Test]
        public void ToTopRunsBottomToTop()
        {
            RgbaImage image = GradientBaker.Bake(Linear(0, new GradientStop(0, Black), new GradientStop(1, White)), 4, 100);
            Assert.That(image.Get(0, 0).R, Is.GreaterThan(250));
            Assert.That(image.Get(0, 99).R, Is.LessThan(5));
        }

        [Test]
        public void ToRightRunsLeftToRight()
        {
            RgbaImage image = GradientBaker.Bake(Linear(90, new GradientStop(0, Black), new GradientStop(1, White)), 100, 4);
            Assert.That(image.Get(0, 0).R, Is.LessThan(5));
            Assert.That(image.Get(99, 0).R, Is.GreaterThan(250));
            Assert.That(image.Get(50, 3).R, Is.EqualTo(image.Get(50, 0).R));
        }

        [Test]
        public void ToLeftRunsRightToLeft()
        {
            RgbaImage image = GradientBaker.Bake(Linear(270, new GradientStop(0, Black), new GradientStop(1, White)), 100, 4);
            Assert.That(image.Get(0, 0).R, Is.GreaterThan(250));
            Assert.That(image.Get(99, 0).R, Is.LessThan(5));
        }

        [Test]
        public void DiagonalCornersHitEndColours()
        {
            RgbaImage image = GradientBaker.Bake(Linear(135, new GradientStop(0, Black), new GradientStop(1, White)), 64, 64);
            Assert.That(image.Get(0, 0).R, Is.LessThan(4));
            Assert.That(image.Get(63, 63).R, Is.GreaterThan(251));
            Assert.That(image.Get(63, 0).R, Is.InRange(120, 135));
        }

        [Test]
        public void StopsOutsideRangeClamp()
        {
            RgbaImage image = GradientBaker.Bake(Linear(180, new GradientStop(0.25, Black), new GradientStop(0.75, White)), 2, 100);
            Assert.That(image.Get(0, 5), Is.EqualTo(Black));
            Assert.That(image.Get(0, 95), Is.EqualTo(White));
        }

        [Test]
        public void HardStopsSwitchAtPosition()
        {
            var spec = Linear(90, new GradientStop(0, Red), new GradientStop(0.5, Red), new GradientStop(0.5, Blue), new GradientStop(1, Blue));
            RgbaImage image = GradientBaker.Bake(spec, 100, 1);
            Assert.That(image.Get(10, 0), Is.EqualTo(Red));
            Assert.That(image.Get(49, 0), Is.EqualTo(Red));
            Assert.That(image.Get(50, 0), Is.EqualTo(Blue));
            Assert.That(image.Get(90, 0), Is.EqualTo(Blue));
        }

        [Test]
        public void AlphaInterpolatesPremultiplied()
        {
            RgbaImage image = GradientBaker.Bake(Linear(90, new GradientStop(0, Clear), new GradientStop(1, Red)), 101, 1);
            Rgba8 mid = image.Get(50, 0);
            Assert.That(mid.R, Is.EqualTo(255));
            Assert.That(mid.G, Is.EqualTo(0));
            Assert.That(mid.A, Is.InRange(126, 129));
        }

        [Test]
        public void FullyTransparentPixelsHaveZeroColour()
        {
            RgbaImage image = GradientBaker.Bake(Linear(90, new GradientStop(0, Clear), new GradientStop(1, Clear)), 4, 1);
            Assert.That(image.Get(2, 0), Is.EqualTo(Clear));
        }

        [Test]
        public void RepeatingLinearWraps()
        {
            var spec = Linear(90, new GradientStop(0, Black), new GradientStop(0.25, White));
            spec.Repeating = true;
            RgbaImage image = GradientBaker.Bake(spec, 100, 1);
            Assert.That(image.Get(0, 0).R, Is.LessThan(12));
            Assert.That(image.Get(24, 0).R, Is.GreaterThan(240));
            Assert.That(image.Get(26, 0).R, Is.LessThan(25));
            Assert.That(image.Get(49, 0).R, Is.GreaterThan(240));
        }

        [Test]
        public void RadialCentreAndEdges()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Radial,
                Shape = RadialShape.Circle,
                Extent = RadialExtent.ClosestSide,
                Stops = new[] { new GradientStop(0, White), new GradientStop(1, Black) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 101, 101);
            Assert.That(image.Get(50, 50).R, Is.GreaterThan(250));
            Assert.That(image.Get(0, 50).R, Is.LessThan(4));
            Assert.That(image.Get(0, 0), Is.EqualTo(Black));
            Assert.That(image.Get(75, 50).R, Is.InRange(120, 135));
        }

        [Test]
        public void RadialFarthestCornerReachesCorners()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Radial,
                Shape = RadialShape.Circle,
                Extent = RadialExtent.FarthestCorner,
                Stops = new[] { new GradientStop(0, White), new GradientStop(1, Black) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 100, 100);
            Assert.That(image.Get(0, 0).R, Is.LessThan(4));
            Assert.That(image.Get(99, 99).R, Is.LessThan(4));
            Assert.That(image.Get(0, 50).R, Is.GreaterThan(60));
        }

        [Test]
        public void RadialEllipseStretchesWithBox()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Radial,
                Shape = RadialShape.Ellipse,
                Extent = RadialExtent.ClosestSide,
                Stops = new[] { new GradientStop(0, White), new GradientStop(1, Black) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 200, 100);
            Assert.That(image.Get(0, 50).R, Is.LessThan(4));
            Assert.That(image.Get(100, 0).R, Is.LessThan(4));
            Assert.That(image.Get(100, 50).R, Is.GreaterThan(250));
        }

        [Test]
        public void RadialExplicitRadius()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Radial,
                Shape = RadialShape.Circle,
                Extent = RadialExtent.Explicit,
                RadiusX = 20,
                Stops = new[] { new GradientStop(0, White), new GradientStop(1, Black) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 100, 100);
            Assert.That(image.Get(50, 50).R, Is.GreaterThan(240));
            Assert.That(image.Get(80, 50), Is.EqualTo(Black));
        }

        [Test]
        public void RadialOffCentre()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Radial,
                Shape = RadialShape.Circle,
                Extent = RadialExtent.ClosestSide,
                CenterX = 0.25,
                CenterY = 0.5,
                Stops = new[] { new GradientStop(0, White), new GradientStop(1, Black) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 100, 100);
            Assert.That(image.Get(25, 50).R, Is.GreaterThan(240));
            Assert.That(image.Get(25, 5), Is.EqualTo(Black));
        }

        [Test]
        public void ConicSweepsClockwiseFromTop()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Conic,
                Stops = new[] { new GradientStop(0, Black), new GradientStop(1, White) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 100, 100);
            Assert.That(image.Get(51, 5).R, Is.LessThan(12));
            Assert.That(image.Get(94, 50).R, Is.InRange(58, 70));
            Assert.That(image.Get(50, 94).R, Is.InRange(120, 135));
            Assert.That(image.Get(5, 50).R, Is.InRange(185, 198));
            Assert.That(image.Get(48, 5).R, Is.GreaterThan(240));
        }

        [Test]
        public void ConicFromAngleRotates()
        {
            var spec = new GradientSpec
            {
                Kind = GradientKind.Conic,
                AngleDegrees = 90,
                Stops = new[] { new GradientStop(0, Black), new GradientStop(1, White) }
            };
            RgbaImage image = GradientBaker.Bake(spec, 100, 100);
            Assert.That(image.Get(94, 51).R, Is.LessThan(12));
            Assert.That(image.Get(94, 48).R, Is.GreaterThan(240));
        }

        [Test]
        public void ThreeStopsInterpolatePiecewise()
        {
            var spec = Linear(90, new GradientStop(0, Red), new GradientStop(0.5, White), new GradientStop(1, Blue));
            RgbaImage image = GradientBaker.Bake(spec, 201, 1);
            Assert.That(image.Get(0, 0).R, Is.EqualTo(255));
            Assert.That(image.Get(100, 0).G, Is.GreaterThan(250));
            Assert.That(image.Get(200, 0).B, Is.EqualTo(255));
            Assert.That(image.Get(200, 0).R, Is.LessThan(4));
        }

        [Test]
        public void BakingIsDeterministic()
        {
            var spec = Linear(37, new GradientStop(0, Red), new GradientStop(1, Blue));
            Assert.That(PngEncoder.Encode(GradientBaker.Bake(spec, 33, 21)), Is.EqualTo(PngEncoder.Encode(GradientBaker.Bake(spec, 33, 21))));
        }

        [Test]
        public void NeedsTwoStops()
        {
            Assert.Throws<ArgumentException>(() => GradientBaker.Bake(Linear(0, new GradientStop(0, Black)), 2, 2));
            Assert.Throws<ArgumentNullException>(() => GradientBaker.Bake(null, 2, 2));
        }

        [Test]
        public void ResolveStopsFillsEnds()
        {
            IReadOnlyList<GradientStop> stops = GradientBaker.ResolveStops(new[]
            {
                new UnresolvedStop(null, Red), new UnresolvedStop(null, White), new UnresolvedStop(null, Blue)
            });
            Assert.That(stops[0].Position, Is.EqualTo(0));
            Assert.That(stops[1].Position, Is.EqualTo(0.5));
            Assert.That(stops[2].Position, Is.EqualTo(1));
        }

        [Test]
        public void ResolveStopsDistributesGaps()
        {
            IReadOnlyList<GradientStop> stops = GradientBaker.ResolveStops(new[]
            {
                new UnresolvedStop(0.1, Red), new UnresolvedStop(null, White), new UnresolvedStop(null, White), new UnresolvedStop(0.7, Blue)
            });
            Assert.That(stops[1].Position, Is.EqualTo(0.3).Within(1e-9));
            Assert.That(stops[2].Position, Is.EqualTo(0.5).Within(1e-9));
        }

        [Test]
        public void ResolveStopsKeepsMonotonic()
        {
            IReadOnlyList<GradientStop> stops = GradientBaker.ResolveStops(new[]
            {
                new UnresolvedStop(0.6, Red), new UnresolvedStop(0.3, White), new UnresolvedStop(1.0, Blue)
            });
            Assert.That(stops[1].Position, Is.EqualTo(0.6));
        }

        [Test]
        public void ResolveStopsRejectsSingleStop()
        {
            Assert.Throws<ArgumentException>(() => GradientBaker.ResolveStops(new[] { new UnresolvedStop(0, Red) }));
        }

        [TestCase(0, 100, 100, 100.0)]
        [TestCase(90, 100, 50, 100.0)]
        [TestCase(180, 100, 50, 50.0)]
        [TestCase(45, 100, 100, 141.4213562)]
        public void LinearLineLengthFollowsCss(double angle, double w, double h, double expected)
        {
            Assert.That(GradientBaker.LinearLineLength(angle, w, h), Is.EqualTo(expected).Within(1e-4));
        }

        [TestCase("", "bottom", 180.0)]
        [TestCase("", "top", 0.0)]
        [TestCase("right", "", 90.0)]
        [TestCase("left", "", 270.0)]
        [TestCase("", "", 180.0)]
        public void CornerAngleSingleSide(string horizontal, string vertical, double expected)
        {
            Assert.That(GradientBaker.CornerAngle(horizontal, vertical, 200, 100), Is.EqualTo(expected).Within(1e-6));
        }

        [Test]
        public void CornerAngleOnSquareIsDiagonal()
        {
            Assert.That(GradientBaker.CornerAngle("right", "top", 100, 100), Is.EqualTo(45).Within(1e-6));
            Assert.That(GradientBaker.CornerAngle("right", "bottom", 100, 100), Is.EqualTo(135).Within(1e-6));
            Assert.That(GradientBaker.CornerAngle("left", "bottom", 100, 100), Is.EqualTo(225).Within(1e-6));
            Assert.That(GradientBaker.CornerAngle("left", "top", 100, 100), Is.EqualTo(315).Within(1e-6));
        }

        [Test]
        public void CornerAngleOnWideBoxIsSteeper()
        {
            double angle = GradientBaker.CornerAngle("right", "top", 200, 100);
            Assert.That(angle, Is.EqualTo(63.4349).Within(1e-3));
        }
    }

    public class ShadowTests
    {
        private static ShadowSprite Sprite(double blur, double spread = 0, double radius = 0, byte alpha = 255)
        {
            return ShadowBaker.Bake(new ShadowSpec { BlurRadius = blur, Spread = spread, CornerRadius = radius, Color = new Rgba8(0, 0, 0, alpha) });
        }

        [TestCase(0.0, 0)]
        [TestCase(1.0, 2)]
        [TestCase(10.0, 15)]
        [TestCase(20.0, 30)]
        [TestCase(6.5, 10)]
        public void MarginIsOneAndAHalfBlur(double blur, int expected)
        {
            Assert.That(ShadowBaker.MarginFor(blur), Is.EqualTo(expected));
        }

        [TestCase(0.0, 5.0, 0.0)]
        [TestCase(8.0, 4.0, 12.0)]
        [TestCase(8.0, -10.0, 0.0)]
        [TestCase(8.0, 0.0, 8.0)]
        public void EffectiveRadiusAddsSpread(double radius, double spread, double expected)
        {
            Assert.That(ShadowBaker.EffectiveCornerRadius(radius, spread), Is.EqualTo(expected));
        }

        [TestCase(4.0, 0.0)]
        [TestCase(10.0, 6.0)]
        [TestCase(20.0, 12.0)]
        [TestCase(0.0, 8.0)]
        public void SpriteSizeMatchesSlices(double blur, double radius)
        {
            ShadowSprite sprite = Sprite(blur, 0, radius);
            Assert.That(sprite.Width, Is.EqualTo(2 * sprite.Slice + 2));
            Assert.That(sprite.Height, Is.EqualTo(sprite.Width));
            Assert.That(sprite.Slice, Is.EqualTo(Math.Max(1, 2 * ShadowBaker.MarginFor(blur) + (int)Math.Ceiling(radius))));
        }

        [Test]
        public void CentreIsOpaqueAndCornersAreClear()
        {
            ShadowSprite sprite = Sprite(8, 0, 4);
            int mid = sprite.Width / 2;
            Assert.That(sprite.Image.Get(mid, mid).A, Is.EqualTo(255));
            Assert.That(sprite.Image.Get(0, 0).A, Is.LessThan(2));
            Assert.That(sprite.Image.Get(sprite.Width - 1, sprite.Height - 1).A, Is.LessThan(2));
            Assert.That(sprite.Image.Get(0, mid).A, Is.LessThan(2));
        }

        [Test]
        public void AlphaFallsMonotonicallyOutward()
        {
            ShadowSprite sprite = Sprite(12, 0, 0);
            int mid = sprite.Width / 2;
            int previous = -1;
            for (int x = 0; x <= mid; x++)
            {
                int a = sprite.Image.Get(x, mid).A;
                Assert.That(a, Is.GreaterThanOrEqualTo(previous));
                previous = a;
            }
        }

        [Test]
        public void EdgeIsAboutHalfAlpha()
        {
            ShadowSprite sprite = Sprite(12, 0, 0);
            int mid = sprite.Width / 2;
            int edge = sprite.Margin;
            int a = sprite.Image.Get(edge, mid).A;
            Assert.That(a, Is.InRange(110, 145));
        }

        [Test]
        public void ColourIsKeptAndAlphaScalesWithColourAlpha()
        {
            ShadowSprite sprite = ShadowBaker.Bake(new ShadowSpec { BlurRadius = 6, Color = new Rgba8(10, 20, 30, 128) });
            int mid = sprite.Width / 2;
            Rgba8 centre = sprite.Image.Get(mid, mid);
            Assert.That(centre.R, Is.EqualTo(10));
            Assert.That(centre.G, Is.EqualTo(20));
            Assert.That(centre.B, Is.EqualTo(30));
            Assert.That(centre.A, Is.EqualTo(128));
        }

        [Test]
        public void ZeroBlurGivesHardEdge()
        {
            ShadowSprite sprite = Sprite(0, 0, 0);
            Assert.That(sprite.Margin, Is.EqualTo(0));
            Assert.That(sprite.Image.Get(0, 0).A, Is.EqualTo(255));
        }

        [Test]
        public void RoundedCornerLeavesCornerPixelClear()
        {
            ShadowSprite sprite = Sprite(0, 0, 10);
            Assert.That(sprite.Image.Get(0, 0).A, Is.EqualTo(0));
            Assert.That(sprite.Image.Get(sprite.Width - 1, 0).A, Is.EqualTo(0));
            Assert.That(sprite.Image.Get(sprite.Width / 2, 0).A, Is.EqualTo(255));
            Assert.That(sprite.Image.Get(0, sprite.Height / 2).A, Is.EqualTo(255));
        }

        [Test]
        public void SymmetricInBothAxes()
        {
            ShadowSprite sprite = Sprite(9, 0, 5);
            int last = sprite.Width - 1;
            for (int y = 0; y < sprite.Height; y += 3)
            {
                for (int x = 0; x < sprite.Width; x += 3)
                {
                    Assert.That(sprite.Image.Get(x, y).A, Is.EqualTo(sprite.Image.Get(last - x, y).A).Within(1));
                    Assert.That(sprite.Image.Get(x, y).A, Is.EqualTo(sprite.Image.Get(x, last - y).A).Within(1));
                }
            }
        }

        [Test]
        public void BakingIsDeterministic()
        {
            Assert.That(PngEncoder.Encode(Sprite(14, 2, 6).Image), Is.EqualTo(PngEncoder.Encode(Sprite(14, 2, 6).Image)));
        }

        [Test]
        public void MinBoxIsTwiceTheFlatInset()
        {
            ShadowSprite sprite = Sprite(10, 0, 4);
            Assert.That(sprite.MinBoxWidth, Is.EqualTo(2 * (sprite.Slice - sprite.Margin)));
            Assert.That(sprite.MinBoxHeight, Is.EqualTo(sprite.MinBoxWidth));
        }

        [Test]
        public void NullSpecIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => ShadowBaker.Bake(null));
        }

        [Test]
        public void NegativeBlurIsTreatedAsZero()
        {
            Assert.That(PngEncoder.Encode(Sprite(-4).Image), Is.EqualTo(PngEncoder.Encode(Sprite(0).Image)));
        }

        [Test]
        public void SurvivesPngRoundTrip()
        {
            ShadowSprite sprite = Sprite(10, 0, 8);
            Assert.That(PngDecoder.Decode(PngEncoder.Encode(sprite.Image)).SameAs(sprite.Image), Is.True);
        }
    }

    public class BlurTests
    {
        [Test]
        public void ZeroSigmaIsIdentity()
        {
            var image = new RgbaImage(5, 5);
            image.Set(2, 2, new Rgba8(255, 0, 0, 255));
            Assert.That(BlurBaker.Gaussian(image, 0).SameAs(image), Is.True);
        }

        [Test]
        public void SinglePixelSpreadsAndKeepsColour()
        {
            var image = new RgbaImage(21, 21);
            image.Set(10, 10, new Rgba8(200, 40, 10, 255));
            RgbaImage blurred = BlurBaker.Gaussian(image, 2);
            Rgba8 centre = blurred.Get(10, 10);
            Assert.That(centre.A, Is.GreaterThan(0));
            Assert.That(centre.A, Is.LessThan(255));
            Assert.That(centre.R, Is.InRange(198, 202));
            Assert.That(blurred.Get(11, 10).A, Is.GreaterThan(0));
        }

        [Test]
        public void TotalAlphaIsRoughlyConserved()
        {
            var image = new RgbaImage(41, 41);
            image.Set(20, 20, new Rgba8(255, 255, 255, 255));
            RgbaImage blurred = BlurBaker.Gaussian(image, 3);
            long total = 0;
            for (int i = 3; i < blurred.Pixels.Length; i += 4)
            {
                total += blurred.Pixels[i];
            }

            Assert.That(total, Is.InRange(230, 280));
        }

        [Test]
        public void FlatOpaqueImageStaysFlatInTheMiddle()
        {
            var image = new RgbaImage(40, 40);
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 40; x++)
                {
                    image.Set(x, y, new Rgba8(10, 90, 200, 255));
                }
            }

            RgbaImage blurred = BlurBaker.Gaussian(image, 2);
            Assert.That(blurred.Get(20, 20), Is.EqualTo(new Rgba8(10, 90, 200, 255)));
            Assert.That(blurred.Get(0, 0).A, Is.LessThan(255));
        }

        [Test]
        public void TransparentPixelsDoNotBleedTheirColour()
        {
            var image = new RgbaImage(11, 11);
            image.Set(5, 5, new Rgba8(255, 0, 0, 255));
            image.Set(6, 5, new Rgba8(0, 0, 255, 0));
            RgbaImage blurred = BlurBaker.Gaussian(image, 1.5);
            Assert.That(blurred.Get(6, 5).B, Is.EqualTo(0));
        }

        [Test]
        public void BlurIsDeterministic()
        {
            var image = new RgbaImage(16, 16);
            image.Set(4, 4, new Rgba8(255, 128, 0, 255));
            Assert.That(BlurBaker.Gaussian(image, 2.5).SameAs(BlurBaker.Gaussian(image, 2.5)), Is.True);
        }

        [TestCase(1.0)]
        [TestCase(2.0)]
        [TestCase(5.0)]
        [TestCase(10.0)]
        public void BoxSizesApproximateTheVariance(double sigma)
        {
            int[] sizes = BoxBlur.BoxSizes(sigma, 3);
            double variance = 0;
            foreach (int size in sizes)
            {
                variance += (size * size - 1) / 12.0;
            }

            Assert.That(Math.Sqrt(variance), Is.EqualTo(sigma).Within(0.5));
            Assert.That(sizes.Length, Is.EqualTo(3));
            foreach (int size in sizes)
            {
                Assert.That(size % 2, Is.EqualTo(1));
            }
        }

        [Test]
        public void NullSourceIsRejected()
        {
            Assert.Throws<ArgumentNullException>(() => BlurBaker.Gaussian(null, 1));
        }
    }
}
