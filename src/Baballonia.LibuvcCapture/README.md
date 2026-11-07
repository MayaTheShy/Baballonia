# Baballonia LibuvcCapture Plugin

libuvc-based capture plugin for Bigscreen Beyond 2E eye tracking cameras on Linux.

## The Problem

The Bigscreen Beyond 2E eye tracking cameras (VID:PID = 35bd:0202) work on Windows but fail on Linux with V4L2. The device reports `Size Image: 0` for its MJPEG format, causing V4L2 buffer allocation to fail with `VIDIOC_REQBUFS: Invalid argument`.

This plugin bypasses V4L2 entirely by using libuvc to communicate directly with the USB device.

## Running

Install the .NET 10 SDK, libuvc, libusb-1.0, and libudev, including development libraries where needed. On Nix, `nix develop` from the repository root provides these dependencies; the upstream flake no longer provides a `nix run` package.

Initialize the submodules, download the desktop dependencies, and build from the repository root:

```bash
git submodule update --init --recursive
./download_dependencies.sh
dotnet build src/Baballonia.Desktop/Baballonia.Desktop.csproj
```

The desktop build places the plugin in its output `Modules` directory. Run the application from the desktop build output.

## Device permissions

To use the BSB2E cameras without root privileges, create a udev rules file (you may need to adjust the `GROUP` to a group your user is in):

Create `/etc/udev/rules.d/99-bigeye.rules`:

```
# Bigscreen Bigeye Eye Tracking Camera
SUBSYSTEM=="usb", ATTR{idVendor}=="35bd", ATTR{idProduct}=="0202", MODE="0666", GROUP="plugdev"
SUBSYSTEM=="video4linux", ATTR{idVendor}=="35bd", ATTR{idProduct}=="0202", MODE="0666", GROUP="plugdev"
```

Then reload udev rules:

```bash
sudo udevadm control --reload-rules
sudo udevadm trigger
```

Replug the device or reboot.

## Usage

1. Launch Baballonia
2. The BSB2E device will appear in the camera dropdown as `/dev/video0` (or similar)
3. Select the device. If a capture backend selector is shown, choose `LibuvcCapture`.

If it fails and the log contains errors about "no support for memory mapping" and "Insufficient buffer memory", then the wrong plugin was selected to handle the device.

## License

This plugin is licensed under LGPL v3. See LICENSE file for details.

Incorporates code from libuvcnet (MIT License) - see LICENSE for attribution.

## Credits

Inspired by sample Go code by Lillia on the "Bigscreen Beyond on Linux" Discord server: https://discord.com/channels/1197445268220694661/1197445268220694663/1408943429718773831

P/Invoke bindings from https://github.com/horizongir/libuvcnet
