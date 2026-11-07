using System.Threading.Tasks;
using Baballonia.SDK;
using Baballonia.Services.Inference;
using Baballonia.Services.Inference.VideoSources;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OpenCvSharp;

namespace Baballonia.Tests.Services;

[TestClass]
public class StereoCameraSourceTest
{
    [TestMethod]
    [DataRow(true, false, 10, 10)]
    [DataRow(false, true, 20, 20)]
    [DataRow(true, true, 10, 20)]
    public void StereoSourcesSelectTheCorrespondingEye(bool left, bool right, int leftPixel, int rightPixel)
    {
        using var leftCapture = new TestCapture(true);
        using var rightCapture = new TestCapture(true);
        using var source = new DualCameraSource
        {
            LeftCam = left ? CreateSource(leftCapture) : null,
            RightCam = right ? CreateSource(rightCapture) : null
        };
        leftCapture.Publish(10, 20);
        rightCapture.Publish(10, 20);

        using var frame = source.GetFrame();

        AssertFrame(frame, 4, leftPixel, rightPixel);
    }

    [TestMethod]
    public void MonoCamerasKeepTheirFullFrames()
    {
        using var leftCapture = new TestCapture(false);
        using var rightCapture = new TestCapture(false);
        using var source = new DualCameraSource
        {
            LeftCam = CreateSource(leftCapture),
            RightCam = CreateSource(rightCapture)
        };
        leftCapture.Publish(10, 11);
        rightCapture.Publish(20, 21);

        using var frame = source.GetFrame();

        AssertFrame(frame, 8, 10, 21);
        Assert.AreEqual((byte)11, frame!.At<byte>(0, 3));
        Assert.AreEqual((byte)20, frame.At<byte>(0, 4));
    }

    [TestMethod]
    public void SharedStereoStreamKeepsBothHalvesForTheExistingSplitter()
    {
        using var capture = new TestCapture(true);
        using var source = CreateSource(capture);
        capture.Publish(10, 20);

        using var frame = source.GetFrame();

        AssertFrame(frame, 4, 10, 20);
    }

    [TestMethod]
    public void CachedEyeIsNotSplitAgain()
    {
        using var leftCapture = new TestCapture(true);
        using var rightCapture = new TestCapture(true);
        using var source = new DualCameraSource
        {
            LeftCam = CreateSource(leftCapture),
            RightCam = CreateSource(rightCapture)
        };
        leftCapture.Publish(10, 20);
        rightCapture.Publish(10, 20);
        using var first = source.GetFrame();
        rightCapture.Publish(30, 40);

        using var second = source.GetFrame();

        AssertFrame(second, 4, 10, 40);
    }

    [TestMethod]
    public void StereoAndMonoSourcesCanBeCombined()
    {
        using var leftCapture = new TestCapture(true);
        using var rightCapture = new TestCapture(false);
        using var source = new DualCameraSource
        {
            LeftCam = CreateSource(leftCapture),
            RightCam = CreateSource(rightCapture)
        };
        leftCapture.Publish(10, 20);
        rightCapture.Publish(30, 30);

        using var frame = source.GetFrame();

        AssertFrame(frame, 4, 10, 30);
    }

    private static SingleCameraSource CreateSource(TestCapture capture)
        => new(NullLogger.Instance, capture, "test");

    private static void AssertFrame(Mat? frame, int width, int firstPixel, int lastPixel)
    {
        Assert.IsNotNull(frame);
        Assert.AreEqual(width, frame.Width);
        Assert.AreEqual(2, frame.Height);
        Assert.AreEqual((byte)firstPixel, frame.At<byte>(0, 0));
        Assert.AreEqual((byte)lastPixel, frame.At<byte>(0, width - 1));
    }

    private sealed class TestCapture(bool stereo) : Capture("test", NullLogger.Instance)
    {
        public override bool HasStereoFrames => stereo;

        public void Publish(byte firstEye, byte secondEye)
        {
            var frame = new Mat(2, 4, MatType.CV_8UC1);
            for (var y = 0; y < frame.Height; y++)
                for (var x = 0; x < frame.Width; x++)
                    frame.Set(y, x, x < 2 ? firstEye : secondEye);
            SetRawMat(frame);
            IsReady = true;
        }

        public override Task<bool> StartCapture() => Task.FromResult(true);
        public override Task<bool> StopCapture() => Task.FromResult(true);
        public override void Dispose() => AcquireRawMat()?.Dispose();
    }
}
